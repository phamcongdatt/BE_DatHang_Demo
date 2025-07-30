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
                .Take(take / 8)
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
                    ImageUrl = m.ImageUrl,
                    StoreName = m.Store.Name,
                    StoreId = m.StoreId,
                    CategoryName = m.CategoryName,
                    StoreRating = m.Store.Rating,
                    StoreReviewCount = m.Store.Reviews.Count,
                    OrderCount = m.OrderDetails.Sum(od => od.Quantity),
                    PopularityScore = (m.Store.Rating ?? 0) * m.Store.Reviews.Count * m.OrderDetails.Sum(od => od.Quantity) / 1000m
                })
                .OrderByDescending(m => m.PopularityScore)
                .Take(take / 8)
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
                ImageUrl = (string?)null,
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
                ImageUrl = (string?)m.ImageUrl,
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
    }
} 