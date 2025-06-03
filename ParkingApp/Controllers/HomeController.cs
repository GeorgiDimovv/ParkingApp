using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Data;
using ParkingApp.Data.Models;
using ParkingApp.Models;
using System.Diagnostics;

namespace ParkingApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ParkingAppDbContext _dbContext;

        public HomeController(ILogger<HomeController> logger, ParkingAppDbContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var billingMonth = DateTime.Now.ToString("yyyy-MM");
            var nextCycle = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);

            var parkingData = await _dbContext.Parkings
                .Include(p => p.Subscribers)
                .ToListAsync();

            var sentLogs = await _dbContext.SentEmailLogs
                .Where(l => l.BillingMonth == billingMonth)
                .ToListAsync();

            var summaries = parkingData.Select(p =>
            {
                var sentCount = p.Subscribers.Count(s =>
                    sentLogs.Any(log => log.SubscriberId == s.Id)
                );
                var totalSubs = p.Subscribers.Count;

                return new ParkingSummaryViewModel
                {
                    ParkingId = p.Id,
                    ParkingLocation = p.Location,
                    TotalSpots = p.Capacity,
                    SpotsTaken = p.Subscribers.Count(s => !string.IsNullOrWhiteSpace(s.ParkingSpot)),
                    SpotsPaid = p.Subscribers.Count(s => s.Paid && !string.IsNullOrWhiteSpace(s.ParkingSpot)),
                    TotalIncome = p.Subscribers.Where(s => s.Paid).Sum(s => s.TotalPriceInBgn),
                    EmailSentThisMonth = sentCount == totalSubs && totalSubs > 0,
                    NextEmailCycleTime = nextCycle
                };
            }).ToList();



            return View(summaries);
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
        public IActionResult AddParking(string name, int capacity)
        {
            if (ModelState.IsValid)
            {
                // Create a new Parking object
                var parking = new Parking
                {
                    Location = name,
                    Capacity = capacity,
                };

                // Save the parking data to the database
                _dbContext.Parkings.Add(parking);
                _dbContext.SaveChanges();

                TempData["SuccessMessage"] = "Parking added successfully!";
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        public IActionResult DeleteParking(int id)
        {
            var parking = _dbContext.Parkings
                .Include(p => p.Subscribers)
                .FirstOrDefault(p => p.Id == id);

            if (parking != null)
            {

                _dbContext.Subscribers.RemoveRange(parking.Subscribers);
                _dbContext.Parkings.Remove(parking);
                _dbContext.SaveChanges();
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCapacityAndRefresh(int id, int newCapacity)
        {
            var parking = await _dbContext.Parkings.FindAsync(id);
            if (parking == null)
            {
                return NotFound();
            }

            parking.Capacity = newCapacity;
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("Index"); // or whatever action loads the main view
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
