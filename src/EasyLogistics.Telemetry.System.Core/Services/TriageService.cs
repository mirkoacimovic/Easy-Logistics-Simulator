using EasyLogistics.Telemetry.System.Core.Interfaces;

namespace EasyLogistics.Telemetry.System.Infrastructure.Services;

public class AiTriageService : ITriageService
{
    public async Task<string> AnalyzeTelemetryAsync(string truckId, double temperature, double fuelLevel)
    {
        // For now, this is a rule-based AI "Mock". 
        // Tomorrow, we replace the body with an LLM API call.
        await Task.Delay(300); // Simulate network latency

        if (temperature > 100) return "🔴 CRITICAL: Engine Overheat Detected. Triage: Immediate Halt.";
        if (fuelLevel < 10) return "🟡 WARNING: Low Fuel. Triage: Reroute to nearest station.";

        return "🟢 STABLE: Fleet operations normal.";
    }
}