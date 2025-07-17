using System;
using System.Threading.Tasks;

namespace QuanLyDatHang.Services
{
    public interface ICommissionService
    {
        decimal GetCommissionRate();
        decimal CalculateCommission(decimal orderAmount);
        decimal CalculateNetRevenue(decimal orderAmount);
    }

    public class CommissionService : ICommissionService
    {
        // Cấu hình phí hoa hồng (có thể lưu trong database hoặc config)
        private const decimal COMMISSION_RATE = 0.05m; // 5% như Shopee, GrabFood
        
        public decimal GetCommissionRate()
        {
            return COMMISSION_RATE;
        }

        public decimal CalculateCommission(decimal orderAmount)
        {
            return Math.Round(orderAmount * COMMISSION_RATE, 2);
        }

        public decimal CalculateNetRevenue(decimal orderAmount)
        {
            return Math.Round(orderAmount * (1 - COMMISSION_RATE), 2);
        }
    }
}   