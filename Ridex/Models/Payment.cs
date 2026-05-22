using System;
using System.ComponentModel.DataAnnotations;

namespace Ridex.Models
{
    public class Payment
    {
        [Key]
        public int Id { get; set; }

        public int RideId { get; set; }
        public Ride Ride { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; }   // Cash / UPI / Card
        public string PaymentStatus { get; set; }   // Pending / Paid

        public DateTime PaymentDate { get; set; } = DateTime.Now;
    }
}