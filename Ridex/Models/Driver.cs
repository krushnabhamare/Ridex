namespace Ridex.Models
{
    public class Driver
    {
        public int Id { get; set; }

        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        public string LicenseNumber { get; set; }

        public bool IsAvailable { get; set; } = false;
        public bool IsApproved { get; set; } = false;
        //temp 
        public DateTime? LastSeenAt { get; set; }
        public bool IsProfileSubmitted { get; set; } = false;
        public bool IsBlockedByAdmin { get; set; } = false;
        public double? CurrentLatitude { get; set; }
        public double? CurrentLongitude { get; set; }
    }
}