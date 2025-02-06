using System.ComponentModel;
using System.Text;
using CloudNative.CloudEvents;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Model;
public class RabbitMqBackgroundService : BackgroundService
{
    private readonly RabbitMqManager _rabbitMqManager;
    private string _queueName;
    private Func<CloudEvent, Task> action;
    
    public RabbitMqBackgroundService(RabbitMqManager rabbitMqManager, string queueName, Func<CloudEvent, Task> action)
    {
        _rabbitMqManager = rabbitMqManager;
        _queueName = queueName;
        this.action = action;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _rabbitMqManager.ConsumeCloudEventMessage(_queueName, action);
    }
}
