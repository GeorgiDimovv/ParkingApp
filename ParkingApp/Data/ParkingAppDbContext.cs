using Microsoft.EntityFrameworkCore;
using ParkingApp.Data.Models;
using ParkingApp.Models;

namespace ParkingApp.Data
{
    public class ParkingAppDbContext : DbContext
    {
        public ParkingAppDbContext(DbContextOptions<ParkingAppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Parking> Parkings { get; set; }
        public DbSet<Subscriber> Subscribers { get; set; }
        public DbSet<SentEmailLog> SentEmailLogs { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure the precision and scale for the PriceInBgn property
            modelBuilder.Entity<Subscriber>()
                .Property(s => s.PriceInBgn)
                .HasColumnType("decimal(18,2)"); // Precision: 18, Scale: 2
        }

    }
}
