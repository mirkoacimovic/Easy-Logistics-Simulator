using System.Data;
using System.Runtime.Versioning;
using EasyLogistics.Telemetry.System.Core.Configuration;
using EasyLogistics.Telemetry.System.Core.Entities;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Infrastructure;
using EasyLogistics.Telemetry.System.Infrastructure.Persistence;
using EasyLogistics.Telemetry.System.Infrastructure.Services;
using EasyLogistics.Telemetry.System.Infrastructure.Workers;
using EasyLogistics.Telemetry.System.Web.Components;
using EasyLogistics.Telemetry.System.Web.Hubs;
using EasyLogistics.Telemetry.System.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Serilog;

[assembly: SupportedOSPlatform("windows")]

var builder = WebApplication.CreateBuilder(args);

// --- 1. PRO LOGGING (Serilog) ---
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// --- 2. INFRASTRUCTURE & DATABASE ---
string dbPath = Path.Combine(builder.Environment.ContentRootPath, "EasyLogistics.db");
string connectionString = $"Data Source={dbPath};Cache=Shared";

builder.Services.AddTransient<IDbConnection>(_ => new SqliteConnection(connectionString));

// PRO: Bind the JSON "FleetEngine" section to the FleetSettings class
builder.Services.Configure<FleetSettings>(builder.Configuration.GetSection("FleetEngine"));

builder.Services.AddFleetInfrastructure();
builder.Services.AddSingleton<ITriageService, AiTriageService>();

// --- 3. IDENTITY & AUTHENTICATION ---
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme).AddIdentityCookies();

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<IUserStore<ApplicationUser>, DapperUserStore>();
builder.Services.AddScoped<IUserPasswordStore<ApplicationUser>, DapperUserStore>();
builder.Services.AddScoped<IUserEmailStore<ApplicationUser>, DapperUserStore>();

builder.Services.ConfigureApplicationCookie(options => {
    options.Cookie.Name = "Fleet.Auth";
    options.LoginPath = "/login";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddAuthorization(options => {
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// --- 4. SIGNALR & BLAZOR ---
builder.Services.AddAntiforgery();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

builder.Services.AddSignalR(o => {
    o.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddSingleton<IFleetHubNotifier, FleetHubNotifier>();
builder.Services.AddHostedService<FleetWorker>();
builder.Services.AddScoped<SignalRClientService>();

var app = builder.Build();

// --- 5. INITIALIZATION ---
await using (var scope = app.Services.CreateAsyncScope())
{
    try
    {
        var repo = scope.ServiceProvider.GetRequiredService<IFleetRepository>();
        if (repo is DapperFleetRepository dapperRepo) dapperRepo.InitializeDatabase();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await IdentitySeeder.SeedAdminUser(userManager);
        Log.Information("✅ System Ready: Database and Admin verified.");
    }
    catch (Exception ex) { Log.Fatal(ex, "Initialization failed."); }
}

// --- 6. MIDDLEWARE ---
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseHttpsRedirection();
app.MapStaticAssets();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// --- 7. ENDPOINTS ---
app.MapHub<FleetHub>("/fleethub");
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

Log.Information("🚀 FLEET ENGINE ONLINE | Port: 7000");
app.Run();