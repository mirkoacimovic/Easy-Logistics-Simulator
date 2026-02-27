using EasyLogistics.Telemetry.System.Core.Models;

namespace EasyLogistics.Telemetry.System.Core.Interfaces;

public interface IFleetRepository
{
    Task SaveSnapshotAsync(IEnumerable<TruckTelemetry> fleet);
    Task<IEnumerable<TruckTelemetry>> GetHistoryByIdAsync(int truckId);
    Task<IEnumerable<TruckTelemetry>> GetLatestPositionsAsync();
}