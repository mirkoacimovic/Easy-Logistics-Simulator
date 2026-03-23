using System.Data;
using Dapper;
using Serilog;

namespace EasyLogistics.Telemetry.System.Infrastructure.Persistence;

public static class TruckSeeder
{
    public static async Task SeedTrucks(IDbConnection db)
    {
        // 1. Ensure Schema is current (Fixes the 'missing DriverId' error)
        await db.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS Drivers (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Experience TEXT,
                LicenseClass TEXT
            );

            CREATE TABLE IF NOT EXISTS Trucks (
                Id TEXT PRIMARY KEY,
                Vin TEXT,
                Status INTEGER,
                DriverId TEXT,
                Latitude REAL,
                Longitude REAL,
                FuelLevel REAL,
                Speed REAL,
                LastUpdated DATETIME,
                FOREIGN KEY(DriverId) REFERENCES Drivers(Id)
            );
            
            CREATE TABLE IF NOT EXISTS TruckHistory (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                TruckId TEXT,
                Latitude REAL,
                Longitude REAL,
                Speed REAL,
                FuelConsumed REAL,
                DistanceTraveled REAL,
                TotalCost REAL,
                Timestamp INTEGER
            );");

        // 2. Check if we need to hydrate the fleet
        var truckCount = await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Trucks");

        if (truckCount == 0)
        {
            if (db.State != ConnectionState.Open) db.Open();
            using var transaction = db.BeginTransaction();

            try
            {
                var random = new Random();
                var driverNames = new[] { "Nikola Tesla", "Milunka Savić", "Mihajlo Pupin", "Mileva Marić", "Novak Đoković" };
                var driverIds = new List<string>();

                foreach (var name in driverNames)
                {
                    var dId = $"DRV-{Guid.NewGuid().ToString()[..4].ToUpper()}";
                    await db.ExecuteAsync(@"
                        INSERT INTO Drivers (Id, Name, Experience, LicenseClass) 
                        VALUES (@Id, @Name, @Exp, 'Class-A')",
                        new { Id = dId, Name = name, Exp = $"{random.Next(5, 20)} Years" },
                        transaction);
                    driverIds.Add(dId);
                }

                for (int i = 1; i <= 50; i++)
                {
                    string assignedDriver = driverIds[random.Next(driverIds.Count)];

                    await db.ExecuteAsync(@"
                        INSERT INTO Trucks (Id, Vin, Status, DriverId, Latitude, Longitude, FuelLevel, Speed, LastUpdated) 
                        VALUES (@Id, @Vin, 1, @DriverId, 44.8125, 20.4612, 100, 0, datetime('now'))",
                        new
                        {
                            Id = $"TRK-{i:D3}",
                            Vin = $"VIN{Guid.NewGuid().ToString()[..8].ToUpper()}",
                            DriverId = assignedDriver
                        }, transaction);
                }

                transaction.Commit();
                Log.Information("✅ SEED SUCCESS: 5 Driver Profiles and 50 Trucks initialized.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "❌ SEED FAILED: Atomic rollback performed.");
                throw;
            }
        }
    }
}