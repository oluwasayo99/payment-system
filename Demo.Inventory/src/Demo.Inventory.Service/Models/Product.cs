namespace Demo.Inventory.Service.Models;

public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public Category? Category { get; set; }
    public List<Review> Reviews { get; set; } = new();
}
