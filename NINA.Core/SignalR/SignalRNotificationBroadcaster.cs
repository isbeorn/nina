using Microsoft.AspNetCore.SignalR;
using NINA.Core.Utility.Notification;
using System;
using System.Threading.Tasks;

namespace NINA.Core.SignalR {
    /// <summary>
    /// Service to broadcast notifications via SignalR
    /// </summary>
    public class SignalRNotificationBroadcaster {
        private readonly IHubContext<NotificationHub> _hubContext;

        public SignalRNotificationBroadcaster(IHubContext<NotificationHub> hubContext) {
            _hubContext = hubContext;
        }

        public async Task BroadcastNotificationAsync(NotificationMessage message) {
            try {
                if (message != null && _hubContext != null) {
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", message);
                }
            } catch (Exception ex) {
                Utility.Logger.Error($"Failed to broadcast notification via SignalR: {ex.Message}");
            }
        }
    }
}
