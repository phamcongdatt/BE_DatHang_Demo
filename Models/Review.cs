using System;
using System.Collections.Generic;
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
        public virtual Order Order { get; set; }

        [Required]
        public Guid StoreId { get; set; }

        [ForeignKey("StoreId")]
        public virtual Store Store { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual User Customer { get; set; }

        [Required]
        [Column(TypeName = "decimal(3, 2)")]
        public decimal Rating { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; }

        // Hệ thống đánh giá theo tiêu chí
        [Column(TypeName = "decimal(3, 2)")]
        public decimal TasteRating { get; set; } // Vị giác

        [Column(TypeName = "decimal(3, 2)")]
        public decimal ServiceRating { get; set; } // Phục vụ

        [Column(TypeName = "decimal(3, 2)")]
        public decimal PriceRating { get; set; } // Giá cả

        [Column(TypeName = "decimal(3, 2)")]
        public decimal QualityRating { get; set; } // Chất lượng

        // Hình ảnh đánh giá
        public string ImageUrls { get; set; } // JSON array of image URLs

        // Trạng thái đánh giá
        public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

        // Đánh giá ẩn danh
        public bool IsAnonymous { get; set; } = false;

        // Đánh giá bị ẩn
        public bool IsHidden { get; set; } = false;

        // Lý do ẩn đánh giá
        [StringLength(500)]
        public string HideReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<ReviewImage> ReviewImages { get; set; }
        public virtual ReviewResponse ReviewResponse { get; set; }
        public virtual ICollection<ReviewReport> ReviewReports { get; set; }
    }

    public class ReviewImage
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ReviewId { get; set; }

        [ForeignKey("ReviewId")]
         public virtual Review Review { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; }

        [StringLength(200)]
        public string Caption { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ReviewResponse
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ReviewId { get; set; }

        [ForeignKey("ReviewId")]
        public virtual Review Review { get; set; }

        [Required]
        public Guid StoreOwnerId { get; set; }

        [ForeignKey("StoreOwnerId")]
        public virtual User StoreOwner { get; set; }

        [Required]
        [StringLength(1000)]
        public string Response { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }

    public class ReviewReport
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid ReviewId { get; set; }

        [ForeignKey("ReviewId")]
        public virtual Review Review { get; set; }

        [Required]
        public Guid ReporterId { get; set; }

        [ForeignKey("ReporterId")]
        public virtual User Reporter { get; set; }

        [Required]
        public ReportReason Reason { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public ReportStatus Status { get; set; } = ReportStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }

        public Guid? ResolvedBy { get; set; }

        [ForeignKey("ResolvedBy")]
        public virtual User ResolvedByUser { get; set; }
    }
} 


