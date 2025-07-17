using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDatHang.Models
{
    public class OrderDetail
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; }

        [Required]
        public Guid MenuId { get; set; }

        [ForeignKey("MenuId")]
        public virtual Menu Menu { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [StringLength(255)]
        public string Note { get; set; }
    }
} 