using System.Collections.Concurrent;
using Model;

namespace DriverService;

public class TaxiManager : ITaxiManager
{
    private readonly RabbitMqManager _rmq;
    // Maps taxi IDs to their CancellationTokenSources
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeTaxis = new();

    public TaxiManager(RabbitMqManager rmq)
    {
        _rmq = rmq;
    }

    // Adds a taxi instance and starts its background loop.
    public async Task<string> AddTaxiAsync()
    {
        string taxiId = Guid.NewGuid().ToString();
        var cts = new CancellationTokenSource();

        if (!_activeTaxis.TryAdd(taxiId, cts))
            throw new Exception("Failed to add taxi instance.");

        Random rnd = new Random();
        var taxi = new Taxi(_rmq, taxiId){X = rnd.Next(0, 50), Y = rnd.Next(0,70)};
        // Fire-and-forget the taxi's background loop.
        _ = Task.Run(() => taxi.RunAsync(cts.Token), cts.Token);
        return taxiId;
    }

    // Cancels the background loop and removes the taxi instance.
    public Task<bool> RemoveTaxiAsync(string taxiId)
    {
        if (_activeTaxis.TryRemove(taxiId, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public IEnumerable<string> GetActiveTaxiIds() => _activeTaxis.Keys;
    
}