using EasyLogistics.Telemetry.System.Core.Configuration;
using EasyLogistics.Telemetry.System.Core.Entities;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Infrastructure.Bridge;
using EasyLogistics.Telemetry.System.Infrastructure.Health;
using EasyLogistics.Telemetry.System.Infrastructure.Persistence;
using EasyLogistics.Telemetry.System.Infrastructure.Services;
using EasyLogistics.Telemetry.System.Infrastructure.Workers;
using EasyLogistics.Telemetry.System.Web.Components;
using EasyLogistics.Telemetry.System.Web.Filters;
using EasyLogistics.Telemetry.System.Web.Hubs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Data.Sqlite;
using Serilog;
using System.Data;

// Immediate feedback for Docker/Console logs
Console.WriteLine(">>>> 🚛 EASYLOGISTICS: BOOT SEQUENCE INITIATED <<<<");

var builder = WebApplication.CreateBuilder(args);

// 1. HIGH-PERFORMANCE LOGGING
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// 2. CONFIGURATION & CORE PLUMBING
builder.Services.Configure<FleetSettings>(builder.Configuration.GetSection("FleetSettings"));
builder.Services.AddDataProtection();

// 🚀 PERSISTENCE: Use the shared Volume Path
string dbPath = "/app/data/EasyLogistics.db";
var dbDirectory = Path.GetDirectoryName(dbPath);
if (!string.IsNullOrEmpty(dbDirectory)) Directory.CreateDirectory(dbDirectory);

builder.Services.AddTransient<IDbConnection>(sp =>
    new SqliteConnection($"Data Source={dbPath};Cache=Shared"));

builder.Services.AddScoped<IFleetRepository, DapperFleetRepository>();
builder.Services.AddScoped<IUserStore<ApplicationUser>, DapperUserStore>();

// 3. IDENTITY & SECURITY
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies(options => {
        options.ApplicationCookie?.Configure(c => {
            c.LoginPath = "/login";
            c.Cookie.Name = "EasyLogistics.Auth.Token";
            c.Cookie.HttpOnly = true;
            c.ExpireTimeSpan = TimeSpan.FromDays(7);
        });
    });

builder.Services.AddIdentityCore<ApplicationUser>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// 4. UI, REAL-TIME & FILTERS
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSignalR();

builder.Services.AddControllers(options => {
    options.Filters.Add<GlobalExceptionFilter>();
});

builder.Services.AddServerSideBlazor().AddCircuitOptions(options => {
    options.DetailedErrors = true;
});

// 5. INFRASTRUCTURE SINGLETONS
builder.Services.AddSingleton<IFleetStateService, FleetStateService>();
builder.Services.AddSingleton<IFleetBridge, MemoryMappedBridge>();
builder.Services.AddHostedService<FleetWorker>();

// 6. HEALTH MONITORING
builder.Services.AddHealthChecks().AddCheck<BridgeHealthCheck>("python_bridge_check");

// 🚀 PORT BINDING: Respect Railway Environment
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

var app = builder.Build();

// 🚀 PROXY FIX: Trust Railway's X-Forwarded Headers
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// 7. DATABASE SCHEMA & SEEDING
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        Console.WriteLine(">>>> 🛠️  PLUMBING: Initializing UserStore...");
        var userStore = (DapperUserStore)services.GetRequiredService<IUserStore<ApplicationUser>>();
        await userStore.EnsureSchemaAsync();

        Console.WriteLine(">>>> 🛠️  PLUMBING: Validating Admin Identity...");
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        await IdentitySeeder.SeedAdminUser(userManager);

        Console.WriteLine(">>>> 🛠️  PLUMBING: Syncing Truck Master Data...");
        using var db = services.GetRequiredService<IDbConnection>();
        await TruckSeeder.SeedTrucks(db);

        Console.WriteLine(">>>> ✅ PLUMBING: System Core Ready.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($">>>> ❌ PLUMBING CRITICAL FAILURE: {ex.Message}");
    }
}

// 8. MIDDLEWARE PIPELINE
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

// 9. ENDPOINT MAPPING
app.MapHealthChecks("/health");

// 🚀 SIGNALR FIX: Allow Long-Polling for proxy stability
app.MapHub<FleetHub>("/fleethub", options => {
    options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets |
                         Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
});

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapControllers();

// Auth Minimal APIs
app.MapPost("/auth/login-endpoint", async ([FromForm] string email, [FromForm] string password, SignInManager<ApplicationUser> sm) => {
    var result = await sm.PasswordSignInAsync(email, password, true, false);
    return result.Succeeded ? Results.Redirect("/") : Results.Redirect("/login?error=1");
});

app.MapPost("/logout", async (SignInManager<ApplicationUser> sm) => {
    await sm.SignOutAsync();
    return Results.Redirect("/login");
}).AllowAnonymous();

app.Run();