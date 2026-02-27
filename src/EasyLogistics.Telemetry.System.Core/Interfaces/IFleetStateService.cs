using EasyLogistics.Telemetry.System.Core.Models;

namespace EasyLogistics.Telemetry.System.Core.Interfaces;

public interface IFleetStateService
{
    // The "Megaphone" that tells the UI to refresh
    event Action<List<TruckDisplayVm>>? OnFleetUpdated;

    Task UpdateFleet(IEnumerable<TruckTelemetry> snapshots);
    List<TruckDisplayVm> GetFormattedFleet();
}