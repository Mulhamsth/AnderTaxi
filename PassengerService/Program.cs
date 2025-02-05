
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
    new RabbitMqBackgroundService(rbs.GetRequiredService<RabbitMqManager>(),"passenger"));

var app = builder.Build();

app.MapGet("/", async (RabbitMqManager rmq) => await rmq.PublishMessage("distancelogic", "Calculate the distance"));

app.Run();