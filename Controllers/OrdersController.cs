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
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;
using QuanLyDatHang.Hubs;
using QuanLyDatHang.Services;

namespace QuanLyDatHang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<OrderHub> _orderHub;
        private readonly NotificationService _notificationService;
        public OrdersController(ApplicationDbContext context, IHubContext<OrderHub> orderHub, NotificationService notificationService)
        {
            _context = context;
            _orderHub = orderHub;
            _notificationService = notificationService;
        }

        // Đặt hàng mới
        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Kiểm tra store tồn tại
            var store = await _context.Stores.FindAsync(dto.StoreId);
            if (store == null)
                return BadRequest("Store không tồn tại!");

            // Kiểm tra các món hợp lệ
            var menuIds = dto.Items.Select(i => i.MenuId).ToList();
            var menus = await _context.Menus.Where(m => menuIds.Contains(m.Id) && m.StoreId == dto.StoreId).ToListAsync();
            if (menus.Count != dto.Items.Count)
                return BadRequest("Một hoặc nhiều món không hợp lệ!");

            // Tính tổng tiền
            decimal total = 0;
            var orderDetails = new List<OrderDetail>();
            foreach (var item in dto.Items)
            {
                var menu = menus.First(m => m.Id == item.MenuId);
                total += menu.Price * item.Quantity;
                orderDetails.Add(new OrderDetail
                {
                    Id = Guid.NewGuid(),
                    MenuId = menu.Id,
                    Quantity = item.Quantity,
                    Price = menu.Price,
                    Note = item.Note
                });
            }

            var order = new Order
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                StoreId = dto.StoreId,
                TotalPrice = total,
                DeliveryAddress = dto.DeliveryAddress,
                DeliveryLatitude = dto.DeliveryLatitude,
                DeliveryLongitude = dto.DeliveryLongitude,
                Status = OrderStatus.Pending,
                PaymentMethod = Enum.TryParse<PaymentMethod>(dto.PaymentMethod, true, out var pm) ? pm : PaymentMethod.COD,
                PaymentStatus = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                OrderDetails = orderDetails
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Gửi thông báo  tới chủ quán
            await _orderHub.Clients.User(store.SellerId.ToString()).SendAsync("ReceiveNewOrder", new
            {
                order.Id,
                order.TotalPrice,
                order.Status,
                order.PaymentMethod,
                order.PaymentStatus,
                order.CreatedAt,
                Items = order.OrderDetails.Select(od => new {
                    od.MenuId,
                    od.Quantity,
                    od.Price,
                    od.Note
                })
            });
            // Gửi notification tới chủ quán
            await _notificationService.SendNotificationAsync(
                store.SellerId,
                "Đơn hàng mới",
                $"Bạn có đơn hàng mới #{order.Id}.",
                "order",
                null
            );

            return Ok(new
            {
                order.Id,
                order.TotalPrice,
                order.Status,
                order.PaymentMethod,
                order.PaymentStatus,
                order.CreatedAt,
                Items = order.OrderDetails.Select(od => new {
                    od.MenuId,
                    od.Quantity,
                    od.Price,
                    od.Note
                })
            });
        }

        // Lấy danh sách đơn hàng của khách hàng hiện tại
        [HttpGet("myorders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var orders = await _context.Orders
                .Where(o => o.CustomerId == customerId)
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new {
                    o.Id,
                    o.TotalPrice,
                    o.Status,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    o.CreatedAt,
                    Items = o.OrderDetails.Select(od => new {
                        od.MenuId,
                        od.Quantity,
                        od.Price,
                        od.Note
                    })
                })
                .ToListAsync();
            return Ok(orders);
        }

        // Chủ quán xem danh sách đơn hàng của quán mình
        [HttpGet("store/{storeId}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> GetOrdersByStore(Guid storeId)
        {
            var sellerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (sellerIdClaim == null) return Unauthorized();
            var sellerId = Guid.Parse(sellerIdClaim);
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == storeId && s.SellerId == sellerId);
            if (store == null) return Forbid();
            var orders = await _context.Orders
                .Where(o => o.StoreId == storeId)
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new {
                    o.Id,
                    o.TotalPrice,
                    o.Status,
                    o.PaymentMethod,
                    o.PaymentStatus,
                    o.CreatedAt,
                    Items = o.OrderDetails.Select(od => new {
                        od.MenuId,
                        od.Quantity,
                        od.Price,
                        od.Note
                    })
                })
                .ToListAsync();
            return Ok(orders);
        }

        // Chủ quán cập nhật trạng thái đơn hàng
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateOrderStatus(Guid id, [FromBody] string status)
        {
            var sellerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (sellerIdClaim == null) return Unauthorized();
            var sellerId = Guid.Parse(sellerIdClaim);
            var order = await _context.Orders.Include(o => o.Store).FirstOrDefaultAsync(o => o.Id == id);
            if (order == null || order.Store.SellerId != sellerId)
                return Forbid();
            if (!Enum.TryParse<OrderStatus>(status, true, out var newStatus))
                return BadRequest("Trạng thái không hợp lệ!");
            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Gửi thông báo  tới khách hàng
            await _orderHub.Clients.User(order.CustomerId.ToString()).SendAsync("ReceiveOrderStatus", new
            {
                order.Id,
                order.Status,
                order.UpdatedAt
            });
            // Gửi notification tới khách hàng
            await _notificationService.SendNotificationAsync(
                order.CustomerId,
                "Cập nhật đơn hàng",
                $"Đơn hàng #{order.Id} đã chuyển sang trạng thái: {order.Status}",
                "order",
                null
            );

            return Ok(new { order.Id, order.Status, order.UpdatedAt });
        }

        // Khách hàng hủy đơn hàng
        [HttpPut("{id}/cancel")]
        [Authorize]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            var customerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var order = await _context.Orders
                .Include(o => o.Store)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == customerId);

            if (order == null)
                return NotFound("Đơn hàng không tồn tại!");

            // Kiểm tra trạng thái đơn hàng có thể hủy không
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
                return BadRequest("Chỉ có thể hủy đơn hàng ở trạng thái Chờ xác nhận hoặc Đã xác nhận!");

            // Cập nhật trạng thái
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Gửi thông báo SignalR tới chủ quán
            await _orderHub.Clients.User(order.Store.SellerId.ToString()).SendAsync("ReceiveOrderCancelled", new
            {
                order.Id,
                order.Status,
                order.UpdatedAt
            });

            // Gửi notification tới chủ quán
            await _notificationService.SendNotificationAsync(
                order.Store.SellerId,
                "Đơn hàng bị hủy",
                $"Khách hàng đã hủy đơn hàng #{order.Id}",
                "order",
                null
            );

            return Ok(new { 
                order.Id, 
                order.Status, 
                order.UpdatedAt,
                Message = "Đơn hàng đã được hủy thành công!"
            });
        }

        // Chủ quán từ chối đơn hàng
        [HttpPut("{id}/reject")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> RejectOrder(Guid id, [FromBody] string? reason = null)
        {
            var sellerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (sellerIdClaim == null) return Unauthorized();
            var sellerId = Guid.Parse(sellerIdClaim);
            var order = await _context.Orders
                .Include(o => o.Store)
                .FirstOrDefaultAsync(o => o.Id == id && o.Store.SellerId == sellerId);

            if (order == null)
                return NotFound("Đơn hàng không tồn tại!");

            // Kiểm tra trạng thái đơn hàng có thể từ chối không
            if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Confirmed)
                return BadRequest("Chỉ có thể từ chối đơn hàng ở trạng thái Chờ xác nhận hoặc Đã xác nhận!");

            // Cập nhật trạng thái
            order.Status = OrderStatus.Rejected;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Gửi thông báo Realtime tới khách hàng
            await _orderHub.Clients.User(order.CustomerId.ToString()).SendAsync("ReceiveOrderRejected", new
            {
                order.Id,
                order.Status,
                order.UpdatedAt,
                Reason = reason
            });

            // Gửi Thong bao  tới khách hàng
            var notificationMessage = reason != null 
                ? $"Đơn hàng #{order.Id} đã bị từ chối. Lý do: {reason}"
                : $"Đơn hàng #{order.Id} đã bị từ chối bởi quán.";

            await _notificationService.SendNotificationAsync(
                order.CustomerId,
                "Đơn hàng bị từ chối",
                notificationMessage,
                "order",
                null
            );

            return Ok(new { 
                order.Id, 
                order.Status, 
                order.UpdatedAt,
                Reason = reason,
                Message = "Đơn hàng đã được từ chối!"
            });
        }

        [HttpPost("from-cart")]
        public async Task<IActionResult> CreateOrderFromCart([FromBody] CheckoutCartDto dto, [FromServices] IOrderService orderService)
        {
            var customerIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (customerIdClaim == null) return Unauthorized();
            var customerId = Guid.Parse(customerIdClaim);
            var (success, message, order) = await orderService.CreateOrderFromCartAsync(customerId, dto);
            if (!success)
                return BadRequest(new { success = false, message });

            return Ok(new
            {
                success = true,
                message,
                data = new
                {
                    order.Id,
                    order.TotalPrice,
                    order.Status,
                    order.PaymentMethod,
                    order.PaymentStatus,
                    order.CreatedAt,
                    Items = order.OrderDetails.Select(od => new {
                        od.MenuId,
                        od.Quantity,
                        od.Price,
                        od.Note
                    })
                }
            });
        }
    }
} 