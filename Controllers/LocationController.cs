using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDatHang.Data;
using QuanLyDatHang.Models;
using QuanLyDatHang.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace QuanLyDatHang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class LocationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly LocationService _locationService;

        public LocationController(ApplicationDbContext context, LocationService locationService)
        {
            _context = context;
            _locationService = locationService;
        }

        // Lấy vị trí hiện tại của user từ IP
        [HttpGet("my-location")]
        public async Task<IActionResult> GetMyLocation()
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var (latitude, longitude) = await _locationService.GetLocationFromIpAsync(clientIp);
            
            if (!latitude.HasValue || !longitude.HasValue)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Không thể xác định vị trí của bạn.",
                    IpAddress = clientIp
                });
            }

            return Ok(new
            {
                Success = true,
                Latitude = latitude,
                Longitude = longitude,
                IpAddress = clientIp
            });
        }

        // Tìm quán gần vị trí hiện tại
        [HttpGet("nearby-stores")]
        public async Task<IActionResult> GetNearbyStores([FromQuery] decimal radiusKm = 5, [FromQuery] int take = 10)
        {
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            var (latitude, longitude) = await _locationService.GetLocationFromIpAsync(clientIp);
            
            if (!latitude.HasValue || !longitude.HasValue)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = "Không thể xác định vị trí của bạn. Vui lòng cung cấp tọa độ thủ công.",
                    IpAddress = clientIp
                });
            }

            var nearbyStores = await _context.Stores
                .Where(s => s.Status == StoreStatus.Approved && 
                           s.Latitude.HasValue && s.Longitude.HasValue)
                .Include(s => s.Seller)
                .Include(s => s.Category)
                .Include(s => s.Reviews)
                .Include(s => s.Orders)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Address,
                    s.Description,
                    s.Latitude,
                    s.Longitude,
                    s.Rating,
                    s.CategoryName,
                    SellerName = s.Seller.FullName,
                    ReviewCount = s.Reviews.Count,
                    OrderCount = s.Orders.Count,
                    DistanceKm = CalculateDistance(latitude.Value, longitude.Value, s.Latitude.Value, s.Longitude.Value),
                    PopularityScore = (s.Rating ?? 0) * s.Reviews.Count * s.Orders.Count / 1000m
                })
                .Where(s => s.DistanceKm <= radiusKm)
                .OrderBy(s => s.DistanceKm)
                .ThenByDescending(s => s.PopularityScore)
                .Take(take)
                .ToListAsync();

            return Ok(new
            {
                Success = true,
                UserLocation = new 
                { 
                    Latitude = latitude, 
                    Longitude = longitude,
                    IpAddress = clientIp
                },
                RadiusKm = radiusKm,
                TotalStores = nearbyStores.Count,
                Stores = nearbyStores
            });
        }

        // Test vị trí với tọa độ cụ thể
        [HttpGet("test-location")]
        public async Task<IActionResult> TestLocation([FromQuery] decimal latitude, [FromQuery] decimal longitude, [FromQuery] decimal radiusKm = 5)
        {
            var nearbyStores = await _context.Stores
                .Where(s => s.Status == StoreStatus.Approved && 
                           s.Latitude.HasValue && s.Longitude.HasValue)
                .Include(s => s.Seller)
                .Include(s => s.Category)
                .Include(s => s.Reviews)
                .Include(s => s.Orders)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Address,
                    s.Description,
                    s.Latitude,
                    s.Longitude,
                    s.Rating,
                    s.CategoryName,
                    SellerName = s.Seller.FullName,
                    ReviewCount = s.Reviews.Count,
                    OrderCount = s.Orders.Count,
                    DistanceKm = CalculateDistance(latitude, longitude, s.Latitude.Value, s.Longitude.Value),
                    PopularityScore = (s.Rating ?? 0) * s.Reviews.Count * s.Orders.Count / 1000m
                })
                .Where(s => s.DistanceKm <= radiusKm)
                .OrderBy(s => s.DistanceKm)
                .ThenByDescending(s => s.PopularityScore)
                .ToListAsync();

            return Ok(new
            {
                Success = true,
                TestLocation = new { Latitude = latitude, Longitude = longitude },
                RadiusKm = radiusKm,
                TotalStores = nearbyStores.Count,
                Stores = nearbyStores
            });
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
    }
} 