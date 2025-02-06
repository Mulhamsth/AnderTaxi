using Model;

namespace DriverService;

public class Taxi
{
        private readonly RabbitMqManager _rmq;
        public string TaxiId { get; }
        public Location Location { get; set; }
        public ETaxiState State { get; set; }
    
        public Taxi(RabbitMqManager rmq, string taxiId)
        {
            _rmq = rmq;
            TaxiId = taxiId;
        }
    
        private TaxiDto GetCurrentLocation()
        {
            return new TaxiDto(){Id = TaxiId, Location = new Location(Location.X, Location.Y), State = State};
        }
    
        // RunAsync starts a background loop that publishes the taxi's location every 2 seconds.
        public async Task RunAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _rmq.PublishMessage("distancelogic", "taxi.location",GetCurrentLocation());
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
        
        public async Task PickupPassengerAsync(PickupRequest pickupRequest)
        {
            State = ETaxiState.OnRouteToPassenger;
            await MoveToLocationAsync(pickupRequest.PickupAt, CancellationToken.None);
            State = ETaxiState.OnRouteToDestination;
            await MoveToLocationAsync(pickupRequest.PickupTo, CancellationToken.None);
        }
        public async Task MoveToLocationAsync(Location targetLocation, CancellationToken token)
        {
            while (!token.IsCancellationRequested && (Location.X != targetLocation.X || Location.Y != targetLocation.Y))
            {
                if (Location.X < targetLocation.X) Location.X++;
                else if (Location.X > targetLocation.X) Location.X--;

                if (Location.X == targetLocation.X)
                {
                    if (Location.Y < targetLocation.Y) Location.Y++;
                    else if (Location.Y > targetLocation.Y) Location.Y--;
                }
                
                Console.WriteLine($"Taxi {TaxiId} moving to ({Location.X}, {Location.Y})");
    
                // Simulate movement delay
                await Task.Delay(2000, token);
            }
    
            if (Location.X == targetLocation.X && Location.Y == targetLocation.Y)
            {
                Console.WriteLine($"Taxi {TaxiId} has arrived at the destination ({Location.X}, {Location.Y})");
                State = ETaxiState.Available;
            }
        }
        public async Task SendInvoiceAsync(PickupRequest pickupRequest)
        {
            // Send an invoice to the passenger
            await _rmq.PublishMessage("invoice", "taxi.invoice", data: pickupRequest);
        }
}