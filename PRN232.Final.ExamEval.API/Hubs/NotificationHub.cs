using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace PRN232.Final.ExamEval.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        // Gửi thông báo đến tất cả user
        public async Task BroadcastNotification(string message)
        {
            await Clients.All.SendAsync("ReceiveNotification", message);
        }

        // Gửi thông báo riêng cho 1 user
        public async Task SendToUser(string userId, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message);
        }

        // Tự động log khi user kết nối
        public override async Task OnConnectedAsync()
        {
            var user = Context.User?.Identity?.Name ?? "Unknown";
            Console.WriteLine($"🔌 User connected: {user}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var user = Context.User?.Identity?.Name ?? "Unknown";
            Console.WriteLine($"❌ User disconnected: {user}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
