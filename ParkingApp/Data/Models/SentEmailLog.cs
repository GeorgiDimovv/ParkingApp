using ParkingApp.Data.Models;
using System;

namespace ParkingApp.Models
{
    public class SentEmailLog
    {
        public int Id { get; set; }

        public int SubscriberId { get; set; }
        public string BillingMonth { get; set; } // Format: "2025-06"

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public Subscriber Subscriber { get; set; }
    }
}
