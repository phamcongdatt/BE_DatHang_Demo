using System;

namespace QuanLyDatHang.DTOs
{
    public class StoreSearchResultDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? Rating { get; set; }
        public int ReviewCount { get; set; }
        public int OrderCount { get; set; }
        public Guid? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string SellerName { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal? DistanceKm { get; set; } // Khoảng cách từ vị trí tìm kiếm
        public decimal PopularityScore { get; set; } // Điểm phổ biến (rating * reviewCount * orderCount)
    }
} 