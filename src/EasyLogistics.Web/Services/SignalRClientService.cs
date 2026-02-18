using EasyLogistics.Core.ViewModels;
using Microsoft.AspNetCore.SignalR.Client;

namespace EasyLogistics.Web.Services;

public class SignalRClientService
{
    private readonly HubConnection _hubConnection;

    // Fixed Warning: Declared as nullable event
    public event Action<List<TruckDisplayVm>>? OnFleetReceived;

    public SignalRClientService()
    {
        // Matches your 'https' profile in launchSettings.json
        _hubConnection = new HubConnectionBuilder()
            .WithUrl("https://localhost:7000/fleethub")
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<List<TruckDisplayVm>>("ReceiveFleetUpdate", (fleet) =>
        {
            OnFleetReceived?.Invoke(fleet);
        });
    }

    public async Task StartAsync()
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            try
            {
                await _hubConnection.StartAsync();
                Console.WriteLine("SignalR: Connection Established Successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SignalR Connection Error: {ex.Message}");
            }
        }
    }
}