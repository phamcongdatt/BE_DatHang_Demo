using System;

namespace QuanLyDatHang.DTOs
{
    public class StoreSearchFilterDto
    {
        public string? SearchTerm { get; set; } // Tìm theo tên quán, địa chỉ
        public Guid? CategoryId { get; set; } // Lọc theo danh mục
        public decimal? MinRating { get; set; } // Đánh giá tối thiểu
        public decimal? MaxRating { get; set; } // Đánh giá tối đa
        public decimal? Latitude { get; set; } // Vị trí hiện tại
        public decimal? Longitude { get; set; }
        public decimal? RadiusKm { get; set; } // Bán kính tìm kiếm (km)
        public string? SortBy { get; set; } // rating, distance, name, orderCount
        public string? SortOrder { get; set; } // asc, desc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class MenuSearchFilterDto                  
    {
        public string? SearchTerm { get; set; } // Tìm theo tên món
        public Guid? CategoryId { get; set; } // Lọc theo danh mục
        public Guid? StoreId { get; set; } // Lọc theo quán
        public decimal? MinPrice { get; set; } // Giá tối thiểu
        public decimal? MaxPrice { get; set; } // Giá tối đa
        public decimal? MinRating { get; set; } // Đánh giá tối thiểu của quán
        public string? SortBy { get; set; } // price, rating, popularity, name
        public string? SortOrder { get; set; } // asc, desc
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
    }
} 