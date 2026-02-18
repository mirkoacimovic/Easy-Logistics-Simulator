using Dapper;
using Microsoft.Data.SqlClient;
using EasyLogistics.Core.Interfaces;
using EasyLogistics.Core.Models;
using Microsoft.Extensions.Configuration;

namespace EasyLogistics.Infrastructure.Persistence;

public class DapperFleetRepository : IFleetRepository
{
    private readonly string _connectionString;

    public DapperFleetRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection") ?? "";
    }

    public async Task SaveSnapshotAsync(IEnumerable<TruckTelemetry> fleet)
    {
        using var conn = new SqlConnection(_connectionString);
        const string sql = @"INSERT INTO TruckHistory (TruckId, Latitude, Longitude, Speed, Timestamp) 
                             VALUES (@Id, @Latitude, @Longitude, @Speed, @Timestamp)";

        // Dapper handles the collection mapping automatically
        await conn.ExecuteAsync(sql, fleet.Where(t => t.Id > 0));
    }

    public async Task<IEnumerable<TruckTelemetry>> GetHistoryByIdAsync(int truckId)
    {
        using var conn = new SqlConnection(_connectionString);
        return await conn.QueryAsync<TruckTelemetry>(
            "SELECT * FROM TruckHistory WHERE TruckId = @truckId ORDER BY Timestamp DESC",
            new { truckId });
    }
}