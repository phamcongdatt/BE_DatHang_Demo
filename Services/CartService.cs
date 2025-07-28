using Microsoft.EntityFrameworkCore;
using QuanLyDatHang.Data;
using QuanLyDatHang.DTOs;
using QuanLyDatHang.Models;
using System.Security.Claims;

namespace QuanLyDatHang.Services
{
    public interface ICartService
    {
        Task<CartDto> GetCartAsync(Guid customerId);
        Task<CartItemDto> AddToCartAsync(Guid customerId, AddToCartDto dto);
        Task<CartItemDto> UpdateCartItemAsync(Guid customerId, Guid itemId, UpdateCartItemDto dto);
        Task<bool> RemoveFromCartAsync(Guid customerId, Guid itemId);
        Task<bool> ClearCartAsync(Guid customerId);
        Task<CartDto> GetCartSummaryAsync(Guid customerId);
    }

    public interface IWishlistService
    {
        Task<WishlistDto> GetWishlistAsync(Guid customerId);
        Task<WishlistItemDto> AddToWishlistAsync(Guid customerId, AddToWishlistDto dto);
        Task<bool> RemoveFromWishlistAsync(Guid customerId, Guid menuId);
        Task<bool> IsInWishlistAsync(Guid customerId, Guid menuId);
    }

    public class CartService : ICartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<CartDto> GetCartAsync(Guid customerId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Menu)
                .ThenInclude(m => m.Store)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
            {
                // Tạo cart mới nếu chưa có
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();  
            }

