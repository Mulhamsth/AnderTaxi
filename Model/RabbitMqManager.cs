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

    public static async Task<RabbitMqManager> Initialize()
    {
        var instance = new RabbitMqManager();
        var credentials = new {username = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME"), password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") };
        var factory = new ConnectionFactory() { HostName = "rabbitmq", UserName = credentials.username, Password = credentials.password };
        var _connection = await factory.CreateConnectionAsync();
        instance.Channel = await _connection.CreateChannelAsync();
        return instance;
    }
    
    public async Task PublishMessage(string routingkey, object? data, string fromQueue = "")
    {
        if (fromQueue == "")
        {
            fromQueue = QueueName;
        }
        
        CloudEvent cloudEvent = new CloudEvent
        {
            Id = Guid.NewGuid().ToString(),
            Type = data?.GetType().Name,
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
   public async Task ConsumeMessage<T>(string queueName, Func<T, Task> action)
   {
       var consumer = new AsyncEventingBasicConsumer(Channel);
       consumer.ReceivedAsync += async (model, ea) =>
       {
           CloudEventFormatter formatter = new JsonEventFormatter();
           var cloudEvent = formatter.DecodeStructuredModeMessage(ea.Body, null, null);
           var messageBody = cloudEvent.Data as JsonElement?;
           if (messageBody.HasValue)
           {
               var message = messageBody.Value.GetRawText();
               var data = JsonSerializer.Deserialize<T>(message);
               if (data != null)
               {
                   await action(data);
               }
           }
       };
       await Channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);
   } 
    
}