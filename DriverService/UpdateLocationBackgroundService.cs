using Model;

namespace DriverService;

public class UpdateLocationBackgroundService : BackgroundService
{
    private readonly RabbitMqManager rmq;
    public UpdateLocationBackgroundService(RabbitMqManager rmq)
    {
        this.rmq = rmq;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await rmq.PublishMessage("distancelogic", "this is my location");
        }
    }
}