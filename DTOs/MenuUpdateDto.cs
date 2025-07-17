using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatHang.DTOs
{
    public class MenuUpdateDto
    {
        [StringLength(100)]
        public string Name { get; set; }
        public decimal? Price { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public Guid? CategoryId { get; set; }
        public string Status { get; set; }
    }
} 