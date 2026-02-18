using EasyLogistics.Core.Interfaces;
using EasyLogistics.Infrastructure.Services;
using EasyLogistics.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EasyLogistics.Web.Services;

public class FleetDispatcher : IHostedService, IDisposable
{
    private readonly IFleetStateService _stateService;
    private readonly IHubContext<FleetHub> _hubContext;
    private Timer? _timer;

    public FleetDispatcher(IFleetStateService stateService, IHubContext<FleetHub> hubContext)
    {
        _stateService = stateService;
        _hubContext = hubContext;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Broadcast to all connected browsers 5 times per second (200ms)
        _timer = new Timer(DoBroadcast, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(200));
        return Task.CompletedTask;
    }

    private void DoBroadcast(object? state)
    {
        var fleet = _stateService.GetFormattedFleet();
        if (fleet.Any())
        {
            // Send to the SignalR Hub
            _hubContext.Clients.All.SendAsync("ReceiveFleetUpdate", fleet);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose() => _timer?.Dispose();
}