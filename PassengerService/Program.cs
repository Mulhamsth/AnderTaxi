
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

builder.Services.AddSingleton<RabbitMqManager>(rmq);
builder.Services.AddHostedService<RabbitMqBackgroundService>(rbs => 
    new RabbitMqBackgroundService(rbs.GetRequiredService<RabbitMqManager>(),"passenger"));

var app = builder.Build();

CloudEvent cloudEvent = new CloudEvent
{
    Id = "event-id",
    Type = "event-type",
    Source = new Uri("https://cloudevents.io/"),
    Time = DateTimeOffset.UtcNow,
    DataContentType = "text/plain",
    Data = "This is CloudEvent data"
};

CloudEventFormatter formatter = new JsonEventFormatter();
HttpRequestMessage request = new HttpRequestMessage
{
    Method = HttpMethod.Post,
    Content = cloudEvent.ToHttpContent(ContentMode.Structured, formatter)
};

app.MapGet("/", async (RabbitMqManager rmq) => await rmq.PublishMessage("driver", "This is a message from passenger!"));

app.Run();