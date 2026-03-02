namespace EasyLogistics.Telemetry.System.Core.Configuration;

/// <summary>
/// Domain configuration model for the Fleet Engine.
/// Placed in Core to allow all layers to reference settings.
/// </summary>
public class FleetSettings
{
    public int MaxTrucks { get; set; } = 50;
    public int RefreshRateHz { get; set; } = 1;
    public string ShmName { get; set; } = "TruckTelemetryBridge";
    public List<HubConfig> LogisticsHubs { get; set; } = new();
}

public class HubConfig
{
    public string Name { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
}