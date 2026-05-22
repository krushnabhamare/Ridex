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
    [Authorize(Roles = "Driver")]
    public class DriverController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<RideHub> _hubContext;

        public DriverController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IHubContext<RideHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        private async Task<Driver?> GetCurrentDriverAsync() //helper method to get current driver
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return null;

            return await _context.Drivers.FirstOrDefaultAsync(x => x.UserId == user.Id);
        }

        private bool IsDriverRestricted(Driver? driver) // for checking if driver is restricted due to pending approval or admin block
        {
            return driver == null || !driver.IsApproved || driver.IsBlockedByAdmin;
        }

        private async Task NotifyRideStatusChanged( int rideId , string status) // helper method to notify clients about ride status changes
        {
            await _hubContext.Clients.All.SendAsync("RideStatusUpdated",new { RideId = rideId ,Status = status } );
        }
        public async Task<IActionResult> Dashboard()
        {
            await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync(); // Use helper method to get current driver

            if (driver == null)
                return RedirectToAction("Profile");

            if (IsDriverRestricted(driver))
                return RedirectToAction("PendingApproval");

            var today = DateTime.Today;

            //online /offline status
            ViewBag.IsDriverOnline = driver?.IsAvailable ?? false;

            ViewBag.TotalEarnings = await _context.Payments
                .Where(x => x.Ride.DriverId == driver.Id && x.PaymentStatus == "Paid")
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            ViewBag.TodayEarnings = await _context.Payments
                .Where(x => x.Ride.DriverId == driver.Id &&
                            x.PaymentStatus == "Paid" &&
                            x.PaymentDate.Date == today)
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            ViewBag.CompletedRides = await _context.Rides
                .CountAsync(x => x.DriverId == driver.Id && (x.Status == "Completed" || x.Status == "Paid"));

            ViewBag.ActiveRides = await _context.Rides
                .CountAsync(x => x.DriverId == driver.Id && (x.Status == "Accepted" || x.Status == "Started"));

            var rides = await _context.Rides
             .Include(x => x.Rider)
             .Where(x => x.DriverId == driver.Id)
             .OrderByDescending(x => x.CreatedAt)
             .ToListAsync();

            return View(rides);
        }

        
        public async Task<IActionResult> RideRequests()
        {
            await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();

            if (IsDriverRestricted(driver))
                return RedirectToAction("PendingApproval");

            ViewBag.IsDriverOnline = driver?.IsAvailable ?? false;

            if (driver == null || !driver.IsAvailable)
            {
                TempData["Error"] = "You must be online to receive ride requests.";
                return View(new List<Ride>());
            }

            var rides = await _context.Rides
                 .Include(x => x.Rider)
                 .Where(x => x.Status == "Pending" && x.DriverId == null)
                 .OrderByDescending(x => x.CreatedAt)
                 .ToListAsync();

            return View(rides);
        }

        public async Task<IActionResult> AcceptRide(int id)
        {
            await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();

            if (IsDriverRestricted(driver))
                return RedirectToAction("PendingApproval");

            if (driver == null)
            {
                TempData["Error"] = "Driver profile not found.";
                return RedirectToAction("RideRequests");
            }

            var ride = await _context.Rides
                .FirstOrDefaultAsync(x => x.Id == id);

            if (ride == null)
            {
                TempData["Error"] = "Ride not found.";
                return RedirectToAction("RideRequests");
            }

            if (ride.Status != "Pending" || ride.DriverId != null)
            {
                TempData["Error"] = "Ride already accepted by another driver.";
                return RedirectToAction("RideRequests");
            }

            ride.DriverId = driver.Id;
            ride.Status = "Accepted";

            await _context.SaveChangesAsync();
            await NotifyRideStatusChanged(ride.Id,ride.Status);

            return RedirectToAction("MyAcceptedRides");
        }

        public async Task<IActionResult> MyAcceptedRides()
        {
            await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();

            ViewBag.IsDriverOnline = driver?.IsAvailable ?? false;

            if (driver == null)
                return RedirectToAction("Profile");
            if (IsDriverRestricted(driver))
                return RedirectToAction("PendingApproval");

            var rides = await _context.Rides
                .Include(x => x.Rider)
                .Where(x => x.DriverId == driver.Id &&
                       (x.Status == "Accepted" || x.Status == "Started" || x.Status == "Completed"))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(rides);
        }
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> StartRide(int id, string otp)
        //{
        //    var driver = await GetCurrentDriverAsync();
        //    if (driver == null)
        //        return RedirectToAction("Profile");

        //    var ride = await _context.Rides.FirstOrDefaultAsync(x => x.Id == id && x.DriverId == driver.Id);

        //    if (ride == null || ride.Status != "Accepted")
        //        return RedirectToAction("MyAcceptedRides");

        //    if (ride.OTP != otp)
        //    {
        //        TempData["Error"] = "Invalid OTP.";
        //        return RedirectToAction("MyAcceptedRides");
        //    }

        //    ride.Status = "Started";
        //    ride.StartTime = DateTime.Now;

        //    await _context.SaveChangesAsync();

        //    await NotifyRideStatusChanged( ride.Id , ride.Status);

        //    TempData["Success"] = "Ride started successfully.";
        //    return RedirectToAction("MyAcceptedRides");
        //}

        public async Task<IActionResult> CompleteRide(int id)
        {
            await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return RedirectToAction("Profile");

            var ride = await _context.Rides.FirstOrDefaultAsync(x => x.Id == id && x.DriverId == driver.Id);

            if (ride == null || ride.Status != "Started")
                return RedirectToAction("MyAcceptedRides");

            ride.Status = "Completed";
            ride.EndTime = DateTime.Now;

            await _context.SaveChangesAsync();
            await _hubContext.Clients.User(ride.RiderId.ToString()).SendAsync("RideCompleted", ride.Id);

            await NotifyRideStatusChanged(ride.Id, ride.Status);

            await _hubContext.Clients.All.SendAsync("RideCompleted", new
            {
                RideId = ride.Id,
                Fare = ride.Fare
            });
            return RedirectToAction("MyAcceptedRides");
        }

        public async Task<IActionResult> Profile()
        {
            await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();
            var user = await _userManager.GetUserAsync(User);

            if (driver == null)
            {
                driver = new Driver
                {
                    UserId = user.Id,
                    LicenseNumber = "",
                    IsApproved = false,
                    IsAvailable = false,
                    IsProfileSubmitted = false
                };

                _context.Drivers.Add(driver);
                await _context.SaveChangesAsync();
            }
            ViewBag.IsDriverOnline = driver?.IsAvailable ?? false;
            ViewBag.DriverInfo = driver;
            ViewBag.TotalRides = await _context.Rides.CountAsync(x => x.DriverId == driver.Id && x.Status == "Paid");
            ViewBag.TotalEarnings = await _context.Payments
                .Where(x => x.Ride.DriverId == driver.Id && x.PaymentStatus == "Paid")
                .SumAsync(x => (decimal?)x.Amount) ?? 0;

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
      ApplicationUser model,
      string LicenseNumber,
      IFormFile? ProfileImage)
        {
            await LoadDriverLayoutData();
            var user = await _userManager.GetUserAsync(User);

            var driver = await GetCurrentDriverAsync();

            if (user == null)
                return RedirectToAction("Profile");

            user.FullName = model.FullName;
            user.PhoneNumber = model.PhoneNumber;

            // Profile photo upload
            if (ProfileImage != null && ProfileImage.Length > 0)
            {
                var uploadsFolder = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot/uploads/profiles");

                Directory.CreateDirectory(uploadsFolder);

                var fileName = Guid.NewGuid().ToString() +
                               Path.GetExtension(ProfileImage.FileName);

                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfileImage.CopyToAsync(stream);
                }

                user.ProfilePhoto = "/uploads/profiles/" + fileName;
            }

            // Driver license update
            if (driver != null)
            {
                driver.LicenseNumber = LicenseNumber;

                if (!string.IsNullOrWhiteSpace(LicenseNumber))
                {
                    driver.IsProfileSubmitted = true;
                }
            }

            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully.";

            return RedirectToAction("Profile");
        }
        [HttpPost]
        public async Task<IActionResult> UpdateLiveLocation(double latitude, double longitude)
        {
            await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return Json(new { success = false });

            driver.CurrentLatitude = latitude;
            driver.CurrentLongitude = longitude;
            driver.LastSeenAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var activeRide = await _context.Rides
                .FirstOrDefaultAsync(x => x.DriverId == driver.Id &&
                                          (x.Status == "Accepted" || x.Status == "Started"));

            if (activeRide != null)
            {
                await _hubContext.Clients.All.SendAsync("DriverLocationUpdated", new
                {
                    RideId = activeRide.Id,
                    Latitude = latitude,
                    Longitude = longitude
                });
            }

            return Json(new { success = true });
        }

        public async Task<IActionResult> NavigateRide(int id)
        {
            await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();
            
            ViewBag.IsDriverOnline = driver?.IsAvailable ?? false;
            if (driver == null)
                return RedirectToAction("Profile");
            if (IsDriverRestricted(driver))
                return RedirectToAction("PendingApproval");

            var ride = await _context.Rides
                .Include(x => x.Driver)
                .Include(x => x.Rider)
                .FirstOrDefaultAsync(x => x.Id == id && x.DriverId == driver.Id);

            if (ride == null)
                return RedirectToAction("MyAcceptedRides");
          
            return View(ride);
        }

        public async Task<IActionResult> RideRequestsMap()
        {
            await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();

            if (IsDriverRestricted(driver))
                return RedirectToAction("PendingApproval");
            ViewBag.IsDriverOnline = driver?.IsAvailable ?? false;

            if (driver == null || !driver.IsAvailable)
            {
                TempData["Error"] = "You must be online to receive ride requests.";
                return View(new List<Ride>());
            }

            var rides = await _context.Rides
                .Include(x => x.Rider)
                .Where(x => x.Status == "Pending" && x.DriverId == null)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(rides);
        }
        public async Task<IActionResult> Earnings()
        {
            await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();
            ViewBag.IsDriverOnline = driver?.IsAvailable ?? false;
            if (driver == null)
                return RedirectToAction("Profile");
            if (IsDriverRestricted(driver))
                return RedirectToAction("PendingApproval");

            var payments = await _context.Payments
                .Include(x => x.Ride)
                .Where(x => x.Ride.DriverId == driver.Id && x.PaymentStatus == "Paid")
                .OrderByDescending(x => x.PaymentDate)
                .ToListAsync();

            return View(payments);
        }

        public async Task<IActionResult> RideHistory()
        {
                await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();
            
            ViewBag.IsDriverOnline = driver?.IsAvailable ?? false;
            if (driver == null)
                return RedirectToAction("Profile");

            if (IsDriverRestricted(driver))
                return RedirectToAction("PendingApproval");

            var rides = await _context.Rides
                 .Include(x => x.Rider)
                .Where(x => x.DriverId == driver.Id &&
                       (x.Status == "Completed" || x.Status == "Paid"))
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();

            return View(rides);
        }

        public async Task<IActionResult> Notifications()
        {
                await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return RedirectToAction("Profile");
            if (IsDriverRestricted(driver))
                return RedirectToAction("PendingApproval");

            ViewBag.IsDriverOnline = driver?.IsAvailable ?? false;

            return View(new List<string>());
        }


        public async Task<IActionResult> Settings()
        {
            await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return RedirectToAction("Profile");
            //if (driver == null || !driver.IsApproved || driver.IsBlockedByAdmin)
            //    return RedirectToAction("PendingApproval");
            ViewBag.IsDriverOnline = driver?.IsAvailable ?? false;

            return View();
        }

        [HttpPost] //online /offline
        public async Task<IActionResult> ToggleAvailability(bool isOnline)
        {
            await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();
            if (driver == null)
                return Json(new { success = false });

            driver.IsAvailable = isOnline;

            if (!isOnline)
            {
                driver.CurrentLatitude = null;
                driver.CurrentLongitude = null;
                driver.LastSeenAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isOnline = driver.IsAvailable
            });
        }

        public IActionResult PendingApproval()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmCashPayment(int rideId)
        {
            await LoadDriverLayoutData();
            var payment = await _context.Payments
                .Include(x => x.Ride)
                .FirstOrDefaultAsync(x => x.RideId == rideId && x.PaymentMethod == "Cash");

            if (payment == null)
                return RedirectToAction("MyAcceptedRides");

            payment.PaymentStatus = "Paid";
            payment.Ride.Status = "Paid";

            await _context.SaveChangesAsync();

            TempData["Success"] = "Cash payment confirmed successfully.";
            return RedirectToAction("MyAcceptedRides");
        }

        [HttpPost]//otp verification before starting ride
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtpAndStartRide(int rideId, string enteredOtp)
        {
                await LoadDriverLayoutData();
            var ride = await _context.Rides.FirstOrDefaultAsync(x => x.Id == rideId);

            if (ride == null)
                return RedirectToAction("MyAcceptedRides");

            if (ride.OTP != enteredOtp)
            {
                TempData["Error"] = "Invalid OTP. Please verify with rider.";
                return RedirectToAction("MyAcceptedRides");
            }

            ride.Status = "Started";
            ride.StartTime = DateTime.Now;

            await _context.SaveChangesAsync();

            await NotifyRideStatusChanged(ride.Id, ride.Status);

            TempData["Success"] = "Ride started successfully.";
            return RedirectToAction("MyAcceptedRides");
        }

        // Ride cancel after ride accept and before otp enter by driver or ride started 
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelAcceptedRide(int rideId)
        {
                await LoadDriverLayoutData();
            var driver = await GetCurrentDriverAsync();

            var ride = await _context.Rides
                .FirstOrDefaultAsync(x =>
                    x.Id == rideId &&
                    x.DriverId == driver.Id);

            if (ride == null)
                return RedirectToAction("MyAcceptedRides");

            if (ride.Status != "Accepted")
            {
                TempData["Error"] = "Ride already started.";
                return RedirectToAction("MyAcceptedRides");
            }

            ride.Status = "Pending";
            ride.DriverId = null;

            await _context.SaveChangesAsync();

            await NotifyRideStatusChanged(ride.Id, ride.Status);

            await _hubContext.Clients.All.SendAsync("ReceiveRideRequest");

            TempData["Success"] = "Ride cancelled successfully.";

            return RedirectToAction("MyAcceptedRides");
        }

        private async Task LoadDriverLayoutData()
        {
            var user = await _userManager.GetUserAsync(User);

            ViewBag.DriverProfilePhoto =
                user?.ProfilePhoto;
        }
    }
}