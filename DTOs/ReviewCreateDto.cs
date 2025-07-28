using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatHang.DTOs
{
    public class ReviewCreateDto
    {
        [Required]
        public Guid OrderId { get; set; }

        [Required]
        public Guid StoreId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5")]
        public decimal Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Bình luận không được quá 1000 ký tự")]
        public string Comment { get; set; }

        public Guid? MenuId { get; set; } 
        public List<string> ImageUrls { get; set; } = new List<string>();
    }

    public class ReviewUpdateDto
    {
        [StringLength(1000, ErrorMessage = "Bình luận không được quá 1000 ký tự")]
        public string Comment { get; set; }

        [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5")]
        public decimal? Rating { get; set; }

        public Guid? MenuId { get; set; } // Nullable
        public List<string> ImageUrls { get; set; } = new List<string>();
    }

    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid StoreId { get; set; }
        public Guid? MenuId { get; set; }
        public string StoreName { get; set; }
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; }
        public decimal Rating { get; set; }
        public string Comment { get; set; }
        public List<string> ImageUrls { get; set; }
        public string Response { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ReviewStatisticsDto
    {
        public decimal AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new Dictionary<int, int>();
    }
}