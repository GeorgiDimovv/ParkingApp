using Microsoft.EntityFrameworkCore;
using ParkingApp.Data;
using ParkingApp.Data.Models;
using ParkingApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ParkingApp.Services
{
    public class MonthlyBillingService
    {
        private readonly ParkingAppDbContext _DbContext;
        private readonly EmailService _emailService;

        public MonthlyBillingService(ParkingAppDbContext dbContext, EmailService emailService)
        {
            _DbContext = dbContext;
            _emailService = emailService;
        }

        public async Task ProcessMonthlyBillingAsync()
        {
            var now = DateTime.Now;
            var billingMonth = DateTime.Now.ToString("yyyy-MM");

            var alreadySentIds = _DbContext.SentEmailLogs
                .Where(log => log.BillingMonth == billingMonth)
                .Select(log => log.SubscriberId)
                .ToHashSet();

            var subscribers = _DbContext.Subscribers
                .Include(s => s.Parking)
                .ToList();

            foreach (var sub in subscribers)
            {
                if (alreadySentIds.Contains(sub.Id))
                {
                    Console.WriteLine($"SKIPPED {sub.Name} — already emailed this month.");
                    continue;
                }

                if (sub.MonthsPaidAhead > 0)
                {
                    sub.MonthsPaidAhead--;
                    sub.Paid = true;
                }
                else
                {
                    sub.Paid = false;
                }

                Console.WriteLine($"PROCESSING {sub.Name} (Paid: {sub.Paid}, MonthsAhead: {sub.MonthsPaidAhead})");

                await _emailService.SendEmailAsync(
                    sub.EmailAddress,
                    $"[Parking: {sub.Parking?.Location}] Payment Due",
                    $"Dear {sub.Name},\n\nYour parking fee of {sub.TotalPriceInBgn} BGN is due for this month.\n\nRegards,\nParking Admin"
                );

                _DbContext.SentEmailLogs.Add(new SentEmailLog
                {
                    SubscriberId = sub.Id,
                    BillingMonth = billingMonth,
                    SentAt = DateTime.UtcNow
                });
            }

            await _DbContext.SaveChangesAsync();
        }



    }
}
