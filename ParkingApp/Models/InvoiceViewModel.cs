namespace ParkingApp.Models.ViewModels
{
    public class InvoiceViewModel
    {
        public string SubscriberName { get; set; }
        public string Email { get; set; }
        public string ParkingLocation { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime BillingDate { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
