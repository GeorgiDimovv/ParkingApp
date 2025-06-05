using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Data;
using ParkingApp.Services;
using ParkingApp.Utilities;

namespace ParkingApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            //Database configuration
            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<ParkingAppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Emails
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<MonthlyBillingService>();
            builder.Services.AddScoped<PaymentProcessingService>();
            builder.Services.AddScoped<InvoiceService>();
            builder.Services.AddScoped<PdfGenerator>();

            // Register the DinkToPdf converter (REQUIRED)
            builder.Services.AddSingleton<IConverter>(new SynchronizedConverter(new PdfTools()));

            // Background task
            builder.Services.AddHostedService<MonthlyTaskRunner>(); // background monthly job


            builder.Services.AddControllersWithViews()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization();




            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            


            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Startup}/{id?}");

            app.Run();
        }
    }
}
