using Dapper;
using EasyLogistics.Telemetry.System.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EasyLogistics.Telemetry.System.Infrastructure.Persistence;

public class DapperUserStore : IUserStore<ApplicationUser>, IUserPasswordStore<ApplicationUser>, IUserEmailStore<ApplicationUser>
{
    private readonly string _connectionString;

    public DapperUserStore(IConfiguration config, IHostEnvironment env)
    {
        string dbPath = Path.Combine(env.ContentRootPath, "data", "EasyLogistics.db");
        _connectionString = $"Data Source={dbPath};Cache=Shared";
    }

    public async Task EnsureSchemaAsync()
    {
        using var conn = new SqliteConnection(_connectionString);
        await conn.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS AspNetUsers (
                Id TEXT PRIMARY KEY,
                UserName TEXT,
                NormalizedUserName TEXT,
                Email TEXT,
                NormalizedEmail TEXT,
                PasswordHash TEXT,
                FirstName TEXT,
                LastName TEXT,
                JoinedDate TEXT,
                IsActive INTEGER
            );");
    }

    private ApplicationUser? MapUser(dynamic? row)
    {
        if (row == null) return null;

        var user = new ApplicationUser
        {
            Id = (string)row.Id,
            UserName = (string)row.UserName,
            NormalizedUserName = (string)row.NormalizedUserName,
            Email = (string)row.Email,
            NormalizedEmail = (string)row.NormalizedEmail,
            PasswordHash = (string)row.PasswordHash,
            FirstName = (string)row.FirstName,
            LastName = (string)row.LastName,
            IsActive = (row.IsActive is long l ? l : 0) == 1
        };

        DateTime parsedDate;
        string? dateString = row.JoinedDate?.ToString();

        if (!string.IsNullOrEmpty(dateString) && DateTime.TryParse(dateString, out parsedDate))
        {
            user.JoinedDate = parsedDate;
        }
        else
        {
            user.JoinedDate = DateTime.UtcNow;
        }

        return user;
    }

    public async Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken ct)
    {
        using var conn = new SqliteConnection(_connectionString);
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM AspNetUsers WHERE NormalizedEmail = @email", new { email = normalizedEmail });
        return MapUser(row);
    }

    public async Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken ct)
    {
        using var conn = new SqliteConnection(_connectionString);
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM AspNetUsers WHERE Id = @id", new { id = userId });
        return MapUser(row);
    }

    public async Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken ct)
    {
        using var conn = new SqliteConnection(_connectionString);
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM AspNetUsers WHERE NormalizedUserName = @name", new { name = normalizedUserName });
        return MapUser(row);
    }

    public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken ct)
    {
        using var conn = new SqliteConnection(_connectionString);
        const string sql = @"
            INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, PasswordHash, FirstName, LastName, JoinedDate, IsActive)
            VALUES (@Id, @UserName, @NormalizedUserName, @Email, @NormalizedEmail, @PasswordHash, @FirstName, @LastName, @JoinedDate, @IsActive)";

        await conn.ExecuteAsync(sql, new
        {
            user.Id,
            user.UserName,
            user.NormalizedUserName,
            user.Email,
            user.NormalizedEmail,
            user.PasswordHash,
            user.FirstName,
            user.LastName,
            JoinedDate = user.JoinedDate.ToString("o"), 
            IsActive = user.IsActive ? 1 : 0
        });
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken ct)
    {
        using var conn = new SqliteConnection(_connectionString);
        const string sql = @"
            UPDATE AspNetUsers 
            SET UserName = @UserName, PasswordHash = @PasswordHash, NormalizedUserName = @NormalizedUserName, 
                Email = @Email, NormalizedEmail = @NormalizedEmail, FirstName = @FirstName, 
                LastName = @LastName, IsActive = @IsActive, JoinedDate = @JoinedDate
            WHERE Id = @Id";

        await conn.ExecuteAsync(sql, new
        {
            user.UserName,
            user.PasswordHash,
            user.NormalizedUserName,
            user.Email,
            user.NormalizedEmail,
            user.FirstName,
            user.LastName,
            IsActive = user.IsActive ? 1 : 0,
            JoinedDate = user.JoinedDate.ToString("o"),
            user.Id
        });
        return IdentityResult.Success;
    }

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
    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken ct) => throw new NotImplementedException();
    public void Dispose() { }
}