using Microsoft.EntityFrameworkCore; // Sử dụng EF Core để làm việc với cơ sở dữ liệu
using QuanLyDatHang.Data; // Chứa context DB (ApplicationDbContext)
using QuanLyDatHang.Models; // Chứa các model như User, Order, OrderStatus
using System; // Cung cấp các lớp cơ bản như DateTime
using System.Collections.Generic; // Sử dụng List và các collection
using System.Linq; // Sử dụng LINQ để truy vấn dữ liệu
using System.Threading.Tasks; // Hỗ trợ các phương thức bất đồng bộ (async/await)

namespace QuanLyDatHang.Services
{
    public interface IRevenueStatisticsService
    {
        Task<object> GetRevenueOverviewAsync(Guid storeId, string period); // Lấy tổng quan doanh thu theo kỳ
        Task<List<object>> GetDailyRevenueAsync(Guid storeId, string period); // Lấy doanh thu theo ngày
        Task<List<object>> GetTopOrdersAsync(Guid storeId, int take, string period); // Lấy top đơn hàng theo giá trị
        Task<List<object>> GetRevenueByCategoryAsync(Guid storeId, string period); // Lấy doanh thu theo danh mục
        Task<List<object>> GetRevenueByPaymentMethodAsync(Guid storeId, string period); // Lấy doanh thu theo phương thức thanh toán
        Task<object> GetDetailedReportAsync(Guid storeId, DateTime? startDate, DateTime? endDate); // Lấy báo cáo chi tiết
    }

    public class RevenueStatisticsService : IRevenueStatisticsService
    {
        private readonly ApplicationDbContext _context; // Context DB để truy vấn dữ liệu
        private readonly ICommissionService _commissionService; // Dịch vụ tính hoa hồng

        public RevenueStatisticsService(ApplicationDbContext context, ICommissionService commissionService)
        {
            _context = context; // Gán context DB
            _commissionService = commissionService; // Gán dịch vụ hoa hồng
        }

        public async Task<object> GetRevenueOverviewAsync(Guid storeId, string period)
        {
            var (startDate, endDate) = GetDateRange(period); // Lấy khoảng thời gian dựa trên kỳ
            var commissionRate = _commissionService.GetCommissionRate(); // Lấy tỷ lệ hoa hồng

            var completedOrders = await _context.Orders
                .Where(o => o.StoreId == storeId && // Lọc theo ID cửa hàng
                           o.Status == OrderStatus.Completed && // Chỉ lấy đơn hàng hoàn thành
                           o.CreatedAt >= startDate && // Lọc theo ngày bắt đầu
                           o.CreatedAt <= endDate) // Lọc theo ngày kết thúc
                .Include(o => o.Customer) // Bao gồm thông tin khách hàng
                .ToListAsync(); // Chuyển thành danh sách

            var totalRevenue = completedOrders.Sum(o => o.TotalPrice); // Tính tổng doanh thu
            var totalCommission = _commissionService.CalculateCommission(totalRevenue); // Tính tổng hoa hồng
            var netRevenue = _commissionService.CalculateNetRevenue(totalRevenue); // Tính doanh thu ròng

            return new
            {
                StoreId = storeId, // ID cửa hàng
                StartDate = startDate, // Ngày bắt đầu
                EndDate = endDate, // Ngày kết thúc
                TotalOrders = completedOrders.Count, // Số lượng đơn hàng
                TotalRevenue = totalRevenue, // Tổng doanh thu
                TotalCommission = totalCommission, // Tổng hoa hồng
                NetRevenue = netRevenue, // Doanh thu ròng
                CommissionRate = commissionRate, // Tỷ lệ hoa hồng
                AverageOrderValue = completedOrders.Any() ? totalRevenue / completedOrders.Count : 0 // Giá trị trung bình mỗi đơn (0 nếu không có đơn)
            };
        }

        public async Task<List<object>> GetDailyRevenueAsync(Guid storeId, string period)
        {
            var (startDate, endDate) = GetDateRange(period); // Lấy khoảng thời gian

            var dailyStats = await _context.Orders
                .Where(o => o.StoreId == storeId && // Lọc theo ID cửa hàng
                           o.Status == OrderStatus.Completed && // Chỉ lấy đơn hàng hoàn thành
                           o.CreatedAt >= startDate && // Lọc theo ngày bắt đầu
                           o.CreatedAt <= endDate) // Lọc theo ngày kết thúc
                .GroupBy(o => o.CreatedAt.Date) // Nhóm theo ngày
                .Select(g => new
                {
                    Date = g.Key, // Ngày
                    OrderCount = g.Count(), // Số lượng đơn hàng
                    Revenue = g.Sum(o => o.TotalPrice), // Tổng doanh thu
                    Commission = g.Sum(o => _commissionService.CalculateCommission(o.TotalPrice)), // Tổng hoa hồng
                    NetRevenue = g.Sum(o => _commissionService.CalculateNetRevenue(o.TotalPrice)) // Doanh thu ròng
                })
                .OrderBy(x => x.Date) // Sắp xếp theo ngày tăng dần
                .ToListAsync(); // Chuyển thành danh sách

            return dailyStats.Cast<object>().ToList(); // Chuyển sang List<object>
        }

