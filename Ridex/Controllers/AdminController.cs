using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Ridex.Data;
using Ridex.Hubs;
using Ridex.Models;

namespace Ridex.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly IHubContext<RideHub> _hubContext;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<RideHub> hubContext)
        {
            _context = context;

            _userManager = userManager;

            _hubContext = hubContext;
        }

        
        // Dashboard
        

        public async Task<IActionResult> Dashboard()
        {
            var today = DateTime.Today;

            var totalDailyRides =
                await _context.Rides
                .CountAsync(x =>
                    x.CreatedAt.Date == today);

            var completedRides =
                await _context.Rides
                .CountAsync(x =>
                    x.CreatedAt.Date == today &&
                    (x.Status == "Completed" ||
                     x.Status == "Paid"));

            var cancelledRides =
                await _context.Rides
                .CountAsync(x =>
                    x.CreatedAt.Date == today &&
                    x.Status == "Cancelled");

            var liveRides =
                await _context.Rides
                .CountAsync(x =>
                    x.Status == "Accepted" ||
                    x.Status == "Started");

            var pendingRequests =
                await _context.Rides
                .CountAsync(x =>
                    x.Status == "Pending");

            var dailyRevenue =
                await _context.Rides
                .Where(x =>
                    x.CreatedAt.Date == today &&
                    (x.Status == "Completed" ||
                     x.Status == "Paid"))
                .SumAsync(x => (decimal?)x.Fare) ?? 0;

            var onlineDrivers =
                await _context.Drivers
                .CountAsync(x =>
                    x.IsApproved &&
                    x.IsAvailable &&
                    !x.IsBlockedByAdmin);

            var totalDrivers =
                await _context.Drivers
                .CountAsync();

            var successRate =
                totalDailyRides == 0
                ? 0
                : Math.Round(
                    (decimal)completedRides * 100 /
                    totalDailyRides,
                    1);

            var rides =
                await _context.Rides
                .Include(x => x.Rider)
                .Include(x => x.Driver)
                .OrderByDescending(x => x.CreatedAt)
                .Take(20)
                .ToListAsync();

            ViewBag.TotalDailyRides = totalDailyRides;
            ViewBag.CompletedRides = completedRides;
            ViewBag.CancelledRides = cancelledRides;
            ViewBag.LiveRides = liveRides;
            ViewBag.PendingRequests = pendingRequests;
            ViewBag.DailyRevenue = dailyRevenue;
            ViewBag.OnlineDrivers = onlineDrivers;
            ViewBag.TotalDrivers = totalDrivers;
            ViewBag.SuccessRate = successRate;

            return View(rides);
        }

        
        // Live Operations
        

        public async Task<IActionResult> LiveOperations()
        {
            var liveRides =
                await _context.Rides
                .Include(x => x.Rider)
                .Include(x => x.Driver)
                .Where(x =>
                    x.Status == "Pending" ||
                    x.Status == "Accepted" ||
                    x.Status == "Started")
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(liveRides);
        }

        
        // Users
        

        public async Task<IActionResult> Users()
        {
            var users =
                await _userManager.Users
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return View(users);
        }

        
        // Drivers
        

        public async Task<IActionResult> Drivers()
        {
            var drivers =
                await _context.Drivers
                .Include(x => x.User)
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            return View(drivers);
        }

        
        // Rides
        

        public async Task<IActionResult> Rides()
        {
            var rides =
                await _context.Rides
                .Include(x => x.Rider)
                .Include(x => x.Driver)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(rides);
        }

        
        // Payments
        

        public async Task<IActionResult> Payments()
        {
            var payments =
                await _context.Payments
                .Include(x => x.Ride)
                .ThenInclude(x => x.Rider)
                .OrderByDescending(x => x.PaymentDate)
                .ToListAsync();

            return View(payments);
        }

        
        // Reports
        

        public async Task<IActionResult> Reports()
        {
            var reportData =
                await _context.Payments
                 .Include(x => x.Ride)
                .OrderByDescending(x => x.PaymentDate)
                .Take(20)
                .ToListAsync();

            return View(reportData);
        }

        
        // Analytics
        

        public async Task<IActionResult> Analytics()
        {
            var rides =
                await _context.Rides
                .Include(x => x.Rider)
                .Include(x => x.Driver)
                .OrderByDescending(x => x.CreatedAt)
                .Take(100)
                .ToListAsync();

            return View(rides);
        }

        
        // Approve Driver
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveDriver(int id)
        {
            var driver =
                await _context.Drivers.FindAsync(id);

            if (driver == null)
            {
                return RedirectToAction("Drivers");
            }

            if (!driver.IsProfileSubmitted ||
                string.IsNullOrWhiteSpace(driver.LicenseNumber))
            {
                TempData["Error"] =
                    "Driver profile is incomplete.";

                return RedirectToAction("Drivers");
            }

            driver.IsApproved = true;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Driver approved successfully.";

            return RedirectToAction("Drivers");
        }

        
        // Disable Driver
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableDriver(int id)
        {
            var driver =
                await _context.Drivers.FindAsync(id);

            if (driver == null)
            {
                return RedirectToAction("Drivers");
            }

            driver.IsBlockedByAdmin = true;

            driver.IsAvailable = false;

            await _context.SaveChangesAsync();
            TempData["Success"] ="Driver disabled successfully.";

            return RedirectToAction("Drivers");
        }

        
        // Enable Driver
        
                [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableDriver(int id)
        {
            var driver =
                await _context.Drivers.FindAsync(id);

            if (driver == null)
            {
                return RedirectToAction("Drivers");
            }

            driver.IsBlockedByAdmin = false;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Driver enabled successfully.";

            return RedirectToAction("Drivers");
        }

        
        // Emergency Notifications
        

        public async Task<IActionResult> Notifications()
        {
            var alerts =
                await _context.EmergencyAlerts
                .Include(x => x.Ride)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(alerts);
        }

        
        // Mark Alert Read
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAlertRead(int id)
        {
            var alert =
                await _context.EmergencyAlerts
                .FindAsync(id);

            if (alert != null)
            {
                alert.IsRead = true;

                await _context.SaveChangesAsync();
                await _hubContext.Clients.User(alert.RiderId.ToString())
                .SendAsync("ReceiveAdminResponse","Your request has been received. We will contact you as soon as possible.");
                //await _hubContext.Clients.All.SendAsync(
                //    "EmergencyAcknowledged",
                //    new
                //    {
                //        RideId = alert.RideId,

                //        Message =
                //        "We received your report. We will reach you as soon as possible."
                //    });
            }

            return RedirectToAction("Notifications");
        }

        
        // Clear Alert
        

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearAlert(int id)
        {
            var alert =
                await _context.EmergencyAlerts
                .FindAsync(id);

            if (alert != null)
            {
                _context.EmergencyAlerts.Remove(alert);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Notifications");
        }
    }
}