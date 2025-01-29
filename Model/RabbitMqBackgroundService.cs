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
        var _channel = await _rabbitMqManager._connection.CreateChannelAsync(cancellationToken: stoppingToken);
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            byte[] body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            Console.WriteLine($" [x] {message}");
        };
        await _channel.BasicConsumeAsync(_queueName, autoAck: true, consumer: consumer);
    }
}
