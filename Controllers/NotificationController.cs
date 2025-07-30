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

     
        // Xóa thông báo
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(Guid id)
        {
            var userId = Guid.Parse(User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier));
            var deleted = await _notificationService.DeleteNotificationAsync(id, userId);
            if (!deleted)
                return NotFound();
            return NoContent();
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