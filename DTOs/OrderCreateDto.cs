public class OrderDto
{
    public Guid Id { get; set; }
    public Guid StoreId { get; set; }

    public Guid CustomerId { get; set; }
    public string StoreName { get; set; }
    public string DeliveryAddress { get; set; }
    public decimal? DeliveryLatitude { get; set; }
    public decimal? DeliveryLongitude { get; set; }
    public string PaymentMethod { get; set; }
    public string Status { get; set; }

    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItemDto> Items { get; set; }
}

public class OrderItemDto
{
    public Guid MenuId { get; set; }
    public string MenuName { get; set; }
    public int Quantity { get; set; }
    public string Note { get; set; }
    public decimal Price { get; set; }
}