using BuhtaApp.Common;
using BuhtaApp.Models;
using BuhtaApp.Services;
using BuhtaApp.Services.Networking;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace BuhtaApp.ViewModels;

public partial class LobbyViewModel : BaseViewModel
{
    private readonly IProfileService _profiles;
    private readonly IShiftCoordinator _coordinator;

    [ObservableProperty]
    private UserProfile? currentProfile;

    [ObservableProperty]
    private string hostIp = "";

    [ObservableProperty]
    private bool isHosting;

    [ObservableProperty]
    private string currentShiftName = "";

    [ObservableProperty]
    private string joinIpInput = "";

    public ObservableCollection<UserProfile> Profiles { get; } = new();

    public LobbyViewModel(IProfileService profiles, IShiftCoordinator coordinator)
    {
        _profiles = profiles;
        _coordinator = coordinator;
        Title = "Лобби";

        _coordinator.ShiftClosed += OnShiftClosed;
    }

    public async Task RefreshAsync()
    {
        await _profiles.EnsureLoadedAsync();

        Profiles.Clear();
        foreach (var p in _profiles.Profiles) Profiles.Add(p);

        CurrentProfile = _profiles.CurrentProfile ?? await _profiles.TryRestoreDefaultAsync();

        // Если смена уже открыта в этом процессе
        IsHosting = _coordinator.IsHost && _coordinator.CurrentShift is not null;
        CurrentShiftName = _coordinator.CurrentShift?.Name ?? "";
        HostIp = _coordinator.CurrentHostIp ?? "";

        if (IsHosting && _coordinator.CurrentShift is not null)
        {
            // Попробуем получить реальный IP хоста через хост-сервис
            var hostSvc = AppServices.Get<IShiftHost>();
            if (!string.IsNullOrEmpty(hostSvc.HostIp) && hostSvc.HostIp != "127.0.0.1")
                HostIp = hostSvc.HostIp;
        }
    }

    [RelayCommand]
    private async Task OpenShiftAsync()
    {
        if (CurrentProfile is null) return;

        var name = await Shell.Current.DisplayPromptAsync(
            "Открыть смену",
            "Название смены:",
            accept: "Открыть",
            cancel: "Отмена",
            placeholder: "Например, Вечер 28.10",
            maxLength: 60);

        if (string.IsNullOrWhiteSpace(name)) return;

        IsBusy = true;
        try
        {
            await _coordinator.OpenShiftAsync(name.Trim(), CurrentProfile);

            await RefreshAsync();
            await Shell.Current.GoToAsync("//shift");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Lobby] OpenShift failed: {ex.Message}");
            await Shell.Current.DisplayAlert("Ошибка", ex.Message, "OK");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task JoinShiftAsync()
    {
        if (CurrentProfile is null) return;
        if (string.IsNullOrWhiteSpace(JoinIpInput)) return;

        IsBusy = true;
        try
        {
            await _coordinator.JoinShiftAsync(JoinIpInput.Trim(), CurrentProfile);
            await Shell.Current.GoToAsync("//shift");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Lobby] JoinShift failed: {ex.Message}");
            await Shell.Current.DisplayAlert("Ошибка подключения",
                $"Не удалось подключиться к {JoinIpInput}.\n{ex.Message}", "OK");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ResumeShiftAsync()
    {
        if (_coordinator.CurrentShift is null) return;
        await Shell.Current.GoToAsync("//shift");
    }

    [RelayCommand]
    private async Task CloseShiftAsync()
    {
        var ok = await Shell.Current.DisplayAlert(
            "Закрыть смену?",
            "Все участники будут отключены, смена завершится.",
            "Закрыть", "Отмена");
        if (!ok) return;

        IsBusy = true;
        try
        {
            await _coordinator.CloseShiftAsync();
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Lobby] CloseShift failed: {ex.Message}");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SwitchProfileAsync(UserProfile profile)
    {
        if (profile is null) return;
        await _profiles.SetCurrentAsync(profile.Id);
        await _profiles.SetDefaultAsync(profile.Id);
        await RefreshAsync();
    }

    [RelayCommand]
    private async Task AddProfileAsync()
    {
        await Shell.Current.GoToAsync("//profile-editor");
    }

    [RelayCommand]
    private async Task CopyIpAsync()
    {
        if (string.IsNullOrWhiteSpace(HostIp)) return;
        await Clipboard.SetTextAsync(HostIp);
        await Shell.Current.DisplayAlert("IP скопирован", HostIp, "OK");
    }

    private void OnShiftClosed(object? sender, EventArgs e)
    {
        // Если смена закрылась (например, по таймауту) — вернём в лобби
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                if (Shell.Current.CurrentState.Location.OriginalString.Contains("shift"))
                    await Shell.Current.GoToAsync("//lobby");
                await RefreshAsync();
            }
            catch (Exception ex) { Debug.WriteLine($"[Lobby] OnShiftClosed nav: {ex.Message}"); }
        });
    }
}