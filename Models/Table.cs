using BuhtaApp.Enums;

namespace BuhtaApp.Models;

public class Table
{
    public int Number { get; set; }
    public string Name { get; set; } = "";
    public TableType Type { get; set; }
    public List<Order> Orders { get; set; } = new();

    public bool IsOccupied => Orders.Any(o => o.Status == OrderStatus.Open);
    public int NextOrderNumber => Orders.Count == 0 ? 1 : Orders.Max(o => o.Number) + 1;
}