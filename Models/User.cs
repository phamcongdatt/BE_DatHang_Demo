using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatHang.Models
{
    public class User
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [StringLength(20)]
        public string PhoneNumber { get; set; }

        [Required]
        public Role Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsLocked { get; set; } = false;

        // Navigation properties
        public virtual ICollection<Store> Stores { get; set; } = new List<Store>();
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
} 