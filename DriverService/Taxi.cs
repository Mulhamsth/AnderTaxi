using Model;

namespace DriverService;

public class Taxi
{
        private readonly RabbitMqManager _rmq;
        public string TaxiId { get; }
        public int X { get; set; }
        public int Y { get; set; }
    
        public Taxi(RabbitMqManager rmq, string taxiId)
        {
            _rmq = rmq;
            TaxiId = taxiId;
        }
    
        private TaxiLocation GetCurrentLocation()
        {
            return new TaxiLocation(){Id = TaxiId, X = X, Y = Y};
        }
    
        // RunAsync starts a background loop that publishes the taxi's location every 2 seconds.
        public async Task RunAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _rmq.PublishMessage("distancelogic", GetCurrentLocation());
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
}