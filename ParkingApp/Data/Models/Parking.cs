using System.ComponentModel.DataAnnotations;

namespace ParkingApp.Data.Models
{
    public class Parking
    {
        public int Id { get; set; }

        [Required]
        public string Location { get; set; } = null!;

        [Required]
        public int Capacity { get; set; }

        [Required]
        public decimal PriceInBgn { get; set; } = 0;

        public ICollection<Subscriber> Subscribers { get; set; } = new List<Subscriber>();
    }
}
