// Connection to RabbitMQ

using System.Text;
using CloudNative.CloudEvents;
using Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

class Program
{
    private static async Task Main(string[] args)
    {
        ManualResetEvent _quitEvent = new ManualResetEvent(false);
        Console.CancelKeyPress += (sender, eArgs) =>
        {
            _quitEvent.Set();
            eArgs.Cancel = true;
        };

        var manager = await RabbitMqManager.Initialize();
        await manager.ConsumeCloudEventMessage("invoice", async (cloudEvent) =>
        {
            if (cloudEvent.Type == "taxi.invoice")
            {
                var ride = await manager.DeserializeCloudEventMessage<PickupRequest>(cloudEvent);
                await HandleIncomingInvoice(ride);
                Console.WriteLine($" [x] Received invoice for {ride.PassengerId}");
            }
        });


        _quitEvent.WaitOne();
    }

    private static async Task HandleIncomingInvoice(PickupRequest pickupRequest)
    {
        // calculate the distance between the two points, every move costs 1
        var distance = Math.Abs(pickupRequest.PickupAt.X - pickupRequest.PickupTo.X) + Math.Abs(pickupRequest.PickupAt.Y - pickupRequest.PickupTo.Y);
        var invoice = new Invoice()
        {
            PassengerId = pickupRequest.PassengerId,
            DriverId = pickupRequest.DriverId,
            Distance = distance,
            Amount = distance * 2.0
        };
        var manager = await RabbitMqManager.Initialize();
        await manager.PublishMessage("Passenger", "invoice.invoice", invoice);
    }
    private static Task OutputMessage(string message)
    {
        Console.WriteLine($" [x] {message}");
        return Task.CompletedTask;
    }
}
