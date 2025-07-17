using System;

namespace QuanLyDatHang.DTOs
{
    public class StoreDto
    {
        public Guid Id { get; set; }
        public Guid SellerId { get; set; }
        public string SellerName { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string Status { get; set; }
        public decimal? Rating { get; set; }
        public DateTime CreatedAt { get; set; }
    }
} 