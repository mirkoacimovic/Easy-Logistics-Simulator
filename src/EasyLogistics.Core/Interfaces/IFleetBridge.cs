using EasyLogistics.Core.Models;

namespace EasyLogistics.Core.Interfaces;

public interface IFleetBridge : IDisposable
{
    void WriteFleet(TruckTelemetry[] fleet);
    TruckTelemetry[] ReadFleet();
}