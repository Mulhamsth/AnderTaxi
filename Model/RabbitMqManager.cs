using System.Text;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Http;
using CloudNative.CloudEvents.SystemTextJson;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Model;

public class RabbitMqManager 
{
    private RabbitMqManager()
    {
    }
    
    public IConnection _connection { get; private set; }

    public static async Task<RabbitMqManager> Initialize()
    {
        var instance = new RabbitMqManager();
        var credentials = new {username = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME"), password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") };
        var factory = new ConnectionFactory() { HostName = "rabbitmq", UserName = credentials.username, Password = credentials.password };
        instance._connection = await factory.CreateConnectionAsync();
        return instance;
    }
    
    public async Task PublishMessage(string routingkey, string message)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var _channel = await _connection.CreateChannelAsync();
        await _channel.BasicPublishAsync(exchange: "taxi.topic", routingKey: routingkey, body: body);
        Console.WriteLine(" [x] Sent {0}", message);
    }
    
    public async Task ConsumeMessage(string queueName, Func<string, Task> action)
    {
        var _channel = await _connection.CreateChannelAsync();
        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            byte[] body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            await action(message);
        };
        await _channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
    }
}