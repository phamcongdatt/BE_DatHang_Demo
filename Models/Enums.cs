namespace QuanLyDatHang.Models
{
    public enum Role
    {
        Customer,
        Seller,
        Admin
    }

    public enum StoreStatus
    {
        Pending,
        Approved,
        Rejected,
        Closed
    }

    public enum MenuStatus
    {
        Available,
        Unavailable
    }

    public enum OrderStatus
    {
        Pending,
        Confirmed,
        Preparing,
        Delivering,
        Completed,
        Cancelled,
        Rejected
    }

    public enum PaymentMethod
    {
        COD,
        Online
    }

    public enum PaymentStatus
    {
        Pending,
        Completed,
        Failed
    }

    // Hệ thống đánh giá nâng cao
    public enum ReviewStatus
    {
        Pending,    // Chờ duyệt
        Approved,   // Đã duyệt
        Rejected,   // Từ chối
        Hidden      // Bị ẩn
    }

    public enum ReportReason
    {
        Spam,           // Spam
        Inappropriate,  // Nội dung không phù hợp
        Fake,           // Đánh giá giả
        Harassment,     // Quấy rối
        Other           // Lý do khác
    }

    public enum ReportStatus
    {
        Pending,    // Chờ xử lý
        Investigating, // Đang điều tra
        Resolved,   // Đã xử lý
        Dismissed   // Bỏ qua
    }
} 