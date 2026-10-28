using BuhtaApp.Enums;

namespace BuhtaApp.Models;

public class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public int Number { get; set; }
    public int TableNumber { get; set; }

    public string CreatedByProfileId { get; set; } = "";
    public string CreatedByName { get; set; } = "";
    public string CreatedByAvatar { get; set; } = "👤";
    public string CreatedByNameColor { get; set; } = "#E96A6A";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Open;
    public List<OrderItem> Items { get; set; } = new();

    public decimal Total => Items.Sum(i => i.Total);
    public bool IsFullyDone => Items.Count > 0 && Items.All(i => i.IsDone);
}