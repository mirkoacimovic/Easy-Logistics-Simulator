using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using EasyLogistics.Telemetry.System.Core.Configuration; // <-- MUST MATCH FILE ABOVE
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasyLogistics.Telemetry.System.Infrastructure.Bridge;

[SupportedOSPlatform("windows")]
public sealed class MemoryMappedBridge : IFleetBridge, IDisposable
{
    private readonly FleetSettings _settings;
    private readonly int _bufferSize;
    private readonly ILogger<MemoryMappedBridge> _logger;
    private MemoryMappedFile? _mmf;

    public MemoryMappedBridge(ILogger<MemoryMappedBridge> logger, IOptions<FleetSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;

        // Use settings to calculate buffer
        _bufferSize = Marshal.SizeOf<TruckTelemetry>() * _settings.MaxTrucks;

        InitializeBridge();
    }

    private void InitializeBridge()
    {
        try
        {
            _mmf = MemoryMappedFile.CreateOrOpen(_settings.ShmName, _bufferSize, MemoryMappedFileAccess.ReadWrite);
            _logger.LogInformation("MMF Bridge Initialized: {Name}", _settings.ShmName);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to initialize MMF.");
        }
    }

    public TruckTelemetry[] ReadFleet()
    {
        if (_mmf == null) return Array.Empty<TruckTelemetry>();

        var fleet = new TruckTelemetry[_settings.MaxTrucks];
        try
        {
            using var accessor = _mmf.CreateViewAccessor(0, _bufferSize, MemoryMappedFileAccess.Read);
            if (accessor.ReadByte(0) == 0) return Array.Empty<TruckTelemetry>();

            accessor.ReadArray(0, fleet, 0, _settings.MaxTrucks);
            return fleet.Where(t => t.TruckId > 0).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MMF Read Failed");
            return Array.Empty<TruckTelemetry>();
        }
    }

    public void WriteFleet(TruckTelemetry[] fleet)
    {
        if (_mmf == null || fleet == null) return;
        try
        {
            using var accessor = _mmf.CreateViewAccessor(0, _bufferSize, MemoryMappedFileAccess.Write);
            accessor.WriteArray(0, fleet, 0, Math.Min(fleet.Length, _settings.MaxTrucks));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MMF Write Failed");
        }
    }

    public void Dispose()
    {
        _mmf?.Dispose();
        GC.SuppressFinalize(this);
    }
}