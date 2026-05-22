using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Ridex.Data;
using Ridex.Hubs;

namespace Ridex.Services
{
    public class RideAutoCancelService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<RideHub> _hubContext;

        public RideAutoCancelService(
            IServiceScopeFactory scopeFactory,
            IHubContext<RideHub> hubContext)
        {
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var expiryTime = DateTime.Now.AddMinutes(-5);
                //var expiryTime = DateTime.Now.AddSeconds(20)
                var expiredRides = await db.Rides
                    .Where(x => x.Status == "Pending"
                             && x.DriverId == null
                             && x.CreatedAt <= expiryTime)
                    .ToListAsync(stoppingToken);

                if (expiredRides.Any())
                {
                    foreach (var ride in expiredRides)
                    {
                        ride.Status = "Cancelled";
                        ride.CancelReason = "No driver accepted within 30 minutes";
                    }

                    await db.SaveChangesAsync(stoppingToken);

                    await _hubContext.Clients.All.SendAsync("RideCancelled", stoppingToken);
                }

                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }
}