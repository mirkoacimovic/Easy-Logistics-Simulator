using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using EasyLogistics.Telemetry.System.Core.Configuration;
using EasyLogistics.Telemetry.System.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Data;
using Xunit;
using System;
using System.Diagnostics.CodeAnalysis;

namespace EasyLogistics.Telemetry.System.Tests.Infrastructure;

public class FakeDbConnection : IDbConnection
{
    [AllowNull]
    public string ConnectionString { get; set; } = string.Empty;
    public int ConnectionTimeout => 0;
    public string Database => "FakeDb";
    public ConnectionState State => ConnectionState.Closed;

    public IDbTransaction BeginTransaction() => null!;
    public IDbTransaction BeginTransaction(IsolationLevel il) => null!;
    public void Close() { }
    public void ChangeDatabase(string databaseName) { }
    public IDbCommand CreateCommand() => null!;
    public void Open() { }
    public void Dispose() { }
}

public class StateServiceTests
{
    private readonly IDbConnection _fakeDb;
    private readonly ILogger<FleetStateService> _fakeLogger;
    private readonly IOptions<FleetSettings> _fakeOptions;
    private readonly FleetStateService _service;

    public StateServiceTests()
    {
        _fakeDb = new FakeDbConnection();
        _fakeLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<FleetStateService>.Instance;

        // Correct Phase 3 Setup: Providing Hubs for the ResolveRoute logic
        var settings = new FleetSettings
        {
            MaxTrucks = 50,
            LogisticsHubs = new List<HubConfig>
            {
                new HubConfig { Name = "Belgrade", Lat = 44.8186, Lng = 20.4689 },
                new HubConfig { Name = "Berlin", Lat = 52.5200, Lng = 13.4050 }
            }
        };
        _fakeOptions = Options.Create(settings);

        _service = new FleetStateService(_fakeDb, _fakeLogger, _fakeOptions);
    }

    [Fact]
    public async Task UpdateFleet_ShouldStoreMultipleTrucksAndProjectCorrectly()
    {
        // Arrange
        var trucks = new List<TruckTelemetry>
        {
            new TruckTelemetry { TruckId = 1, Latitude = 44.81, Longitude = 20.46, Speed = 0, FuelConsumed = 10.5 },
            new TruckTelemetry { TruckId = 2, Latitude = 52.51, Longitude = 13.39, Speed = 85, FuelConsumed = 20.1 }
        };

        // Act
        await _service.UpdateFleet(trucks);
        var formatted = _service.GetFormattedFleet();

        // Assert
        Assert.Equal(2, formatted.Count);
        Assert.Contains(formatted, t => t.TruckId == 1 && t.Status == "Idle");
        Assert.Contains(formatted, t => t.TruckId == 2 && t.Status == "Moving");

        // Verify Route Resolver (Phase 3 logic)
        var truck1 = formatted.First(t => t.TruckId == 1);
        Assert.Contains("Belgrade", truck1.RouteName);
    }
}