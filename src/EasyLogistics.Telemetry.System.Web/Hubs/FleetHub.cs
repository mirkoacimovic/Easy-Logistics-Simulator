using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace EasyLogistics.Telemetry.System.Web.Hubs;

/// <summary>
/// High-frequency SignalR hub for telemetry broadcasting.
/// Optimized for Phase 4: Uses Group management to reduce unnecessary traffic.
/// </summary>
[Authorize]
public sealed class FleetHub : Hub
{
    private readonly ILogger<FleetHub> _logger;
    private const string MONITORING_GROUP = "ActiveFleetMonitoring";

    public FleetHub(ILogger<FleetHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// When a user opens the Map or Analytics, they join the monitoring group.
    /// </summary>
    public async Task StartMonitoring()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, MONITORING_GROUP);
        _logger.LogInformation("📈 User {User} started live monitoring.", Context.User?.Identity?.Name);
    }

    /// <summary>
    /// Stops the stream for this specific client when they navigate away.
    /// </summary>
    public async Task StopMonitoring()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, MONITORING_GROUP);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("🚚 Client connected: {ConnectionId} (User: {User})",
            Context.ConnectionId,
            Context.User?.Identity?.Name ?? "Authenticated User");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception != null)
        {
            _logger.LogWarning("Client disconnected with error: {Id}. Reason: {Msg}",
                Context.ConnectionId, exception.Message);
        }
        await base.OnDisconnectedAsync(exception);
    }
}