using EasyLogistics.Telemetry.System.Core.Models;

namespace EasyLogistics.Telemetry.System.Core.Interfaces;

public interface IFleetRepository
{
    Task SaveSnapshotAsync(IEnumerable<TruckTelemetry> fleet);
    Task<IEnumerable<TruckDisplayVm>> GetLatestPositionsAsync();
    Task<IEnumerable<TruckDisplayVm>> GetHistoryByIdAsync(int truckId);
}