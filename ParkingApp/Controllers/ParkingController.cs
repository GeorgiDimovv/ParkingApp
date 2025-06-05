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
                .FirstOrDefault(p => p.Id == id);

            if (parking == null)
            {
                return NotFound();
            }

            return View(parking);
        }

        [HttpGet]
        public IActionResult AddSubscriber(int parkingId)
        {
            ViewBag.ParkingId = parkingId;
            return View();
        }

        // Adds a new subscriber to the database
        [HttpPost]
        public IActionResult AddSubscriber(int parkingId, string spot, string name, string engBuisness, string bgBuisness, string email, string phoneNumbers, string barrierPhoneNumbers, PaymentMethod paymentMethod, decimal priceInBgn, bool paid)
        {
            if (!ModelState.IsValid)
            {
                return View("AddSubscriber");
            }

            var phoneList = phoneNumbers.Split(",").Select(p => p.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            var barrierList = barrierPhoneNumbers.Split(",").Select(b => b.Trim()).Where(b => !string.IsNullOrWhiteSpace(b)).ToList();

            var takenSpots = _DbContext.Subscribers
                .Where(s => s.ParkingId == parkingId)
                .Select(s => s.ParkingSpot)
                .ToHashSet();

            if (string.IsNullOrWhiteSpace(spot))
            {
                TempData["Error"] = "Моля, въведете валидно място за паркиране.";
                return RedirectToAction("Details", new { id = parkingId });
            }

            if (takenSpots.Contains(spot))
            {
                TempData["Error"] = $"Мястото {spot} вече е заето.";
                return RedirectToAction("Details", new { id = parkingId });
            }

            var newSubscriber = new Subscriber
            {

                ParkingSpot = spot,
                Name = name,
                ENGBuisnessName = engBuisness,
                BGBuisnessName = bgBuisness,
                PhoneNumber = phoneList,
                EmailAddress = email,
                BarrierPhoneNumbers = barrierList,
                PaymentMethod = paymentMethod,
                PriceInBgn = priceInBgn,
                Paid = paid,
                ParkingId = parkingId
            };

            _DbContext.Subscribers.Add(newSubscriber);
            _DbContext.SaveChanges();

            TempData["SuccessMessage"] = "Абонатът беше добавен успешно!";
            return RedirectToAction("Details", new { id = parkingId });
        }




        // Deletes a subscriber from a parking lot
        [HttpPost]
        public IActionResult DeleteSubscriber(int subscriberId, int parkingId)
        {
            var subscriber = _DbContext.Subscribers
                .FirstOrDefault(s => s.Id == subscriberId);

            if (subscriber != null)
            {
                _DbContext.Subscribers.Remove(subscriber);
                _DbContext.SaveChanges();
            }

            return RedirectToAction("Details", new { id = parkingId });
        }

        [HttpGet]
        public IActionResult SearchSubscribers(int parkingId, string searchTerm)
        {
            var normalizedTerm = searchTerm?.ToLower();

            var subscribers = _DbContext.Subscribers
                .Where(s => s.ParkingId == parkingId &&
                    (string.IsNullOrEmpty(normalizedTerm) ||
                     s.Name.ToLower().Contains(normalizedTerm) ||
                     s.PhoneNumber.Any(p => p.ToLower().Contains(normalizedTerm))
                    )
                )
                .ToList();

            return PartialView("_SubscribersTable", subscribers);
        }




        // Toggles the "paid" status of a subscriber
        [HttpPost]
        public IActionResult TogglePaid(int subscriberId, int parkingId)
        {
            var subscriber = _DbContext.Subscribers
                .FirstOrDefault(s => s.Id == subscriberId && s.ParkingId == parkingId);

            if (subscriber == null)
                return Json(new { success = false });

            subscriber.Paid = !subscriber.Paid;
            _DbContext.SaveChanges();

            return Json(new { success = true, paid = subscriber.Paid });
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
            return View(subscriber);
        }

        // Updates a subscriber’s details from the edit form
        [HttpPost]
        public IActionResult EditSubscriber(int parkingId, int subscriberId, string name, string engBuisness, string bgBuisness, string email, string phoneNumbers, string barrierPhoneNumbers, string spots, PaymentMethod paymentMethod, decimal priceInBgn, bool paid)
        {
            var parking = _DbContext.Parkings
                .Include(p => p.Subscribers)
                .FirstOrDefault(p => p.Id == parkingId);

            if (parking == null)
                return NotFound();

            var subscriber = parking.Subscribers.FirstOrDefault(s => s.Id == subscriberId);
            if (subscriber == null)
                return NotFound();

            var newSpot = spots?.Trim();

            // Check if new spot is taken by another subscriber
            if (_DbContext.Subscribers.Any(s => s.ParkingId == parkingId && s.Id != subscriberId && s.ParkingSpot == newSpot))
            {
                TempData["Error"] = $"Мястото {newSpot} вече е заето.";
                return RedirectToAction("Details", new { id = parkingId });
            }

            // Update properties
            subscriber.Name = name;
            subscriber.ENGBuisnessName = engBuisness;
            subscriber.BGBuisnessName = bgBuisness;
            subscriber.PhoneNumber = phoneNumbers.Split(",").Select(p => p.Trim()).ToList();
            subscriber.EmailAddress = email;
            subscriber.ParkingSpot = newSpot;
            subscriber.BarrierPhoneNumbers = barrierPhoneNumbers.Split(",").Select(b => b.Trim()).ToList();
            subscriber.PaymentMethod = paymentMethod;
            subscriber.PriceInBgn = priceInBgn;
            subscriber.Paid = paid;

            _DbContext.SaveChanges();
            TempData["SuccessMessage"] = "Абонатът беше обновен успешно!";
            return RedirectToAction("Details", new { id = parkingId });
        }


        // Imports subscribers from an uploaded Excel file
        [HttpPost]
        public async Task<IActionResult> ImportSubscribersFromExcel(IFormFile excelFile, int parkingId)
        {
            ExcelPackage.License.SetNonCommercialOrganization("My Noncommercial organization");


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
                    for (int row = 3; row <= rowCount; row++)
                    {
                        string spot = worksheet.Cells[row, 1].Text;
                        string name = worksheet.Cells[row, 2].Text;
                        string engBuisness = worksheet.Cells[row, 3].Text;
                        string bgBuisness = worksheet.Cells[row, 4].Text;
                        string phone = worksheet.Cells[row, 5].Text;
                        string barrier = worksheet.Cells[row, 6].Text;
                        var priceText = worksheet.Cells[row, 7].Text;
                        var paymentMethod = worksheet.Cells[row, 8].Text;

                        if (string.IsNullOrWhiteSpace(name)) continue; // Skip empty names

                        subscribers.Add(new Subscriber
                        {
                            ParkingSpot = spot,
                            Name = name,
                            ENGBuisnessName = engBuisness,
                            BGBuisnessName = bgBuisness,
                            PhoneNumber = new List<string> { phone },
                            BarrierPhoneNumbers = new List<string> { barrier },
                            PriceInBgn = decimal.TryParse(priceText, out var price) ? price : 0,
                            PaymentMethod = Enum.TryParse<PaymentMethod>(paymentMethod.Trim(), true, out var parsedMethod)
                                ? parsedMethod
                                : PaymentMethod.Cash, // Default to cash

                            Paid = false,
                            MonthsPaidAhead = 0,
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