namespace DriverService;

public interface ITaxiManager
{
    Task<string> AddTaxiAsync();
    Task<bool> RemoveTaxiAsync(string taxiId);
    IEnumerable<string> GetActiveTaxiIds();
}