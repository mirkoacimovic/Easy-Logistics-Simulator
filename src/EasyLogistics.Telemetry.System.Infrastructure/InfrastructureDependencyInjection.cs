using System.Runtime.Versioning;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Infrastructure.Bridge;
using EasyLogistics.Telemetry.System.Infrastructure.Persistence;
using EasyLogistics.Telemetry.System.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EasyLogistics.Telemetry.System.Infrastructure;

[SupportedOSPlatform("windows")]
public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddFleetInfrastructure(this IServiceCollection services)
    {
        // 1. DATA PERSISTENCE (Fixes DB Seed Failure)
        services.AddScoped<IFleetRepository, DapperFleetRepository>();

        // 2. THE IPC BRIDGE (Connects Python memory to C#)
        // Note: Using the MemoryMappedBridge class you provided
        services.AddSingleton<IFleetBridge, MemoryMappedBridge>();

        // 3. THE LIVE STATE (Singleton shared across all UI sessions)
        services.AddSingleton<IFleetStateService, FleetStateService>();

        return services;
    }
}