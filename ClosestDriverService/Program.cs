using System.Text;
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
        await manager.ConsumeMessage<TaxiLocation>("distancelogic", OutputMessage);


        _quitEvent.WaitOne();
    }
    private static Task OutputMessage(TaxiLocation location)
    {
        Console.WriteLine($" [x] {location.Id} is at ({location.X}, {location.Y})");
        return Task.CompletedTask;
    }
}
