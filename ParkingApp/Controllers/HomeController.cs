using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Data; // Namespace for DbContext
using ParkingApp.Data.Models;
using ParkingApp.Models;
using System.Diagnostics;

namespace ParkingApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ParkingAppDbContext _dbContext; // Inject DbContext

        public HomeController(ILogger<HomeController> logger, ParkingAppDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var billingMonth = DateTime.UtcNow.ToString("yyyy-MM");
            var now = DateTime.UtcNow;
            var nextCycle = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(1);

            var parkingData = await _dbContext.Parkings
                .Include(p => p.Subscribers)
                .ToListAsync();

            var summaries = parkingData.Select(p => new ParkingSummaryViewModel
            {
                ParkingLocation = p.Location,
                TotalSpots = p.Capacity,
                SpotsTaken = p.Subscribers.Sum(s => s.ParkingSpots.Count),
                SpotsPaid = p.Subscribers.Count(s => s.Paid),
                TotalIncome = p.Subscribers.Where(s => s.Paid).Sum(s => s.TotalPriceInBgn),
                EmailSentThisMonth = _dbContext.SentEmailLogs.Any(l => l.Subscriber.ParkingId == p.Id && l.BillingMonth == billingMonth),
                NextEmailCycleTime = nextCycle
            }).ToList();

            return View(summaries);
        }


        private TimeSpan GetTimeUntilNextCycle()
        {
            var now = DateTime.Now;
            var next = new DateTime(now.Year, now.Month, 1, 8, 0, 0).AddMonths(1);
            return next - now;
        }

        public IActionResult Startup()
        {
            return View();
        }

        public IActionResult LoadSidebar()
        {
            var parkings = _dbContext.Parkings
                .Include(p => p.Subscribers)
                .ToList();
            return PartialView("_ParkingSidebar", parkings);
        }

        [HttpGet]
        public IActionResult AddParking()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddParking(string name, int capacity, decimal price)
        {
            if (ModelState.IsValid)
            {
                // Create a new Parking object
                var parking = new Parking
                {
                    Location = name, // Map the "name" field to the "Location" property
                    Capacity = capacity,
                    PriceInBgn = price // Ensure the Parking model has this property
                };

                // Save the parking data to the database
                _dbContext.Parkings.Add(parking);
                _dbContext.SaveChanges();

                TempData["SuccessMessage"] = "Parking added successfully!";
                return RedirectToAction("Index");
            }

            return View();
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ParkingApp.Models.ErrorViewModel
            { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public IActionResult SetLanguage(string culture)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            return Redirect(Request.Headers["Referer"].ToString());
        }

    }
}
