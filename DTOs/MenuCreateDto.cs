using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatHang.DTOs
{
    public class MenuCreateDto
    {
        [Required]
        public Guid StoreId { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        [Required]
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        [Required]
        public Guid CategoryId { get; set; }
    }
} 