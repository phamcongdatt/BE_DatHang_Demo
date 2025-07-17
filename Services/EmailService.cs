using System.Threading.Tasks;

namespace QuanLyDatHang.Services
{
    public class EmailService : IEmailService
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            // Logic for sending email will be implemented here.
            // For now, we can just log it to the console or a file.
            System.Console.WriteLine($"Email to {email}, Subject: {subject}, Message: {message}");
            return Task.CompletedTask;
        }
    }
} 