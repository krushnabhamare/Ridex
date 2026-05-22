using System;

namespace Ridex.Models
{
    public class Ride
    {
        public int Id { get; set; }

        public string RiderId { get; set; }
        public ApplicationUser Rider { get; set; }

        public int? DriverId { get; set; }
        public Driver Driver { get; set; }

        public string PickupLocation { get; set; }
        public string DropLocation { get; set; }

        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }

        public double DropLatitude { get; set; }
        public double DropLongitude { get; set; }

        public string VehicleType { get; set; }

        public double DistanceInKm { get; set; }
        public decimal Fare { get; set; }
        public string Status { get; set; }
        public string? CancelReason { get; set; }
        public string OTP { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }
}