        public async Task<List<object>> GetTopOrdersAsync(Guid storeId, int take, string period)
        {
            var (startDate, endDate) = GetDateRange(period); // Lấy khoảng thời gian

            var topOrders = await _context.Orders
                .Where(o => o.StoreId == storeId && // Lọc theo ID cửa hàng
                           o.Status == OrderStatus.Completed && // Chỉ lấy đơn hàng hoàn thành
                           o.CreatedAt >= startDate && // Lọc theo ngày bắt đầu
                           o.CreatedAt <= endDate) // Lọc theo ngày kết thúc
                .Include(o => o.Customer) // Bao gồm thông tin khách hàng
                .OrderByDescending(o => o.TotalPrice) // Sắp xếp giảm dần theo giá trị
                .Take(take) // Lấy số lượng top
                .Select(o => new
                {
                    OrderId = o.Id, // ID đơn hàng
                    OrderDate = o.CreatedAt, // Thời gian tạo
                    OrderAmount = o.TotalPrice, // Giá trị đơn hàng
                    Commission = _commissionService.CalculateCommission(o.TotalPrice), // Hoa hồng
                    NetRevenue = _commissionService.CalculateNetRevenue(o.TotalPrice), // Doanh thu ròng
                    CustomerName = o.Customer.FullName // Tên khách hàng
                })
                .ToListAsync(); // Chuyển thành danh sách

            return topOrders.Cast<object>().ToList(); // Chuyển sang List<object>
        }

        public async Task<List<object>> GetRevenueByCategoryAsync(Guid storeId, string period)
        {
            var (startDate, endDate) = GetDateRange(period); // Lấy khoảng thời gian

            var categoryStats = await _context.Orders
                .Where(o => o.StoreId == storeId && // Lọc theo ID cửa hàng
                           o.Status == OrderStatus.Completed && // Chỉ lấy đơn hàng hoàn thành
                           o.CreatedAt >= startDate && // Lọc theo ngày bắt đầu
                           o.CreatedAt <= endDate) // Lọc theo ngày kết thúc
                .Include(o => o.OrderDetails) // Bao gồm chi tiết đơn hàng
                .ThenInclude(od => od.Menu) // Bao gồm thông tin menu
                .SelectMany(o => o.OrderDetails.Select(od => new
                {
                    CategoryName = od.Menu.CategoryName, // Tên danh mục
                    Revenue = od.Price * od.Quantity, // Doanh thu của mục
                    Quantity = od.Quantity // Số lượng
                }))
                .GroupBy(x => x.CategoryName) // Nhóm theo danh mục
                .Select(g => new
                {
                    CategoryName = g.Key, // Tên danh mục
                    TotalRevenue = g.Sum(x => x.Revenue), // Tổng doanh thu
                    TotalQuantity = g.Sum(x => x.Quantity), // Tổng số lượng
                    Commission = g.Sum(x => _commissionService.CalculateCommission(x.Revenue)), // Tổng hoa hồng
                    NetRevenue = g.Sum(x => _commissionService.CalculateNetRevenue(x.Revenue)), // Doanh thu ròng
                    OrderCount = g.Count() // Số lượng đơn hàng
                })
                .OrderByDescending(x => x.TotalRevenue) // Sắp xếp giảm dần theo doanh thu
                .ToListAsync(); // Chuyển thành danh sách

            return categoryStats.Cast<object>().ToList(); // Chuyển sang List<object>
        }

        public async Task<List<object>> GetRevenueByPaymentMethodAsync(Guid storeId, string period)
        {
            var (startDate, endDate) = GetDateRange(period); // Lấy khoảng thời gian

            var paymentStats = await _context.Orders
                .Where(o => o.StoreId == storeId && // Lọc theo ID cửa hàng
                           o.Status == OrderStatus.Completed && // Chỉ lấy đơn hàng hoàn thành
                           o.CreatedAt >= startDate && // Lọc theo ngày bắt đầu
                           o.CreatedAt <= endDate) // Lọc theo ngày kết thúc
                .GroupBy(o => o.PaymentMethod) // Nhóm theo phương thức thanh toán
                .Select(g => new
                {
                    PaymentMethod = g.Key.ToString(), // Tên phương thức thanh toán
                    TotalRevenue = g.Sum(o => o.TotalPrice), // Tổng doanh thu
                    OrderCount = g.Count(), // Số lượng đơn hàng
                    Commission = g.Sum(o => _commissionService.CalculateCommission(o.TotalPrice)), // Tổng hoa hồng
                    NetRevenue = g.Sum(o => _commissionService.CalculateNetRevenue(o.TotalPrice)) // Doanh thu ròng
                })
                .OrderByDescending(x => x.TotalRevenue) // Sắp xếp giảm dần theo doanh thu
                .ToListAsync(); // Chuyển thành danh sách

            return paymentStats.Cast<object>().ToList(); // Chuyển sang List<object>
        }

