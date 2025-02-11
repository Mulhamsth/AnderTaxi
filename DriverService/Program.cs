using System.Text;
using CloudNative.CloudEvents;
using DriverService;
using Model;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var builder = WebApplication.CreateBuilder(args);

var rmq = await RabbitMqManager.Initialize();
rmq.QueueName = "driver";

builder.Services.AddSingleton<RabbitMqManager>(rmq);
builder.Services.AddHostedService<RabbitMqBackgroundService>(rbs => 
    new RabbitMqBackgroundService(
        rbs.GetRequiredService<RabbitMqManager>(),
        "driver", 
        cloudEvent => HandlingCloudEventMessage(cloudEvent, rbs.GetRequiredService<ITaxiManager>())));

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

app.MapDelete("/taxis", async (ITaxiManager _taxiManager) =>
{
    foreach (var taxiId in _taxiManager.GetActiveTaxiIds())
    {
        await _taxiManager.RemoveTaxiAsync(taxiId);
    }
    return Results.NoContent();
});

app.MapGet("/taxis", async (ITaxiManager _taxiManager) => _taxiManager.GetActiveTaxiIds());

//app.MapGet("/", async (RabbitMqManager rmq) => await rmq.PublishMessage("passenger", "driver.message","This is a message from driver!"));


async Task HandlingCloudEventMessage(CloudEvent cloudEvent, ITaxiManager taxiManager)
{
    if(cloudEvent.Type == "distancelogic.pickup")
    {
        var pickupRequest = await rmq.DeserializeCloudEventMessage<PickupRequest>(cloudEvent);
        Console.WriteLine($" [x] Received pickup message: {pickupRequest.PassengerId} wants to be picked up by {pickupRequest.DriverId} at ({pickupRequest.PickupAt.X}, {pickupRequest.PickupAt.Y})");
        await taxiManager.HandlePickupRequestAsync(pickupRequest);
    }
    else
    {
        Console.WriteLine(" [x] Received Unknown Message");
    }
}


app.Run();

