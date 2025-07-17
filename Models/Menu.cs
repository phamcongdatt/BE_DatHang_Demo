using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDatHang.Models
{
    public class Menu
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid StoreId { get; set; }

        [ForeignKey("StoreId")]
        public virtual Store Store { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }


        public string Description { get; set; }

        [StringLength(255)]
        public string ImageUrl { get; set; }

        [Required]
        public MenuStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public Guid CategoryId { get; set; }
        [StringLength(100)]
        public string CategoryName { get; set; }
        public virtual Category Category { get; set; }

        // Navigation properties
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
} 