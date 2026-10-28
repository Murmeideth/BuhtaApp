using System.Diagnostics;
using System.Text.Json;
using BuhtaApp.Common;
using BuhtaApp.Enums;
using BuhtaApp.Models;
using BuhtaApp.Services;
using BuhtaApp.Shared.Protocol;

namespace BuhtaApp.Services.Networking;

public class ShiftCoordinator : IShiftCoordinator
{
    private readonly IShiftHost _host;
    private readonly IShiftClient _client;
    private readonly IWebSocketHub _hub;
    private readonly IProfileService _profiles;
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);

    private Shift? _shift;
    private CancellationTokenSource? _reconnectCts;

    public bool IsHost { get; private set; }
    public bool IsConnected => _client.IsConnected;
    public bool IsReconnecting { get; private set; }
    public Shift? CurrentShift => _shift;
    public UserProfile? CurrentProfile { get; private set; }
    public string? CurrentHostIp { get; private set; }

    public event EventHandler<Shift>? ShiftOpened;
    public event EventHandler? ShiftClosed;
    public event EventHandler<Order>? OrderCreated;
    public event EventHandler<Order>? OrderUpdated;
    public event EventHandler<Order>? OrderClosed;
    public event EventHandler<string>? ConnectionStateChanged;

    public ShiftCoordinator(
        IShiftHost host,
        IShiftClient client,
        IWebSocketHub hub,
        IProfileService profiles)
    {
        _host = host;
        _client = client;
        _hub = hub;
        _profiles = profiles;

        _client.MessageReceived += OnClientMessage;
        _client.ConnectionChanged += OnConnectionChanged;
        _hub.MessageReceived += OnHubMessage;
    }

    // ------------------ Открытие / присоединение ------------------

    public async Task<Shift> OpenShiftAsync(string shiftName, UserProfile profile)
    {
        CurrentProfile = profile;
        CurrentHostIp = "127.0.0.1";
        IsHost = true;

        _shift = await _host.StartAsync(shiftName, profile);
        ShiftOpened?.Invoke(this, _shift);
        return _shift;
    }

    public async Task JoinShiftAsync(string hostIp, UserProfile profile)
    {
        CurrentProfile = profile;
        CurrentHostIp = hostIp;
        IsHost = false;

        await _client.ConnectAsync(AppConstants.BuildWebSocketUrl(hostIp), new JoinShiftPayload
        {
            ProfileId = profile.Id,
            Name = profile.Name,
            Role = profile.Role,
            Avatar = profile.Avatar,
            NameColor = profile.NameColor
        });
        // _shift придёт с ShiftState от хоста асинхронно
    }

    public async Task LeaveAsync()
    {
        _reconnectCts?.Cancel();
        IsReconnecting = false;

        if (IsHost) await _host.StopAsync();
        else await _client.DisconnectAsync();

        _shift = null;
        IsHost = false;
        CurrentHostIp = null;
        ShiftClosed?.Invoke(this, EventArgs.Empty);
    }

    public async Task CloseShiftAsync()
    {
        if (!IsHost) throw new InvalidOperationException("Только хост может закрыть смену");
        await _host.StopAsync();
        _shift = null;
        IsHost = false;
        CurrentHostIp = null;
        ShiftClosed?.Invoke(this, EventArgs.Empty);
    }

    // ------------------ Команды ------------------

    public Task CreateOrderAsync(int tableNumber, List<OrderItem> items) =>
        _client.SendAsync(new Envelope
        {
            Type = MessageTypes.CreateOrder,
            Payload = JsonSerializer.Serialize(new CreateOrderPayload
            {
                TableNumber = tableNumber,
                Items = items
            }, AppJson.Options)
        });

    public Task MarkItemDoneAsync(string orderId, string itemId) =>
        _client.SendAsync(new Envelope
        {
            Type = MessageTypes.MarkItemDone,
            Payload = JsonSerializer.Serialize(new MarkItemDonePayload
            {
                OrderId = orderId,
                ItemId = itemId
            }, AppJson.Options)
        });

    public Task CloseOrderAsync(string orderId) =>
        _client.SendAsync(new Envelope
        {
            Type = MessageTypes.CloseOrder,
            Payload = JsonSerializer.Serialize(new CloseOrderPayload { OrderId = orderId }, AppJson.Options)
        });

    public Task CancelOrderAsync(string orderId) =>
        _client.SendAsync(new Envelope
        {
            Type = MessageTypes.CancelOrder,
            Payload = JsonSerializer.Serialize(new CancelOrderPayload { OrderId = orderId }, AppJson.Options)
        });

    // ------------------ Обработчики ------------------

    private void OnHubMessage(object? sender, ClientMessageEventArgs e)
    {
        if (!IsHost || _shift is null) return;

        try
        {
            switch (e.Envelope.Type)
            {
                case MessageTypes.CreateOrder:
                    HandleCreateOrder(e);
                    break;
                case MessageTypes.MarkItemDone:
                    HandleMarkItemDone(e);
                    break;
                case MessageTypes.CloseOrder:
                    HandleCloseOrder(e);
                    break;
                case MessageTypes.CancelOrder:
                    HandleCancelOrder(e);
                    break;
                case MessageTypes.JoinShift:
                    HandleJoinMember(e);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Coordinator] Hub message failed: {ex.Message}");
        }
    }

    private void OnClientMessage(object? sender, Envelope envelope)
    {
        try
        {
            switch (envelope.Type)
            {
                case MessageTypes.ShiftState:
                    var state = Deserialize<ShiftStatePayload>(envelope);
                    if (state is not null)
                    {
                        _shift = state.Shift;
                        ShiftOpened?.Invoke(this, _shift);
                    }
                    break;

                case MessageTypes.OrderCreated:
                case MessageTypes.OrderUpdated:
                case MessageTypes.OrderClosed:
                    var orderPayload = Deserialize<OrderPayload>(envelope);
                    if (orderPayload is not null)
                    {
                        ApplyOrderToLocal(orderPayload.Order);

                        if (envelope.Type == MessageTypes.OrderCreated)
                            OrderCreated?.Invoke(this, orderPayload.Order);
                        else if (envelope.Type == MessageTypes.OrderUpdated)
                            OrderUpdated?.Invoke(this, orderPayload.Order);
                        else
                            OrderClosed?.Invoke(this, orderPayload.Order);
                    }
                    break;

                case MessageTypes.ShiftClosed:
                    ShiftClosed?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Coordinator] Client message failed: {ex.Message}");
        }
    }

    private void HandleCreateOrder(ClientMessageEventArgs e)
    {
        if (_shift is null) return;
        var payload = JsonSerializer.Deserialize<CreateOrderPayload>(e.Envelope.Payload!, AppJson.Options);
        if (payload is null) return;

        var table = _shift.Tables.FirstOrDefault(t => t.Number == payload.TableNumber);
        if (table is null) return;

        // MVP: считаем создателем заказа хоста (точное сопоставление ClientId(Guid) <-> ProfileId(string) — позже)
        var creator = _shift.Members.FirstOrDefault(m => m.IsHost)
                      ?? _shift.Members.FirstOrDefault();

        var order = new Order
        {
            Number = table.NextOrderNumber,
            TableNumber = payload.TableNumber,
            CreatedByProfileId = creator?.ProfileId ?? "",
            CreatedByName = creator?.Name ?? "—",
            CreatedByAvatar = creator?.Avatar ?? "👤",
            CreatedByNameColor = creator?.NameColor ?? AppConstants.DefaultNameColor,
            Items = payload.Items
        };

        table.Orders.Add(order);

        _ = _hub.BroadcastAsync(new Envelope
        {
            Type = MessageTypes.OrderCreated,
            Payload = JsonSerializer.Serialize(new OrderPayload { Order = order }, AppJson.Options)
        });

        NotifyStations(order);
    }

    private void HandleMarkItemDone(ClientMessageEventArgs e)
    {
        if (_shift is null) return;
        var payload = JsonSerializer.Deserialize<MarkItemDonePayload>(e.Envelope.Payload!, AppJson.Options);
        if (payload is null) return;

        var order = _shift.Tables.SelectMany(t => t.Orders).FirstOrDefault(o => o.Id == payload.OrderId);
        var item = order?.Items.FirstOrDefault(i => i.Id == payload.ItemId);
        if (order is null || item is null) return;

        item.IsDone = true;

        _ = _hub.BroadcastAsync(new Envelope
        {
            Type = MessageTypes.OrderUpdated,
            Payload = JsonSerializer.Serialize(new OrderPayload { Order = order }, AppJson.Options)
        });
    }

    private void HandleCloseOrder(ClientMessageEventArgs e)
    {
        if (_shift is null) return;
        var payload = JsonSerializer.Deserialize<CloseOrderPayload>(e.Envelope.Payload!, AppJson.Options);
        if (payload is null) return;

        var order = _shift.Tables.SelectMany(t => t.Orders).FirstOrDefault(o => o.Id == payload.OrderId);
        if (order is null) return;

        order.Status = OrderStatus.Closed;
        order.ClosedAt = DateTime.UtcNow;

        _ = _hub.BroadcastAsync(new Envelope
        {
            Type = MessageTypes.OrderClosed,
            Payload = JsonSerializer.Serialize(new OrderPayload { Order = order }, AppJson.Options)
        });
    }

    private void HandleCancelOrder(ClientMessageEventArgs e)
    {
        if (_shift is null) return;
        var payload = JsonSerializer.Deserialize<CancelOrderPayload>(e.Envelope.Payload!, AppJson.Options);
        if (payload is null) return;

        var order = _shift.Tables.SelectMany(t => t.Orders).FirstOrDefault(o => o.Id == payload.OrderId);
        if (order is null) return;

        order.Status = OrderStatus.Cancelled;

        _ = _hub.BroadcastAsync(new Envelope
        {
            Type = MessageTypes.OrderClosed,
            Payload = JsonSerializer.Serialize(new OrderPayload { Order = order }, AppJson.Options)
        });
    }

    private void HandleJoinMember(ClientMessageEventArgs e)
    {
        if (_shift is null) return;
        var payload = JsonSerializer.Deserialize<JoinShiftPayload>(e.Envelope.Payload!, AppJson.Options);
        if (payload is null) return;

        if (_shift.Members.Any(m => m.ProfileId == payload.ProfileId)) return;

        var member = new ShiftMember
        {
            ProfileId = payload.ProfileId,
            Name = payload.Name,
            Role = payload.Role,
            Avatar = payload.Avatar,
            NameColor = payload.NameColor
        };
        _shift.Members.Add(member);

        _ = _hub.BroadcastAsync(new Envelope
        {
            Type = MessageTypes.MemberJoined,
            Payload = JsonSerializer.Serialize(new MemberPayload { Member = member }, AppJson.Options)
        });
    }

    private void NotifyStations(Order order)
    {
        var barItems = order.Items.Where(i => i.Station == StationType.Bar).ToList();
        var kitchenItems = order.Items.Where(i => i.Station == StationType.Kitchen).ToList();

        if (barItems.Count > 0)
        {
            _ = _hub.SendToStationAsync(StationType.Bar, new Envelope
            {
                Type = MessageTypes.StationNotification,
                Payload = JsonSerializer.Serialize(new StationNotificationPayload
                {
                    Station = StationType.Bar,
                    Order = CloneWithItems(order, barItems)
                }, AppJson.Options)
            });
        }

        if (kitchenItems.Count > 0)
        {
            _ = _hub.SendToStationAsync(StationType.Kitchen, new Envelope
            {
                Type = MessageTypes.StationNotification,
                Payload = JsonSerializer.Serialize(new StationNotificationPayload
                {
                    Station = StationType.Kitchen,
                    Order = CloneWithItems(order, kitchenItems)
                }, AppJson.Options)
            });
        }
    }

    private static Order CloneWithItems(Order source, List<OrderItem> items) => new()
    {
        Id = source.Id,
        Number = source.Number,
        TableNumber = source.TableNumber,
        CreatedByName = source.CreatedByName,
        CreatedByAvatar = source.CreatedByAvatar,
        CreatedByNameColor = source.CreatedByNameColor,
        Items = items
    };

    private void ApplyOrderToLocal(Order order)
    {
        if (_shift is null) return;
        var table = _shift.Tables.FirstOrDefault(t => t.Number == order.TableNumber);
        if (table is null) return;

        var idx = table.Orders.FindIndex(o => o.Id == order.Id);
        if (idx >= 0) table.Orders[idx] = order;
        else table.Orders.Add(order);
    }

    private static T? Deserialize<T>(Envelope envelope) where T : class
    {
        if (string.IsNullOrWhiteSpace(envelope.Payload)) return null;
        return JsonSerializer.Deserialize<T>(envelope.Payload, AppJson.Options);
    }

    // ------------------ Reconnect ------------------

    private void OnConnectionChanged(object? sender, bool connected)
    {
        ConnectionStateChanged?.Invoke(this, connected ? "connected" : "disconnected");

        if (!connected && !IsHost && CurrentHostIp is not null)
            _ = StartReconnectLoopAsync();
    }

    private async Task StartReconnectLoopAsync()
    {
        await _reconnectLock.WaitAsync();
        try
        {
            if (IsReconnecting) return;
            IsReconnecting = true;

            _reconnectCts = new CancellationTokenSource();
            var ct = _reconnectCts.Token;

            for (int attempt = 1; attempt <= AppConstants.ReconnectMaxAttempts; attempt++)
            {
                if (ct.IsCancellationRequested) return;
                if (_client.IsConnected) return;

                var delaySec = Math.Min(
                    AppConstants.ReconnectMaxDelaySeconds,
                    AppConstants.ReconnectBaseDelaySeconds * attempt);

                Debug.WriteLine($"[Coordinator] Reconnect attempt {attempt} in {delaySec}s");
                await Task.Delay(TimeSpan.FromSeconds(delaySec), ct);

                if (await _client.TryReconnectAsync())
                {
                    Debug.WriteLine("[Coordinator] Reconnected");
                    ConnectionStateChanged?.Invoke(this, "reconnected");
                    return;
                }
            }

            Debug.WriteLine("[Coordinator] Reconnect failed, giving up");
            ConnectionStateChanged?.Invoke(this, "reconnect-failed");
        }
        catch (TaskCanceledException) { /* ок */ }
        finally
        {
            IsReconnecting = false;
            _reconnectLock.Release();
        }
    }
}