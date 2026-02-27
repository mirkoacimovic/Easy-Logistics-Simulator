using Dapper;
using EasyLogistics.Telemetry.System.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace EasyLogistics.Telemetry.System.Infrastructure.Persistence;

public class DapperUserStore : IUserStore<ApplicationUser>, IUserPasswordStore<ApplicationUser>, IUserEmailStore<ApplicationUser>
{
    private readonly string _connectionString;
    private readonly string _absoluteDbPath;

    public DapperUserStore(IConfiguration config)
    {
        // Aligning with the anchor path defined in DapperFleetRepository
        _absoluteDbPath = @"C:\Users\macim\FinalProject\EasyLogistics\src\EasyLogistics.Telemetry.System.Web\EasyLogistics.db";
        _connectionString = $"Data Source={_absoluteDbPath}";
    }

    public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken ct)
    {
        using var conn = new SqliteConnection(_connectionString);
        const string sql = @"
            INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, PasswordHash, FirstName, LastName, JoinedDate, IsActive)
            VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @PasswordHash, @FirstName, @LastName, @JoinedDate, @IsActive)";

        await conn.ExecuteAsync(sql, user);
        return IdentityResult.Success;
    }

    public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct)
    {
        using var conn = new SqliteConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<ApplicationUser>(
            "SELECT * FROM AspNetUsers WHERE NormalizedUserName = @name", new { name = normalizedUserName });
    }

    public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken ct)
    {
        using var conn = new SqliteConnection(_connectionString);
        return await conn.QueryFirstOrDefaultAsync<ApplicationUser>(
            "SELECT * FROM AspNetUsers WHERE Id = @id", new { id = userId });
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken ct)
    {
        using var conn = new SqliteConnection(_connectionString);
        // NormalizedEmail is what UserManager uses for lookups
        return await conn.QueryFirstOrDefaultAsync<ApplicationUser>(
            "SELECT * FROM AspNetUsers WHERE NormalizedEmail = @email", new { email = normalizedEmail });
    }

    // --- Required Interface Implementations ---
    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.Id);
    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.UserName);
    public Task SetUserNameAsync(ApplicationUser user, string? name, CancellationToken ct) { user.UserName = name; return Task.CompletedTask; }
    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.NormalizedUserName);
    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? name, CancellationToken ct) { user.NormalizedUserName = name; return Task.CompletedTask; }

    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.PasswordHash);
    public Task SetPasswordHashAsync(ApplicationUser user, string? hash, CancellationToken ct) { user.PasswordHash = hash; return Task.CompletedTask; }
    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(!string.IsNullOrEmpty(user.PasswordHash));

    public Task<string?> GetEmailAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.Email);
    public Task SetEmailAsync(ApplicationUser user, string? email, CancellationToken ct) { user.Email = email; return Task.CompletedTask; }
    public Task<string?> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(user.NormalizedEmail);
    public Task SetNormalizedEmailAsync(ApplicationUser user, string? email, CancellationToken ct) { user.NormalizedEmail = email; return Task.CompletedTask; }
    public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(true);
    public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken ct) => Task.CompletedTask;

    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken ct)
    {
        using var conn = new SqliteConnection(_connectionString);
        const string sql = "UPDATE AspNetUsers SET UserName = @UserName, PasswordHash = @PasswordHash, NormalizedUserName = @NormalizedUserName, Email = @Email, NormalizedEmail = @NormalizedEmail WHERE Id = @Id";
        await conn.ExecuteAsync(sql, user);
        return IdentityResult.Success;
    }

    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken ct) => throw new NotImplementedException();
    public void Dispose() { }
}