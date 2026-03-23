using Dapper;
using Microsoft.Data.Sqlite;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace EasyLogistics.Telemetry.System.Infrastructure.Persistence;

public sealed class DapperFleetRepository : IFleetRepository
{
    private readonly string _connectionString;

    public DapperFleetRepository(IConfiguration config, IHostEnvironment env)
    {
        string dbPath = Path.Combine(env.ContentRootPath, "data", "EasyLogistics.db");
        _connectionString = $"Data Source={dbPath};Cache=Shared";
    }

    public async Task<IEnumerable<TruckDisplayVm>> GetLatestPositionsAsync()
    {
        using var conn = new SqliteConnection(_connectionString);
        const string sql = @"
            SELECT h.*, t.Vin as DriverName 
            FROM (
                SELECT *, ROW_NUMBER() OVER (PARTITION BY TruckId ORDER BY Timestamp DESC) as rn
                FROM TruckHistory
            ) h
            LEFT JOIN Trucks t ON h.TruckId = t.Id
            WHERE h.rn = 1";

        var rawData = await conn.QueryAsync<dynamic>(sql);
        return rawData.Select(d => MapDynamicToVm(d)).Cast<TruckDisplayVm>().ToList();
    }

    public async Task<IEnumerable<TruckDisplayVm>> GetHistoryByIdAsync(int truckId)
    {
        using var conn = new SqliteConnection(_connectionString);
        
        var rawData = await conn.QueryAsync<dynamic>(
            "SELECT * FROM TruckHistory WHERE TruckId = @truckId ORDER BY Timestamp DESC LIMIT 50",
            new { truckId });

        return rawData.Select(d => MapDynamicToVm(d)).Cast<TruckDisplayVm>().ToList();
    }

    public async Task SaveSnapshotAsync(IEnumerable<TruckTelemetry> fleet)
    {
        using var conn = new SqliteConnection(_connectionString);

        const string sql = @"
        INSERT INTO TruckHistory (
            TruckId, Latitude, Longitude, Speed, 
            FuelConsumed, DistanceTraveled, TotalCost, Timestamp
        )
        VALUES (
            @TruckId, @Latitude, @Longitude, @Speed, 
            @FuelConsumed, @DistanceTraveled, @TotalCost, @Timestamp
        )";

        try
        {
            var parameters = fleet.Select(t => new {
                TruckId = t.TruckId,
                Latitude = t.Latitude,
                Longitude = t.Longitude,
                Speed = t.Speed,
                FuelConsumed = t.FuelConsumed,
                DistanceTraveled = t.DistanceTraveled,
                TotalCost = t.TotalCost,
                Timestamp = t.Timestamp
            }).ToList();

            await conn.ExecuteAsync(sql, parameters);
        }
        catch (Exception ex)
        {
            Console.WriteLine($">>>> ❌ DATABASE INSERT ERROR: {ex.Message}");
        }
    }

    private TruckDisplayVm MapDynamicToVm(dynamic d)
    {
        // SAFE CONVERSION LOGIC (Prevents InvalidCastException from dynamic)
        long ticks = d.Timestamp != null ? (long)d.Timestamp : 0;
        double speed = d.Speed != null ? Convert.ToDouble(d.Speed) : 0.0;

        return new TruckDisplayVm
        {
            TruckId = d.TruckId != null ? Convert.ToInt32(d.TruckId) : 0,
            Latitude = d.Latitude != null ? Convert.ToDouble(d.Latitude) : 0.0,
            Longitude = d.Longitude != null ? Convert.ToDouble(d.Longitude) : 0.0,
            Speed = speed,
            FuelConsumed = d.FuelConsumed != null ? Convert.ToDouble(d.FuelConsumed) : 0.0,
            DistanceTraveled = d.DistanceTraveled != null ? Convert.ToDouble(d.DistanceTraveled) : 0.0,
            TotalCost = d.TotalCost != null ? Convert.ToDouble(d.TotalCost) : 0.0,
            Timestamp = ticks,

            // AI Analysis Logic
            DriverName = d.DriverName ?? $"UNIT_{d.TruckId}",
            LastUpdated = ticks > 0
                ? new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().ToString("HH:mm:ss")
                : DateTime.Now.ToString("HH:mm:ss"),

            AiStatus = speed > 85 ? "CRITICAL: OVERSPEED" : "NOMINAL: STEADY",
            SeverityClass = speed > 85 ? "text-danger fw-bold blink" : "text-success",
            Status = speed > 0 ? "In Transit" : "Stationary"
        };
    }
}