            return MapToCartDto(cart);
        }

        public async Task<CartItemDto> AddToCartAsync(Guid customerId, AddToCartDto dto)
        {
            // Kiểm tra menu tồn tại
            var menu = await _context.Menus
                .Include(m => m.Store)
                .FirstOrDefaultAsync(m => m.Id == dto.MenuId && m.Status == MenuStatus.Available);

            if (menu == null)
                throw new InvalidOperationException("Món ăn không tồn tại hoặc không khả dụng");

            // Lấy hoặc tạo cart
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
            {
                cart = new Cart
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
            }

            // Kiểm tra item đã có trong cart chưa
            var existingItem = cart.CartItems.FirstOrDefault(ci => ci.MenuId == dto.MenuId);

            if (existingItem != null)
            {
                // Cập nhật số lượng
                existingItem.Quantity += dto.Quantity;
                existingItem.Note = dto.Note;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                // Thêm item mới
                var cartItem = new CartItem
                {
                    Id = Guid.NewGuid(),
                    CartId = cart.Id,
                    MenuId = dto.MenuId,
                    Quantity = dto.Quantity,
                    Note = dto.Note,
                    CreatedAt = DateTime.UtcNow
                };
                _context.CartItems.Add(cartItem);
            }

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Trả về item vừa thêm/cập nhật
            var updatedItem = await _context.CartItems
                .Include(ci => ci.Menu)
                .Include(ci => ci.Menu.Store)
                .FirstOrDefaultAsync(ci => ci.CartId == cart.Id && ci.MenuId == dto.MenuId);

            return MapToCartItemDto(updatedItem);
        }

        public async Task<CartItemDto> UpdateCartItemAsync(Guid customerId, Guid itemId, UpdateCartItemDto dto)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Menu)
                .Include(ci => ci.Menu.Store)
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart.CustomerId == customerId);

            if (cartItem == null)
                throw new InvalidOperationException("Không tìm thấy item trong giỏ hàng");

            cartItem.Quantity = dto.Quantity;
            cartItem.Note = dto.Note;
            cartItem.UpdatedAt = DateTime.UtcNow;

            cartItem.Cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToCartItemDto(cartItem);
        }

        public async Task<bool> RemoveFromCartAsync(Guid customerId, Guid itemId)
        {
            var cartItem = await _context.CartItems
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.Id == itemId && ci.Cart.CustomerId == customerId);

            if (cartItem == null)
                return false;

            _context.CartItems.Remove(cartItem);
            cartItem.Cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ClearCartAsync(Guid customerId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
                return false;

            _context.CartItems.RemoveRange(cart.CartItems);
            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<CartDto> GetCartSummaryAsync(Guid customerId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Menu)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);

            if (cart == null)
                return new CartDto { CustomerId = customerId };

            return MapToCartDto(cart);
        }

        private CartDto MapToCartDto(Cart cart)
        {
            var items = cart.CartItems.Select(MapToCartItemDto).ToList();
            var totalAmount = items.Sum(item => item.SubTotal);
            var totalItems = items.Sum(item => item.Quantity);

            return new CartDto
            {
                Id = cart.Id,
                CustomerId = cart.CustomerId,
                Items = items,
                TotalItems = totalItems,
                TotalAmount = totalAmount,
                CreatedAt = cart.CreatedAt,
                UpdatedAt = cart.UpdatedAt
            };
        }

        private CartItemDto MapToCartItemDto(CartItem item)
        {
            return new CartItemDto
            {
                Id = item.Id,
                MenuId = item.MenuId,
                MenuName = item.Menu?.Name,
                MenuImage = item.Menu?.ImageUrl,
                MenuPrice = item.Menu?.Price ?? 0,
                Quantity = item.Quantity,
                Note = item.Note,
                SubTotal = (item.Menu?.Price ?? 0) * item.Quantity,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            };
        }
    }

    public class WishlistService : IWishlistService
    {
        private readonly ApplicationDbContext _context;

        public WishlistService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<WishlistDto> GetWishlistAsync(Guid customerId)
        {
            var wishlistItems = await _context.Wishlists
                .Include(w => w.Menu)
                .Include(w => w.Menu.Store)
                .Where(w => w.CustomerId == customerId)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            var items = wishlistItems.Select(MapToWishlistItemDto).ToList();

            return new WishlistDto
            {
                Items = items,
                TotalItems = items.Count
            };
        }

        public async Task<WishlistItemDto> AddToWishlistAsync(Guid customerId, AddToWishlistDto dto)
        {
            // Kiểm tra menu tồn tại
            var menu = await _context.Menus
                .Include(m => m.Store)
                .FirstOrDefaultAsync(m => m.Id == dto.MenuId && m.Status == MenuStatus.Available);

            if (menu == null)
                throw new InvalidOperationException("Món ăn không tồn tại hoặc không khả dụng");

            // Kiểm tra đã có trong wishlist chưa
            var existingItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.CustomerId == customerId && w.MenuId == dto.MenuId);

            if (existingItem != null)
                throw new InvalidOperationException("Món ăn đã có trong danh sách yêu thích");

            var wishlistItem = new Wishlist
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                MenuId = dto.MenuId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Wishlists.Add(wishlistItem);
            await _context.SaveChangesAsync();

            // Reload để lấy thông tin đầy đủ
            var addedItem = await _context.Wishlists
                .Include(w => w.Menu)
                .Include(w => w.Menu.Store)
                .FirstOrDefaultAsync(w => w.Id == wishlistItem.Id);

            return MapToWishlistItemDto(addedItem);
        }

        /*        public async Task<bool> RemoveFromWishlistAsync(Guid customerId, Guid menuId)
                {
                    var wishlistItem = await _context.Wishlists
                        .FirstOrDefaultAsync(w => w.CustomerId == customerId && w.MenuId == menuId);

                    if (wishlistItem == null)
                        return false;

                    _context.Wishlists.Remove(wishlistItem);
                    await _context.SaveChangesAsync();

                    return true;
                } */
        public async Task<bool> RemoveFromWishlistAsync(Guid customerId, Guid Id)
        {
            var list = await _context.Wishlists
                .Where(w => w.CustomerId == customerId)
                .ToListAsync();

            Console.WriteLine($"CustomerId: {customerId}");
            Console.WriteLine($"MenuId tìm kiếm: {Id}");
            foreach (var item in list)
            {
                Console.WriteLine($"- Có MenuId: {item.Id}");
            }

            var wishlistItem = list.FirstOrDefault(w => w.Id == Id);
            if (wishlistItem == null)
            {
                Console.WriteLine("Không tìm thấy MenuId trùng khớp trong danh sách");
                return false;
            }

            _context.Wishlists.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            return true;
        }


        public async Task<bool> IsInWishlistAsync(Guid customerId, Guid menuId)
        {
            return await _context.Wishlists
                .AnyAsync(w => w.CustomerId == customerId && w.MenuId == menuId);
        }

        private WishlistItemDto MapToWishlistItemDto(Wishlist item)
        {
            return new WishlistItemDto
            {
                // Id của mục wishlist
                Id = item.Id,
                // Id của món ăn trong wishlist
                MenuId = item.MenuId,
                // Tên món ăn (có thể null nếu Menu chưa được include)
                MenuName = item.Menu?.Name,
                // Đường dẫn hình ảnh món ăn (có thể null)
                MenuImage = item.Menu?.ImageUrl,
                // Giá món ăn, nếu không có thì trả về 0
                MenuPrice = item.Menu?.Price ?? 0,
                // Tên cửa hàng của món ăn (có thể null)
                StoreName = item.Menu?.Store?.Name,
                StoreId = item.Menu?.StoreId ?? Guid.Empty,
                CreatedAt = item.CreatedAt
            };
        }
    }
} 