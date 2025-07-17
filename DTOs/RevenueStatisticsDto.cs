using System;
using System.Collections.Generic;

namespace QuanLyDatHang.DTOs
{
    public class RevenueStatisticsDto
    {
        public Guid StoreId { get; set; }
        public string StoreName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal CommissionRate { get; set; }
        public List<DailyRevenueDto> DailyStats { get; set; } = new List<DailyRevenueDto>();
        public List<OrderRevenueDto> TopOrders { get; set; } = new List<OrderRevenueDto>();
    }

    public class DailyRevenueDto
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Commission { get; set; }
        public decimal NetRevenue { get; set; }
    }

    public class OrderRevenueDto
    {
        public Guid OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal OrderAmount { get; set; }
        public decimal Commission { get; set; }
        public decimal NetRevenue { get; set; }
        public string CustomerName { get; set; }
    }

    public class RevenueFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Period { get; set; } = "month"; // tinh theo ngay thang nam 
    }
} 