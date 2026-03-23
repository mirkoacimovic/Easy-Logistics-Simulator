using EasyLogistics.Telemetry.System.Core.Models;

namespace EasyLogistics.Telemetry.System.Core.Interfaces;

public interface IFleetStateService
{
    event Action<List<TruckDisplayVm>>? OnFleetUpdated;
    Task UpdateFleet(IEnumerable<TruckTelemetry> rawData);
    List<TruckDisplayVm> GetFormattedFleet();
}