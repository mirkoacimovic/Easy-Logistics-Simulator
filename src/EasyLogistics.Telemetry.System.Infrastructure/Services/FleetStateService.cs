using EasyLogistics.Telemetry.System.Core.Configuration;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Data;

namespace EasyLogistics.Telemetry.System.Infrastructure.Services;

public class FleetStateService : IFleetStateService
{
    private readonly ConcurrentDictionary<int, TruckTelemetry> _latestStats = new();
    private readonly FleetSettings _settings;
    private readonly ILogger<FleetStateService> _logger;

    public event Action<List<TruckDisplayVm>>? OnFleetUpdated;

    public FleetStateService(IDbConnection db, ILogger<FleetStateService> logger, IOptions<FleetSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task UpdateFleet(IEnumerable<TruckTelemetry> snapshots)
    {
        bool changed = false;
        foreach (var truck in snapshots)
        {
            _latestStats[truck.TruckId] = truck;
            changed = true;
        }

        if (changed && OnFleetUpdated != null)
        {
            OnFleetUpdated.Invoke(GetFormattedFleet());
        }
        await Task.CompletedTask;
    }

    public List<TruckDisplayVm> GetFormattedFleet()
    {
        return _latestStats.Values.Select(t => new TruckDisplayVm
        {
            TruckId = t.TruckId,
            Latitude = t.Latitude,
            Longitude = t.Longitude,
            Speed = Math.Round(t.Speed, 1),
            FuelConsumed = t.FuelConsumed,
            DistanceTraveled = t.DistanceTraveled,
            TotalCost = t.TotalCost,
            Status = t.Speed > 5 ? "Moving" : "Idle",
            RouteName = ResolveRoute(t.Latitude, t.Longitude),
            LastUpdated = t.FormattedTime
        }).ToList();
    }

    private string ResolveRoute(double lat, double lng)
    {
        if (_settings.LogisticsHubs == null || !_settings.LogisticsHubs.Any())
        {
            _logger.LogWarning("Route resolution failed: No hubs in configuration.");
            return "Global Route";
        }

        var closestHubs = _settings.LogisticsHubs
            .Select(h => new {
                h.Name,
                Distance = Math.Sqrt(Math.Pow(h.Lat - lat, 2) + Math.Pow(h.Lng - lng, 2))
            })
            .OrderBy(h => h.Distance)
            .ToList();

        var primary = closestHubs[0];
        if (primary.Distance < 0.08) return $"At {primary.Name} Hub";

        var secondary = closestHubs.Skip(1).FirstOrDefault();
        return secondary != null ? $"{primary.Name} - {secondary.Name}" : $"{primary.Name} Regional";
    }
}