namespace Demo.Inventory.Service.Models;

public class Order
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<OrderItem> OrderItems { get; set; } = new();
}
