using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace QuanLyDatHang.DTOs
{
    public class AddToCartDto
    {
        [Required]
        public Guid MenuId { get; set; }

        [Required]
        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100")]
        public int Quantity { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được quá 500 ký tự")]
        public string Note { get; set; }
    }

    public class UpdateCartItemDto
    {
        [Required]
        [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100")]
        public int Quantity { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được quá 500 ký tự")]
        public string Note { get; set; }
    }

    public class CartItemDto
    {
        public Guid Id { get; set; }
        public Guid MenuId { get; set; }
        public string MenuName { get; set; }
        public string MenuImage { get; set; }
        public decimal MenuPrice { get; set; }
        public int Quantity { get; set; }
        public string Note { get; set; }
        public decimal SubTotal { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CartDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public int TotalItems { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class AddToWishlistDto
    {
        [Required]
        public Guid MenuId { get; set; }
    }

    public class WishlistItemDto
    {
        public Guid Id { get; set; }
        public Guid MenuId { get; set; }
        public string MenuName { get; set; }
        public string MenuImage { get; set; }
        public decimal MenuPrice { get; set; }
        public string StoreName { get; set; }
        public Guid StoreId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WishlistDto
    {
        public List<WishlistItemDto> Items { get; set; } = new List<WishlistItemDto>();
        public int TotalItems { get; set; }
    }

    public class CheckoutCartDto
    {
        [Required]
        public string DeliveryAddress { get; set; }
        public decimal? DeliveryLatitude { get; set; }
        public decimal? DeliveryLongitude { get; set; }
        [Required]
        public string PaymentMethod { get; set; } // "COD" hoặc "VNPAY"
        public string Note { get; set; }
    }
} 