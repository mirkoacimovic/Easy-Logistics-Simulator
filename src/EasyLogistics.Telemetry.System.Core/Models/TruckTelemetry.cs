using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct TruckTelemetry
{
    public int TruckId;             // 0-3 (4 bytes)
    private int _pad;               // 4-7 (4 bytes) - REQUIRED FOR DOUBLE ALIGNMENT
    public double Latitude;         // 8-15
    public double Longitude;        // 16-23
    public double Speed;            // 24-31
    public double FuelConsumed;     // 32-39
    public double DistanceTraveled; // 40-47
    public double TotalCost;        // 48-55
    public long Timestamp;          // 56-63 (.NET Ticks)
}

public class TruckDisplayModel
{
    public TruckTelemetry Telemetry { get; set; }

    // Metadata from SQL Joins
    public string? DriverName { get; set; }
    public string? DriverExperience { get; set; }
    public string? AiStatus { get; set; }
}