        public async Task<object> GetDetailedReportAsync(Guid storeId, DateTime? startDate, DateTime? endDate)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30); // Ngày bắt đầu (mặc định 30 ngày trước nếu null)
            var end = endDate ?? DateTime.UtcNow; // Ngày kết thúc (mặc định hiện tại nếu null)

            var orders = await _context.Orders
                .Where(o => o.StoreId == storeId && // Lọc theo ID cửa hàng
                           o.Status == OrderStatus.Completed && // Chỉ lấy đơn hàng hoàn thành
                           o.CreatedAt >= start && // Lọc theo ngày bắt đầu
                           o.CreatedAt <= end) // Lọc theo ngày kết thúc
                .Include(o => o.Customer) // Bao gồm thông tin khách hàng
                .Include(o => o.OrderDetails) // Bao gồm chi tiết đơn hàng
                .ThenInclude(od => od.Menu) // Bao gồm thông tin menu
                .OrderByDescending(o => o.CreatedAt) // Sắp xếp giảm dần theo thời gian
                .ToListAsync(); // Chuyển thành danh sách

            var totalRevenue = orders.Sum(o => o.TotalPrice); // Tính tổng doanh thu
            var totalCommission = _commissionService.CalculateCommission(totalRevenue); // Tính tổng hoa hồng
            var netRevenue = _commissionService.CalculateNetRevenue(totalRevenue); // Tính doanh thu ròng

            return new
            {
                StoreId = storeId, // ID cửa hàng
                StartDate = start, // Ngày bắt đầu
                EndDate = end, // Ngày kết thúc
                Summary = new
                {
                    TotalOrders = orders.Count, // Số lượng đơn hàng
                    TotalRevenue = totalRevenue, // Tổng doanh thu
                    TotalCommission = totalCommission, // Tổng hoa hồng
                    NetRevenue = netRevenue, // Doanh thu ròng
                    CommissionRate = _commissionService.GetCommissionRate(), // Tỷ lệ hoa hồng
                    AverageOrderValue = orders.Any() ? totalRevenue / orders.Count : 0 // Giá trị trung bình mỗi đơn (0 nếu không có đơn)
                },
                Orders = orders.Select(o => new
                {
                    o.Id, // ID đơn hàng
                    o.CreatedAt, // Thời gian tạo
                    o.TotalPrice, // Giá trị đơn hàng
                    Commission = _commissionService.CalculateCommission(o.TotalPrice), // Hoa hồng
                    NetRevenue = _commissionService.CalculateNetRevenue(o.TotalPrice), // Doanh thu ròng
                    CustomerName = o.Customer.FullName, // Tên khách hàng
                    Items = o.OrderDetails.Select(od => new
                    {
                        od.Menu.Name, // Tên món
                        od.Quantity, // Số lượng
                        od.Price, // Giá
                        SubTotal = od.Price * od.Quantity // Thành tiền
                    })
                })
            };
        }

        private (DateTime startDate, DateTime endDate) GetDateRange(string period)
        {
            var now = DateTime.UtcNow; // Lấy thời gian hiện tại (UTC)
            DateTime startDate, endDate; // Khai báo biến ngày bắt đầu và kết thúc

            switch (period.ToLower()) // Xử lý kỳ thống kê (không phân biệt hoa/thường)
            {
                case "day": // Kỳ là 1 ngày
                    startDate = now.Date; // Bắt đầu từ 00:00 ngày hiện tại
                    endDate = now.Date.AddDays(1).AddSeconds(-1); // Kết thúc lúc 23:59:59
                    break;
                case "week": // Kỳ là 1 tuần
                    startDate = now.Date.AddDays(-(int)now.DayOfWeek); // Bắt đầu từ Chủ nhật
                    endDate = startDate.AddDays(7).AddSeconds(-1); // Kết thúc sau 7 ngày
                    break;
                case "month": // Kỳ là 1 tháng
                    startDate = new DateTime(now.Year, now.Month, 1); // Bắt đầu từ ngày 1
                    endDate = startDate.AddMonths(1).AddSeconds(-1); // Kết thúc cuối tháng
                    break;
                case "year": // Kỳ là 1 năm
                    startDate = new DateTime(now.Year, 1, 1); // Bắt đầu từ 1/1
                    endDate = startDate.AddYears(1).AddSeconds(-1); // Kết thúc 31/12
                    break;
                default: // Mặc định là 30 ngày
                    startDate = now.Date.AddDays(-30); // Bắt đầu 30 ngày trước
                    endDate = now; // Kết thúc hiện tại
                    break;
            }

            return (startDate, endDate); // Trả về tuple chứa khoảng thời gian
        }
    }
}