using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace QuanLyDatHang.Hubs
{
    public class OrderHub : Hub
    {
        // Gửi thông báo đơn hàng mới tới chủ quán
        public async Task NotifyNewOrder(string sellerId, object order)
        {
            await Clients.User(sellerId).SendAsync("ReceiveNewOrder", order);
        }

        // Gửi thông báo trạng thái đơn hàng tới khách hàng
        public async Task NotifyOrderStatusChanged(string customerId, object orderStatus)
        {
            await Clients.User(customerId).SendAsync("ReceiveOrderStatus", orderStatus);
        }
    }
} 