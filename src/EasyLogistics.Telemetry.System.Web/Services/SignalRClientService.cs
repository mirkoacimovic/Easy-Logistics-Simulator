using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using EasyLogistics.Telemetry.System.Core.Models;
using Microsoft.AspNetCore.Http;

namespace EasyLogistics.Telemetry.System.Web.Services;

/// <summary>
/// Bridge between the SignalR FleetHub and the Blazor UI.
/// Optimized for Phase 4: Manually bridges the Identity Cookie to prevent 401s.
/// </summary>
public class SignalRClientService : IAsyncDisposable
{
    private HubConnection? _connection;
    private readonly ILogger<SignalRClientService> _logger;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly NavigationManager _navigationManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public event Action<IEnumerable<TruckDisplayVm>>? OnFleetReceived;

    public SignalRClientService(
        ILogger<SignalRClientService> logger,
        AuthenticationStateProvider authStateProvider,
        NavigationManager navigationManager,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger;
        _authStateProvider = authStateProvider;
        _navigationManager = navigationManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task StartAsync()
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated != true)
            {
                _logger.LogWarning("SignalR: Connection deferred - Anonymous user.");
                return;
            }

            if (_connection != null && _connection.State != HubConnectionState.Disconnected)
                return;

            // CRITICAL: Extract the browser's identity cookie to bypass the 401 gate
            var requestCookies = _httpContextAccessor.HttpContext?.Request.Headers["Cookie"].ToString();

            _connection = new HubConnectionBuilder()
                .WithUrl(_navigationManager.ToAbsoluteUri("/fleethub"), options =>
                {
                    // Manually inject the cookies into the SignalR Handshake headers
                    if (!string.IsNullOrEmpty(requestCookies))
                    {
                        options.Headers.Add("Cookie", requestCookies);
                    }

                    options.HttpMessageHandlerFactory = handler =>
                    {
                        if (handler is HttpClientHandler clientHandler)
                        {
                            clientHandler.UseDefaultCredentials = true;
                        }
                        return handler;
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.On<IEnumerable<TruckDisplayVm>>("ReceiveFleetUpdate", fleet =>
            {
                OnFleetReceived?.Invoke(fleet);
            });

            await _connection.StartAsync();
            _logger.LogInformation("🚀 SignalR: Connected with Identity Cookie Bridge.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR: Bridge failed to authenticate.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }
}