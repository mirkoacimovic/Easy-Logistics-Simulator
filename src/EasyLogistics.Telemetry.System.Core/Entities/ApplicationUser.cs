using Microsoft.AspNetCore.Identity;
using System;

namespace EasyLogistics.Telemetry.System.Core.Entities;

public class ApplicationUser : IdentityUser
{
    // Properties missing from your current build:
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Helper for the UI
    public string FullName => $"{FirstName} {LastName}";
}