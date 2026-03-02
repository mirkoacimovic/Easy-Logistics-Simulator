using EasyLogistics.Telemetry.System.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace EasyLogistics.Telemetry.System.Infrastructure.Persistence;

public static class IdentitySeeder
{
    /// <summary>
    /// Ensures at least one Admin user exists in the Dapper SQLite database.
    /// This follows the Noah Gift principle of "Automated Environment Setup."
    /// </summary>
    public static async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
    {
        const string adminEmail = "admin@trucksim.com";
        const string defaultPass = "Trucker123!";

        // 1. Check if the user already exists
        var existingUser = await userManager.FindByEmailAsync(adminEmail);

        if (existingUser == null)
        {
            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = adminEmail,
                Email = adminEmail,
                NormalizedUserName = adminEmail.ToUpperInvariant(),
                NormalizedEmail = adminEmail.ToUpperInvariant(),
                FirstName = "Fleet",
                LastName = "Manager",
                JoinedDate = DateTime.UtcNow,
                IsActive = true,
                EmailConfirmed = true
            };

            // 2. Create user with Hashed Password
            // This triggers your DapperUserStore.CreateAsync internally
            var result = await userManager.CreateAsync(admin, defaultPass);

            if (result.Succeeded)
            {
                // Note: We use the static Serilog Log here because this often 
                // runs during app startup before ILogger is injected via DI scope.
                Serilog.Log.Information("✅ IDENTITY SEED: Admin user created ({Email})", adminEmail);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Serilog.Log.Error("❌ IDENTITY SEED FAILED: {Errors}", errors);
            }
        }
        else
        {
            Serilog.Log.Debug("ℹ️ IDENTITY SEED: Admin user already exists.");
        }
    }
}