using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Infrastructure.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class FleetWorker : BackgroundService
{
    private readonly IFleetBridge _bridge;
    private readonly IFleetStateService _stateService; // Added
    private readonly ILogger<FleetWorker> _logger;

    public FleetWorker(IFleetBridge bridge, IFleetStateService stateService, ILogger<FleetWorker> logger)
    {
        _bridge = bridge;
        _stateService = stateService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var data = _bridge.ReadFleet();
            var activeCount = data.Count(t => t.Id > 0);

            // This will show up in your C# Debug Console
            if (activeCount > 0)
                Console.WriteLine($"[Worker] Reading {activeCount} trucks from memory...");

            _stateService.UpdateFleet(data);
            await Task.Delay(100, stoppingToken);
        }
    }
}