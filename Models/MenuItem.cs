using BuhtaApp.Enums;

namespace BuhtaApp.Models;

public class MenuItem
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public string? Portion { get; set; }
    public bool IsStopListed { get; set; }
    public List<string> Modifiers { get; set; } = new();
}

public class MenuCategory
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
    public StationType TargetStation { get; set; }
    public List<MenuItem> Items { get; set; } = new();
}

public class MenuConfig
{
    public List<MenuCategory> Categories { get; set; } = new();
    public List<TableConfig> Tables { get; set; } = new();
}

public class TableConfig
{
    public int Number { get; set; }
    public string Name { get; set; } = "";
    public TableType Type { get; set; }
}