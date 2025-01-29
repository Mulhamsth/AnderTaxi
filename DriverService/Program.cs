using System.Text;
using Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var builder = WebApplication.CreateBuilder(args);

var rmq = await RabbitMqManager.Initialize();

builder.Services.AddSingleton<RabbitMqManager>(rmq);
builder.Services.AddHostedService<RabbitMqBackgroundService>(rbs => 
    new RabbitMqBackgroundService(rbs.GetRequiredService<RabbitMqManager>(),"driver"));

var app = builder.Build();

app.MapGet("/", async (RabbitMqManager rmq) => await rmq.PublishMessage("passenger", "This is a message from driver!"));

app.Run();