using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using EasyLogistics.Telemetry.System.Infrastructure.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace EasyLogistics.Telemetry.System.Tests.Infrastructure
{
    public class StateServiceTests
    {
        [Fact]
        public async Task UpdateFleet_ShouldStoreMultipleTrucksAndProjectCorrectly()
        {
            // Arrange
            // FIX: FleetStateService is now parameterless. 
            // Notification logic has been moved to the Worker/Notifier layer.
            var service = new FleetStateService();

            var trucks = new[]
            {
                new TruckTelemetry
                {
                    TruckId = 1,
                    Latitude = 44.81,
                    Longitude = 20.46,
                    Speed = 0,
                    Timestamp = 1700000000
                },
                new TruckTelemetry
                {
                    TruckId = 2,
                    Latitude = 44.82,
                    Longitude = 20.47,
                    Speed = 85,
                    Timestamp = 1700000000
                }
            };

            // Act
            await service.UpdateFleet(trucks);
            var formatted = service.GetFormattedFleet();

            // Assert
            Assert.Equal(2, formatted.Count);

            // Verify Idle logic (Speed <= 5)
            Assert.Contains(formatted, t => t.Id == 1 && t.Status == "Idle");

            // Verify Moving logic (Speed > 5)
            Assert.Contains(formatted, t => t.Id == 2 && t.Status == "Moving");

            // Verify Data Integrity
            var truck2 = formatted.Find(t => t.Id == 2);
            Assert.Equal(44.82, truck2?.Lat);
            Assert.Equal(85, truck2?.Speed);
        }
    }
}