using System; // Cung cấp kiểu Guid, DateTime...
using System.Threading.Tasks; // Cho phép sử dụng async/await
using System.Collections.Generic; // Dùng cho List<>
using Microsoft.AspNetCore.SignalR; // Dùng để tương tác với Hub của SignalR
using Microsoft.EntityFrameworkCore; // Dùng để truy vấn Entity Framework
using QuanLyDatHang.Data; // Chứa ApplicationDbContext để truy cập DB
using QuanLyDatHang.Models; // Chứa entity Notification
using QuanLyDatHang.Hubs; // Chứa NotificationHub dùng để gửi realtime

namespace QuanLyDatHang.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context; // Truy cập DB
        private readonly IHubContext<NotificationHub> _hubContext; // Dùng để gửi realtime qua SignalR

        // Constructor nhận DI context và hub context
        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // Gửi thông báo tới 1 người dùng cụ thể
        public async Task SendNotificationAsync(Guid userId, string title, string message, string type = "system", string data = null)
        {
            // Tạo thông báo mới
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                Data = data ?? string.Empty, // Đảm bảo không null
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.Notifications.Add(notification); // Thêm vào DB
            await _context.SaveChangesAsync(); // Lưu DB

            // Gửi realtime tới client bằng SignalR
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                notification.Type,
                notification.CreatedAt,
                notification.IsRead,
                notification.Data
            });
        }

        // Đánh dấu một thông báo là đã đọc
        public async Task MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var noti = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (noti != null && !noti.IsRead)
            {
                noti.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        // Lấy danh sách thông báo của người dùng (mặc định lấy 20 mới nhất)
        public async Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int take = 20)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .ToListAsync();
        }

        // Xóa thông báo
        public async Task<bool> DeleteNotificationAsync(Guid notificationId, Guid userId)
        {
            var noti = await _context.Notifications.FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
            if (noti == null)
                return false;
            _context.Notifications.Remove(noti);
            await _context.SaveChangesAsync();
            return true;
        }

        // Gửi thông báo đơn hàng đến người bán bằng SignalR
        public async Task SendOrderCreatedToSellerAsync(string sellerId, string orderId)
        {
            await _hubContext.Clients.User(sellerId).SendAsync("ReceiveNotification", $"Bạn có đơn hàng mới: {orderId}");
        }
    }
}
