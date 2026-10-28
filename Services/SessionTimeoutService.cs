using System.Diagnostics;
using BuhtaApp.Common;
using BuhtaApp.Services.Networking;

namespace BuhtaApp.Services;

public class SessionTimeoutService
{
    private readonly IShiftCoordinator _coordinator;
    private Window? _window;
    private CancellationTokenSource? _cts;
    private bool _isForeground = true;

    public SessionTimeoutService(IShiftCoordinator coordinator)
    {
        _coordinator = coordinator;
    }

    public void Attach(Window window)
    {
        _window = window;
        window.Deactivated += OnDeactivated;
        window.Activated += OnActivated;
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        _isForeground = false;
        StartTimer();
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        _isForeground = true;
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    private void StartTimer()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(AppConstants.SessionIdleTimeoutMinutes), token);
                if (!_isForeground && _coordinator.IsHost && _coordinator.CurrentShift is not null)
                {
                    Debug.WriteLine("[Timeout] Shift auto-closed after idle");
                    await _coordinator.CloseShiftAsync();
                }
            }
            catch (TaskCanceledException) { /* вернулись */ }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Timeout] {ex.Message}");
            }
        }, token);
    }
}