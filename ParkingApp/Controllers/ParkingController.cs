using Microsoft.AspNetCore.Mvc;
using ParkingApp.Data;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Data.Models;
using ParkingApp.Data.Enum;
using Microsoft.Extensions.Localization;
using OfficeOpenXml;
using SendGrid.Helpers.Mail;

namespace ParkingApp.Controllers
{
    public class ParkingController : Controller
    {
        private readonly ParkingAppDbContext _DbContext;
        private readonly IStringLocalizer<ParkingController> _localizer;

        public ParkingController(ParkingAppDbContext context, IStringLocalizer<ParkingController> localizer)
        {
            _DbContext = context;
            _localizer = localizer;
        }

        // Shows details of a specific parking lot, including its subscribers
        public IActionResult Details(int id)
        {
            var parking = _DbContext.Parkings
                .Include(p => p.Subscribers)
                .FirstOrDefault(p => p.Id == id); // Get the parking with the given ID

            if (parking == null)
            {
                return NotFound();
            }

            return View(parking); // Pass data to the view
        }

        [HttpGet]
        public IActionResult AddSubscriber(int parkingId)
        {
            ViewBag.ParkingId = parkingId; // Pass parkingId to the view
            return View();
        }

        // Adds a new subscriber to the database
        [HttpPost]
        public IActionResult AddSubscriber(int parkingId, string name, string email, string phoneNumbers, string barrierPhoneNumbers, string spots, PaymentMethod paymentMethod, decimal priceInBgn, bool paid)
        {
            if (ModelState.IsValid)
            {
                // Create a new Subscriber object with form data
                var subscriber = new Subscriber
                {
                    Name = name,
                    PhoneNumber = phoneNumbers.Split(",").ToList(),
                    EmailAddress = email,
                    ParkingSpots = spots.Split(",").ToList(),
                    BarrierPhoneNumbers = barrierPhoneNumbers.Split(",").ToList(),
                    PaymentMethod = paymentMethod,
                    PriceInBgn = priceInBgn,
                    Paid = paid,
                    ParkingId = parkingId
                    // UPDATE: You might want to add a check for existing subscribers or parking spots here
                };

                subscriber.TotalPriceInBgn = subscriber.ParkingSpots.Count * subscriber.PriceInBgn;

                _DbContext.Subscribers.Add(subscriber);
                _DbContext.SaveChanges();

                TempData["SuccessMessage"] = "Subscriber added successfully!";
                return RedirectToAction("Details", new { id = parkingId });
            }

            return View("AddSubscriber");
        }

        // Deletes a subscriber from a parking lot
        [HttpPost]
        public IActionResult DeleteSubscriber(int subscriberId, int parkingId)
        {
            var subscriber = _DbContext.Subscribers
                .FirstOrDefault(s => s.Id == subscriberId); // Find subscriber

            if (subscriber != null)
            {
                _DbContext.Subscribers.Remove(subscriber);
                _DbContext.SaveChanges();
            }

            return RedirectToAction("Details", new { id = parkingId });
        }



        // Toggles the "paid" status of a subscriber
        [HttpPost]
        public IActionResult TogglePaid(int subscriberId, int parkingId)
        {
            var subscriber = _DbContext.Subscribers.FirstOrDefault(s => s.Id == subscriberId); // Find subscriber
            if (subscriber == null)
            {
                return NotFound();
            }

            subscriber.Paid = !subscriber.Paid;
            _DbContext.SaveChanges();

            return RedirectToAction("Details", new { id = parkingId });
        }

        // Shows the edit form for a specific subscriber
        public IActionResult EditSubscriber(int parkingId, int subscriberId)
        {
            var parking = _DbContext.Parkings
                .Include(p => p.Subscribers)
                .FirstOrDefault(p => p.Id == parkingId);

            if (parking == null)
                return NotFound();

            var subscriber = parking.Subscribers.FirstOrDefault(s => s.Id == subscriberId);

            if (subscriber == null)
                return NotFound();

            ViewBag.ParkingId = parkingId;
            return View(subscriber); // Show form with subscriber info
        }

