using EasyLogistics.Telemetry.System.Core.Configuration;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyLogistics.Telemetry.System.Infrastructure.Services;

public class FleetStateService : IFleetStateService
{
    private readonly ILogger<FleetStateService> _logger;
    private readonly FleetSettings _settings;
    private readonly object _lock = new();
    private List<TruckDisplayVm> _currentFleet = new();

    public event Action<List<TruckDisplayVm>>? OnFleetUpdated;

    public FleetStateService(ILogger<FleetStateService> logger, IOptions<FleetSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
    }

    /// <summary>
    /// Returns a thread-safe snapshot of the current fleet state.
    /// Used by the Sidebar to count "SPEEDING" trucks.
    /// </summary>
    public List<TruckDisplayVm> GetFormattedFleet()
    {
        lock (_lock)
        {
            return _currentFleet.ToList();
        }
    }

    /// <summary>
    /// Processes raw telemetry from the Python Bridge, 
    /// applies business rules, and notifies subscribers.
    /// </summary>
    public async Task UpdateFleet(IEnumerable<TruckTelemetry> rawData)
    {
        if (rawData == null) return;

        // One-pass mapping and rule application
        var mapped = rawData.Select(t => {

            // Logic derived from simulation thresholds
            string status = "STATIONARY";
            string severity = "text-success";
            string aiStatus = "NOMINAL: STEADY";

            if (t.Speed > 90.0)
            {
                status = "CRITICAL";
                severity = "text-danger fw-bold blink";
                aiStatus = "CRITICAL: OVERSPEED";
            }
            else if (t.Speed > 1.0)
            {
                status = "IN_TRANSIT";
                severity = "text-primary";
                aiStatus = "NOMINAL: ACTIVE";
            }

            return new TruckDisplayVm
            {
                TruckId = t.TruckId,
                Latitude = t.Latitude,
                Longitude = t.Longitude,
                Speed = t.Speed,
                FuelConsumed = t.FuelConsumed,
                DistanceTraveled = t.DistanceTraveled,
                TotalCost = t.TotalCost,
                Timestamp = t.Timestamp,
                Status = status,
                SeverityClass = severity,
                AiStatus = aiStatus,
                LastUpdated = DateTime.Now.ToString("HH:mm:ss"),
                DriverName = $"UNIT_{t.TruckId:D3}"
            };
        }).ToList();

        lock (_lock)
        {
            _currentFleet = mapped;
        }

        OnFleetUpdated?.Invoke(mapped);

        await Task.CompletedTask;
    }
}