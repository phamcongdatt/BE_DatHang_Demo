using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatHang.Data;
using QuanLyDatHang.Services;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuanLyDatHang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Seller")]
    public class RevenueController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRevenueStatisticsService _revenueService;

        public RevenueController(ApplicationDbContext context, IRevenueStatisticsService revenueService)
        {
            _context = context;
            _revenueService = revenueService;
        }

        // Thống kê doanh thu tổng quan
        [HttpGet("store/{storeId}/overview")]
        public async Task<IActionResult> GetRevenueOverview(Guid storeId, [FromQuery] string period = "month")
        {
            var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId && s.SellerId == sellerId);
            if (store == null) return Forbid();

            var result = await _revenueService.GetRevenueOverviewAsync(storeId, period);
            return Ok(result);
        }

        // Thống kê doanh thu chi tiết theo ngày
        [HttpGet("store/{storeId}/daily")]
        public async Task<IActionResult> GetDailyRevenue(Guid storeId, [FromQuery] string period = "month")
        {
            var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId && s.SellerId == sellerId);
            if (store == null) return Forbid();

            var result = await _revenueService.GetDailyRevenueAsync(storeId, period);
            return Ok(result);
        }

        // Top đơn hàng có giá trị cao nhất
        [HttpGet("store/{storeId}/top-orders")]
        public async Task<IActionResult> GetTopOrders(Guid storeId, [FromQuery] int take = 10, [FromQuery] string period = "month")
        {
            var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId && s.SellerId == sellerId);
            if (store == null) return Forbid();

            var result = await _revenueService.GetTopOrdersAsync(storeId, take, period);
            return Ok(result);
        }

        // Thống kê doanh thu theo danh mục món ăn
        [HttpGet("store/{storeId}/by-category")]
        public async Task<IActionResult> GetRevenueByCategory(Guid storeId, [FromQuery] string period = "month")
        {
            var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId && s.SellerId == sellerId);
            if (store == null) return Forbid();

            var result = await _revenueService.GetRevenueByCategoryAsync(storeId, period);
            return Ok(result);
        }

        // Thống kê doanh thu theo phương thức thanh toán
        [HttpGet("store/{storeId}/by-payment")]
        public async Task<IActionResult> GetRevenueByPaymentMethod(Guid storeId, [FromQuery] string period = "month")
        {
            var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId && s.SellerId == sellerId);
            if (store == null) return Forbid();

            var result = await _revenueService.GetRevenueByPaymentMethodAsync(storeId, period);
            return Ok(result);
        }

        // Báo cáo doanh thu chi tiết
        [HttpGet("store/{storeId}/detailed-report")]
        public async Task<IActionResult> GetDetailedReport(Guid storeId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId && s.SellerId == sellerId);
            if (store == null) return Forbid();

            var result = await _revenueService.GetDetailedReportAsync(storeId, startDate, endDate);
            return Ok(result);
        }
    }
} 