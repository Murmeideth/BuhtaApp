using BuhtaApp.Common;
using BuhtaApp.Enums;
using BuhtaApp.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BuhtaApp.Services;

public partial class ProfileService : ObservableObject, IProfileService
{
    private readonly IStorageService _storage;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private DeviceSettings _settings = new();
    private bool _loaded;

    [ObservableProperty]
    private UserProfile? currentProfile;

    public ProfileService(IStorageService storage) => _storage = storage;

    public IReadOnlyList<UserProfile> Profiles => _settings.Profiles;
    public DeviceSettings Settings => _settings;

    public async Task EnsureLoadedAsync()
    {
        if (_loaded) return;

        await _lock.WaitAsync();
        try
        {
            if (_loaded) return;
            _settings = await _storage.LoadAsync<DeviceSettings>(AppConstants.DeviceSettingsFile) ?? new();
            _loaded = true;
        }
        finally { _lock.Release(); }
    }

    public async Task<UserProfile> CreateProfileAsync(string name, UserRole role, string avatar, string nameColor)
    {
        await EnsureLoadedAsync();
        var profile = new UserProfile
        {
            Name = name,
            Role = role,
            Avatar = string.IsNullOrWhiteSpace(avatar) ? AppConstants.DefaultAvatar : avatar,
            NameColor = string.IsNullOrWhiteSpace(nameColor) ? AppConstants.DefaultNameColor : nameColor
        };
        _settings.Profiles.Add(profile);
        _settings.LastModified = DateTime.UtcNow;
        await SaveAsync();
        return profile;
    }

    public async Task UpdateProfileAsync(UserProfile profile)
    {
        await EnsureLoadedAsync();
        var idx = _settings.Profiles.FindIndex(p => p.Id == profile.Id);
        if (idx < 0) return;

        _settings.Profiles[idx] = profile;
        _settings.LastModified = DateTime.UtcNow;
        await SaveAsync();

        if (CurrentProfile?.Id == profile.Id)
            CurrentProfile = profile;
    }

    public async Task DeleteProfileAsync(string profileId)
    {
        await EnsureLoadedAsync();
        _settings.Profiles.RemoveAll(p => p.Id == profileId);

        if (_settings.DefaultProfileId == profileId)
            _settings.DefaultProfileId = null;
        if (CurrentProfile?.Id == profileId)
            CurrentProfile = null;

        _settings.LastModified = DateTime.UtcNow;
        await SaveAsync();
    }

    public async Task SetDefaultAsync(string? profileId)
    {
        await EnsureLoadedAsync();
        _settings.DefaultProfileId = profileId;
        _settings.LastModified = DateTime.UtcNow;
        await SaveAsync();
    }

    public async Task SetCurrentAsync(string profileId)
    {
        await EnsureLoadedAsync();
        var profile = _settings.Profiles.FirstOrDefault(p => p.Id == profileId);
        if (profile is not null) CurrentProfile = profile;
    }

    public async Task<UserProfile?> TryRestoreDefaultAsync()
    {
        await EnsureLoadedAsync();
        if (_settings.DefaultProfileId is null) return null;

        var profile = _settings.Profiles.FirstOrDefault(p => p.Id == _settings.DefaultProfileId);
        if (profile is not null) CurrentProfile = profile;
        return profile;
    }

    private Task SaveAsync() => _storage.SaveAsync(AppConstants.DeviceSettingsFile, _settings);
}