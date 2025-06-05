using ParkingApp.Data;
using System;
using System.Linq;

namespace ParkingApp.Services
{
    public class PaymentProcessingService
    {
        private readonly ParkingAppDbContext _DbContext;

        public PaymentProcessingService(ParkingAppDbContext dbContext)
        {
            _DbContext = dbContext;
        }

        public void ProcessBankPayment(string email, decimal amount)
        {
            var subscriber = _DbContext.Subscribers.FirstOrDefault(s => s.EmailAddress == email);
            if (subscriber == null) return;

            int monthsPaid = (int)Math.Floor(amount / subscriber.PriceInBgn);
            if (monthsPaid > 0)
            {
                subscriber.Paid = true;
                subscriber.MonthsPaidAhead = monthsPaid - 1;
            }

            _DbContext.SaveChanges();
        }
    }
}
