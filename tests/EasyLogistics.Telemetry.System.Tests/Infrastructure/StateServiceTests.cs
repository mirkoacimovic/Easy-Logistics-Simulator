using EasyLogistics.Telemetry.System.Core.Models;
using EasyLogistics.Telemetry.System.Infrastructure.Services;
using Xunit;

namespace EasyLogistics.Telemetry.System.Tests.Infrastructure;

public class StateServiceTests
{
    [Fact]
    public void UpdateFleet_ShouldStoreMultipleTrucksAndProjectCorrectly()
    {
        // Arrange
        var service = new FleetStateService();
        var trucks = new[]
        {
            new TruckTelemetry { Id = 1, Lat = 44.81, Lng = 20.46, Speed = 0 },
            new TruckTelemetry { Id = 2, Lat = 44.82, Lng = 20.47, Speed = 85 }
        };

        // Act
        service.UpdateFleet(trucks);
        var formatted = service.GetFormattedFleet();

        // Assert
        Assert.Equal(2, formatted.Count);
        Assert.Contains(formatted, t => t.Id == 1 && t.Status == "Idle");
        Assert.Contains(formatted, t => t.Id == 2 && t.Status == "Moving");
    }
}