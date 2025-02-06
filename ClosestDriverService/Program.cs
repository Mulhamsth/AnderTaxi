using System.Collections.Concurrent;
using System.Text;
using Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

class Program
{
    private static ConcurrentDictionary<string, TaxiDto> _drivers = new();
    private static async Task Main(string[] args)
    {
        ManualResetEvent _quitEvent = new ManualResetEvent(false);
        Console.CancelKeyPress += (sender, eArgs) =>
        {
            _quitEvent.Set();
            eArgs.Cancel = true;
        };

        var manager = await RabbitMqManager.Initialize();
        manager.QueueName = "distancelogic";

        await manager.ConsumeCloudEventMessage( "distancelogic", async (cloudEvent) =>
        {
            // determining the type of message.
            if(cloudEvent.Type == "taxi.location")
            {
                var taxiLocation = await manager.DeserializeCloudEventMessage<TaxiDto>(cloudEvent);
                _drivers[taxiLocation.Id] = taxiLocation;
                Console.WriteLine($" [x] {taxiLocation.Id} is at ({taxiLocation.Location.X}, {taxiLocation.Location.Y}), State: {taxiLocation.State}");
            }
            else if(cloudEvent.Type == "passenger.location")
            {
                var passengerLocation = await manager.DeserializeCloudEventMessage<PassengerRequest>(cloudEvent);
                await HandlePassengerRequest(manager, passengerLocation);
                Console.WriteLine($" [x] Passenger is at ({passengerLocation.Location.X}, {passengerLocation.Location.Y}), desired location: ({passengerLocation.DesiredLocation.X}, {passengerLocation.DesiredLocation.Y})");
            }
            else
            {
                Console.WriteLine("[x] Received Unknown Message");
            }
        });

        _quitEvent.WaitOne();
    }
    
    private static async Task HandlePassengerRequest(RabbitMqManager rmq, PassengerRequest passengerRequest)
    {
        TaxiDto nearestDriver = FindNearestDriver(passengerRequest.Location);
        if (nearestDriver != null)
        {
            Console.WriteLine($" [x] Nearest driver is {nearestDriver.Id}");
            // Send a message to the driver to pick up the passenger
            await rmq.PublishMessage(
                "driver",
                "distancelogic.pickup",
                data: new PickupRequest()
                    { PassengerId = passengerRequest.PassengerId, 
                        DriverId = nearestDriver.Id,
                        PickupAt = passengerRequest.Location,
                        PickupTo = passengerRequest.DesiredLocation
                    }
                );
        }
        else
        {
            Console.WriteLine(" [x] No available drivers");
        }
    }
    private static TaxiDto FindNearestDriver(Location passengerLocation)
    {
        TaxiDto nearestDriver = null;
        double minDistance = double.MaxValue;

        foreach (var driver in _drivers.Values)
        {
            if (driver.State == ETaxiState.Available)
            {
                double distance = CalculateDistance(driver.Location, passengerLocation);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestDriver = driver;
                }
            }
        }

        return nearestDriver;
    }
    
    public static double CalculateDistance(Location loc1, Location loc2)
    {
        double deltaX = loc1.X - loc2.X;
        double deltaY = loc1.Y - loc2.Y;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
    
}
