using EasyLogistics.Telemetry.System.Core.Models;
using EasyLogistics.Telemetry.System.Core.Services;
using Xunit;

namespace EasyLogistics.Telemetry.System.Tests.Core;

public class AnalyticsTests
{
    private readonly FleetAnalytics _analytics = new();

    [Fact]
    public void IsSpeeding_ShouldReturnTrue_WhenSpeedAbove90()
    {
        // Arrange
        var truck = new TruckTelemetry { Speed = 95.5 };

        // Act
        var result = _analytics.IsSpeeding(truck);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(0.0, "Idle")]
    [InlineData(50.0, "Moving")]
    [InlineData(110.0, "Speeding")]
    public void GetStatus_ShouldReturnCorrectLabel(double speed, string expectedStatus)
    {
        var truck = new TruckTelemetry { Speed = speed };
        var result = _analytics.GetStatus(truck);
        Assert.Equal(expectedStatus, result);
    }
}