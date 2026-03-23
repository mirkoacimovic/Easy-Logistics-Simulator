//using EasyLogistics.Telemetry.System.Core.Interfaces;
//using EasyLogistics.Telemetry.System.Infrastructure.Bridge;
//using EasyLogistics.Telemetry.System.Infrastructure.Persistence;
//using EasyLogistics.Telemetry.System.Infrastructure.Services;
//using Microsoft.Extensions.DependencyInjection;

//namespace EasyLogistics.Telemetry.System.Infrastructure;

//public static class DependencyInjection
//{
//    public static IServiceCollection AddFleetInfrastructure(this IServiceCollection services)
//    {
//        services.AddSingleton<IFleetStateService, FleetStateService>();
//        services.AddScoped<IFleetRepository, DapperFleetRepository>();
//        services.AddSingleton<EasyLogistics.Telemetry.System.Core.Interfaces.IFleetBridge, MemoryMappedBridge>();
//        return services;
//    }
//}
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Infrastructure.Bridge;
using EasyLogistics.Telemetry.System.Infrastructure.Persistence;
using EasyLogistics.Telemetry.System.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EasyLogistics.Telemetry.System.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddFleetInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IFleetStateService, FleetStateService>();
        services.AddSingleton<IFleetBridge, MemoryMappedBridge>();
        services.AddScoped<IFleetRepository, DapperFleetRepository>();

        return services;
    }
}