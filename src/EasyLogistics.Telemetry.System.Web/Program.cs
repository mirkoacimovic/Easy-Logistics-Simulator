using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Services;
using EasyLogistics.Telemetry.System.Infrastructure.Bridge;
using EasyLogistics.Telemetry.System.Infrastructure.Health;
using EasyLogistics.Telemetry.System.Infrastructure.Persistence;
using EasyLogistics.Telemetry.System.Infrastructure.Services;
using EasyLogistics.Telemetry.System.Web.Components;
using EasyLogistics.Telemetry.System.Web.Hubs;
using EasyLogistics.Telemetry.System.Web.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);


// 1. Core Services (Singletons - The "Source of Truth")
builder.Services.AddSingleton<IFleetStateService, FleetStateService>();
builder.Services.AddSingleton<IFleetBridge, MemoryMappedBridge>();
builder.Services.AddSingleton<FleetAnalytics>();

// 2. Infrastructure (The "Pipes")
builder.Services.AddSignalR();
// CHANGE: Make this Singleton so the connection persists during navigation
builder.Services.AddSingleton<SignalRClientService>();

// 3. Background Workers (The "Engine")
builder.Services.AddHostedService<FleetWorker>();     // Reads from Python
builder.Services.AddHostedService<FleetDispatcher>(); // Pushes to SignalR Hub

// 4. Scoped Services (Per-User/Database)
builder.Services.AddScoped<IFleetRepository, DapperFleetRepository>();

// 5. UI and Logging
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddHealthChecks()
    .AddCheck<BridgeHealthCheck>("bridge_check");

var app = builder.Build();

app.MapHealthChecks("/health");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}


app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();

app.MapHub<FleetHub>("/fleethub");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
