using BuhtaApp.Shared.Protocol;

namespace BuhtaApp.Services.Networking;

public interface IShiftClient
{
    bool IsConnected { get; }
    string? ClientId { get; }

    Task ConnectAsync(string url, JoinShiftPayload joinPayload);
    Task DisconnectAsync();
    Task SendAsync(Envelope envelope);
    Task<bool> TryReconnectAsync();

    event EventHandler<Envelope>? MessageReceived;
    event EventHandler<bool>? ConnectionChanged;
}