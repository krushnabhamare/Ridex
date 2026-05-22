using System.ComponentModel.DataAnnotations;

namespace Ridex.Models
{
    public class EmergencyAlert
    {
        [Key]
        public int Id { get; set; }

        public int RideId { get; set; }
        public Ride Ride { get; set; }

        public string RiderId { get; set; }
        public ApplicationUser Rider { get; set; }

        public string RiderName { get; set; }
        public string RiderPhone { get; set; }

        public string Message { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}