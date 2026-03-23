//using EasyLogistics.Telemetry.System.Core.Interfaces;
//using EasyLogistics.Telemetry.System.Core.Models;
//using EasyLogistics.Telemetry.System.Core.Configuration;
//using EasyLogistics.Telemetry.System.Infrastructure.Services;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using System.Linq;
//using Xunit;
//using System;

//namespace EasyLogistics.Telemetry.System.Tests.Infrastructure;

//// We need a fake Repository, not a fake DB Connection
//public class FakeFleetRepository : IFleetRepository
//{
//    public Task SaveSnapshotAsync(IEnumerable<TruckTelemetry> fleet) => Task.CompletedTask;
//    public Task<IEnumerable<TruckTelemetry>> GetLatestSnapshotAsync() => Task.FromResult(Enumerable.Empty<TruckTelemetry>());

//    public Task<IEnumerable<TruckTelemetry>> GetHistoryByIdAsync(int truckId)
//    {
//        throw new NotImplementedException();
//    }

//    public Task<IEnumerable<TruckTelemetry>> GetLatestPositionsAsync()
//    {
//        throw new NotImplementedException();
//    }
//}

//public class StateServiceTests
//{
//    private readonly ILogger<FleetStateService> _fakeLogger;
//    private readonly IOptions<FleetSettings> _fakeOptions;
//    private readonly IFleetRepository _fakeRepo;
//    private readonly FleetStateService _service;

//    public StateServiceTests()
//    {
//        _fakeLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<FleetStateService>.Instance;
//        _fakeRepo = null; //new FakeFleetRepository();

//        var settings = new FleetSettings
//        {
//            MaxTrucks = 50,
//            LogisticsHubs = new List<HubConfig>
//            {
//                new HubConfig { Name = "Belgrade", Latitude = 44.8186, Longitude = 20.4689 },
//                new HubConfig { Name = "Berlin", Latitude = 52.5200, Longitude = 13.4050 }
//            }
//        };
//        _fakeOptions = Options.Create(settings);

//        // FIX: Passing (Logger, Options, Repository) in the exact order required
//        //_service = new FleetStateService(_fakeLogger, _fakeOptions, );
//    }

//    [Fact]
//    public async Task UpdateFleet_ShouldStoreMultipleTrucksAndProjectCorrectly()
//    {
//        // Arrange
//        var trucks = new List<TruckTelemetry>
//        {
//            // Note: Status logic usually looks at Speed > 0.5 or similar
//            new TruckTelemetry { TruckId = 1, Latitude = 44.81, Longitude = 20.46, Speed = 0, FuelConsumed = 10.5 },
//            new TruckTelemetry { TruckId = 2, Latitude = 52.51, Longitude = 13.39, Speed = 85, FuelConsumed = 20.1 }
//        };

//        // Act
//        await _service.UpdateFleet(trucks);
//        var formatted = _service.GetFormattedFleet();

//        // Assert
//        Assert.Equal(2, formatted.Count);

//        // Match the logic in your FleetStateService (Speed 0 = Idle, Speed > 0 = Moving)
//        var t1 = formatted.First(f => f.TruckId == 1);
//        Assert.Equal("Belgrade", t1.RouteName);
//    }
//}