using System.ComponentModel.DataAnnotations;

namespace Ridex.ViewModels
{
    public class BookRideViewModel
    {
        [Required]
        public string PickupLocation { get; set; }

        [Required]
        public string DropLocation { get; set; }

        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }

        public double DropLatitude { get; set; }
        public double DropLongitude { get; set; }

        [Required]
        public string VehicleType { get; set; }
        public double DistanceInKm { get; set; }

        public decimal EstimatedFare { get; set; }
    }
}