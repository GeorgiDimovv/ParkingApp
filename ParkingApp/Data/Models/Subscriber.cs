using ParkingApp.Data.Enum;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ParkingApp.Data.Models
{
    public class Subscriber
    {
        public int Id { get; set; }


        [Required]
        public string ParkingSpot { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;


        public string? ENGBusinessName { get; set; } = null!;

        public string? BGBusinessName { get; set; } = null!;


        [Required]
        public List<string> PhoneNumber { get; set; } = new List<string>();

        [EmailAddress]
        public string EmailAddress { get; set; } = null!;

        [Required]
        public List<string> BarrierPhoneNumbers { get; set; } = new List<string>();

        public PaymentMethod PaymentMethod { get; set; }

        [ForeignKey(nameof(Parking))]
        public int ParkingId { get; set; }
        public Parking Parking { get; set; } = null!;

        [Required]
        public decimal PriceInBgn { get; set; }


        public decimal TotalPriceInBgn { get; set; } = 0;

        [Required]
        public bool Paid { get; set; } = false;

        public int MonthsPaidAhead { get; set; } = 0;
    }
}
