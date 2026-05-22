using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Ridex.Data;
using Ridex.Hubs;
using Ridex.Models;
using Ridex.Services;
using Ridex.ViewModels;

namespace Ridex.Controllers
{
    [Authorize(Roles = "Rider")]
    public class RiderController : Controller
    {
        private readonly EmailService _emailService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<RideHub> _hubContext;

        public RiderController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<RideHub> hubContext,
            EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _emailService = emailService;
        }

        // helper method to get current rider
        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            return await _userManager.GetUserAsync(User);
        }

        // helper method to notify ride status updates
        private async Task NotifyRideStatusChanged(int rideId, string status)
        {
            await _hubContext.Clients.All.SendAsync(
                "RideStatusUpdated",
                new
                {
                    RideId = rideId,
                    Status = status
                });
        }

        public async Task<IActionResult> Dashboard()
        {
            await LoadRiderLayoutData();
            var user = await GetCurrentUserAsync();

            if (user == null)
                return RedirectToAction("Login", "Account");

            var totalRides = await _context.Rides
                .CountAsync(x => x.RiderId == user.Id);

            var activeRide = await _context.Rides
                .Where(x =>
                    x.RiderId == user.Id &&
                    (x.Status == "Pending" ||
                     x.Status == "Accepted" ||
                     x.Status == "Started"))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();

            var totalSpent = await _context.Payments
                .Where(x =>
                    x.Ride.RiderId == user.Id &&
                    x.PaymentStatus == "Paid")
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            var recentRides = await _context.Rides
                .Where(x => x.RiderId == user.Id)
                .OrderByDescending(x => x.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalRides = totalRides;
            ViewBag.ActiveRide = activeRide;
            ViewBag.TotalSpent = totalSpent;

            return View(recentRides);
        }

        [HttpGet]
        public IActionResult BookRide()
        {   
            return View(new BookRideViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookRide(BookRideViewModel model)
        {
            await LoadRiderLayoutData();
            if (!ModelState.IsValid)
                return View(model);

            var user = await GetCurrentUserAsync();

            if (user == null)
                return RedirectToAction("Login", "Account");

            var existingActiveRide = await _context.Rides.AnyAsync(x =>
                x.RiderId == user.Id &&
                (x.Status == "Pending" ||
                 x.Status == "Accepted" ||
                 x.Status == "Started"));

            if (existingActiveRide)
            {
                TempData["Error"] = "You already have an active ride.";

                return RedirectToAction("MyRides");
            }

            decimal fare = model.VehicleType switch
            {
                "Bike" => 50 + ((decimal)model.DistanceInKm * 12),
                "Auto" => 80 + ((decimal)model.DistanceInKm * 15),
                "Car" => 120 + ((decimal)model.DistanceInKm * 20),
                _ => 50
            };

            var ride = new Ride
            {
                RiderId = user.Id,
                PickupLocation = model.PickupLocation,
                DropLocation = model.DropLocation,
                PickupLatitude = model.PickupLatitude,
                PickupLongitude = model.PickupLongitude,
                DropLatitude = model.DropLatitude,
                DropLongitude = model.DropLongitude,
                VehicleType = model.VehicleType,
                DistanceInKm = model.DistanceInKm,
                Fare = fare,
                Status = "Pending",
                OTP = Random.Shared.Next(1000, 9999).ToString()
            };

            _context.Rides.Add(ride);

            await _context.SaveChangesAsync();

            // send OTP mail
            await _emailService.SendOtpEmailAsync(
                user.Email,
                user.FullName,
                ride.OTP);

            // notify drivers
            await _hubContext.Clients.All.SendAsync(
                "ReceiveRideRequest");

            TempData["Success"] = "Ride booked successfully.";

            return RedirectToAction("MyRides");
        }

        public async Task<IActionResult> MyRides()
        {
            await LoadRiderLayoutData();
            var user = await GetCurrentUserAsync();

            if (user == null)
                return RedirectToAction("Login", "Account");

            var rides = await _context.Rides
                .Include(x => x.Driver)
                .Where(x => x.RiderId == user.Id)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(rides);
        }

        public async Task<IActionResult> Profile()
        {
            await LoadRiderLayoutData();
            var user = await GetCurrentUserAsync();

            if (user == null)
                return RedirectToAction("Login", "Account");

            ViewBag.TotalRides = await _context.Rides
                .CountAsync(x => x.RiderId == user.Id);

            ViewBag.TotalSpent = await _context.Payments
                .Where(x =>
                    x.Ride.RiderId == user.Id &&
                    x.PaymentStatus == "Paid")
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            string FullName,
            string PhoneNumber,
            IFormFile? ProfileImage)
        {
            await LoadRiderLayoutData();
            var user = await GetCurrentUserAsync();

            if (user == null)
                return RedirectToAction("Profile");

            user.FullName = FullName;
            user.PhoneNumber = PhoneNumber;

            // Profile photo upload
            if (ProfileImage != null &&
                ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/profiles");

                Directory.CreateDirectory(uploadsFolder);

                var fileName =
                    Guid.NewGuid() +
                    Path.GetExtension(ProfileImage.FileName);

                var filePath =
                    Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(
                    filePath,
                    FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(stream);
                }

                user.ProfilePhoto =
                    "/uploads/profiles/" + fileName;
            }

            await _userManager.UpdateAsync(user);

            TempData["Success"] =
                "Profile updated successfully.";

            return RedirectToAction("Profile");
        }

        public async Task<IActionResult> TrackRide(int id)
        {
            await LoadRiderLayoutData();
            var user = await GetCurrentUserAsync();

            if (user == null)
                return RedirectToAction("Login", "Account");

            var ride = await _context.Rides
                .Include(x => x.Driver)
                .Include(x => x.Rider)
                .FirstOrDefaultAsync(x =>
                    x.Id == id &&
                    x.RiderId == user.Id);

            if (ride == null)
                return NotFound();

            ViewBag.HasReportedEmergency =
                await _context.EmergencyAlerts
                .AnyAsync(x =>
                    x.RideId == id &&
                    x.RiderId == user.Id);

            return View(ride);
        }

        [HttpGet]
        public async Task<IActionResult> GetDriverLiveLocation(
            int rideId)
        {
            await LoadRiderLayoutData();
            var user = await GetCurrentUserAsync();

            if (user == null)
                return Json(new { success = false });

            var ride = await _context.Rides
                .Include(x => x.Driver)
                .FirstOrDefaultAsync(x =>
                    x.Id == rideId &&
                    x.RiderId == user.Id);

            if (ride == null ||
                ride.Driver == null ||
                !ride.Driver.CurrentLatitude.HasValue ||
                !ride.Driver.CurrentLongitude.HasValue)
            {
                return Json(new { success = false });
            }

            return Json(new
            {
                success = true,
                latitude = ride.Driver.CurrentLatitude,
                longitude = ride.Driver.CurrentLongitude
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // for Emergency Alerts
        public async Task<IActionResult> ReportEmergency(
            int rideId)
        {
            await LoadRiderLayoutData();
            var user = await GetCurrentUserAsync();

            if (user == null)
                return RedirectToAction("Login", "Account");

            var ride = await _context.Rides
                .Include(x => x.Rider)
                .FirstOrDefaultAsync(x =>
                    x.Id == rideId &&
                    x.RiderId == user.Id);

            if (ride == null)
                return RedirectToAction("MyRides");

            var alreadyReported =
                await _context.EmergencyAlerts
                .AnyAsync(x =>
                    x.RideId == rideId &&
                    x.RiderId == user.Id);

            if (alreadyReported)
            {
                TempData["Error"] =
                    "Emergency report already submitted.";

                return RedirectToAction(
                    "TrackRide",
                    new { id = rideId });
            }

            if (ride == null ||
                ride.Status != "Started")
            {
                return RedirectToAction("MyRides");
            }

            var alert = new EmergencyAlert
            {
                RideId = ride.Id,
                RiderId = user.Id,
                RiderName = user.FullName,
                RiderPhone = user.PhoneNumber,
                Message = "Emergency assistance requested by rider."
            };

            _context.EmergencyAlerts.Add(alert);

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync(
                "EmergencyAlertReceived");

            TempData["Success"] =
                "Report submitted successfully.";

            return RedirectToAction(
                "TrackRide",
                new { id = rideId });
        }

        [HttpPost] // for ride cancel
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRide(int rideId)
        {
            await LoadRiderLayoutData();
            var user = await GetCurrentUserAsync();

            if (user == null)
                return RedirectToAction("Login", "Account");

            var ride = await _context.Rides
                .FirstOrDefaultAsync(x =>
                    x.Id == rideId &&
                    x.RiderId == user.Id);

            if (ride == null)
                return RedirectToAction("MyRides");

            if (ride.Status != "Accepted")
            {
                TempData["Error"] =
                    "Ride can no longer be cancelled.";

                return RedirectToAction("MyRides");
            }

            ride.Status = "Cancelled";
            ride.DriverId = null;

            await _context.SaveChangesAsync();

            await NotifyRideStatusChanged(
                ride.Id,
                "Cancelled");
            await _hubContext.Clients.All.SendAsync("RideCancelled");
            TempData["Success"] =
                "Ride cancelled successfully.";

            return RedirectToAction("MyRides");
        }
        private async Task LoadRiderLayoutData()
        {
            var user = await _userManager.GetUserAsync(User);

            ViewBag.RiderProfilePhoto =
                user?.ProfilePhoto;
        }
    }
}