using ParkingApp.Models;

namespace ParkingApp.Models;

public class DashboardViewModel
{
    public List<ParkingSummaryViewModel> Parkings { get; set; } = new();
}
