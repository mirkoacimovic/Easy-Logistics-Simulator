using EasyLogistics.Telemetry.System.Core.Models;

namespace EasyLogistics.Telemetry.System.Core.Interfaces;

/// <summary>
/// Hardware abstraction for the IPC (Inter-Process Communication) bridge.
/// </summary>
public interface IFleetBridge : IDisposable
{
    /// <summary>
    /// Reads the current snapshot from the Shared Memory (Python Engine).
    /// </summary>
    TruckTelemetry[] ReadFleet();

    /// <summary>
    /// Optional: Writes control signals back to the engine.
    /// </summary>
    void WriteFleet(TruckTelemetry[] fleet);
}