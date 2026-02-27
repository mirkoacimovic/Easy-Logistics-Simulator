using System.Runtime.InteropServices;

namespace EasyLogistics.Telemetry.System.Core.Models;

/// <summary>
/// The raw binary representation of a truck's state.
/// This struct MUST match the memory layout of the Python simulator.
/// Pack = 1 ensures no padding is added between fields.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct TruckTelemetry
{
    public int Id;               // Unique record ID (4 bytes)
    public int TruckId;          // Vehicle identifier (4 bytes)
    public double Latitude;      // WGS84 Lat (8 bytes)
    public double Longitude;     // WGS84 Lng (8 bytes)
    public double Speed;         // Current km/h (8 bytes)
    public double FuelConsumed;  // Cumulative liters (8 bytes)
    public double DistanceTraveled; // Cumulative km (8 bytes)
    public double TotalCost;     // Financial impact (8 bytes)
    public long Timestamp;       // Windows FileTime or Unix (8 bytes)

    // Total size per struct: 64 bytes.

    /// <summary>
    /// Helper property to convert the raw long timestamp into a readable DateTime.
    /// </summary>
    public readonly DateTime FormattedTime => DateTime.FromFileTime(Timestamp);
}