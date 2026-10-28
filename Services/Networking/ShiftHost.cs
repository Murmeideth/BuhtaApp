using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using BuhtaApp.Common;
using BuhtaApp.Enums;
using BuhtaApp.Models;
using BuhtaApp.Services;
using BuhtaApp.Shared.Protocol;

namespace BuhtaApp.Services.Networking;

public class ShiftHost : IShiftHost
{
    private readonly IWebSocketHub _hub;
    private readonly IShiftClient _localClient;
    private readonly IMenuService _menuService;
    private Shift? _shift;

    public bool IsActive => _hub.IsRunning;
    public Shift? CurrentShift => _shift;
    public string HostIp { get; private set; } = "127.0.0.1";

    public ShiftHost(IWebSocketHub hub, IShiftClient localClient, IMenuService menuService)
    {
        _hub = hub;
        _localClient = localClient;
        _menuService = menuService;
    }

    public async Task<Shift> StartAsync(string shiftName, UserProfile hostProfile)
    {
        if (IsActive) return _shift!;

        HostIp = GetLocalIp();
        await _hub.StartAsync(AppConstants.DefaultHostPort);

        var menu = await _menuService.LoadAsync();

        _shift = new Shift
        {
            Name = shiftName,
            HostProfileId = hostProfile.Id,
            HostName = hostProfile.Name,
            Members =
            {
                new ShiftMember
                {
                    ProfileId = hostProfile.Id,
                    Name = hostProfile.Name,
                    Role = hostProfile.Role,
                    Avatar = hostProfile.Avatar,
                    NameColor = hostProfile.NameColor,
                    IsHost = true
                }
            },
            Tables = menu.Tables.Select(t => new Table
            {
                Number = t.Number,
                Name = t.Name,
                Type = t.Type
            }).ToList()
        };

        await _localClient.ConnectAsync(AppConstants.BuildLocalWebSocketUrl(), new JoinShiftPayload
        {
            ProfileId = hostProfile.Id,
            Name = hostProfile.Name,
            Role = hostProfile.Role,
            Avatar = hostProfile.Avatar,
            NameColor = hostProfile.NameColor
        });

        Debug.WriteLine($"[Host] Shift '{shiftName}' opened on {HostIp}:{AppConstants.DefaultHostPort}");
        return _shift;
    }

    public async Task StopAsync()
    {
        await _localClient.DisconnectAsync();
        await _hub.StopAsync();
        _shift = null;
        Debug.WriteLine("[Host] Shift stopped");
    }

    public Shift? GetShiftSnapshot() => _shift;

    public void RegisterClientRole(Guid clientId, UserRole role)
        => _hub.RegisterClient(clientId, role);

    private static string GetLocalIp()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList
                .FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(a))
                ?.ToString() ?? "127.0.0.1";
        }
        catch { return "127.0.0.1"; }
    }
}