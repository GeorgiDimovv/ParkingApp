using Microsoft.AspNetCore.Mvc;
using ParkingApp.Models.ViewModels;

namespace ParkingApp.Controllers
{
    public class InvoicesController : Controller
    {
        public IActionResult InvoicePreview()
        {
            var model = new InvoiceViewModel
            {
                SubscriberName = "Петя Праматарова",
                Email = "petya@example.com",
                ParkingLocation = "София - Център",
                InvoiceNumber = "0000000942",
                BillingDate = DateTime.Now,
                TotalPrice = 150.00m
            };

            return View(model);
        }
    }
}
