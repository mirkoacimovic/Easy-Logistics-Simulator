using EasyLogistics.Core.Models;

namespace EasyLogistics.Core.Services;

public class FleetAnalytics
{
    private const double SpeedLimit = 90.0; // km/h for trucks in many regions

    public bool IsSpeeding(TruckTelemetry truck) => truck.Speed > SpeedLimit;

    public string GetStatus(TruckTelemetry truck)
    {
        if (truck.Speed < 1.0) return "Idle";
        if (IsSpeeding(truck)) return "Speeding";
        return "Moving";
    }
}