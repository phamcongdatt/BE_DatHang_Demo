using System.ComponentModel.DataAnnotations;

namespace QuanLyDatHang.DTOs
{
    public class UpdateProfileDto
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên không được quá 100 ký tự")]
        public string FullName { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [StringLength(20, ErrorMessage = "Số điện thoại không được quá 20 ký tự")]
        public string? PhoneNumber { get; set; }
    }
} 