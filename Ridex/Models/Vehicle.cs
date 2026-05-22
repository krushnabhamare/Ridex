namespace Ridex.Models
{
    public class Vehicle
    {
        public int Id { get; set; }

        public int DriverId { get; set; }
        public Driver Driver { get; set; }

        public string VehicleType { get; set; }   // Bike / Auto / Car
        public string VehicleNumber { get; set; }
        public string Model { get; set; }
        public string Color { get; set; }
    }
}