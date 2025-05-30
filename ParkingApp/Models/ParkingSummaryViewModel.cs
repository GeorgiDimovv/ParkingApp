namespace ParkingApp.Models;

public class ParkingSummaryViewModel
{
    public int ParkingId { get; set; }
    public string ParkingLocation { get; set; }
    public int TotalSpots { get; set; }
    public int SpotsTaken { get; set; }
    public int SpotsAvailable => TotalSpots - SpotsTaken;
    public int SpotsPaid { get; set; }
    public decimal TotalIncome { get; set; }
    public bool EmailSentThisMonth { get; set; }
    public DateTime NextEmailCycleTime { get; set; }
}
