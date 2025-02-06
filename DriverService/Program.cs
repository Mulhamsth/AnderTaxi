using System.Text;
using DriverService;
using Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var builder = WebApplication.CreateBuilder(args);

var rmq = await RabbitMqManager.Initialize();
rmq.QueueName = "driver";

builder.Services.AddSingleton<RabbitMqManager>(rmq);
builder.Services.AddHostedService<RabbitMqBackgroundService>(rbs => 
    new RabbitMqBackgroundService(rbs.GetRequiredService<RabbitMqManager>(),"driver"));
//builder.Services.AddHostedService<UpdateLocationBackgroundService>();

builder.Services.AddSingleton<ITaxiManager, TaxiManager>();

var app = builder.Build(); 

app.MapPost("/taxi", async (ITaxiManager _taxiManager) =>
{
    string taxiId = await _taxiManager.AddTaxiAsync();
    return Results.Created($"/taxi/{taxiId}", taxiId);
});

app.MapDelete("/taxi/{taxiId}", async (ITaxiManager _taxiManager, string taxiId) =>
{
    if (await _taxiManager.RemoveTaxiAsync(taxiId))
        return Results.NoContent();
    return Results.NotFound();
    
});

app.MapGet("/taxis", async (ITaxiManager _taxiManager) => _taxiManager.GetActiveTaxiIds());

app.MapGet("/", async (RabbitMqManager rmq) => await rmq.PublishMessage("passenger", "This is a message from driver!"));

app.Run();