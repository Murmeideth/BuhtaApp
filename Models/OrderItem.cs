using BuhtaApp.Enums;

namespace BuhtaApp.Models;

public class OrderItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MenuItemId { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Quantity { get; set; } = 1;
    public bool IsDone { get; set; }
    public string? Comment { get; set; }
    public List<string> AppliedModifiers { get; set; } = new();
    public StationType Station { get; set; }

    public decimal Total => Price * Quantity;
}