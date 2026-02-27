using System;

namespace EasyLogistics.Telemetry.System.Core.Models;

/// <summary>
/// View Model optimized for Blazor UI and SignalR broadcasting.
/// Provides aliases and display helpers for Chart.js and Leaflet integration.
/// </summary>
public class TruckDisplayVm
{
    // --- CORE PROPERTIES ---
    // These match the logic in the FleetWorker and FleetStateService
    public int TruckId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Speed { get; set; }
    public double FuelConsumed { get; set; }
    public double DistanceTraveled { get; set; }
    public double TotalCost { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    // --- UI ALIASES ---
    // These allow frontend JS (Map/Charts) to use shorter property names,
    // reducing SignalR payload size and matching standard JS naming conventions.
    public int Id { get => TruckId; set => TruckId = value; }
    public double Lat { get => Latitude; set => Latitude = value; }
    public double Lng { get => Longitude; set => Longitude = value; }
    public double Fuel { get => FuelConsumed; set => FuelConsumed = value; }
    public double Distance { get => DistanceTraveled; set => DistanceTraveled = value; }
    public double Cost { get => TotalCost; set => TotalCost = value; }

    // --- DISPLAY HELPERS ---
    // Used in Data Grids and Tooltips for better readability

    public string DisplayId => $"TRK-{TruckId:D3}";

    public string SpeedDisplay => $"{Speed:F1} km/h";

    public string FuelDisplay => $"{FuelConsumed:F2} L";

    public string DistanceDisplay => $"{DistanceTraveled:N1} km";

    public string CostDisplay => $"€{TotalCost:N2}";

    /// <summary>
    /// Returns a Bootstrap-compatible color class based on the truck status.
    /// </summary>
    public string StatusClass => Status switch
    {
        "Moving" => "success",
        "Speeding" => "danger",
        "Idle" => "warning",
        _ => "secondary"
    };
}