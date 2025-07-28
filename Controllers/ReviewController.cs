using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatHang.Data;
using QuanLyDatHang.DTOs;
using QuanLyDatHang.Models;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using QuanLyDatHang.Services;

namespace QuanLyDatHang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;
        private readonly NotificationService _notificationService;
        private readonly ApplicationDbContext _context;

        public ReviewController(IReviewService reviewService, NotificationService notificationService, ApplicationDbContext context)
        {
            _reviewService = reviewService;
            _notificationService = notificationService;
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        [HttpPost("CreateReview")]
        public async Task<IActionResult> CreateReview([FromBody] ReviewCreateDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var review = await _reviewService.CreateReviewAsync(dto, userId);

                await _notificationService.SendNotificationAsync(
                    review.StoreId,
                    "Đánh giá mới cho quán của bạn",
                    $"{review.CustomerName} vừa đánh giá {review.Rating} sao cho quán của bạn.",
                    "review",
                    review.Id.ToString()
                );

                return Ok(new { success = true, data = review, message = "Đánh giá đã được tạo thành công" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi tạo đánh giá" });
            }
        }

        [HttpPut("Update/{reviewId}")]
        public async Task<IActionResult> UpdateReview(Guid reviewId, [FromBody] ReviewUpdateDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var review = await _reviewService.UpdateReviewAsync(reviewId, dto, userId);
                return Ok(new { success = true, data = review, message = "Đánh giá đã được cập nhật thành công" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi cập nhật đánh giá" });
            }
        }

        [HttpDelete("Delete/{reviewId}")]
        public async Task<IActionResult> DeleteReview(Guid reviewId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _reviewService.DeleteReviewAsync(reviewId, userId);

                if (result)
                {
                    return Ok(new { success = true, message = "Đánh giá đã được xóa thành công" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Không tìm thấy đánh giá hoặc không có quyền xóa" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi xóa đánh giá" });
            }
        }

        [HttpGet("GetAllById/{reviewId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewById(Guid reviewId)
        {
            try
            {
                var review = await _reviewService.GetReviewByIdAsync(reviewId);
                if (review == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy đánh giá" });
                }
                return Ok(new { success = true, data = review });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi lấy thông tin đánh giá" });
            }
        }

        [HttpGet("store/{storeId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewsByStore(Guid storeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var reviews = await _reviewService.GetReviewsByStoreAsync(storeId, page, pageSize);
                return Ok(new { success = true, data = reviews });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi lấy danh sách đánh giá" });
            }
        }

        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetReviewsByCustomer(Guid customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                if (userId != customerId && !User.IsInRole("Admin"))
                {
                    return Forbid();
                }

                var reviews = await _reviewService.GetReviewsByCustomerAsync(customerId, page, pageSize);
                return Ok(new { success = true, data = reviews });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi lấy danh sách đánh giá" });
            }
        }

        [HttpGet("store/{storeId}/statistics")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewStatistics(Guid storeId)
        {
            try
            {
                var statistics = await _reviewService.GetReviewStatisticsAsync(storeId);
                return Ok(new { success = true, data = statistics });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi lấy thống kê đánh giá" });
            }
        }

        [HttpGet("menu/{menuId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewsByMenu(Guid menuId)
        {
            try
            {
                var reviews = await _reviewService.GetReviewsByMenuAsync(menuId);
                return Ok(new { success = true, data = reviews });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi lấy danh sách đánh giá theo món ăn" });
            }
        }
    }
}