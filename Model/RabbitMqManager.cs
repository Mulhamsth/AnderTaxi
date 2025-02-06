using System.Text;
using System.Text.Json;
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

    public string QueueName { get; set; }
    public IChannel Channel { get; set; }
    private static RabbitMqManager _instance = null;

    public static async Task<RabbitMqManager> Initialize()
    {
        if (_instance != null)
        {
            return _instance;
        }
        
        _instance = new RabbitMqManager();
        var credentials = new {username = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME"), password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") };
        var factory = new ConnectionFactory() { HostName = "rabbitmq", UserName = credentials.username, Password = credentials.password };
        var _connection = await factory.CreateConnectionAsync();
        _instance.Channel = await _connection.CreateChannelAsync();
        return _instance;
    }
    
    public async Task PublishMessage(string routingkey, string messageType, object? data, string fromQueue = "")
    {
        if (fromQueue == "")
        {
            fromQueue = QueueName;
        }
        
        CloudEvent cloudEvent = new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Type = messageType,
            Source = new Uri($"http://{fromQueue}"),
            Time = DateTimeOffset.UtcNow,
            DataContentType = "application/json",
            Data = data
        };

        CloudEventFormatter formatter = new JsonEventFormatter();
        var messageBody = formatter.EncodeStructuredModeMessage(cloudEvent, out var contentType);
        
        await Channel.BasicPublishAsync(exchange: "taxi.topic", routingKey: routingkey, body: messageBody);
        Console.WriteLine(" [x] Sent {0}", data);
    }
    
    public async Task ConsumeMessage(string queueName, Func<string, Task> action)
    {
        var consumer = new AsyncEventingBasicConsumer(Channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            byte[] body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            await action(message);
        };
        await Channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
    }
    
    // Handling Generic Type Messages
    public async Task ConsumeCloudEventMessage(string queueName, Func<CloudEvent, Task> action)
    {
        var consumer = new AsyncEventingBasicConsumer(Channel);
        
        //returning the cloud event to handle it as needed
        consumer.ReceivedAsync += async (model, ea) =>
        {
            try
            {
                // Decode the CloudEvent from the message body.
                var formatter = new JsonEventFormatter();
                var cloudEvent = formatter.DecodeStructuredModeMessage(ea.Body, null, null);
                
                await action(cloudEvent);
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to process message from queue {QueueName}");
            }
        };

        await Channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
    }

    public async Task<T> DeserializeCloudEventMessage<T>(CloudEvent cloudEvent)
    {
        if (cloudEvent.Data is JsonElement jsonData)
        {
            // Deserialize the inner payload. (the actual data)
            T data = JsonSerializer.Deserialize<T>(jsonData.GetRawText());
            if (data != null)
            {
                return data;
            }
        } 
        return default;
    }
    
}