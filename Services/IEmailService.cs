using System.Threading.Tasks;

namespace QuanLyDatHang.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string email, string subject, string message);
    }
} 