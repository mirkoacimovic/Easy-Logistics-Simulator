namespace EasyLogistics.Telemetry.System.Core.Models;

public class TruckDisplayVm
{
    public int TruckId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Speed { get; set; }
    public double FuelConsumed { get; set; }
    public double DistanceTraveled { get; set; }
    public double TotalCost { get; set; }
    public long Timestamp { get; set; }

    // UI Logic
    public string Status { get; set; } = "Stationary";
    public string LastUpdated { get; set; } = "";
    public string AiStatus { get; set; } = "NOMINAL";
    public string SeverityClass { get; set; } = "text-success";
    public string DriverName { get; set; } = "Unknown";
}