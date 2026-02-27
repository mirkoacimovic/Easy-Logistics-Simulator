namespace EasyLogistics.Telemetry.System.Core.Interfaces;

public interface ITriageService
{
    Task<string> AnalyzeTelemetryAsync(string truckId, double temperature, double fuelLevel);
}