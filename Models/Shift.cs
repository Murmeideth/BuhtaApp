using BuhtaApp.Enums;

namespace BuhtaApp.Models;

public class Shift
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public string HostProfileId { get; set; } = "";
    public string HostName { get; set; } = "";
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Open;
    public List<ShiftMember> Members { get; set; } = new();
    public List<Table> Tables { get; set; } = new();
}

public class ShiftMember
{
    public string ProfileId { get; set; } = "";
    public string Name { get; set; } = "";
    public UserRole Role { get; set; }
    public string Avatar { get; set; } = "👤";
    public string NameColor { get; set; } = "#E96A6A";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsHost { get; set; }
}

public class ShiftHistoryEntry
{
    public string ShiftId { get; set; } = "";
    public string Name { get; set; } = "";
    public string HostName { get; set; } = "";
    public string HostProfileId { get; set; } = "";
    public string MyProfileId { get; set; } = "";
    public bool IWasHost { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public decimal TotalRevenue { get; set; }
    public int OrdersCount { get; set; }
}