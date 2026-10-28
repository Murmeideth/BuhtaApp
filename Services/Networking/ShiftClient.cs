using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using BuhtaApp.Services;
using BuhtaApp.Shared.Protocol;

namespace BuhtaApp.Services.Networking;

public class ShiftClient : IShiftClient, IAsyncDisposable
{
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _cts;
    private Task? _receiveLoop;
    private Task? _flushLoop;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private readonly Queue<Envelope> _outbox = new();

    private string? _lastUrl;
    private JoinShiftPayload? _lastJoin;

    public bool IsConnected => _socket?.State == WebSocketState.Open;
    public string? ClientId { get; private set; }

    public event EventHandler<Envelope>? MessageReceived;
    public event EventHandler<bool>? ConnectionChanged;

    public async Task ConnectAsync(string url, JoinShiftPayload joinPayload)
    {
        await DisconnectAsync();

        _lastUrl = url;
        _lastJoin = joinPayload;

        _cts = new CancellationTokenSource();
        _socket = new ClientWebSocket();
        await _socket.ConnectAsync(new Uri(url), _cts.Token);

        ClientId = Guid.NewGuid().ToString("N");
        ConnectionChanged?.Invoke(this, true);

        // Регистрация на хабе
        await SendAsync(new Envelope
        {
            Type = MessageTypes.JoinShift,
            Payload = JsonSerializer.Serialize(joinPayload, AppJson.Options)
        });

        _receiveLoop = Task.Run(() => ReceiveLoopAsync(_cts.Token));
        _flushLoop = Task.Run(() => FlushOutboxAsync(_cts.Token));
    }

    public async Task<bool> TryReconnectAsync()
    {
        if (_lastUrl is null || _lastJoin is null) return false;
        if (IsConnected) return true;

        try
        {
            await ConnectAsync(_lastUrl, _lastJoin);
            return IsConnected;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Client] Reconnect failed: {ex.Message}");
            return false;
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];
        var sb = new StringBuilder();

        try
        {
            while (!ct.IsCancellationRequested && _socket is { State: WebSocketState.Open })
            {
                var result = await _socket.ReceiveAsync(buffer, ct);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", ct);
                    break;
                }

                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                if (!result.EndOfMessage) continue;

                var json = sb.ToString();
                sb.Clear();

                var envelope = JsonSerializer.Deserialize<Envelope>(json, AppJson.Options);
                if (envelope is not null)
                    MessageReceived?.Invoke(this, envelope);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Client] Receive loop: {ex.Message}");
        }
        finally
        {
            ConnectionChanged?.Invoke(this, false);
        }
    }

    public async Task SendAsync(Envelope envelope)
    {
        var json = JsonSerializer.Serialize(envelope, AppJson.Options);
        var bytes = Encoding.UTF8.GetBytes(json);

        if (_socket is { State: WebSocketState.Open })
        {
            await _sendLock.WaitAsync();
            try
            {
                await _socket.SendAsync(bytes, WebSocketMessageType.Text, true, _cts?.Token ?? default);
            }
            finally { _sendLock.Release(); }
        }
        else
        {
            lock (_outbox) _outbox.Enqueue(envelope);
        }
    }

    private async Task FlushOutboxAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (IsConnected)
                {
                    Envelope? next = null;
                    lock (_outbox) { if (_outbox.Count > 0) next = _outbox.Dequeue(); }
                    if (next is not null) await SendAsync(next);
                }
                await Task.Delay(1000, ct);
            }
            catch (TaskCanceledException) { return; }
            catch (Exception ex) { Debug.WriteLine($"[Client] Flush: {ex.Message}"); }
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _cts?.Cancel();
            if (_socket is { State: WebSocketState.Open })
                await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
        }
        catch { /* ignore */ }

        _socket?.Dispose();
        _socket = null;
        _cts?.Dispose();
        _cts = null;
        ConnectionChanged?.Invoke(this, false);
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}