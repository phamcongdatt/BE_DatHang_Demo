using System;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatHang.Models
{
    public class Category
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Ten { get; set; } // Tên danh mục

        [StringLength(255)]
        public string MoTa { get; set; } // Mô tả

        public bool KichHoat { get; set; } = true; 

        public bool MacDinh { get; set; } = false; // Có phải mặc định không
    }
} 