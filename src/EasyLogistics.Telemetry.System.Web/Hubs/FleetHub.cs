using EasyLogistics.Telemetry.System.Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EasyLogistics.Telemetry.System.Web.Hubs;

public sealed class FleetHub : Hub
{
    private readonly IFleetStateService _stateService;

    public FleetHub(IFleetStateService stateService)
    {
        _stateService = stateService;
    }

    public override async Task OnConnectedAsync()
    {
        var currentFleet = _stateService.GetFormattedFleet();
        if (currentFleet != null && currentFleet.Any())
        {
            await Clients.Caller.SendAsync("ReceiveFleetUpdate", currentFleet);
        }

        await base.OnConnectedAsync();
    }
}