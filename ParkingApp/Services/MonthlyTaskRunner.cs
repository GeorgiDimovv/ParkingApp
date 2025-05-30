using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ParkingApp.Services
{
    public class MonthlyTaskRunner : BackgroundService
    {
        private readonly IServiceProvider _services;

        public MonthlyTaskRunner(IServiceProvider services)
        {
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {

            var ranOnce = false; // Initialize to false to run on the first check

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                // Run only on 1st day of the month at 08:00
                if (now.Day == 1 && now.Hour == 8) //now.Day == 1 && now.Hour == 8
                {
                    using var scope = _services.CreateScope();
                    var billingService = scope.ServiceProvider.GetRequiredService<MonthlyBillingService>();
                    await billingService.ProcessMonthlyBillingAsync();


                    //ranOnce = true;
                    // Add email sending here too later if you want
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Check every hour
            }
        }


    }
}
