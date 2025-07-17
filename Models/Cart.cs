using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDatHang.Models
{
    public class Cart
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual User Customer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }

    public class CartItem
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CartId { get; set; }

        [ForeignKey("CartId")]
        public virtual Cart Cart { get; set; }

        [Required]
        public Guid MenuId { get; set; }

        [ForeignKey("MenuId")]
        public virtual Menu Menu { get; set; }

        [Required]
        public int Quantity { get; set; }

        [StringLength(500)]
        public string Note { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class Wishlist
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual User Customer { get; set; }

        [Required]
        public Guid MenuId { get; set; }

        [ForeignKey("MenuId")]
        public virtual Menu Menu { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 