using Microsoft.AspNetCore.SignalR;

namespace Ridex.Hubs
{
    public class RideHub : Hub
    {
        // =============================
        // Connection Events
        // =============================

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine(
                $"SignalR Connected: {Context.ConnectionId}");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine(
                $"SignalR Disconnected: {Context.ConnectionId}");

            await base.OnDisconnectedAsync(exception);
        }

        // =============================
        // Ride Status Updates
        // =============================

        public async Task NotifyRideStatusUpdated()
        {
            await Clients.All.SendAsync("RideStatusUpdated");
        }

        // =============================
        // New Ride Request
        // =============================

        public async Task NotifyNewRideRequest()
        {
            await Clients.All.SendAsync("ReceiveRideRequest");
        }

        // =============================
        // Emergency Alerts
        // =============================

        public async Task SendEmergencyAlert(string message)
        {
            await Clients.All.SendAsync(
                "ReceiveEmergencyAlert",
                new
                {
                    message = message
                });
        }

        // =============================
        // Emergency Acknowledgement
        // =============================

        public async Task EmergencyAcknowledged(string message)
        {
            await Clients.All.SendAsync(
                "EmergencyAcknowledged",
                new
                {
                    message = message
                });
        }

        // =============================
        // Payment Updates
        // =============================

        public async Task NotifyPaymentCompleted()
        {
            await Clients.All.SendAsync("PaymentCompleted");
        }
    }
}