using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuanLyDatHang.Data;
using QuanLyDatHang.DTOs;
using QuanLyDatHang.Models;
using QuanLyDatHang.Hubs;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyDatHang.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _notificationHub;
        public AdminController(ApplicationDbContext context, IHubContext<NotificationHub> notificationHub)
        {
            _context = context;
            _notificationHub = notificationHub;
        }

        // Lấy danh sách store chờ duyệt
        [HttpGet("stores/pending")]
        public async Task<IActionResult> GetPendingStores()
        {
            var stores = await _context.Stores
                .Where(s => s.Status == StoreStatus.Pending)
                .Include(s => s.Seller)
                .Select(s => new StoreDto
                {
                    Id = s.Id,
                    SellerId = s.SellerId,
                    SellerName = s.Seller.FullName,
                    Name = s.Name,
                    Address = s.Address,
                    Description = s.Description,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Status = s.Status.ToString(),
                    Rating = s.Rating,
                    CreatedAt = s.CreatedAt
                    
                })
                .ToListAsync();
            return Ok(stores);
        }

        // Duyệt store
        [HttpPut("stores/{id}/approve")]
        public async Task<IActionResult> ApproveStore(Guid id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return NotFound();
            store.Status = StoreStatus.Approved;
            store.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            // Gửi thông báo realtime tới seller
            await _notificationHub.Clients.User(store.SellerId.ToString()).SendAsync("StoreApproved", store.Id);
            return Ok(new { message = "Gian hang da duoc chap nhan" });
        }

        // Từ chối store
        [HttpPut("stores/{id}/reject")]
        public async Task<IActionResult> RejectStore(Guid id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return NotFound();
            store.Status = StoreStatus.Rejected;
            store.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Gian hanh da bi tu choi" });
        }

        // Xem danh sách user
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FullName = u.FullName,
                    PhoneNumber = u.PhoneNumber,
                    Role = u.Role.ToString(),
                    CreatedAt = u.CreatedAt,
                    IsLocked = u.IsLocked
                })
                .ToListAsync();
            return Ok(users);
        }

        // Khóa user
        [HttpPut("users/{id}/lock")]
        public async Task<IActionResult> LockUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsLocked = true;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Nguoi dung da bi khoa" });
        }

        // Mở khóa user
        [HttpPut("users/{id}/unlock")]
        public async Task<IActionResult> UnlockUser(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            user.IsLocked = false;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Nguoi dung da duoc mo khoa" });
        }
        // Quan ly danh muc 

        /// <summary>
        /// Tạo danh mục mới (chỉ admin)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CategoryCreateDto categoryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }
                
            var category = new Category
            {
                Id = Guid.NewGuid(),
                Ten = categoryDto.Ten,
                MoTa = categoryDto.MoTa,
                KichHoat = true,
                MacDinh = categoryDto.MacDinh
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Tạo danh mục thành công",
                data = new
                {
                    category.Id,
                    category.Ten,
                    category.MoTa,
                    category.MacDinh
                }
            });
        }

        /// <summary>
        /// Cập nhật danh mục (chỉ admin)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] CategoryUpdateDto categoryDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Dữ liệu không hợp lệ",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy danh mục"
                });
            }

            category.Ten = categoryDto.Ten;
            category.MoTa = categoryDto.MoTa;
            category.MacDinh = categoryDto.MacDinh;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Cập nhật danh mục thành công",
                data = new
                {
                    category.Id,
                    category.Ten,
                    category.MoTa,
                    category.MacDinh
                }
            });
        }

        /// <summary>
        /// Duyệt đề xuất danh mục (chỉ admin)
        /// </summary>
        [HttpPut("{id}/approve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveCategory(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy danh mục"
                });
            }

            category.KichHoat = true;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đã phê duyệt danh mục",
                data = new
                {
                    category.Id,
                    category.Ten,
                    category.MoTa,
                    status = "Đã kích hoạt"
                }
            });
        }

        /// <summary>
        /// Duyệt đề xuất cập nhật danh mục (chỉ admin)
        /// </summary>
        [HttpPut("{suggestionId}/approve-update/{originalId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ApproveCategoryUpdate(Guid suggestionId, Guid originalId)
        {
            var suggestion = await _context.Categories.FindAsync(suggestionId);
            var original = await _context.Categories.FindAsync(originalId);

            if (suggestion == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy đề xuất cập nhật"
                });
            }

            if (original == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy danh mục gốc"
                });
            }

            // Cập nhật danh mục gốc với dữ liệu từ đề xuất
            original.Ten = suggestion.Ten;
            original.MoTa = suggestion.MoTa;
            original.MacDinh = suggestion.MacDinh;

            // Xóa đề xuất
            _context.Categories.Remove(suggestion);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đã phê duyệt và áp dụng cập nhật danh mục",
                data = new
                {
                    originalId = original.Id,
                    original.Ten,
                    original.MoTa,
                    original.MacDinh,
                    status = "Đã cập nhật"
                }
            });
        }

        /// <summary>
        /// Từ chối đề xuất (chỉ admin)
        /// </summary>
        [HttpDelete("{id}/reject")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RejectSuggestion(Guid id)
        {
            var suggestion = await _context.Categories.FindAsync(id);
            if (suggestion == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy đề xuất"
                });
            }

            if (suggestion.KichHoat)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Không thể từ chối danh mục đã được kích hoạt"
                });
            }

            _context.Categories.Remove(suggestion);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Đã từ chối đề xuất",
                data = new
                {
                    suggestionId = id,
                    suggestion.Ten,
                    status = "Đã từ chối"
                }
            });
        }

        /// <summary>
        /// Xóa danh mục (chỉ admin)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Không tìm thấy danh mục"
                });
            }

            // Kiểm tra xem có cửa hàng/món ăn nào đang sử dụng danh mục này không
            var hasStores = await _context.Stores.AnyAsync(s => s.CategoryId == id);
            var hasMenus = await _context.Menus.AnyAsync(m => m.CategoryId == id);

            if (hasStores || hasMenus)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Không thể xóa danh mục đang được sử dụng"
                });
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Xóa danh mục thành công"
            });
        }
        /// <summary>
        /// Lấy danh sách đề xuất chờ duyệt (Admin only)
        /// </summary>
        [HttpGet("pending-suggestions")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingSuggestions()
        {
            var pendingSuggestions = await _context.Categories
                .Where(c => !c.KichHoat)
                .OrderBy(c => c.Ten)
                .Select(c => new
                {
                    c.Id,
                    c.Ten,
                    c.MoTa,
                    c.MacDinh,
                    status = "Pending"
                })
                .ToListAsync();

            return Ok(new
            {
                success = true,
                message = "Lấy danh sách đề xuất chờ duyệt thành công",
                data = pendingSuggestions
            });
        }
        //Lay danh sach tat ca cua hang
        [HttpGet("stores")]
        public async Task<IActionResult> GetAllStores()
        {
            var stores = await _context.Stores
                .Include(s => s.Seller)
                .Select(s => new StoreDto
                {
                    Id = s.Id,
                    SellerId = s.SellerId,
                    SellerName = s.Seller.FullName,
                    Name = s.Name,
                    Address = s.Address,
                    Description = s.Description,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Status = s.Status.ToString(),
                    Rating = s.Rating,
                    CreatedAt = s.CreatedAt,
                })
                .ToListAsync();
            return Ok(stores);
        }
        [HttpGet("GetAllCategory")]
        public async Task<IActionResult> GetAllCategory()
        {
            var categories = await _context.Categories
                .OrderBy(c => c.Ten)
                .Select(c => new
                {
                    c.Id,
                    c.Ten,
                    c.MoTa,
                    c.MacDinh,
                    c.KichHoat,
                    status = c.KichHoat ? "Approved" : "Pending"
                })
                .ToListAsync();
            return Ok(new
            {
                success = true,
                message = "Lấy danh sách danh mục thành công",
                data = categories
            });
        }

    
        [HttpGet("dashboard-stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var usersCount = await _context.Users.CountAsync();
            var storesCount = await _context.Stores.CountAsync();
            var menusCount = await _context.Menus.CountAsync();
            var ordersCount = await _context.Orders.CountAsync();
            var revenue = await _context.Orders.Where(o => o.Status == OrderStatus.Completed).SumAsync(o => (decimal?)o.TotalPrice) ?? 0;
            return Ok(new
            {
                users = usersCount,
                stores = storesCount,
                menus = menusCount,
                orders = ordersCount,
                revenue = revenue
            });
        }

    /// ADMIN MENU 
        [HttpGet("GetAllMenus")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllMenus([FromQuery] Guid? storeId = null)
        {
            var query = _context.Menus
                .Include(m => m.Store)
                .Include(m => m.Category)
                .Include(m => m.OrderDetails)
                .AsQueryable();

            if (storeId.HasValue)
                query = query.Where(m => m.StoreId == storeId.Value);

            var menus = await query.ToListAsync();

            var result = menus.Select(m => new MenuSearchResultDto
            {
                Id = m.Id,
                StoreId = m.StoreId,
                StoreName = m.Store?.Name,
                Name = m.Name,
                Price = m.Price,
                Description = m.Description,
                ImageUrl = m.ImageUrl,
                CategoryId = m.CategoryId,
                CategoryName = m.Category?.Ten,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt,
                OrderCount = m.OrderDetails?.Sum(od => od.Quantity) ?? 0,
                Status = m.Status.ToString() 
            })
            .OrderByDescending(m => m.CreatedAt)
            .ToList();

            return Ok(new
            {
                success = true,
                message = "Lấy tất cả sản phẩm thành công",
                data = result
            });
        }
 //ADMIN Oder
[HttpGet("GetAllOrders")]
[AllowAnonymous]
public async Task<IActionResult> GetAllOrders([FromQuery] Guid? storeId = null)
{
    var query = _context.Orders
        .Include(o => o.Store)
        .Include(o => o.OrderDetails)
        .ThenInclude(od => od.Menu) // Include Menu if OrderDetails has a Menu relationship
        .AsQueryable();

    if (storeId.HasValue)
        query = query.Where(o => o.StoreId == storeId.Value);

    var orders = await query
        .OrderByDescending(o => o.CreatedAt)
        .ToListAsync();

    var result = orders.Select(o => new OrderDto
    {
        Id = o.Id,
        StoreId = o.StoreId,
        StoreName = o.Store?.Name,
        CustomerId = o.CustomerId,
        DeliveryAddress = o.DeliveryAddress,
        DeliveryLatitude = o.DeliveryLatitude,
        DeliveryLongitude = o.DeliveryLongitude,
        TotalPrice = o.TotalPrice,
        PaymentMethod = o.PaymentMethod.ToString(),
        Status = o.Status.ToString(),
        CreatedAt = o.CreatedAt,
        Items = o.OrderDetails.Select(od => new OrderItemDto
        {
            MenuId = od.MenuId,
            MenuName = od.Menu?.Name, 
            Quantity = od.Quantity,
            Note = od.Note,
            Price = od.Price 
        }).ToList()
    }).ToList();

    return Ok(new
    {
        success = true,
        message = "Lấy tất cả đơn hàng thành công",
        data = result
    });
}
    
}
}
