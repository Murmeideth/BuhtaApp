using BuhtaApp.Services.Networking;
using BuhtaApp.Views;

namespace BuhtaApp.Views;

public partial class ShiftPage : ContentPage
{
    private readonly IShiftCoordinator _coordinator;

    public ShiftPage(IShiftCoordinator coordinator)
    {
        InitializeComponent();
        _coordinator = coordinator;

        _coordinator.ShiftOpened += OnShiftOpened;
        _coordinator.ShiftClosed += OnShiftClosed;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Если данные уже есть — покажем сразу
        if (_coordinator.CurrentShift is not null)
            UpdateFromShift();

        UpdateStatus();
    }

    private void OnShiftOpened(object? sender, BuhtaApp.Models.Shift shift)
        => MainThread.BeginInvokeOnMainThread(UpdateFromShift);

    private void OnShiftClosed(object? sender, EventArgs e)
        => MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (Shell.Current.CurrentState.Location.OriginalString.Contains("shift"))
                await Shell.Current.GoToAsync("//lobby");
        });

    private void UpdateFromShift()
    {
        var shift = _coordinator.CurrentShift;
        if (shift is null) return;

        ShiftNameLabel.Text = shift.Name;
        MembersView.ItemsSource = shift.Members;

        UpdateStatus();
    }

    private void UpdateStatus()
    {
        var role = _coordinator.CurrentProfile?.Role.ToString() ?? "—";
        var host = _coordinator.IsHost ? "хост" : "гость";
        ShiftStatusLabel.Text = $"{host} · {role} · {(_coordinator.IsConnected ? "🟢 онлайн" : "🟠 оффлайн")}";
    }

    private async void OnLeaveClicked(object? sender, EventArgs e)
    {
        var ok = await DisplayAlert("Выйти?", _coordinator.IsHost
            ? "Вы закроете смену для всех участников."
            : "Отключиться от смены?", "Выйти", "Отмена");
        if (!ok) return;

        if (_coordinator.IsHost)
            await _coordinator.CloseShiftAsync();
        else
            await _coordinator.LeaveAsync();

        await Shell.Current.GoToAsync("//lobby");
    }
}