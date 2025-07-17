using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatHang.DTOs
{
    public class OrderCreateDto
    {
        [Required]
        public Guid StoreId { get; set; }

        [Required]
        [StringLength(255)]
        public string DeliveryAddress { get; set; }

        public decimal? DeliveryLatitude { get; set; }
        public decimal? DeliveryLongitude { get; set; }

        [Required]
        public string PaymentMethod { get; set; } // Enum dưới dạng string: "Cash", "Momo", ...

        [Required]
        public List<OrderDetailCreateDto> Items { get; set; }
    }

    public class OrderDetailCreateDto
    {
        [Required]
        public Guid MenuId { get; set; }

        [Required]
        public int Quantity { get; set; }

        public string Note { get; set; }
    }
} 