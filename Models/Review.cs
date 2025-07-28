using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDatHang.Models
{
    public class Review
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual Order Order { get; set; } // Sử dụng bảng Order hiện có

        [Required]
        public Guid StoreId { get; set; }

        [ForeignKey("StoreId")]
        public virtual Store Store { get; set; } // Sử dụng bảng Store hiện có

        [Required]
        public Guid CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual User Customer { get; set; } // Sử dụng bảng User hiện có

        [Required]
        [Column(TypeName = "decimal(3, 2)")]
        public decimal Rating { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; }

        public Guid? MenuId { get; set; }

        [ForeignKey("MenuId")]
        public virtual Menu Menu { get; set; } // Sử dụng bảng Menu hiện có, nullable

        public string ImageUrls { get; set; }

        public bool IsApproved { get; set; } = true;
        public string Response { get; set; }
        public Guid? ResponderId { get; set; }

        [ForeignKey("ResponderId")]
        public virtual User Responder { get; set; } // Sử dụng bảng User cho người phản hồi

        public DateTime? ResponseDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    // Các bảng khác không cần định nghĩa lại, chỉ sử dụng quan hệ
    // Giả định các bảng sau đã tồn tại từ các chức năng khác:
    /*
    public class Order { ... } // Đã có
    public class OrderDetail { ... } // Đã có
    public class Menu { ... } // Đã có
    public class Store { ... } // Đã có
    public class User { ... } // Đã có
    */
}