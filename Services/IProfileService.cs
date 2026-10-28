using BuhtaApp.Models;
using BuhtaApp.Enums;

namespace BuhtaApp.Services;

public interface IProfileService
{
    IReadOnlyList<UserProfile> Profiles { get; }
    DeviceSettings Settings { get; }
    UserProfile? CurrentProfile { get; }

    Task EnsureLoadedAsync();
    Task<UserProfile> CreateProfileAsync(string name, UserRole role, string avatar, string nameColor);
    Task UpdateProfileAsync(UserProfile profile);
    Task DeleteProfileAsync(string profileId);
    Task SetDefaultAsync(string? profileId);
    Task SetCurrentAsync(string profileId);
    Task<UserProfile?> TryRestoreDefaultAsync();
}