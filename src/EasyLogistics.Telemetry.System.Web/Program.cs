using System.Runtime.Versioning;
using EasyLogistics.Telemetry.System.Core.Entities;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using EasyLogistics.Telemetry.System.Infrastructure;
using EasyLogistics.Telemetry.System.Infrastructure.Persistence;
using EasyLogistics.Telemetry.System.Infrastructure.Services;
using EasyLogistics.Telemetry.System.Web.Components;
using EasyLogistics.Telemetry.System.Web.Hubs;
using EasyLogistics.Telemetry.System.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components;
using Serilog;

[assembly: SupportedOSPlatform("windows")]

var builder = WebApplication.CreateBuilder(args);

// --- 1. LOGGING ---
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// --- 2. CORE SERVICES ---
builder.Services.AddFleetInfrastructure();
builder.Services.AddSingleton<ITriageService, AiTriageService>();

// --- 3. IDENTITY CORE ---
builder.Services.AddIdentityCore<ApplicationUser>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserStore<ApplicationUser>, DapperUserStore>();
builder.Services.AddScoped<IUserPasswordStore<ApplicationUser>, DapperUserStore>();
builder.Services.AddScoped<IUserEmailStore<ApplicationUser>, DapperUserStore>();

// --- 4. AUTHENTICATION & LOCKDOWN ---
builder.Services.AddAuthentication(options => {
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options => {
    options.Cookie.Name = "Fleet.Identity";
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthorization(options => {
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddCascadingAuthenticationState();

// --- 5. BLAZOR & SIGNALR PLUMBING ---
builder.Services.AddAntiforgery();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.AddSingleton<IFleetHubNotifier, FleetHubNotifier>();
builder.Services.AddHostedService<FleetWorker>();

// Fixed: Register the HubConnection factory for DI
builder.Services.AddScoped<HubConnection>(sp => {
    var nav = sp.GetRequiredService<NavigationManager>();
    return new HubConnectionBuilder()
        .WithUrl(nav.ToAbsoluteUri("/fleethub"))
        .WithAutomaticReconnect()
        .Build();
});

// Fixed: SignalRClientService now gets its HubConnection via DI
builder.Services.AddScoped<SignalRClientService>();

var app = builder.Build();

// --- 6. DATABASE SEEDING ---
using (var scope = app.Services.CreateScope())
{
    try
    {
        var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();
        if (repo is DapperFleetRepository d) d.InitializeDatabase();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await IdentitySeeder.SeedAdminUser(userManager);
    }
    catch (Exception ex) { Log.Error(ex, "Seeding failed."); }
}

// --- 7. MIDDLEWARE ---
app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

// --- 8. ENDPOINTS ---
app.MapGet("/logout-action", async (SignInManager<ApplicationUser> sm) => {
    await sm.SignOutAsync();
    return Results.Redirect("/login");
}).AllowAnonymous();

app.MapHub<FleetHub>("/fleethub");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AllowAnonymous();

Log.Information("🚀 FLEET ENGINE ONLINE: Port 7000");
app.Run();