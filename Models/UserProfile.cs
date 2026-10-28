using BuhtaApp.Enums;

namespace BuhtaApp.Models;

public class UserProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";
    public UserRole Role { get; set; }
    public string Avatar { get; set; } = "👤";
    public string NameColor { get; set; } = "#E96A6A";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class DeviceSettings
{
    public List<UserProfile> Profiles { get; set; } = new();
    public string? DefaultProfileId { get; set; }
    public string? LastConnectedShiftId { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}