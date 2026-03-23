namespace EasyLogistics.Telemetry.System.Core.Interfaces;

public interface IFleetHubNotifier
{
    Task EmitFleetUpdate(IEnumerable<TruckTelemetry> fleet);
}