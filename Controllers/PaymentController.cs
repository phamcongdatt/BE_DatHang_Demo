using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatHang.Data;
using QuanLyDatHang.Models;
using QuanLyDatHang.Services;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace QuanLyDatHang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IVnPayService _vnPayService;

        public PaymentController(ApplicationDbContext context, IVnPayService vnPayService)
        {
            _context = context;
            _vnPayService = vnPayService;
        }

        // Tạo URL thanh toán VNPAY
        [HttpPost("create-payment")]
        public async Task<IActionResult> CreateVnPayPayment([FromBody] CreatePaymentRequest request)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var order = await _context.Orders
                .Include(o => o.Store)
                .FirstOrDefaultAsync(o => o.Id == request.OrderId && o.CustomerId == userId);
            if (order == null)
                return BadRequest("Đơn hàng không tồn tại!");
            if (order.PaymentStatus != PaymentStatus.Pending)
                return BadRequest("Đơn hàng đã được thanh toán hoặc không thể thanh toán!");

            var paymentInfo = new PaymentInformationModel
            {
                OrderType = "billpayment",
                Amount = (double)order.TotalPrice,
                OrderDescription = $"Thanh toán đơn hàng {order.Id} - {order.Store.Name}",
                Name = order.Store.Name
            };
            var paymentUrl = _vnPayService.CreatePaymentUrl(paymentInfo, HttpContext);
            return Ok(new { PaymentUrl = paymentUrl });
        }

        // Callback từ VNPAY sau khi thanh toán
        [HttpGet("vnpay-return")]
        [AllowAnonymous]
        public async Task<IActionResult> VnPayReturn()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            if (!response.Success)
                return BadRequest("Chữ ký không hợp lệ hoặc giao dịch thất bại!");

            if (!Guid.TryParse(response.OrderId, out var orderId))
                return BadRequest("Mã đơn hàng không hợp lệ!");
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return BadRequest("Đơn hàng không tồn tại!");

            // Kiểm tra số tiền
            var expectedAmount = (int)(order.TotalPrice * 100);
            if ((int)(response.Amount * 100) != expectedAmount)
                return BadRequest("Số tiền không khớp!");

            // Cập nhật trạng thái thanh toán
            if (response.VnPayResponseCode == "00")
            {
                order.PaymentStatus = PaymentStatus.Completed;
                order.Status = OrderStatus.Confirmed;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Redirect($"/payment-success?orderId={orderId}");
            }
            else
            {
                order.PaymentStatus = PaymentStatus.Failed;
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Redirect($"/payment-failed?orderId={orderId}&error={response.VnPayResponseCode}");
            }
        }

        // Lấy thông tin thanh toán của đơn hàng
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetPaymentInfo(Guid orderId)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var order = await _context.Orders
                .Include(o => o.Store)
                .Where(o => o.Id == orderId && o.CustomerId == userId)
                .Select(o => new
                {
                    o.Id,
                    o.TotalPrice,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    o.Status,
                    o.CreatedAt,
                    StoreName = o.Store.Name
                })
                .FirstOrDefaultAsync();
            if (order == null)
                return NotFound();
            return Ok(order);
        }
    }

    public class CreatePaymentRequest
    {
        public Guid OrderId { get; set; }
    }
} 