using System.Collections.Concurrent;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using EasyLogistics.Telemetry.System.Core.Entities;

namespace EasyLogistics.Telemetry.System.Infrastructure.Services;

/// <summary>
/// Manages the high-speed, in-memory state of the entire truck fleet.
/// Acts as the primary data source for real-time UI components.
/// </summary>
public class FleetStateService : IFleetStateService
{
    // ConcurrentDictionary ensures thread-safety during the BackgroundWorker's 1Hz write loop
    private readonly ConcurrentDictionary<int, TruckDisplayVm> _currentFleet = new();

    // Event invoked whenever the fleet state changes, allowing UI components to react
    public event Action<List<TruckDisplayVm>>? OnFleetUpdated;

    /// <summary>
    /// Processes fresh snapshots from the Python Engine and updates the memory store.
    /// Maps the raw binary struct (TruckTelemetry) to the UI-friendly ViewModel (TruckDisplayVm).
    /// </summary>
    public Task UpdateFleet(IEnumerable<TruckTelemetry> snapshots)
    {
        if (snapshots == null) return Task.CompletedTask;

        foreach (var s in snapshots)
        {
            if (s.TruckId <= 0) continue;

            var vm = new TruckDisplayVm
            {
                TruckId = s.TruckId,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Speed = Math.Round(s.Speed, 1),
                FuelConsumed = Math.Round(s.FuelConsumed, 2),
                DistanceTraveled = Math.Round(s.DistanceTraveled, 1),
                TotalCost = s.TotalCost,

                // Real-time status logic derived from the simulation speed
                Status = s.Speed > 90.0 ? "Speeding" : (s.Speed > 5.0 ? "Moving" : "Idle"),

                // Fallback to local time if timestamp is corrupted
                LastUpdated = s.Timestamp > 0 ? DateTime.FromFileTime(s.Timestamp) : DateTime.Now
            };

            // Atomic update: ensures the UI never sees a partial truck object
            _currentFleet.AddOrUpdate(s.TruckId, vm, (id, oldVm) => vm);
        }

        // Snapshot the current values and broadcast to all subscribers (UI/Maps)
        var latestList = GetFormattedFleet();
        OnFleetUpdated?.Invoke(latestList);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Returns the entire fleet ordered by ID for consistent UI display.
    /// </summary>
    public List<TruckDisplayVm> GetFormattedFleet()
    {
        return _currentFleet.Values
            .OrderBy(t => t.TruckId)
            .ToList();
    }
}