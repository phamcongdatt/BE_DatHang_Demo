using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDatHang.Models
{
    public class Store
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid SellerId { get; set; }

        [ForeignKey("SellerId")]
        public virtual User Seller { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [StringLength(255)]
        public string Address { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "decimal(9, 6)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(9, 6)")]
        public decimal? Longitude { get; set; }

        [Required]
        public StoreStatus Status { get; set; }

        [Column(TypeName = "decimal(3, 2)")]
        public decimal? Rating { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        public Guid? CategoryId { get; set; }
        [StringLength(100)]
        public string? CategoryName { get; set; }
        public virtual Category Category { get; set; }


        // Navigation properties
        public virtual ICollection<Menu> Menus { get; set; } = new List<Menu>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
} 