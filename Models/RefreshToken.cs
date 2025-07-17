using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDatHang.Models
{
    public class RefreshToken
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        [Required]
        public string Token { get; set; }

        public DateTime ExpiresAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
} 