using EasyLogistics.Telemetry.System.Core.Models;

namespace EasyLogistics.Telemetry.System.Core.Interfaces;

public interface IFleetHubNotifier
{
    // Ensure this matches the implementation you just provided
    Task EmitFleetUpdate(IEnumerable<TruckTelemetry> fleet);
}