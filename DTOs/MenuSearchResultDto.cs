using System;

namespace QuanLyDatHang.DTOs
{
    public class MenuSearchResultDto
    {
        public Guid Id { get; set; }
        public Guid StoreId { get; set; }
        public string StoreName { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; }
        public DateTime CreatedAt { get; set; }
        public decimal? StoreRating { get; set; } // Đánh giá của quán
        public int StoreReviewCount { get; set; } // Số đánh giá của quán
        public int OrderCount { get; set; } // Số lượt đặt món này
        public decimal PopularityScore { get; set; } // Điểm phổ biến (storeRating * orderCount)
        public DateTime? UpdatedAt { get; internal set; }
        public string Status { get; set; } // Trạng thái sản phẩm (Available/Unavailable)
    }
} 