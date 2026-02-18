using Microsoft.AspNetCore.SignalR;
using EasyLogistics.Core.Interfaces;

namespace EasyLogistics.Web.Hubs;

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