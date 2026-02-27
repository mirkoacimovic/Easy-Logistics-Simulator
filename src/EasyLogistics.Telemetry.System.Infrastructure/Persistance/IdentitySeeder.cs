using EasyLogistics.Telemetry.System.Core.Entities;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

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

        // Check if the user already exists in AspNetUsers via Dapper
        var existingUser = await userManager.FindByEmailAsync(adminEmail);

        if (existingUser == null)
        {
            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid().ToString(),
                UserName = adminEmail,
                Email = adminEmail,
                NormalizedUserName = adminEmail.ToUpper(),
                NormalizedEmail = adminEmail.ToUpper(),
                FirstName = "Fleet",
                LastName = "Manager",
                JoinedDate = DateTime.UtcNow,
                IsActive = true,
                EmailConfirmed = true
            };

            // UserManager handles the Dapper Insert + Password Hashing via your DapperUserStore
            var result = await userManager.CreateAsync(admin, "Trucker123!");

            if (result.Succeeded)
            {
                // Using Console here as it's early in the boot process
                Console.WriteLine("✅ IDENTITY SEED: Created admin@trucksim.com [Pass: Trucker123!]");
            }
            else
            {
                Console.WriteLine("❌ IDENTITY SEED FAILED: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}