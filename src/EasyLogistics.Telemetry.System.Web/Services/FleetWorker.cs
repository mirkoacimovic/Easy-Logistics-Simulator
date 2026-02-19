using EasyLogistics.Telemetry.System.Core.Interfaces;

public class FleetWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IFleetBridge _bridge;
    private readonly IFleetStateService _stateService;
    private DateTime _lastSaveTime = DateTime.MinValue;

    public FleetWorker(IFleetBridge bridge, IFleetStateService stateService, IServiceProvider serviceProvider)
    {
        _bridge = bridge;
        _stateService = stateService;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var fleetData = _bridge.ReadFleet();

            if (fleetData != null && fleetData.Any())
            {
                _stateService.UpdateFleet(fleetData);

                // Throttled Save
                if (DateTime.UtcNow - _lastSaveTime > TimeSpan.FromSeconds(5))
                {
                    // This is how you use a Scoped Repository in a Singleton Worker:
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();
                        await repo.SaveSnapshotAsync(fleetData);
                    }
                    _lastSaveTime = DateTime.UtcNow;
                }
            }
            await Task.Delay(100, stoppingToken);
        }
    }
}