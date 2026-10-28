using BuhtaApp.Models;

namespace BuhtaApp.Services.Networking;

public interface IShiftCoordinator
{
    bool IsHost { get; }
    bool IsConnected { get; }
    bool IsReconnecting { get; }
    Shift? CurrentShift { get; }
    UserProfile? CurrentProfile { get; }
    string? CurrentHostIp { get; }

    Task<Shift> OpenShiftAsync(string shiftName, UserProfile profile);
    Task JoinShiftAsync(string hostIp, UserProfile profile);
    Task LeaveAsync();
    Task CloseShiftAsync();

    Task CreateOrderAsync(int tableNumber, List<OrderItem> items);
    Task MarkItemDoneAsync(string orderId, string itemId);
    Task CloseOrderAsync(string orderId);
    Task CancelOrderAsync(string orderId);

    event EventHandler<Shift>? ShiftOpened;
    event EventHandler? ShiftClosed;
    event EventHandler<Order>? OrderCreated;
    event EventHandler<Order>? OrderUpdated;
    event EventHandler<Order>? OrderClosed;
    event EventHandler<string>? ConnectionStateChanged;
}