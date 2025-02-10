
using System.Security.AccessControl;
using System.Text;
using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Http;
using CloudNative.CloudEvents.SystemTextJson;
using Microsoft.Extensions.Options;
using Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var builder = WebApplication.CreateBuilder(args);

var rmq = await RabbitMqManager.Initialize();
rmq.QueueName = "passenger";

builder.Services.AddSingleton<RabbitMqManager>(rmq);
builder.Services.AddHostedService<RabbitMqBackgroundService>(rbs => 
    new RabbitMqBackgroundService(rbs.GetRequiredService<RabbitMqManager>(),"passenger", 
        (cloudEvent) => HandlingCloudEventMessage(cloudEvent, rbs.GetRequiredService<RabbitMqManager>())));

var app = builder.Build();

app.MapGet("/", async (RabbitMqManager rmq) => 
{
    var random = new Random();
    var passengerRequest = new PassengerRequest()
    {
        PassengerId = Guid.NewGuid().ToString(),
        Location = new Location(random.Next(0, 31), random.Next(0, 31)),
        DesiredLocation = new Location(random.Next(0, 31), random.Next(0, 31))
    };
    await rmq.PublishMessage("distancelogic", "passenger.location", passengerRequest);
});

app.Run();

static async Task HandlingCloudEventMessage(CloudEvent cloudEvent, RabbitMqManager rmq)
{
    if(cloudEvent.Type == "driver.message")
    {
        Console.WriteLine(" [x] Received driver message");
    }

    if (cloudEvent.Type == "invoice.invoice")
    {
        var invoice = await rmq.DeserializeCloudEventMessage<Invoice>(cloudEvent);
        Console.WriteLine(" [x] Received invoice with amount: " + cloudEvent.Data);
    }
    else
    {
        Console.WriteLine(" [x] Received Unknown Message");
    }
}