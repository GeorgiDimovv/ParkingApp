using Microsoft.AspNetCore.Mvc;
using ParkingApp.Data;
using Microsoft.EntityFrameworkCore;
using ParkingApp.Data.Models;
using ParkingApp.Data.Enum;
using Microsoft.Extensions.Localization;

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

        // make them async 

        [HttpGet]
        public IActionResult AddSubscriber(int parkingId)
        {
            ViewBag.ParkingId = parkingId;
            return View();
        }

        [HttpPost]
        public IActionResult AddSubscriber(int parkingId, string name, string email, string phoneNumbers,string barrierPhoneNumbers, string spots, PaymentMethod paymentMethod, decimal priceInBgn, bool paid)
        {
            if (ModelState.IsValid)
            {
                // Create a new Parking object
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
                    //UPDATE: You might want to add a check for existing subscribers or parking spots here
                    //UPDATE: You might also want to add logic to handle the case where a subscriber already exists for the same parking spot
                };

                subscriber.TotalPriceInBgn = subscriber.ParkingSpots.Count * subscriber.PriceInBgn;

                // Save the parking data to the database
                _DbContext.Subscribers.Add(subscriber);
                _DbContext.SaveChanges();

                TempData["SuccessMessage"] = "Subscriber added successfully!";
                return RedirectToAction("Details", new { id = parkingId });
            }

            return View("AddSubscriber");
        }

        [HttpPost]
        public IActionResult UpdateSubscribers(int parkingId, List<SubscriberUpdateModel> subscribers)
        {
            foreach (var sub in subscribers)
            {
                var dbSub = _DbContext.Subscribers.FirstOrDefault(s => s.Id == sub.Id);
                if (dbSub != null)
                {
                    dbSub.PriceInBgn = sub.PriceInBgn;
                    dbSub.TotalPriceInBgn = sub.PriceInBgn * dbSub.ParkingSpots.Count;
                }
            }

            _DbContext.SaveChanges();

            TempData["SuccessMessage"] = "Subscribers updated successfully!";
            return RedirectToAction("Details", new { id = parkingId });
        }

        [HttpPost]
        public IActionResult TogglePaid(int subscriberId, int parkingId)
        {
            var subscriber = _DbContext.Subscribers.FirstOrDefault(s => s.Id == subscriberId);
            if (subscriber == null)
            {
                return NotFound();
            }

            subscriber.Paid = !subscriber.Paid;
            _DbContext.SaveChanges();

            return RedirectToAction("Details", new { id = parkingId });
        }


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

        // POST: Parking/EditSubscriber
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

            // Update fields
            subscriber.Name = name;
            subscriber.PhoneNumber = phoneNumbers.Split(",").ToList();
            subscriber.EmailAddress = email;
            subscriber.ParkingSpots = spots.Split(",").ToList();
            subscriber.BarrierPhoneNumbers = barrierPhoneNumbers.Split(",").ToList();
            subscriber.PaymentMethod = paymentMethod;
            subscriber.PriceInBgn = priceInBgn;
            subscriber.TotalPriceInBgn = subscriber.ParkingSpots.Count * priceInBgn;

            _DbContext.SaveChanges();

            return RedirectToAction("Details", new { id = parkingId });
        }




    }
}
