using Dapper;
using Microsoft.Data.Sqlite;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace EasyLogistics.Telemetry.System.Infrastructure.Persistence;

/// <summary>
/// High-performance SQLite persistence layer using Dapper.
/// Aligned with IFleetRepository. Handles binary struct-to-DB mapping.
/// </summary>
public sealed class DapperFleetRepository : IFleetRepository
{
    private readonly string _connectionString;
    private readonly ILogger<DapperFleetRepository> _logger;
    private readonly string _absoluteDbPath;

    public DapperFleetRepository(IConfiguration config, ILogger<DapperFleetRepository> logger)
    {
        _logger = logger;

        // Verify the Anchor Path - Hardcoded as requested for local stability
        _absoluteDbPath = @"C:\Users\macim\FinalProject\EasyLogistics\src\EasyLogistics.Telemetry.System.Web\EasyLogistics.db";
        _connectionString = $"Data Source={_absoluteDbPath};Cache=Shared";

        InitializeDatabase();
    }

    public void InitializeDatabase()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();

        // Noah Gift Principle: Optimize for Write Performance (WAL Mode)
        conn.Execute("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;");

        // Telemetry History Table
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS TruckHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TruckId INTEGER NOT NULL,
                Latitude REAL NOT NULL,
                Longitude REAL NOT NULL,
                Speed REAL NOT NULL,
                FuelConsumed REAL NOT NULL,
                DistanceTraveled REAL NOT NULL,
                TotalCost REAL NOT NULL,
                Timestamp INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS IX_TruckHistory_TruckId_Timestamp ON TruckHistory(TruckId, Timestamp DESC);");

        // Identity Table (Dapper User Store)
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS AspNetUsers (
                Id TEXT PRIMARY KEY,
                UserName TEXT,
                NormalizedUserName TEXT,
                Email TEXT,
                NormalizedEmail TEXT,
                PasswordHash TEXT,
                SecurityStamp TEXT,
                ConcurrencyStamp TEXT,
                FirstName TEXT,
                LastName TEXT,
                JoinedDate TEXT,
                IsActive INTEGER
            );");
    }

    /// <summary>
    /// FIXED: Explicitly maps Struct Fields to Dapper Parameters to prevent the 404/Crash.
    /// </summary>
    public async Task SaveSnapshotAsync(IEnumerable<TruckTelemetry> fleet)
    {
        if (fleet == null || !fleet.Any()) return;

        const string sql = @"
            INSERT INTO TruckHistory (TruckId, Latitude, Longitude, Speed, FuelConsumed, DistanceTraveled, TotalCost, Timestamp)
            VALUES (@TruckId, @Latitude, @Longitude, @Speed, @FuelConsumed, @DistanceTraveled, @TotalCost, @Timestamp)";

        try
        {
            using var conn = new SqliteConnection(_connectionString);

            // Dapper cannot "see" fields in a struct. We must project to an anonymous type.
            var projectedParams = fleet.Select(f => new {
                f.TruckId,
                f.Latitude,
                f.Longitude,
                f.Speed,
                f.FuelConsumed,
                f.DistanceTraveled,
                f.TotalCost,
                f.Timestamp
            });

            await conn.ExecuteAsync(sql, projectedParams);
        }
        catch (Exception ex)
        {
            // Logging prevents the BackgroundService from dying (and the 404/SignalR drop)
            _logger.LogError(ex, "[DB] Write transaction failed for fleet snapshot mapping.");
        }
    }

    public async Task<IEnumerable<TruckTelemetry>> GetLatestPositionsAsync()
    {
        using var conn = new SqliteConnection(_connectionString);
        const string sql = @"
            SELECT TruckId, Latitude, Longitude, Speed, FuelConsumed, DistanceTraveled, TotalCost, Timestamp
            FROM (
                SELECT *, ROW_NUMBER() OVER (PARTITION BY TruckId ORDER BY Timestamp DESC) as rn
                FROM TruckHistory
            ) WHERE rn = 1";

        try
        {
            return await conn.QueryAsync<TruckTelemetry>(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DB] Failed to fetch latest fleet positions");
            return Enumerable.Empty<TruckTelemetry>();
        }
    }

    public async Task<IEnumerable<TruckTelemetry>> GetHistoryByIdAsync(int truckId)
    {
        using var conn = new SqliteConnection(_connectionString);
        const string sql = "SELECT * FROM TruckHistory WHERE TruckId = @truckId ORDER BY Timestamp DESC LIMIT 100";
        return await conn.QueryAsync<TruckTelemetry>(sql, new { truckId });
    }
}