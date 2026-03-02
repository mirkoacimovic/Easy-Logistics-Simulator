using EasyLogistics.Telemetry.System.Core.Configuration;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasyLogistics.Telemetry.System.Infrastructure.Workers;

public class FleetWorker : BackgroundService
{
    private readonly IHubContext<FleetHub> _hubContext;
    private readonly IFleetBridge _bridge;
    private readonly IFleetStateService _stateService;
    private readonly FleetSettings _settings;
    private readonly ILogger<FleetWorker> _logger;

    public FleetWorker(
        IHubContext<FleetHub> hubContext,
        IFleetBridge bridge,
        IFleetStateService stateService,
        IOptions<FleetSettings> settings,
        ILogger<FleetWorker> logger)
    {
        _hubContext = hubContext;
        _bridge = bridge;
        _stateService = stateService;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚛 FleetWorker Loop Started. Target MMF: {ShmName}", _settings.ShmName);

        // Calculate frequency based on Hz configuration
        var delayMs = 1000 / (_settings.RefreshRateHz > 0 ? _settings.RefreshRateHz : 1);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1. Read raw bytes from Python-generated Shared Memory
                var snapshots = _bridge.ReadFleet();

                if (snapshots != null && snapshots.Any())
                {
                    // 2. Process logic (Route resolution, Speed status)
                    await _stateService.UpdateFleet(snapshots);

                    // 3. Retrieve the ViewModels (TruckDisplayVm)
                    var updatedFleet = _stateService.GetFormattedFleet();

                    // 4. Broadcast to SignalR Hub
                    await _hubContext.Clients.All.SendAsync(
                        "ReceiveFleetUpdate",
                        updatedFleet,
                        cancellationToken: stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("FleetWorker is shutting down gracefully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error in FleetWorker loop.");
            }

            await Task.Delay(delayMs, stoppingToken);
        }
    }
}