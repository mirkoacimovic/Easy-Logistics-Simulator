using Dapper;
using Microsoft.Data.Sqlite;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using EasyLogistics.Telemetry.System.Core.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting; // PRO: Use generic Hosting Abstractions

namespace EasyLogistics.Telemetry.System.Infrastructure.Persistence;

public sealed class DapperFleetRepository : IFleetRepository
{
    private readonly string _connectionString;
    private readonly ILogger<DapperFleetRepository> _logger;

    // Use IHostEnvironment instead of IWebHostEnvironment for Infrastructure purity
    public DapperFleetRepository(IConfiguration config, ILogger<DapperFleetRepository> logger, IHostEnvironment env)
    {
        _logger = logger;

        // PRO: Resolve DB path relative to ContentRoot
        string dbPath = Path.Combine(env.ContentRootPath, "EasyLogistics.db");
        _connectionString = $"Data Source={dbPath};Cache=Shared";

        InitializeDatabase();
    }

    public void InitializeDatabase()
    {
        try
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            // WAL mode for high-frequency writes
            conn.Execute("PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL;");

            // 1. Current State
            conn.Execute(@"
                CREATE TABLE IF NOT EXISTS Trucks (
                    TruckId INTEGER PRIMARY KEY,
                    Latitude REAL NOT NULL,
                    Longitude REAL NOT NULL,
                    Speed REAL NOT NULL,
                    Status TEXT NOT NULL,
                    FuelConsumed REAL NOT NULL,
                    DistanceTraveled REAL NOT NULL,
                    TotalCost REAL NOT NULL,
                    LastUpdated TEXT NOT NULL,
                    OriginLat REAL, OriginLon REAL,
                    TargetLat REAL, TargetLon REAL
                );");

            // 2. Telemetry History
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
                CREATE INDEX IF NOT EXISTS IX_TruckHistory_Lookup ON TruckHistory(TruckId, Timestamp DESC);");

            // 3. Identity
            conn.Execute(@"
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

            SeedInitialFleet(conn);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "FATAL: Database initialization failed.");
        }
    }

    private void SeedInitialFleet(SqliteConnection conn)
    {
        var count = conn.ExecuteScalar<int>("SELECT COUNT(*) FROM Trucks");
        if (count == 0)
        {
            conn.Execute(@"
                INSERT INTO Trucks (TruckId, Latitude, Longitude, Speed, Status, FuelConsumed, DistanceTraveled, TotalCost, LastUpdated)
                VALUES (1, 44.8186, 20.4689, 0, 'Idle', 0, 0, 0, datetime('now'))");
            _logger.LogInformation("🌱 Database Seeded: Initial fleet state created.");
        }
    }

    public async Task SaveSnapshotAsync(IEnumerable<TruckTelemetry> fleet)
    {
        if (fleet == null || !fleet.Any()) return;

        const string sql = @"
            INSERT INTO TruckHistory (TruckId, Latitude, Longitude, Speed, FuelConsumed, DistanceTraveled, TotalCost, Timestamp)
            VALUES (@TruckId, @Latitude, @Longitude, @Speed, @FuelConsumed, @DistanceTraveled, @TotalCost, @Timestamp)";

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        using var transaction = conn.BeginTransaction();

        try
        {
            await conn.ExecuteAsync(sql, fleet, transaction);
            await transaction.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DB] Snapshot batch write failed.");
            await transaction.RollbackAsync();
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

        return await conn.QueryAsync<TruckTelemetry>(sql);
    }

    public async Task<IEnumerable<TruckTelemetry>> GetHistoryByIdAsync(int truckId)
    {
        using var conn = new SqliteConnection(_connectionString);
        const string sql = "SELECT * FROM TruckHistory WHERE TruckId = @truckId ORDER BY Timestamp DESC LIMIT 100";
        return await conn.QueryAsync<TruckTelemetry>(sql, new { truckId });
    }
}