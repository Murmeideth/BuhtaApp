using BuhtaApp.Enums;

namespace BuhtaApp.Common;

public static class AppConstants
{
    // --- Сеть (WebSocket) ---
    public const int DefaultHostPort = 5000;
    public const string WebSocketHubPath = "/ws/shift";

    public static string BuildWebSocketUrl(string host, int port = DefaultHostPort)
        => $"ws://{host}:{port}{WebSocketHubPath}";

    public static string BuildLocalWebSocketUrl(int port = DefaultHostPort)
        => BuildWebSocketUrl("127.0.0.1", port);

    // --- Таймауты и сессия ---
    public const int SessionIdleTimeoutMinutes = 5;   // автозатухание хоста в фоне
    public const int MdnsScanIntervalMs = 3000;       // период поиска смен в сети

    // --- Reconnect ---
    public const int ReconnectMaxAttempts = 10;
    public const int ReconnectBaseDelaySeconds = 2;
    public const int ReconnectMaxDelaySeconds = 30;

    // --- Файлы ---
    public const string DeviceSettingsFile = "device.json";
    public const string ShiftHistoryFile = "shift-history.json";
    public const string MenuBundleFile = "menu.json";
    public const string MenuUserFile = "menu.json";
    public const string ShiftDataFolder = "ShiftData";

    // --- mDNS ---
    public const string MdnsServiceType = "_buhta-shift._tcp";

    // --- Прочее ---
    public const string DefaultNameColor = "#E96A6A";
    public const string DefaultAvatar = "👤";
}