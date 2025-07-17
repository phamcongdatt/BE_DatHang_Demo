using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace QuanLyDatHang.Hubs
{
    public class NotificationHub : Hub
    {
        // Gửi thông báo cho 1 user cụ thể
        public async Task SendNotification(string userId, string message)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message);
        }
    }
} 