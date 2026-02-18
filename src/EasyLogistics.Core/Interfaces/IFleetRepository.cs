using EasyLogistics.Core.Models;

namespace EasyLogistics.Core.Interfaces;

public interface IFleetRepository
{
    Task SaveSnapshotAsync(IEnumerable<TruckTelemetry> fleet);
    Task<IEnumerable<TruckTelemetry>> GetHistoryByIdAsync(int truckId);
}