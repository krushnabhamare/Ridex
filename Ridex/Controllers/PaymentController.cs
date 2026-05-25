using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Ridex.Data;
using Ridex.Models;

namespace Ridex.Controllers
{
    [Authorize(Roles = "Rider")]
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;

            _userManager = userManager;
        }

        
        // Payment Page
        

        public async Task<IActionResult> Pay(int rideId)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var ride =
                await _context.Rides
                .FirstOrDefaultAsync(x =>
                    x.Id == rideId &&
                    x.RiderId == user.Id);

            if (ride == null ||
                ride.Status != "Completed")
            {
                return RedirectToAction(
                    "MyRides",
                    "Rider");
            }

            return View(ride);
        }

        
        // Complete Payment
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayNow(
            int rideId,
            string paymentMethod)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction(
                    "Login",
                    "Account");
            }

            var ride =
                await _context.Rides
                .FirstOrDefaultAsync(x =>
                    x.Id == rideId &&
                    x.RiderId == user.Id);

            if (ride == null ||
                ride.Status != "Completed")
            {
                return RedirectToAction(
                    "MyRides",
                    "Rider");
            }

            // Validate payment method
            if (paymentMethod != "Cash" &&
                paymentMethod != "UPI" &&
                paymentMethod != "Card")
            {
                TempData["Error"] =
                    "Invalid payment method.";

                return RedirectToAction(
                    "Pay",
                    new { rideId });
            }

            // Prevent duplicate payment
            var alreadyPaid =
                await _context.Payments
                .AnyAsync(x =>
                    x.RideId == rideId &&
                    x.PaymentStatus == "Paid");

            if (alreadyPaid)
            {
                TempData["Success"] =
                    "Payment already completed.";

                return RedirectToAction(
                    "MyRides",
                    "Rider");
            }

            // Create payment
            var payment = new Payment
            {
                RideId = ride.Id,

                Amount = ride.Fare,

                PaymentMethod = paymentMethod,

                PaymentStatus =
                    paymentMethod == "Cash"
                    ? "Pending"
                    : "Paid"
            };

            _context.Payments.Add(payment);

            // Update ride status
            if (paymentMethod == "Cash")
            {
                ride.Status = "Completed";

                TempData["CashAlert"] ="Cash selected. Please pay the driver directly and wait for confirmation.";
            }
            else
            {
                ride.Status = "Paid";

                TempData["Success"] =
                    "Payment completed successfully.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                "MyRides",
                "Rider");
        }
    }
}