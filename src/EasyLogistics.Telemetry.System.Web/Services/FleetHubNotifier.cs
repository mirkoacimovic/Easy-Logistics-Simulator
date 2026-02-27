using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using EasyLogistics.Telemetry.System.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EasyLogistics.Telemetry.System.Web.Services;

/// <summary>
/// Server-side notifier that broadcasts processed telemetry to all connected clients.
/// </summary>
public sealed class FleetHubNotifier : IFleetHubNotifier
{
    private readonly IHubContext<FleetHub> _hubContext;
    private readonly ILogger<FleetHubNotifier> _logger;

    public FleetHubNotifier(IHubContext<FleetHub> hubContext, ILogger<FleetHubNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Transforms raw truck telemetry into UI ViewModels and broadcasts via SignalR.
    /// </summary>
    public async Task EmitFleetUpdate(IEnumerable<TruckTelemetry> fleet)
    {
        if (fleet == null || !fleet.Any())
        {
            return;
        }

        // Mapping raw telemetry to UI ViewModels (TruckDisplayVm)
        // We ensure all alias fields (Lat, Lng, etc.) are populated for compatibility with Dashboard and Map
        var vms = fleet.Select(t => new TruckDisplayVm
        {
            // Primary Identifiers
            TruckId = t.TruckId,
            Id = t.TruckId,

            // GPS Coordinates
            Latitude = t.Latitude,
            Lat = t.Latitude,
            Longitude = t.Longitude,
            Lng = t.Longitude,

            // Performance Metrics
            Speed = Math.Round(t.Speed, 1),
            FuelConsumed = Math.Round(t.FuelConsumed, 2),
            Fuel = Math.Round(t.FuelConsumed, 2),
            DistanceTraveled = Math.Round(t.DistanceTraveled, 1),
            Distance = Math.Round(t.DistanceTraveled, 1),

            // Financials
            TotalCost = t.TotalCost,
            Cost = t.TotalCost,

            // Derived Operational Status
            Status = GetOperationalStatus(t.Speed),
            LastUpdated = DateTime.Now // Timestamp for the UI heartbeat
        }).ToList();

        _logger.LogDebug("[Plumbing] Broadcasting update for {Count} trucks.", vms.Count);

        // Send to all connected dispatchers. 
        // JavaScript/SignalR Client listens for "ReceiveFleetUpdate"
        await _hubContext.Clients.All.SendAsync("ReceiveFleetUpdate", vms);
    }

    /// <summary>
    /// Logic-based status determination for the UI indicators.
    /// Aligns with the StatusClass logic in TruckDisplayVm.
    /// </summary>
    private static string GetOperationalStatus(double speed)
    {
        return speed switch
        {
            > 90 => "Speeding",
            > 5 => "Moving",
            > 0 => "Idle",
            _ => "Idle"
        };
    }
}