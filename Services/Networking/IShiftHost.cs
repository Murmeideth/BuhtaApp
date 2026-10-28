using BuhtaApp.Enums;
using BuhtaApp.Models;

namespace BuhtaApp.Services.Networking;

public interface IShiftHost
{
    bool IsActive { get; }
    Shift? CurrentShift { get; }
    string HostIp { get; }

    Task<Shift> StartAsync(string shiftName, UserProfile hostProfile);
    Task StopAsync();
    Shift? GetShiftSnapshot();
    void RegisterClientRole(Guid clientId, UserRole role);
}