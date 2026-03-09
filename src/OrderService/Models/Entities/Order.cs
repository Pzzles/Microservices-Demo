using OrderService.Models.Enums;

namespace OrderService.Models.Entities
{
    /// <summary>
    /// ProductName and UnitPrice are intentional snapshots — they capture the product state at order time and are not affected by future product updates. This is the snapshot pattern.
    /// </summary>
    public class Order
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public DateTime CreatedAt { get; set; }
    }
}
