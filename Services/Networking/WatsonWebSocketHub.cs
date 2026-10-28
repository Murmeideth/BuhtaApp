using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BuhtaApp.Enums;
using BuhtaApp.Services;
using BuhtaApp.Shared.Protocol;
using WatsonWebsocket;

namespace BuhtaApp.Services.Networking;

public class WatsonWebSocketHub : IWebSocketHub, IAsyncDisposable
{
    private WatsonWsServer? _server;
    private readonly ConcurrentDictionary<Guid, ClientInfo> _clients = new();
    private readonly SemaphoreSlim _startLock = new(1, 1);

    public bool IsRunning => _server?.IsListening ?? false;
    public int Port { get; private set; }

    public event EventHandler<ClientMessageEventArgs>? MessageReceived;
    public event EventHandler<ClientConnectionEventArgs>? ClientConnected;
    public event EventHandler<ClientConnectionEventArgs>? ClientDisconnected;

    public async Task StartAsync(int port)
    {
        if (IsRunning) return;

        await _startLock.WaitAsync();
        try
        {
            if (IsRunning) return;

            Port = port;
            _server = new WatsonWsServer("0.0.0.0", port, false);

            _server.ClientConnected += (s, e) =>
            {
                var clientGuid = e.Client.Guid;
                _clients[clientGuid] = new ClientInfo { ClientId = clientGuid };
                ClientConnected?.Invoke(this, new ClientConnectionEventArgs { ClientId = clientGuid });
                Debug.WriteLine($"[Hub] Client connected: {clientGuid}");
            };

            _server.ClientDisconnected += (s, e) =>
            {
                var clientGuid = e.Client.Guid;
                _clients.TryRemove(clientGuid, out _);
                ClientDisconnected?.Invoke(this, new ClientConnectionEventArgs { ClientId = clientGuid });
                Debug.WriteLine($"[Hub] Client disconnected: {clientGuid}");
            };

            _server.MessageReceived += (s, e) =>
            {
                try
                {
                    var clientGuid = e.Client.Guid;
                    var json = Encoding.UTF8.GetString(e.Data);
                    var envelope = JsonSerializer.Deserialize<Envelope>(json, AppJson.Options);
                    if (envelope is null) return;

                    if (envelope.Type == MessageTypes.JoinShift && !string.IsNullOrEmpty(envelope.Payload))
                    {
                        var join = JsonSerializer.Deserialize<JoinShiftPayload>(envelope.Payload, AppJson.Options);
                        if (join is not null && _clients.TryGetValue(clientGuid, out var info))
                            info.Role = join.Role;
                    }

                    MessageReceived?.Invoke(this, new ClientMessageEventArgs
                    {
                        ClientId = clientGuid,
                        Envelope = envelope
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[Hub] Bad message: {ex.Message}");
                }
            };

            _server.Start();
            Debug.WriteLine($"[Hub] Started on port {port}");
        }
        finally { _startLock.Release(); }
    }

    public Task StopAsync()
    {
        try { _server?.Stop(); } catch { /* ignore */ }
        _server?.Dispose();
        _server = null;
        _clients.Clear();
        Debug.WriteLine("[Hub] Stopped");
        return Task.CompletedTask;
    }

    public Task SendToClientAsync(Guid clientId, Envelope envelope)
    {
        if (_server is null || !_clients.ContainsKey(clientId)) return Task.CompletedTask;
        var json = JsonSerializer.Serialize(envelope, AppJson.Options);
        _ = _server.SendAsync(clientId, json);
        return Task.CompletedTask;
    }

    public async Task BroadcastAsync(Envelope envelope)
    {
        foreach (var id in _clients.Keys.ToList())
            await SendToClientAsync(id, envelope);
    }

    public async Task SendToRoleAsync(UserRole role, Envelope envelope)
    {
        foreach (var info in _clients.Values.Where(c => c.Role == role).ToList())
            await SendToClientAsync(info.ClientId, envelope);
    }

    public async Task SendToStationAsync(StationType station, Envelope envelope)
    {
        var role = station == StationType.Bar ? UserRole.Barman : UserRole.Cook;
        await SendToRoleAsync(role, envelope);
    }

    public void RegisterClient(Guid clientId, UserRole role)
    {
        if (_clients.TryGetValue(clientId, out var info))
            info.Role = role;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _startLock.Dispose();
    }

    private class ClientInfo
    {
        public Guid ClientId { get; set; }
        public UserRole? Role { get; set; }
    }
}