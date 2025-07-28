using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatHang.Data;
using QuanLyDatHang.DTOs;
using QuanLyDatHang.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyDatHang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class SearchController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SearchController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Tìm kiếm tổng hợp (cả quán và món ăn)
        [HttpGet("GetSearch")]
        public async Task<IActionResult> UnifiedSearch([FromQuery] string searchTerm, [FromQuery] int take = 20)
        {
            if (string.IsNullOrEmpty(searchTerm))
                return BadRequest("Từ khóa tìm kiếm không được để trống");

            var searchTermLower = searchTerm.ToLower();

            // Tìm kiếm quán
            var stores = await _context.Stores
                .Where(s => s.Status == StoreStatus.Approved)
                .Include(s => s.Seller)
                .Include(s => s.Category)
                .Include(s => s.Reviews)
                .Include(s => s.Orders)
                .Where(s => s.Name.ToLower().Contains(searchTermLower) || 
                           s.Address.ToLower().Contains(searchTermLower) ||
                           s.Description.ToLower().Contains(searchTermLower))
                .Select(s => new
                {
                    Type = "store",
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    Address = s.Address,
                    Rating = s.Rating,
                    ReviewCount = s.Reviews.Count,
                    OrderCount = s.Orders.Count,
                    CategoryName = s.CategoryName,
                    SellerName = s.Seller.FullName,
                    PopularityScore = (s.Rating ?? 0) * s.Reviews.Count * s.Orders.Count / 1000m
                })
                .OrderByDescending(s => s.PopularityScore)
                .Take(take / 2)
                .ToListAsync();

            // Tìm kiếm món ăn
            var menus = await _context.Menus
                .Where(m => m.Status == MenuStatus.Available)
                .Include(m => m.Store)
                .Include(m => m.Store.Reviews)
                .Include(m => m.Category)
                .Include(m => m.OrderDetails)
                .Where(m => m.Name.ToLower().Contains(searchTermLower) || 
                           m.Description.ToLower().Contains(searchTermLower))
                .Select(m => new
                {
                    Type = "menu",
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    Price = m.Price,
                    StoreName = m.Store.Name,
                    StoreId = m.StoreId,
                    CategoryName = m.CategoryName,
                    StoreRating = m.Store.Rating,
                    StoreReviewCount = m.Store.Reviews.Count,
                    OrderCount = m.OrderDetails.Sum(od => od.Quantity),
                    PopularityScore = (m.Store.Rating ?? 0) * m.Store.Reviews.Count * m.OrderDetails.Sum(od => od.Quantity) / 1000m
                })
                .OrderByDescending(m => m.PopularityScore)
                .Take(take / 2)
                .ToListAsync();

            // Kết hợp và sắp xếp theo độ phổ biến
            var combinedResults = stores.Select(s => new
            {
                s.Type,
                s.Id,
                s.Name,
                s.Description,
                s.Rating,
                s.ReviewCount,
                s.OrderCount,
                s.CategoryName,
                s.PopularityScore,
                StoreName = s.Name,
                SellerName = s.SellerName,
                Price = (decimal?)null
            }).Concat(menus.Select(m => new
            {
                m.Type,
                m.Id,
                m.Name,
                m.Description,
                Rating = m.StoreRating,
                ReviewCount = m.StoreReviewCount,
                m.OrderCount,
                m.CategoryName,
                m.PopularityScore,
                m.StoreName,
                SellerName = (string?)null,
                Price = (decimal?)m.Price
            }))
            .OrderByDescending(r => r.PopularityScore)
            .Take(take)
            .ToList();

            return Ok(new
            {
                SearchTerm = searchTerm,
                TotalResults = combinedResults.Count(),
                Results = combinedResults
            });
        }

     
        // Gợi ý tìm kiem
        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSearchSuggestions([FromQuery] string query, [FromQuery] int take = 5)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 2)
                return Ok(new { suggestions = new string[0] });

            var queryLower = query.ToLower();

            // Gợi ý từ tên quán
            var storeSuggestions = await _context.Stores
                .Where(s => s.Status == StoreStatus.Approved && s.Name.ToLower().Contains(queryLower))
                .Select(s => s.Name)
                .Take(take)
                .ToListAsync();

            // Gợi ý từ tên món ăn
            var menuSuggestions = await _context.Menus
                .Where(m => m.Status == MenuStatus.Available && m.Name.ToLower().Contains(queryLower))
                .Select(m => m.Name)
                .Take(take)
                .ToListAsync();

            // Gợi ý từ danh mục
            var categorySuggestions = await _context.Categories
                .Where(c => c.Ten.ToLower().Contains(queryLower))
                .Select(c => c.Ten)
                .Take(take)
                .ToListAsync();

            // Kết hợp và loại bỏ trùng lặp
            var allSuggestions = storeSuggestions
                .Concat(menuSuggestions)
                .Concat(categorySuggestions)
                .Distinct()
                .Take(take)
                .ToList();

            return Ok(new { suggestions = allSuggestions });
        }

    }
} 