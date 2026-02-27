using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace EasyLogistics.Telemetry.System.Web.Hubs;

/// <summary>
/// High-frequency SignalR hub for telemetry broadcasting.
/// Only authorized users can subscribe to the stream.
/// </summary>
[Authorize]
public sealed class FleetHub : Hub
{
    private readonly ILogger<FleetHub> _logger;

    public FleetHub(ILogger<FleetHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("🚚 Client connected to FleetHub: {ConnectionId} (User: {User})",
            Context.ConnectionId,
            Context.User?.Identity?.Name ?? "Authenticated User");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogError(exception, "Client disconnected with error: {ConnectionId}", Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}