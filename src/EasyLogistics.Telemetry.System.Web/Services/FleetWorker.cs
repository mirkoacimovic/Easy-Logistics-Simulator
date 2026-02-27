using System.Runtime.Versioning;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasyLogistics.Telemetry.System.Infrastructure.Services;

[SupportedOSPlatform("windows")]
public sealed class FleetWorker : BackgroundService
{
    private readonly ILogger<FleetWorker> _logger;
    private readonly IFleetBridge _bridge;
    private readonly IFleetStateService _stateService;

    public FleetWorker(
        ILogger<FleetWorker> logger,
        IFleetBridge bridge,
        IFleetStateService stateService)
    {
        _logger = logger;
        _bridge = bridge;
        _stateService = stateService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("===> [PHASE 4] TELEMETRY LOOP ACTIVE");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var snapshots = _bridge.ReadFleet();

                if (snapshots != null && snapshots.Any())
                {
                    await _stateService.UpdateFleet(snapshots);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telemetry Loop pulse failure.");
            }

            await Task.Delay(1000, stoppingToken);
        }
    }
}