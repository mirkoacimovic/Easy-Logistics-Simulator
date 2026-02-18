using EasyLogistics.Core.Models;
using EasyLogistics.Core.ViewModels;
using EasyLogistics.Core.Services;
using System.Collections.Concurrent;
using EasyLogistics.Core.Interfaces;

namespace EasyLogistics.Infrastructure.Services;

public class FleetStateService : IFleetStateService
{
    private readonly ConcurrentDictionary<int, TruckTelemetry> _latestFleet = new();
    private readonly FleetAnalytics _analytics = new(); // Domain Logic

    public void UpdateFleet(IEnumerable<TruckTelemetry> trucks)
    {
        foreach (var truck in trucks.Where(t => t.Id > 0))
        {
            _latestFleet[truck.Id] = truck;
        }
    }

    // This is what the UI (SignalR/Blazor) will actually consume
    public List<TruckDisplayVm> GetFormattedFleet()
    {
        return _latestFleet.Values.Select(t => new TruckDisplayVm
        {
            Id = t.Id,
            Status = t.Speed > 80 ? "Speeding" : "Moving",
            // Use the fixed names 'Lat' and 'Lng'
            Position = $"{t.Lat:F4}, {t.Lng:F4}",
            SpeedDisplay = $"{t.Speed:F1} km/h",
            LastSeen = DateTimeOffset.FromUnixTimeSeconds(t.Timestamp).DateTime.ToLocalTime().ToString("HH:mm:ss")
        }).ToList();
    }

    public List<TruckTelemetry> GetCurrentFleet() => _latestFleet.Values.ToList();
}