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
    [Authorize]
    public class StoresController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StoresController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("createStore")]
        // Cho phép nguoi dung đăng ký gian hàng để trở thành Seller
        [Authorize]
        public async Task<IActionResult> CreateStore(StoreCreateDto storeDto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            // Kiểm tra user hiện tại
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound("Không tìm thấy người dùng");

            // Nếu đã là Seller, không cho phép đăng ký thêm
            if (user.Role == Role.Seller)
                return BadRequest("Bạn đã là người bán rồi! Không cần đăng ký gian hàng nữa.");

            var store = new Store
            {
                Name = storeDto.Name,
                Address = storeDto.Address,
                Description = storeDto.Description,
                Latitude = storeDto.Latitude,
                Longitude = storeDto.Longitude,
                SellerId = userId,
                Status = StoreStatus.Pending // Tao store moi voi trang thai la pendding
            };

            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            var resultDto = new StoreDto
            {
                Id = store.Id,
                SellerId = store.SellerId,
                Name = store.Name,
                Address = store.Address,
                Description = store.Description,
                Status = store.Status.ToString(),
                CreatedAt = store.CreatedAt
            };

            return CreatedAtAction(nameof(GetStoreById), new { id = store.Id }, resultDto);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetStoreById(Guid id)
        {
            var store = await _context.Stores
                .Include(s => s.Seller) // Include seller info
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
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null || store.Status != nameof(StoreStatus.Approved))
            {
                // Optionally, allow seller to see their own non-approved store
                // For now, keep it simple: only approved stores are public
                return NotFound();
            }

            return Ok(store);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllStores()
        {
            var stores = await _context.Stores
                .Where(s => s.Status == StoreStatus.Approved)
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

        [HttpGet("mystores")]
        [Authorize]
        public async Task<IActionResult> GetMyStores()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var stores = await _context.Stores
                .Where(s => s.SellerId == userId)
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

        [HttpPut("{id}")]
        // chi nguoi dung da dang nhap va co quyen la nguoi ban moi co quyen sua san pham
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateStore(Guid id, StoreUpdateDto storeDto)
        {
            var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var store = await _context.Stores.FindAsync(id);

            if (store == null)
            {
                return NotFound();
            }

            if (store.SellerId != sellerId)
            {
                return Forbid(); // User does not own this store
            }

            // Update only the provided fields
            if (!string.IsNullOrEmpty(storeDto.Name))
                store.Name = storeDto.Name;
            if (!string.IsNullOrEmpty(storeDto.Address))
                store.Address = storeDto.Address;
            if (storeDto.Description != null)
                store.Description = storeDto.Description;
            if (storeDto.Latitude.HasValue)
                store.Latitude = storeDto.Latitude;
            if (storeDto.Longitude.HasValue)
                store.Longitude = storeDto.Longitude;

            store.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        // chi nguoi dung da dang nhap va co vai tro la nguoi ban moi co the thuc hien chuc nang nay
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> DeleteStore(Guid id)
        {
            var sellerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var store = await _context.Stores.FindAsync(id);

            if (store == null)
            {
                return NotFound();
            }

            if (store.SellerId != sellerId)
            {
                return Forbid();
            }

            _context.Stores.Remove(store);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchStores([FromQuery] StoreSearchFilterDto filter)
        {
            var query = _context.Stores
                .Where(s => s.Status == StoreStatus.Approved)
                .Include(s => s.Seller)
                .Include(s => s.Category)
                .Include(s => s.Reviews)
                .Include(s => s.Orders)
                .AsQueryable();

            // Tìm kiếm theo tên, địa chỉ
            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(searchTerm) ||
                    s.Address.ToLower().Contains(searchTerm) ||
                    s.Description.ToLower().Contains(searchTerm));
            }

            // Lọc theo danh mục
            if (filter.CategoryId.HasValue)
            {
                query = query.Where(s => s.CategoryId == filter.CategoryId);
            }

            // Lọc theo đánh giá
            if (filter.MinRating.HasValue)
            {
                query = query.Where(s => s.Rating >= filter.MinRating);
            }
            if (filter.MaxRating.HasValue)
            {
                query = query.Where(s => s.Rating <= filter.MaxRating);
            }

            // Tính toán khoảng cách nếu có vị trí
            if (filter.Latitude.HasValue && filter.Longitude.HasValue)
            {
                query = query.Where(s => s.Latitude.HasValue && s.Longitude.HasValue);
            }

            // Lấy dữ liệu với thống kê
            var storesWithStats = await query
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Address,
                    s.Description,
                    s.Latitude,
                    s.Longitude,
                    s.Rating,
                    s.CategoryId,
                    s.CategoryName,
                    s.CreatedAt,
                    SellerName = s.Seller.FullName,
                    ReviewCount = s.Reviews.Count,
                    OrderCount = s.Orders.Count,
                    // Tính khoảng cách nếu có vị trí
                    DistanceKm = filter.Latitude.HasValue && filter.Longitude.HasValue && s.Latitude.HasValue && s.Longitude.HasValue
                        ? CalculateDistance(filter.Latitude.Value, filter.Longitude.Value, s.Latitude.Value, s.Longitude.Value)
                        : (decimal?)null
                })
                .ToListAsync();

            // Lọc theo bán kính
            if (filter.RadiusKm.HasValue && filter.Latitude.HasValue && filter.Longitude.HasValue)
            {
                storesWithStats = storesWithStats
                    .Where(s => s.DistanceKm <= filter.RadiusKm)
                    .ToList();
            }

            // Tính điểm phổ biến và chuyển đổi sang DTO
            var results = storesWithStats.Select(s => new StoreSearchResultDto
            {
                Id = s.Id,
                Name = s.Name,
                Address = s.Address,
                Description = s.Description,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Rating = s.Rating,
                ReviewCount = s.ReviewCount,
                OrderCount = s.OrderCount,
                CategoryId = s.CategoryId,
                CategoryName = s.CategoryName,
                SellerName = s.SellerName,
                CreatedAt = s.CreatedAt,
                DistanceKm = s.DistanceKm,
                PopularityScore = CalculatePopularityScore(s.Rating ?? 0, s.ReviewCount, s.OrderCount)
            }).ToList();

            // Sắp xếp
            results = SortStores(results, filter.SortBy, filter.SortOrder);

            // Phân trang
            var totalCount = results.Count;
            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);
            var items = results
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToList();

            return Ok(new PaginatedResult<StoreSearchResultDto>
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

        // Lấy danh sách quán phổ biến theo don hang
        [HttpGet("popular")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPopularStores([FromQuery] int take = 10)
        {
            var stores = await _context.Stores
                .Where(s => s.Status == StoreStatus.Approved)
                .Include(s => s.Seller)
                .Include(s => s.Category)
                .Include(s => s.Reviews)
                .Include(s => s.Orders)
                .Select(s => new StoreSearchResultDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Address = s.Address,
                    Description = s.Description,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    Rating = s.Rating,
                    ReviewCount = s.Reviews.Count,
                    OrderCount = s.Orders.Count,
                    CategoryId = s.CategoryId,
                    CategoryName = s.CategoryName,
                    SellerName = s.Seller.FullName,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            foreach (var s in stores)
            {
                s.PopularityScore = CalculatePopularityScore(s.Rating ?? 0, s.ReviewCount, s.OrderCount);
            }

            var popularStores = stores
                .OrderByDescending(s => s.PopularityScore)
                .Take(take)
                .ToList();

            return Ok(popularStores);
        }

        private static decimal CalculateDistance(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
        {
            const double R = 6371; // Bán kính Trái Đất (km)
            var dLat = (double)(lat2 - lat1) * Math.PI / 180;
            var dLon = (double)(lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos((double)lat1 * Math.PI / 180) * Math.Cos((double)lat2 * Math.PI / 180) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return (decimal)(R * c);
        }

        private static decimal CalculatePopularityScore(decimal rating, int reviewCount, int orderCount)
        {
            
            return (rating * reviewCount * orderCount) / 1000m;
        }

        private static List<StoreSearchResultDto> SortStores(List<StoreSearchResultDto> stores, string? sortBy, string? sortOrder)
        {
            var isDescending = sortOrder?.ToLower() == "desc";

            return sortBy?.ToLower() switch
            {
                "rating" => isDescending
                    ? stores.OrderByDescending(s => s.Rating).ToList()
                    : stores.OrderBy(s => s.Rating).ToList(),
                "distance" => isDescending
                    ? stores.OrderByDescending(s => s.DistanceKm).ToList()
                    : stores.OrderBy(s => s.DistanceKm).ToList(),
                "name" => isDescending
                    ? stores.OrderByDescending(s => s.Name).ToList()
                    : stores.OrderBy(s => s.Name).ToList(),
                "ordercount" => isDescending
                    ? stores.OrderByDescending(s => s.OrderCount).ToList()
                    : stores.OrderBy(s => s.OrderCount).ToList(),
                "popularity" => isDescending
                    ? stores.OrderByDescending(s => s.PopularityScore).ToList()
                    : stores.OrderBy(s => s.PopularityScore).ToList(),
                _ => stores.OrderByDescending(s => s.PopularityScore).ToList() // Mặc định sắp xếp theo độ phổ biến
            };
        }

        [HttpPut("{id}/Toggle-open")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateStoreStatus(Guid id, [FromBody] StatusStoreSeller status)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null) return NotFound();

            store.StatusStoreSeller = status;
            await _context.SaveChangesAsync();
            return Ok(new { store.Id, store.StatusStoreSeller });
        }


    }


}


