using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyDatHang.Services;
using System;
using System.Threading.Tasks;
using System.Security.Claims;

namespace QuanLyDatHang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly NotificationService _notificationService;
        public NotificationController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }
                
        // Lấy danh sách thông báo của user
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notis = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(notis);
        }

        // Đánh dấu đã đọc
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            await _notificationService.MarkAsReadAsync(id, userId);
            return NoContent();
        }

        // (Optional) Gửi thông báo test (chỉ cho admin)
        [HttpPost("send")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationRequest req)
        {
            await _notificationService.SendNotificationAsync(req.UserId, req.Title, req.Message, req.Type, req.Data);
            return Ok();
        }
    }

    public class SendNotificationRequest
    {
        public Guid UserId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public string? Data { get; set; }
    }
} 