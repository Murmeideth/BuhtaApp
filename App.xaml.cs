using BuhtaApp.Services;

namespace BuhtaApp;

public partial class App : Application
{
    private readonly SessionTimeoutService _timeout;

    public App(SessionTimeoutService timeout)
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark;
        _timeout = timeout;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new AppShell());
        _timeout.Attach(window);
        return window;
    }
}