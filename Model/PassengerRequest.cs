namespace Model;

public class PassengerRequest
{
    public string PassengerId { get; set; }
    public Location Location { get; set; }
    public Location DesiredLocation { get; set; }
}