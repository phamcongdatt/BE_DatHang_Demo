using QuanLyDatHang.Models;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatHang.DTOs
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; }

        [Required]
        public string FullName { get; set; }

        public string PhoneNumber { get; set; }

        [Required]
        public Role Role { get; set; }
    }
} 