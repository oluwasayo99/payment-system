namespace Demo.Inventory.Service.Dtos;

public class UserWithOrdersDto
{
    public string Username { get; set; } = string.Empty;
    public List<OrderDto> Orders { get; set; } = new();
}

public class OrderDto
{
    public string OrderId { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public List<ProductInOrderDto> Products { get; set; } = new();
    public decimal Total { get; set; }
}

public class ProductInOrderDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
}
