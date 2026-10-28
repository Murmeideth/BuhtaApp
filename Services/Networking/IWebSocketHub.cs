using BuhtaApp.Enums;
using BuhtaApp.Shared.Protocol;

namespace BuhtaApp.Services.Networking;

public interface IWebSocketHub
{
    bool IsRunning { get; }
    int Port { get; }

    Task StartAsync(int port);
    Task StopAsync();

    Task SendToClientAsync(Guid clientId, Envelope envelope);
    Task BroadcastAsync(Envelope envelope);
    Task SendToRoleAsync(UserRole role, Envelope envelope);
    Task SendToStationAsync(StationType station, Envelope envelope);

    void RegisterClient(Guid clientId, UserRole role);

    event EventHandler<ClientMessageEventArgs>? MessageReceived;
    event EventHandler<ClientConnectionEventArgs>? ClientConnected;
    event EventHandler<ClientConnectionEventArgs>? ClientDisconnected;
}

public class ClientMessageEventArgs : EventArgs
{
    public Guid ClientId { get; init; }
    public Envelope Envelope { get; init; } = new();
}

public class ClientConnectionEventArgs : EventArgs
{
    public Guid ClientId { get; init; }
}