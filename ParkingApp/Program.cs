using Microsoft.EntityFrameworkCore;
using ParkingApp.Data;
using ParkingApp.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using System.Globalization;

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

            //Emails
            builder.Services.AddScoped<EmailService>();
            builder.Services.AddScoped<MonthlyBillingService>();
            builder.Services.AddScoped<PaymentProcessingService>();
            builder.Services.AddHostedService<MonthlyTaskRunner>(); // background monthly job

            //Translation
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

            builder.Services.AddControllersWithViews()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization();




            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            var supportedCultures = new[] { "en", "bg" };
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture("en")
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            localizationOptions.RequestCultureProviders.Insert(0, new QueryStringRequestCultureProvider());

            app.UseRequestLocalization(localizationOptions);


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
