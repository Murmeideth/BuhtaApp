using BuhtaApp.Enums;
using BuhtaApp.Models;

namespace BuhtaApp.Shared.Protocol;

public class Envelope
{
    public string Type { get; set; } = "";
    public string? RequestId { get; set; }
    public string? Payload { get; set; }
}

public static class MessageTypes
{
    // клиент → хост
    public const string JoinShift = "JoinShift";
    public const string LeaveShift = "LeaveShift";
    public const string CreateOrder = "CreateOrder";
    public const string AddOrderItem = "AddOrderItem";
    public const string MarkItemDone = "MarkItemDone";
    public const string CloseOrder = "CloseOrder";
    public const string CancelOrder = "CancelOrder";

    // хост → клиенты
    public const string ShiftState = "ShiftState";
    public const string MemberJoined = "MemberJoined";
    public const string MemberLeft = "MemberLeft";
    public const string OrderCreated = "OrderCreated";
    public const string OrderUpdated = "OrderUpdated";
    public const string OrderClosed = "OrderClosed";
    public const string ItemDone = "ItemDone";
    public const string StationNotification = "StationNotification";
    public const string ShiftClosed = "ShiftClosed";
    public const string Error = "Error";
}

// --- DTO команд ---

public class JoinShiftPayload
{
    public string ProfileId { get; set; } = "";
    public string Name { get; set; } = "";
    public UserRole Role { get; set; }
    public string Avatar { get; set; } = "👤";
    public string NameColor { get; set; } = "#E96A6A";
}

public class CreateOrderPayload
{
    public int TableNumber { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class AddOrderItemPayload
{
    public string OrderId { get; set; } = "";
    public OrderItem Item { get; set; } = new();
}

public class MarkItemDonePayload
{
    public string OrderId { get; set; } = "";
    public string ItemId { get; set; } = "";
}

public class CloseOrderPayload { public string OrderId { get; set; } = ""; }
public class CancelOrderPayload { public string OrderId { get; set; } = ""; }

// --- DTO событий ---

public class ShiftStatePayload { public Shift Shift { get; set; } = new(); }
public class MemberPayload { public ShiftMember Member { get; set; } = new(); }
public class OrderPayload { public Order Order { get; set; } = new(); }

public class ItemDonePayload
{
    public string OrderId { get; set; } = "";
    public string ItemId { get; set; } = "";
}

public class StationNotificationPayload
{
    public StationType Station { get; set; }
    public Order Order { get; set; } = new();
}

public class ErrorPayload { public string Message { get; set; } = ""; }