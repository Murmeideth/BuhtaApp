using BuhtaApp.Services;
using BuhtaApp.Services.Networking;
using BuhtaApp.ViewModels;
using BuhtaApp.Views;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;

namespace BuhtaApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // --- Storage / Menu / Profile ---
        builder.Services.AddSingleton<IStorageService, JsonStorageService>();
        builder.Services.AddSingleton<IMenuService, MenuService>();
        builder.Services.AddSingleton<IProfileService, ProfileService>();

        // --- Networking ---
        builder.Services.AddSingleton<IWebSocketHub, WatsonWebSocketHub>();
        builder.Services.AddSingleton<IShiftClient, ShiftClient>();
        builder.Services.AddSingleton<IShiftHost, ShiftHost>();
        builder.Services.AddSingleton<IShiftCoordinator, ShiftCoordinator>();
        builder.Services.AddSingleton<SessionTimeoutService>();

        // ViewModels
        builder.Services.AddTransient<ProfileEditorViewModel>();
        builder.Services.AddSingleton<LobbyViewModel>();   // Singleton: подписан на ShiftClosed

        // Pages
        builder.Services.AddTransient<BootstrapPage>();
        builder.Services.AddTransient<ProfileEditorPage>();
        builder.Services.AddTransient<LobbyPage>();
        builder.Services.AddTransient<ShiftPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}