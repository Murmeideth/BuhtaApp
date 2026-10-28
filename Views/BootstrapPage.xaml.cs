using System.Diagnostics;
using BuhtaApp.Services;

namespace BuhtaApp.Views;

public partial class BootstrapPage : ContentPage
{
    private readonly IProfileService _profileService;
    private readonly IMenuService _menuService;
    private bool _routed;

    public BootstrapPage(IProfileService profileService, IMenuService menuService)
    {
        InitializeComponent();
        _profileService = profileService;
        _menuService = menuService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_routed) return;
        _routed = true;

        Dispatcher.Dispatch(async () =>
        {
            try { await RouteAsync(); }
            catch (Exception ex) { Debug.WriteLine($"[Bootstrap] route failed: {ex.Message}"); }
        });
    }

    private async Task RouteAsync()
    {
        // Прогреваем меню в фоне — пригодится в лобби
        _ = _menuService.LoadAsync();

        // Дать Shell'у полностью инициализироваться
        await Task.Yield();

        var profile = await _profileService.TryRestoreDefaultAsync();
        var route = profile is null ? "//profile-editor" : "//lobby";
        await Shell.Current.GoToAsync(route);
    }
}