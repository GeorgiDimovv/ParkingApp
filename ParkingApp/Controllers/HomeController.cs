    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using OfficeOpenXml;
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

        public IActionResult Startup()
        {
            return View();
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
                        TotalIncome = p.Subscribers.Where(s => s.Paid).Sum(s => s.PriceInBgn),
                        EmailSentThisMonth = sentCount == totalSubs && totalSubs > 0,
                        NextEmailCycleTime = nextCycle
                    };
                }).ToList();



                return View(summaries);
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

        public async Task<IActionResult> ImportPaidSubscribersByNameFromExcel(IFormFile bankExcelFile)
        {
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");

            if (bankExcelFile == null || bankExcelFile.Length == 0)
            {
                TempData["Error"] = "Моля, качете валиден Excel файл.";
                return RedirectToAction("Index");
            }

            var allParkings = await _dbContext.Parkings
                .Include(p => p.Subscribers)
                .ToListAsync();

            var matchedCounts = new Dictionary<int, int>(); // SubscriberId -> Months paid

            using (var stream = new MemoryStream())
            {
                await bankExcelFile.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    foreach (var worksheet in package.Workbook.Worksheets)
                    {
                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++) // Skip header
                        {
                            string cellC = worksheet.Cells[row, 3]?.Text?.Trim().ToUpperInvariant() ?? "";
                            string amountText = worksheet.Cells[row, 5]?.Text?.Trim().Replace(" ", "").Replace(",", ".") ?? "";

                            if (!decimal.TryParse(amountText, out decimal amountPaid) || amountPaid <= 0)
                                continue;

                            // Extract name from cellC — just first 3 words
                            var namePartsFromExcel = cellC.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            string excelName = string.Join(" ", namePartsFromExcel.Take(3)).ToUpperInvariant();

                            // Step 1: Find all matching subscribers
                            var matchedSubscribers = allParkings
                                .SelectMany(p => p.Subscribers)
                                .Where(s =>
                                {
                                    var dbNameParts = s.Name?.ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                    if (dbNameParts == null || dbNameParts.Length < 2) return false;

                                    string firstName = dbNameParts[0];
                                    string lastName = dbNameParts[^1];

                                    return excelName.Contains(firstName) && excelName.Contains(lastName);
                                })
                                .ToList();

                            if (matchedSubscribers.Count == 0)
                                continue;

                            // Step 2: Divide payment fairly among all matched subscriptions
                            decimal splitAmount = amountPaid / matchedSubscribers.Count;

                            foreach (var subscriber in matchedSubscribers)
                            {
                                decimal expectedPrice = subscriber.PriceInBgn;
                                if (expectedPrice <= 0)
                                    continue;

                                int monthsPaid = (int)(splitAmount / expectedPrice);
                                if (monthsPaid > 0)
                                    monthsPaid -= 1;

                                if (!matchedCounts.ContainsKey(subscriber.Id))
                                    matchedCounts[subscriber.Id] = 0;

                                matchedCounts[subscriber.Id] += monthsPaid;
                            }
                        }
                    }
                }
            }

            // Apply changes
            foreach (var kvp in matchedCounts)
            {
                var subscriber = await _dbContext.Subscribers.FindAsync(kvp.Key);
                if (subscriber != null)
                {
                    subscriber.Paid = true;
                    subscriber.MonthsPaidAhead += kvp.Value;
                }
            }

            await _dbContext.SaveChangesAsync();

            TempData["Success"] = $"Намерени съвпадения за {matchedCounts.Count} абоната. Общ брой платени месеци: {matchedCounts.Values.Sum()}.";

            return RedirectToAction("Index");
        }




        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
            public IActionResult Error()
            {
                return View(new ParkingApp.Models.ErrorViewModel
                { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }
    }
