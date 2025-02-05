using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Model;
public class RabbitMqBackgroundService : BackgroundService
{
    private readonly RabbitMqManager _rabbitMqManager;
    private string _queueName;
    
    public RabbitMqBackgroundService(RabbitMqManager rabbitMqManager, string queueName)
    {
        _rabbitMqManager = rabbitMqManager;
        _queueName = queueName;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _rabbitMqManager.ConsumeMessage(_queueName, async (message) => {Console.WriteLine(message); });
    }
}
