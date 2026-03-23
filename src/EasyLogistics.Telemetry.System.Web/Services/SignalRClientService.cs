using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using EasyLogistics.Telemetry.System.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace EasyLogistics.Telemetry.System.Web.Services;

public class SignalRClientService : IDisposable
{
    private readonly IHubContext<FleetHub> _hubContext;
    private readonly IFleetStateService _stateService;
    private const string MONITORING_GROUP = "ActiveFleetMonitoring";

    public SignalRClientService(IHubContext<FleetHub> hubContext, IFleetStateService stateService)
    {
        _hubContext = hubContext;
        _stateService = stateService;

        // Subscribe to the state update event
        _stateService.OnFleetUpdated += HandleFleetUpdated;
    }

    private async void HandleFleetUpdated(List<TruckDisplayVm> fleet)
    {
        try
        {
            // Broadcast formatted data to the specific monitoring group
            await _hubContext.Clients.Group(MONITORING_GROUP).SendAsync("ReceiveFleetUpdate", fleet);
        }
        catch (Exception)
        {
            // Fail silently to prevent worker interruption
        }
    }

    public void Dispose()
    {
        _stateService.OnFleetUpdated -= HandleFleetUpdated;
    }
}