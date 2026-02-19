using Microsoft.AspNetCore.SignalR;
using EasyLogistics.Telemetry.System.Core.Interfaces;

namespace EasyLogistics.Telemetry.System.Web.Hubs;

public class FleetHub : Hub
{
    private readonly IFleetStateService _stateService;

    // Fixed: Injecting IFleetStateService (Interface) instead of the concrete class
    public FleetHub(IFleetStateService stateService)
    {
        _stateService = stateService;
    }

    public override async Task OnConnectedAsync()
    {
        var initialFleet = _stateService.GetFormattedFleet();
        await Clients.Caller.SendAsync("ReceiveFleetUpdate", _stateService.GetFormattedFleet());
        await base.OnConnectedAsync();
    }
}