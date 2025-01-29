// Connection to RabbitMQ

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
        await manager.ConsumeMessage("invoice", OutputMessage);


        _quitEvent.WaitOne();
    }
    private static Task OutputMessage(string message)
    {
        Console.WriteLine($" [x] {message}");
        return Task.CompletedTask;
    }
}
