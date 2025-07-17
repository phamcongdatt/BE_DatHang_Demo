using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using QuanLyDatHang.Models;

namespace QuanLyDatHang.DTOs
{
    public class ReviewCreateDto
    {
        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid StoreId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Diem danh gia  phải từ 1 đến 5")]
        public decimal Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Binh Luan không được quá 1000 ký tự")]
        public string Comment { get; set; }

        // Đánh giá theo tiêu chí
        [Range(1, 5, ErrorMessage = "Diem danh gia phải từ 1 đến 5")]
        public decimal TasteRating { get; set; }

        [Range(1, 5, ErrorMessage = "diem danh gia phải từ 1 đến 5")]
        public decimal ServiceRating { get; set; }

        [Range(1, 5, ErrorMessage = "diem danh gia  phải từ 1 đến 5")]
        public decimal PriceRating { get; set; }

        [Range(1, 5, ErrorMessage = "QualityRating phải từ 1 đến 5")]
        public decimal QualityRating { get; set; }

        // Hình ảnh đánh giá
        public List<string> ImageUrls { get; set; } = new List<string>();

        // Đánh giá ẩn danh
        public bool IsAnonymous { get; set; } = false;
    }

    public class ReviewUpdateDto
    {
        [StringLength(1000, ErrorMessage = "Comment không được quá 1000 ký tự")]
        public string Comment { get; set; }

        [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5")]
        public decimal? Rating { get; set; }

        [Range(1, 5, ErrorMessage = "TasteRating phải từ 1 đến 5")]
        public decimal? TasteRating { get; set; }

        [Range(1, 5, ErrorMessage = "ServiceRating phải từ 1 đến 5")]
        public decimal? ServiceRating { get; set; }

        [Range(1, 5, ErrorMessage = "PriceRating phải từ 1 đến 5")]
        public decimal? PriceRating { get; set; }

        [Range(1, 5, ErrorMessage = "QualityRating phải từ 1 đến 5")]
        public decimal? QualityRating { get; set; }

        public List<string> ImageUrls { get; set; } = new List<string>();
    }

    public class ReviewResponseDto
    {
        [Required]
        [StringLength(1000, ErrorMessage = "Response không được quá 1000 ký tự")]
        public string Response { get; set; }
    }

    public class ReviewReportDto
    {
        [Required]
        public ReportReason Reason { get; set; }

        [StringLength(500, ErrorMessage = "Mo ta không được quá 500 ký tự")]
        public string Description { get; set; }
    }

    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid StoreId { get; set; }
        public string StoreName { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAvatar { get; set; }
        public decimal Rating { get; set; }
        public decimal TasteRating { get; set; }
        public decimal ServiceRating { get; set; }
        public decimal PriceRating { get; set; }
        public decimal QualityRating { get; set; }
        public string Comment { get; set; }
        public List<string> ImageUrls { get; set; }
        public ReviewStatus Status { get; set; }
        public bool IsAnonymous { get; set; }
        public bool IsHidden { get; set; }
        public string HideReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ReviewResponseDto Response { get; set; }
        public int ReportCount { get; set; }
        public bool CanReport { get; set; }
        public bool CanRespond { get; set; }
    }

    public class ReviewStatisticsDto
    {
        public decimal AverageRating { get; set; }
        public decimal AverageTasteRating { get; set; }
        public decimal AverageServiceRating { get; set; }
        public decimal AveragePriceRating { get; set; }
        public decimal AverageQualityRating { get; set; }
        public int TotalReviews { get; set; }
        public int PendingReviews { get; set; }
        public int ApprovedReviews { get; set; }
        public int HiddenReviews { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new Dictionary<int, int>();
    }
} 

