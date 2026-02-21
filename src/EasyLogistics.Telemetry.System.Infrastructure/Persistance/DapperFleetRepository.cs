using Dapper;
using Microsoft.Data.Sqlite;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using Microsoft.Extensions.Configuration;

namespace EasyLogistics.Telemetry.System.Infrastructure.Persistence;

public class DapperFleetRepository : IFleetRepository
{
    private readonly string _connectionString;

    public DapperFleetRepository(IConfiguration config)
    {
        // Points to "EasyLogistics.db" file in your project root
        _connectionString = config.GetConnectionString("Default") ?? "Data Source=EasyLogistics.db";
        using var conn = new SqliteConnection(_connectionString);
        conn.Execute(@"
        CREATE TABLE IF NOT EXISTS TruckHistory (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            TruckId INTEGER NOT NULL,
            Latitude REAL NOT NULL,
            Longitude REAL NOT NULL,
            Speed REAL NOT NULL,
            Timestamp INTEGER NOT NULL
        )");
    }

    public async Task SaveSnapshotAsync(IEnumerable<TruckTelemetry> fleet)
    {
        var dataToSave = fleet?.Where(t => t.Id > 0).ToList();
        if (dataToSave == null || !dataToSave.Any()) return;

        using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        // Manual mapping into Dapper's optimized parameter collection
        var parameters = dataToSave.Select(t => {
            var p = new DynamicParameters();
            p.Add("@Id", t.Id);
            p.Add("@Lat", t.Lat);
            p.Add("@Lng", t.Lng);
            p.Add("@Speed", t.Speed);
            p.Add("@Timestamp", t.Timestamp);
            return p;
        });

        const string sql = @"INSERT INTO TruckHistory (TruckId, Latitude, Longitude, Speed, Timestamp) 
                         VALUES (@Id, @Lat, @Lng, @Speed, @Timestamp)";

        await conn.ExecuteAsync(sql, parameters);
    }

    public async Task<IEnumerable<TruckTelemetry>> GetHistoryByIdAsync(int truckId)
    {
        using var conn = new SqliteConnection(_connectionString);
        return await conn.QueryAsync<TruckTelemetry>(
            "SELECT * FROM TruckHistory WHERE TruckId = @truckId ORDER BY Timestamp DESC LIMIT 100",
            new { truckId });
    }
}