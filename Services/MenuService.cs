using System.Diagnostics;
using System.Text.Json;
using BuhtaApp.Common;
using BuhtaApp.Models;

namespace BuhtaApp.Services;

public class MenuService : IMenuService
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private MenuConfig? _cached;

    public MenuConfig? Current => _cached;

    public async Task<MenuConfig> LoadAsync()
    {
        if (_cached is not null) return _cached;

        await _lock.WaitAsync();
        try
        {
            if (_cached is not null) return _cached;

            await using var stream = await FileSystem.OpenAppPackageFileAsync(AppConstants.MenuBundleFile);
            var config = await JsonSerializer.DeserializeAsync<MenuConfig>(stream, AppJson.Options);

            _cached = config ?? new MenuConfig();
            return _cached;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Menu] Load failed: {ex.Message}");
            _cached = new MenuConfig();
            return _cached;
        }
        finally
        {
            _lock.Release();
        }
    }
}