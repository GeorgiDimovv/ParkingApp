using Microsoft.EntityFrameworkCore.Query;
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

            //var ranOnce = false; // test

            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                // Run only on 1st day of the month
                if (now.Day == 1) //now.Day == 1
                {
                    using var scope = _services.CreateScope();
                    var billingService = scope.ServiceProvider.GetRequiredService<MonthlyBillingService>();
                    await billingService.ProcessMonthlyBillingAsync();


                    //ranOnce = true; //test
                }

                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken); //Check every 10 minutes
            }
        }


    }
}
