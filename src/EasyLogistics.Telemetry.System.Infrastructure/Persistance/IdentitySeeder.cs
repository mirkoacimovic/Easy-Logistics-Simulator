using EasyLogistics.Telemetry.System.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace EasyLogistics.Telemetry.System.Infrastructure.Persistence;

public static class IdentitySeeder
{
    public static async Task SeedAdminUser(UserManager<ApplicationUser> userManager)
    {
        const string adminEmail = "admin@trucksim.com";
        const string defaultPass = "Trucker123!";

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

            var result = await userManager.CreateAsync(admin, defaultPass);

            if (result.Succeeded)
            {
                Log.Information("✅ IDENTITY SEED: Admin user created ({Email})", adminEmail);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Log.Error("❌ IDENTITY SEED FAILED: {Errors}", errors);
            }
        }
    }
}