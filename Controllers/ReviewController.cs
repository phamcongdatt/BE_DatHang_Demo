using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatHang.Data;
using QuanLyDatHang.DTOs;
using QuanLyDatHang.Models;
using System;
using System.Linq;
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

        public ReviewController(IReviewService reviewService, NotificationService notificationService)
        {
            _reviewService = reviewService;
            _notificationService = notificationService;
        }

        /// <summary>
        /// Tạo đánh giá mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] ReviewCreateDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var review = await _reviewService.CreateReviewAsync(dto, userId);

                // Gửi notification cho chủ quán
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

        /// <summary>
        /// Cập nhật đánh giá
        /// </summary>
        [HttpPut("{reviewId}")]
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

        /// <summary>
        /// Xóa đánh giá
        /// </summary>
        [HttpDelete("{reviewId}")]
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

        /// <summary>
        /// Lấy thông tin đánh giá theo ID
        /// </summary>
        [HttpGet("{reviewId}")]
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

        /// <summary>
        /// Lấy danh sách đánh giá của cửa hàng
        /// </summary>
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

        /// <summary>
        /// Lấy danh sách đánh giá của khách hàng
        /// </summary>
        [HttpGet("customer/{customerId}")]
        public async Task<IActionResult> GetReviewsByCustomer(Guid customerId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                
                // Chỉ cho phép xem đánh giá của chính mình hoặc admin
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

        /// <summary>
        /// Lấy thống kê đánh giá của cửa hàng
        /// </summary>
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

        /// <summary>
        /// Tạo phản hồi cho đánh giá (chủ cửa hàng)
        /// </summary>
        [HttpPost("{reviewId}/response")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CreateReviewResponse(Guid reviewId, [FromBody] ReviewResponseDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var response = await _reviewService.CreateReviewResponseAsync(reviewId, dto, userId);

                return Ok(new { success = true, data = response, message = "Phản hồi đã được tạo thành công" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi tạo phản hồi" });
            }
        }

        /// <summary>
        /// Cập nhật phản hồi cho đánh giá (chủ cửa hàng)
        /// </summary>
        [HttpPut("{reviewId}/response")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateReviewResponse(Guid reviewId, [FromBody] ReviewResponseDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var response = await _reviewService.UpdateReviewResponseAsync(reviewId, dto, userId);

                return Ok(new { success = true, data = response, message = "Phản hồi đã được cập nhật thành công" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi cập nhật phản hồi" });
            }
        }

        /// <summary>
        /// Báo cáo đánh giá
        /// </summary>
        [HttpPost("{reviewId}/report")]
        public async Task<IActionResult> ReportReview(Guid reviewId, [FromBody] ReviewReportDto dto)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _reviewService.ReportReviewAsync(reviewId, dto, userId);

                if (result)
                {
                    return Ok(new { success = true, message = "Báo cáo đã được gửi thành công" });
                }
                else
                {
                    return BadRequest(new { success = false, message = "Không thể gửi báo cáo" });
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi gửi báo cáo" });
            }
        }

        /// <summary>
        /// Ẩn đánh giá (Admin)
        /// </summary>
        [HttpPut("{reviewId}/hide")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> HideReview(Guid reviewId, [FromBody] HideReviewRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _reviewService.HideReviewAsync(reviewId, request.Reason, userId);

                if (result)
                {
                    return Ok(new { success = true, message = "Đánh giá đã được ẩn thành công" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Không tìm thấy đánh giá" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi ẩn đánh giá" });
            }
        }

        /// <summary>
        /// Duyệt đánh giá (Admin)
        /// </summary>
        [HttpPut("{reviewId}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveReview(Guid reviewId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _reviewService.ApproveReviewAsync(reviewId, userId);

                if (result)
                {
                    return Ok(new { success = true, message = "Đánh giá đã được duyệt thành công" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Không tìm thấy đánh giá" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi duyệt đánh giá" });
            }
        }

        /// <summary>
        /// Từ chối đánh giá (Admin)
        /// </summary>
        [HttpPut("{reviewId}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectReview(Guid reviewId, [FromBody] RejectReviewRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _reviewService.RejectReviewAsync(reviewId, request.Reason, userId);

                if (result)
                {
                    return Ok(new { success = true, message = "Đánh giá đã được từ chối thành công" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Không tìm thấy đánh giá" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi từ chối đánh giá" });
            }
        }

        /// <summary>
        /// Lấy danh sách đánh giá chờ duyệt (Admin)
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingReviews([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var reviews = await _reviewService.GetPendingReviewsAsync(page, pageSize);
                return Ok(new { success = true, data = reviews });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi lấy danh sách đánh giá chờ duyệt" });
            }
        }

        /// <summary>
        /// Lấy danh sách báo cáo đánh giá (Admin)
        /// </summary>
        [HttpGet("reports")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetReviewReports([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var reports = await _reviewService.GetReviewReportsAsync(page, pageSize);
                return Ok(new { success = true, data = reports });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi lấy danh sách báo cáo" });
            }
        }

        /// <summary>
        /// Xử lý báo cáo (Admin)
        /// </summary>
        [HttpPut("reports/{reportId}/resolve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResolveReport(Guid reportId, [FromBody] ResolveReportRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var result = await _reviewService.ResolveReportAsync(reportId, request.Status, userId);

                if (result)
                {
                    return Ok(new { success = true, message = "Báo cáo đã được xử lý thành công" });
                }
                else
                {
                    return NotFound(new { success = false, message = "Không tìm thấy báo cáo" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Có lỗi xảy ra khi xử lý báo cáo" });
            }
        }
    }

    public class HideReviewRequest
    {
        public string Reason { get; set; }
    }

    public class RejectReviewRequest
    {
        public string Reason { get; set; }
    }

    public class ResolveReportRequest
    {
        public ReportStatus Status { get; set; }
    }
} 