using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EasyLogistics.Telemetry.System.Web.Services;

public class FleetHubNotifier
{
    private readonly IHubContext<FleetHub> _hubContext;

    public FleetHubNotifier(IFleetStateService stateService, IHubContext<FleetHub> hubContext)
    {
        _hubContext = hubContext;

        // Subscribe to the StateService event
        stateService.OnFleetUpdated += (trucks) =>
        {
            // Fire-and-forget task to ensure telemetry loop remains low-latency
            _ = Task.Run(async () =>
            {
                try
                {
                    // Broadcasters to the monitoring group
                    // This handles the real-time push to all active dashboard/map users
                    await _hubContext.Clients.All.SendAsync("ReceiveFleetUpdate", trucks);
                }
                catch (Exception ex)
                {
                    // Log to console for debugging bridge stability
                    Console.WriteLine($"[Notifier Error]: {ex.Message}");
                }
            });
        };
    }
}