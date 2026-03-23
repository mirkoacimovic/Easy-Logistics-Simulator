using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EasyLogistics.Telemetry.System.Infrastructure.Workers;

public sealed class FleetWorker : BackgroundService
{
    private readonly ILogger<FleetWorker> _logger;
    private readonly IFleetBridge _bridge;
    private readonly IFleetStateService _stateService;
    private readonly IHubContext<FleetHub> _hubContext;
    private readonly IServiceProvider _serviceProvider;
    private int _tickCounter = 0;

    public FleetWorker(
        ILogger<FleetWorker> logger,
        IFleetBridge bridge,
        IFleetStateService stateService,
        IHubContext<FleetHub> hubContext,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _bridge = bridge;
        _stateService = stateService;
        _hubContext = hubContext;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for Python Engine to map the Memory File
        await Task.Delay(3000, stoppingToken);
        _logger.LogInformation("🚛 FLEET PULSE ONLINE: Persistence loop starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var rawData = _bridge.ReadFleet();
                var activeTrucks = rawData?.Where(t => t.TruckId > 0).ToList();

                if (activeTrucks != null && activeTrucks.Any())
                {
                    // VITAL: Standardize the Timestamp for all trucks in this 'pulse'
                    long currentTicks = DateTime.UtcNow.Ticks;
                    activeTrucks.ForEach(t => t.Timestamp = DateTime.UtcNow.Ticks);

                    // 1. UI Broadcast (Immediate Real-time)
                    await _stateService.UpdateFleet(activeTrucks);
                    var displayFleet = _stateService.GetFormattedFleet();
                    await _hubContext.Clients.All.SendAsync("ReceiveFleetUpdate", displayFleet, stoppingToken);

                    // 2. Persistence (Tick = 500ms, Save = 5000ms)
                    _tickCounter++;
                    if (_tickCounter >= 10)
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var repository = scope.ServiceProvider.GetRequiredService<IFleetRepository>();

                            // This saves the data that populates your History/AI page
                            await repository.SaveSnapshotAsync(activeTrucks);

                            // High-visibility marker for monitoring performance
                            Console.WriteLine($">>>> 💾 PERSISTENCE: Saved {activeTrucks.Count} rows at {DateTime.Now:HH:mm:ss}");
                        }
                        _tickCounter = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Telemetry Loop Error.");
            }

            await Task.Delay(500, stoppingToken);
        }
    }
}