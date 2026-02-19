using EasyLogistics.Telemetry.System.Core.Models;

namespace EasyLogistics.Telemetry.System.Core.Interfaces;

public interface IFleetBridge : IDisposable
{
    void WriteFleet(TruckTelemetry[] fleet);
    TruckTelemetry[] ReadFleet();
}