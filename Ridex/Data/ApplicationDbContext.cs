using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Ridex.Models;

namespace Ridex.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Ride> Rides { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<EmergencyAlert> EmergencyAlerts { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Ride>()
                .HasOne(r => r.Rider)
                .WithMany()
                .HasForeignKey(r => r.RiderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Ride>()
                .HasOne(r => r.Driver)
                .WithMany()
                .HasForeignKey(r => r.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Driver>()
                .HasOne(d => d.User)
                .WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Vehicle>()
                .HasOne(v => v.Driver)
                .WithMany()
                .HasForeignKey(v => v.DriverId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Payment>()
                .HasOne(p => p.Ride)
                .WithMany()
                .HasForeignKey(p => p.RideId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Ride>()
                .Property(r => r.Fare)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Payment>()
                .Property(p => p.Amount)
                .HasColumnType("decimal(18,2)");
        }
    }
}