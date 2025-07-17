using System;

namespace QuanLyDatHang.DTOs
{
    public class MenuDto
    {
        public Guid Id { get; set; }
        public Guid StoreId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
} 