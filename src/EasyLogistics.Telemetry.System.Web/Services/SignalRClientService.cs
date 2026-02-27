using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components.Authorization;
using EasyLogistics.Telemetry.System.Core.Models;

namespace EasyLogistics.Telemetry.System.Web.Services;

public class SignalRClientService : IAsyncDisposable
{
    private readonly HubConnection _connection;
    private readonly ILogger<SignalRClientService> _logger;
    private readonly AuthenticationStateProvider _authStateProvider;

    // Fixed: Now using the display-optimized View Model
    public event Action<IEnumerable<TruckDisplayVm>>? OnFleetReceived;

    public SignalRClientService(
        HubConnection connection,
        ILogger<SignalRClientService> logger,
        AuthenticationStateProvider authStateProvider)
    {
        _connection = connection;
        _logger = logger;
        _authStateProvider = authStateProvider;

        // Listener for the Hub broadcast
        _connection.On<IEnumerable<TruckDisplayVm>>("ReceiveFleetUpdate", fleet =>
        {
            OnFleetReceived?.Invoke(fleet);
        });
    }

    public async Task StartAsync()
    {
        try
        {
            if (_connection.State != HubConnectionState.Disconnected) return;

            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("SignalR: Connection deferred - User is anonymous.");
                return;
            }

            await _connection.StartAsync();
            _logger.LogInformation("🚀 SignalR: FleetHub Connected.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR: Failed to connect.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.DisposeAsync();
    }
}