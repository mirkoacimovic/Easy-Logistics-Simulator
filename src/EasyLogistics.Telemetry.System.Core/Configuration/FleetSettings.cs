namespace EasyLogistics.Telemetry.System.Core.Configuration;

public class FleetSettings
{
    public int MaxTrucks { get; set; } = 50;
    public int RefreshRateHz { get; set; } = 1;

    // The name used for Windows MMF or Linux SHM handles
    public string ShmName { get; set; } = "TruckTelemetryBridge";

    public string SharedMemoryPath { get; set; } = "/app/data/TruckTelemetryBridge.bin";

    public List<HubConfig> LogisticsHubs { get; set; } = new();
}

public class HubConfig
{
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}