using System;

namespace QuanLyDatHang.DTOs
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsLocked { get; set; }
    }
} 