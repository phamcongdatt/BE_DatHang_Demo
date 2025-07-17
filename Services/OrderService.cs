using Microsoft.EntityFrameworkCore;
using QuanLyDatHang.Data;
using QuanLyDatHang.DTOs;
using QuanLyDatHang.Models;

namespace QuanLyDatHang.Services
{
    public interface IOrderService
    {
        Task<(bool Success, string Message, Order Order)> CreateOrderFromCartAsync(Guid customerId, CheckoutCartDto dto);
    }

    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICartService _cartService;

        public OrderService(ApplicationDbContext context, ICartService cartService)
        {
            _context = context;
            _cartService = cartService;
        }

        public async Task<(bool Success, string Message, Order Order)> CreateOrderFromCartAsync(Guid customerId, CheckoutCartDto dto)
        {
            var cart = await _cartService.GetCartAsync(customerId);
            if (cart == null || cart.Items.Count == 0)
                return (false, "Giỏ hàng trống", null);

            // Lấy storeId từ menu đầu tiên
            var firstMenuId = cart.Items.First().MenuId;
            var storeId = await _context.Menus.Where(m => m.Id == firstMenuId).Select(m => m.StoreId).FirstOrDefaultAsync();
            if (storeId == Guid.Empty)
                return (false, "Không xác định được cửa hàng", null);

            // Kiểm tra tất cả sản phẩm cùng 1 cửa hàng
            var menuIds = cart.Items.Select(i => i.MenuId).ToList();
            var storeIds = await _context.Menus.Where(m => menuIds.Contains(m.Id)).Select(m => m.StoreId).Distinct().ToListAsync();
            if (storeIds.Count != 1)
                return (false, "Chỉ được đặt hàng từ 1 cửa hàng/lần", null);

            // Kiểm tra tồn tại store
            var store = await _context.Stores.FindAsync(storeId);
            if (store == null)
                return (false, "Cửa hàng không tồn tại!", null);

            // Kiểm tra các món hợp lệ
            var menus = await _context.Menus.Where(m => menuIds.Contains(m.Id) && m.StoreId == storeId).ToListAsync();
            if (menus.Count != cart.Items.Count)
                return (false, "Một hoặc nhiều món không hợp lệ!", null);

            // Tính tổng tiền
            decimal total = 0;
            var orderDetails = new List<OrderDetail>();
            foreach (var item in cart.Items)
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
                StoreId = storeId,
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

            // Xóa giỏ hàng
            await _cartService.ClearCartAsync(customerId);

            return (true, "Đặt hàng thành công", order);
        }
    }
} 