using EasyLogistics.Core.Interfaces;
using EasyLogistics.Core.Services;
using EasyLogistics.Infrastructure.Bridge;
using EasyLogistics.Infrastructure.Health;
using EasyLogistics.Infrastructure.Persistence;
using EasyLogistics.Infrastructure.Services;
using EasyLogistics.Web.Components;
using EasyLogistics.Web.Hubs;
using EasyLogistics.Web.Services;
using Serilog;

//var builder = WebApplication.CreateBuilder(args);

//// Add services to the container.
//builder.Services.AddRazorComponents()
//    .AddInteractiveServerComponents();

//// 2. Register the Worker as a Hosted Service
//builder.Services.AddHostedService<FleetWorker>();
//builder.Services.AddSingleton<IFleetStateService, FleetStateService>();
//builder.Services.AddSingleton<IFleetBridge, MemoryMappedBridge>();
//// This worker pushes to the Browser (SignalR)
//builder.Services.AddHostedService<FleetDispatcher>();
//builder.Services.AddSingleton<FleetAnalytics>();
//builder.Services.AddHealthChecks()
//    .AddCheck<BridgeHealthCheck>("bridge_check");
//builder.Services.AddScoped<SignalRClientService>();
//builder.Services.AddScoped<IFleetRepository, DapperFleetRepository>();
//builder.Services.AddSignalR();

//Log.Logger = new LoggerConfiguration()
//    .WriteTo.Console()
//    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
//    .CreateLogger();

//builder.Host.UseSerilog();

//var app = builder.Build();

//app.MapHealthChecks("/health");

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
