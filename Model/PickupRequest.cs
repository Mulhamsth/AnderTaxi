namespace Model;

public class PickupRequest
{
    public string PassengerId { get; set; }
    public string DriverId { get; set; }
    public Location PickupAt { get; set; }
    public Location PickupTo { get; set; }
}