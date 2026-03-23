using Microsoft.Extensions.Diagnostics.HealthChecks;
using EasyLogistics.Telemetry.System.Core.Interfaces;

namespace EasyLogistics.Telemetry.System.Infrastructure.Health;

public class BridgeHealthCheck : IHealthCheck
{
    private readonly IFleetBridge _bridge;

    public BridgeHealthCheck(IFleetBridge bridge) => _bridge = bridge;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var data = _bridge.ReadFleet();

        if (data.Any())
        {
            return Task.FromResult(HealthCheckResult.Healthy($"Bridge Online: {data.Length} units reporting."));
        }

        // If data is empty, it means EnsureInitialized() failed or the engine is idling
        return Task.FromResult(HealthCheckResult.Degraded("Bridge Offline: Waiting for Python Engine SHM..."));
    }
}