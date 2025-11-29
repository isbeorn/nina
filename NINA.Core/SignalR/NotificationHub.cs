using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace NINA.Core.SignalR {
    /// <summary>
    /// SignalR hub for real-time notification broadcasting
    /// </summary>
    public class NotificationHub : Hub {
        public override async Task OnConnectedAsync() {
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception) {
            await base.OnDisconnectedAsync(exception);
        }
    }
}
