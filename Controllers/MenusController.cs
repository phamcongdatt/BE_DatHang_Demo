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

namespace QuanLyDatHang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenusController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public MenusController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Lấy danh sách menu của một store (công khai)
        [HttpGet("bystore/{storeId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMenusByStore(Guid storeId)
        {
            var menus = await _context.Menus
                .Where(m => m.StoreId == storeId && m.Status == MenuStatus.Available)
                .Select(m => new MenuDto
                {
                    Id = m.Id,
                    StoreId = m.StoreId,
                    Name = m.Name,
                    Price = m.Price,
                    Description = m.Description,
                    ImageUrl = m.ImageUrl,
                    CategoryId = m.CategoryId,
                    CategoryName = m.CategoryName,
                    Status = m.Status.ToString(),
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .ToListAsync();
            return Ok(menus);
        }

        // Thêm món ăn mới (chỉ seller sở hữu store đã duyệt)
        [HttpPost]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CreateMenu(MenuCreateDto dto)
        {
            var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var store = await _context.Stores.FirstOrDefaultAsync(s => s.Id == dto.StoreId && s.SellerId == sellerId && s.Status == StoreStatus.Approved);
            if (store == null)
                return Forbid();

            var category = await _context.Categories.FindAsync(dto.CategoryId);
            if (category == null) return BadRequest("Danh mục không tồn tại!");

            var menu = new Menu
            {
                StoreId = dto.StoreId,
                Name = dto.Name,
                Price = dto.Price,
                Description = dto.Description,
                ImageUrl = dto.ImageUrl,
                CategoryId = category.Id,
                CategoryName = category.Ten,
                Status = MenuStatus.Available
            };
            _context.Menus.Add(menu);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Menu item created successfully." });
        }

        // Sửa món ăn (chỉ seller sở hữu store)
        [HttpPut("{id}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateMenu(Guid id, MenuUpdateDto dto)
        {
            var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var menu = await _context.Menus.Include(m => m.Store).FirstOrDefaultAsync(m => m.Id == id);
            if (menu == null || menu.Store.SellerId != sellerId)
                return Forbid();

            if (!string.IsNullOrEmpty(dto.Name)) menu.Name = dto.Name;
            if (dto.Price.HasValue) menu.Price = dto.Price.Value;
            if (dto.Description != null) menu.Description = dto.Description;
            if (dto.ImageUrl != null) menu.ImageUrl = dto.ImageUrl;
            
            if (dto.CategoryId.HasValue)
            {
                var category = await _context.Categories.FindAsync(dto.CategoryId.Value);
                if (category == null) return BadRequest("Danh mục không tồn tại!");
                menu.CategoryId = category.Id;
                menu.CategoryName = category.Ten;
            }

            if (dto.Status != null && Enum.TryParse<MenuStatus>(dto.Status, out var status)) menu.Status = status;
            menu.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Xóa món ăn (chỉ seller sở hữu store)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> DeleteMenu(Guid id)
        {
            var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var menu = await _context.Menus.Include(m => m.Store).FirstOrDefaultAsync(m => m.Id == id);
            if (menu == null || menu.Store.SellerId != sellerId)
                return Forbid();
            _context.Menus.Remove(menu);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Tìm kiếm và lọc món ăn
        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchMenus([FromQuery] MenuSearchFilterDto filter)
        {
            var query = _context.Menus
                .Where(m => m.Status == MenuStatus.Available)
                .Include(m => m.Store)
                .Include(m => m.Store.Seller)
                .Include(m => m.Store.Reviews)
                .Include(m => m.Store.Orders)
                .Include(m => m.Category)
                .Include(m => m.OrderDetails)
                .AsQueryable();

            // Tìm kiếm theo tên món
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(m => 
                    m.Name.ToLower().Contains(searchTerm) || 
                    m.Description.ToLower().Contains(searchTerm));
            }

            // Lọc theo danh mục
            if (filter.CategoryId.HasValue)
            {
                query = query.Where(m => m.CategoryId == filter.CategoryId);
            }

            // Lọc theo quán
            if (filter.StoreId.HasValue)
            {
                query = query.Where(m => m.StoreId == filter.StoreId);
            }

            // Lọc theo giá
            if (filter.MinPrice.HasValue)
            {
                query = query.Where(m => m.Price >= filter.MinPrice);
            }
            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(m => m.Price <= filter.MaxPrice);
            }

            // Lọc theo đánh giá của quán
            if (filter.MinRating.HasValue)
            {
                query = query.Where(m => m.Store.Rating >= filter.MinRating);
            }

            // Lấy dữ liệu với thống kê
            var menusWithStats = await query
                .Select(m => new
                {
                    m.Id,
                    m.StoreId,
                    StoreName = m.Store.Name,
                    m.Name,
                    m.Price,
                    m.Description,
                    m.ImageUrl,
                    m.CategoryId,
                    m.CategoryName,
                    m.CreatedAt,
                    StoreRating = m.Store.Rating,
                    StoreReviewCount = m.Store.Reviews.Count,
                    OrderCount = m.OrderDetails.Sum(od => od.Quantity)
                })
                .ToListAsync();

            // Tính điểm phổ biến và chuyển đổi sang DTO
            var results = menusWithStats.Select(m => new MenuSearchResultDto
            {
                Id = m.Id,
                StoreId = m.StoreId,
                StoreName = m.StoreName,
                Name = m.Name,
                Price = m.Price,
                Description = m.Description,
                ImageUrl = m.ImageUrl,
                CategoryId = m.CategoryId,
                CategoryName = m.CategoryName,
                CreatedAt = m.CreatedAt,
                StoreRating = m.StoreRating,
                StoreReviewCount = m.StoreReviewCount,
                OrderCount = m.OrderCount,
                PopularityScore = CalculateMenuPopularityScore(m.StoreRating ?? 0, m.StoreReviewCount, m.OrderCount)
            }).ToList();

            // Sắp xếp
            results = SortMenus(results, filter.SortBy, filter.SortOrder);

            // Phân trang
            var totalCount = results.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);
            var items = results
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return Ok(new PaginatedResult<MenuSearchResultDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = totalPages,
                HasNextPage = filter.Page < totalPages,
                HasPreviousPage = filter.Page > 1
            });
        }

    
        [HttpGet("popular")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPopularMenus([FromQuery] int take = 10)
        {
            var menus = await _context.Menus
                .Where(m => m.Status == MenuStatus.Available)
                .Include(m => m.Store)
                .Include(m => m.Store.Reviews)
                .Include(m => m.Category)
                .Include(m => m.OrderDetails)
                .Select(m => new MenuSearchResultDto
                {
                    Id = m.Id,
                    StoreId = m.StoreId,
                    StoreName = m.Store.Name,
                    Name = m.Name,
                    Price = m.Price,
                    Description = m.Description,
                    ImageUrl = m.ImageUrl,
                    CategoryId = m.CategoryId,
                    CategoryName = m.CategoryName,
                    CreatedAt = m.CreatedAt,
                    StoreRating = m.Store.Rating,
                    StoreReviewCount = m.Store.Reviews.Count,
                    OrderCount = m.OrderDetails.Sum(od => od.Quantity)
                })
                .ToListAsync();

            // Tính điểm phổ biến trên C#
            foreach (var m in menus)
            {
                m.PopularityScore = CalculateMenuPopularityScore(m.StoreRating ?? 0, m.StoreReviewCount, m.OrderCount);
            }

            var popularMenus = menus
                .OrderByDescending(m => m.PopularityScore)
                .Take(take)
                .ToList();

            return Ok(popularMenus);
        }

        // Lấy món ăn theo danh mục với sắp xếp theo độ phổ biến
        [HttpGet("category/{categoryId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMenusByCategory(Guid categoryId, [FromQuery] int take = 20)
        {
            var menus = await _context.Menus
                .Where(m => m.Status == MenuStatus.Available && m.CategoryId == categoryId)
                .Include(m => m.Store)
                .Include(m => m.Store.Reviews)
                .Include(m => m.Category)
                .Include(m => m.OrderDetails)
                .Select(m => new MenuSearchResultDto
                {
                    Id = m.Id,
                    StoreId = m.StoreId,
                    StoreName = m.Store.Name,
                    Name = m.Name,
                    Price = m.Price,
                    Description = m.Description,
                    ImageUrl = m.ImageUrl,
                    CategoryId = m.CategoryId,
                    CategoryName = m.CategoryName,
                    CreatedAt = m.CreatedAt,
                    StoreRating = m.Store.Rating,
                    StoreReviewCount = m.Store.Reviews.Count,
                    OrderCount = m.OrderDetails.Sum(od => od.Quantity),
                    PopularityScore = CalculateMenuPopularityScore(m.Store.Rating ?? 0, m.Store.Reviews.Count, m.OrderDetails.Sum(od => od.Quantity))
                })
                .OrderByDescending(m => m.PopularityScore)
                .Take(take)
                .ToListAsync();

            return Ok(menus);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetMenuById(Guid id)
        {
            var menu = await _context.Menus
                .Include(m => m.Category)
                .Where(m => m.Id == id)
                .Select(m => new MenuDto
                {
                    Id = m.Id,
                    StoreId = m.StoreId, 
                    Name = m.Name,
                    Price = m.Price,
                    Description = m.Description,
                    ImageUrl = m.ImageUrl,
                    CategoryId = m.CategoryId,
                    CategoryName = m.Category.Ten,
                    Status = m.Status.ToString(),
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (menu == null)
                return NotFound();

            return Ok(menu);
        }

        private static decimal CalculateMenuPopularityScore(decimal storeRating, int storeReviewCount, int orderCount)
        {
            // Công thức: (storeRating * storeReviewCount * orderCount) / 1000
            // Ưu tiên món từ quán có đánh giá cao, nhiều đánh giá và món được đặt nhiều
            return (storeRating * storeReviewCount * orderCount) / 1000m;
        }

        [HttpGet("all")]
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

        private static List<MenuSearchResultDto> SortMenus(List<MenuSearchResultDto> menus, string? sortBy, string? sortOrder)
        {
            var isDescending = sortOrder?.ToLower() == "desc";
            
            return sortBy?.ToLower() switch
            {
                "price" => isDescending 
                    ? menus.OrderByDescending(m => m.Price).ToList()
                    : menus.OrderBy(m => m.Price).ToList(),
                "rating" => isDescending
                    ? menus.OrderByDescending(m => m.StoreRating).ToList()
                    : menus.OrderBy(m => m.StoreRating).ToList(),
                "popularity" => isDescending
                    ? menus.OrderByDescending(m => m.PopularityScore).ToList()
                    : menus.OrderBy(m => m.PopularityScore).ToList(),
                "name" => isDescending
                    ? menus.OrderByDescending(m => m.Name).ToList()
                    : menus.OrderBy(m => m.Name).ToList(),
                "ordercount" => isDescending
                    ? menus.OrderByDescending(m => m.OrderCount).ToList()
                    : menus.OrderBy(m => m.OrderCount).ToList(),
                _ => menus.OrderByDescending(m => m.PopularityScore).ToList() // Mặc định sắp xếp theo độ phổ biến
            };
        }
    }
} 