using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDatHang.Models
{
    public class Order
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual User Customer { get; set; }

        [Required]
        public Guid StoreId { get; set; }

        [ForeignKey("StoreId")]
        public virtual Store Store { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalPrice { get; set; }

        [Required]
        [StringLength(255)]
        public string DeliveryAddress { get; set; }

        [Column(TypeName = "decimal(9, 6)")]
        public decimal? DeliveryLatitude { get; set; }

        [Column(TypeName = "decimal(9, 6)")]
        public decimal? DeliveryLongitude { get; set; }

        [Required]
        public OrderStatus Status { get; set; }

        [Required]
        public PaymentMethod PaymentMethod { get; set; }

        [Required]
        public PaymentStatus PaymentStatus { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public virtual Review Review { get; set; }
    }
} 