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
        private readonly InvoiceService _invoiceService;
        public MonthlyBillingService(ParkingAppDbContext dbContext, EmailService emailService, InvoiceService invoiceService)
        {
            _DbContext = dbContext;
            _emailService = emailService;
            _invoiceService = invoiceService;
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

            var groupedSubscribers = subscribers
                .GroupBy(s => s.EmailAddress)
                .ToList();

            foreach (var group in groupedSubscribers)
            {
                var sub = group.First(); // pick one for name/email

                if (alreadySentIds.Contains(sub.Id)) continue;

                // Merge prices from all entries
                var totalPrice = group.Sum(s => s.TotalPriceInBgn);
                var combinedSpots = string.Join(", ", group.Select(s => s.ParkingSpot));
                var parkingLocation = sub.Parking?.Location ?? "";

                // Decrement MonthsPaidAhead for all spots
                foreach (var s in group)
                {
                    if (s.MonthsPaidAhead > 0) s.MonthsPaidAhead--;
                    s.Paid = s.MonthsPaidAhead > 0;
                }

                var mergedSub = new Subscriber
                {
                    Id = sub.Id,
                    Name = sub.Name,
                    EmailAddress = sub.EmailAddress,
                    ParkingSpot = combinedSpots,
                    TotalPriceInBgn = totalPrice,
                    Parking = sub.Parking // optional if needed for location
                };

                var invoicePdf = _invoiceService.GenerateInvoicePdf(mergedSub);

                await _emailService.SendEmailWithAttachmentAsync(
                    mergedSub.EmailAddress,
                    $"[Parking: {parkingLocation}] Месечна фактура",
                    $"Здравейте {mergedSub.Name},\n\nПрикачена е вашата фактура за този месец.\n\nПоздрави,\nПаркинг администрация",
                    invoicePdf,
                    $"Фактура-{DateTime.Now:yyyyMM}-{mergedSub.Id}.pdf"
                );

                _DbContext.SentEmailLogs.Add(new SentEmailLog
                {
                    SubscriberId = mergedSub.Id,
                    BillingMonth = billingMonth,
                    SentAt = DateTime.UtcNow
                });
            }
            await _DbContext.SaveChangesAsync();
        }
    }
}
