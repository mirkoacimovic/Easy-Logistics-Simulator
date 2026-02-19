using Microsoft.Extensions.Diagnostics.HealthChecks;
using EasyLogistics.Telemetry.System.Core.Interfaces;

namespace EasyLogistics.Telemetry.System.Infrastructure.Health;

public class BridgeHealthCheck : IHealthCheck
{
    private readonly IFleetBridge _bridge;

    public BridgeHealthCheck(IFleetBridge bridge) => _bridge = bridge;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            // Try a test read
            _bridge.ReadFleet();
            return Task.FromResult(HealthCheckResult.Healthy("Memory Map Bridge is responding."));
        }
        catch
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Memory Map Bridge is disconnected."));
        }
    }
}