        // Updates a subscriber’s details from the edit form
        [HttpPost]
        public IActionResult EditSubscriber(int parkingId, int subscriberId, string name, string email, string phoneNumbers, string barrierPhoneNumbers, string spots, PaymentMethod paymentMethod, decimal priceInBgn, bool paid)
        {
            var parking = _DbContext.Parkings
                .Include(p => p.Subscribers)
                .FirstOrDefault(p => p.Id == parkingId);

            if (parking == null)
                return NotFound();

            var subscriber = parking.Subscribers.FirstOrDefault(s => s.Id == subscriberId);
            if (subscriber == null)
                return NotFound();

            // Update subscriber properties
            subscriber.Name = name;
            subscriber.PhoneNumber = phoneNumbers.Split(",").ToList();
            subscriber.EmailAddress = email;
            subscriber.ParkingSpots = spots.Split(",").ToList();
            subscriber.BarrierPhoneNumbers = barrierPhoneNumbers.Split(",").ToList();
            subscriber.PaymentMethod = paymentMethod;
            subscriber.PriceInBgn = priceInBgn;
            subscriber.TotalPriceInBgn = subscriber.ParkingSpots.Count * priceInBgn;

            _DbContext.SaveChanges(); // Save updates

            return RedirectToAction("Details", new { id = parkingId });
        }

        // Imports subscribers from an uploaded Excel file
        [HttpPost]
        public async Task<IActionResult> ImportSubscribersFromExcel(IFormFile excelFile, int parkingId)
        {
            if (excelFile == null || excelFile.Length == 0)
            {
                TempData["Error"] = "Моля, изберете валиден Excel файл.";
                return RedirectToAction("Details", new { id = parkingId });
            }

            var subscribers = new List<Subscriber>();

            using (var stream = new MemoryStream())
            {
                await excelFile.CopyToAsync(stream); // Copy file to memory
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault(); // Get first worksheet
                    if (worksheet == null)
                    {
                        TempData["Error"] = "Файлът няма валиден worksheet.";
                        return RedirectToAction("Details", new { id = parkingId });
                    }

                    int rowCount = worksheet.Dimension.Rows; // Get number of rows

                    // Loop through rows starting from 2 (assuming row 1 is headers)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var name = worksheet.Cells[row, 1].Text;
                        var phone = worksheet.Cells[row, 4].Text;
                        var barrier = worksheet.Cells[row, 5].Text;
                        var priceText = worksheet.Cells[row, 6].Text;
                        var paymentMethod = worksheet.Cells[row, 7].Text;

                        if (string.IsNullOrWhiteSpace(name)) continue; // Skip empty names

                        subscribers.Add(new Subscriber
                        {
                            Name = name,
                            PhoneNumber = new List<string> { phone },
                            BarrierPhoneNumbers = new List<string> { barrier },
                            PriceInBgn = decimal.TryParse(priceText, out var price) ? price : 0, // Parse or default to 0
                            PaymentMethod = Enum.TryParse<PaymentMethod>(paymentMethod.Trim(), true, out var parsedMethod)
                                ? parsedMethod
                                : PaymentMethod.Cash, // Default to cash

                            Paid = false,
                            MonthsPaidAhead = 0,
                            TotalPriceInBgn = 0,
                            EmailAddress = "", // Optional email
                            ParkingId = parkingId
                        });
                    }
                }
            }

            _DbContext.Subscribers.AddRange(subscribers); // Add all to DB
            await _DbContext.SaveChangesAsync(); // Save changes

            TempData["Success"] = $"{subscribers.Count} абоната бяха добавени успешно.";
            return RedirectToAction("Details", new { id = parkingId });
        }
    }
}