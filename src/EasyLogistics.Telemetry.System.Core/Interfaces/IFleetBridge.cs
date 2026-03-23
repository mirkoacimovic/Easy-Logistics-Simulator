namespace EasyLogistics.Telemetry.System.Core.Interfaces;

public interface IFleetBridge : IDisposable
{
    TruckTelemetry[] ReadFleet();
    void WriteFleet(TruckTelemetry[] fleet);
}