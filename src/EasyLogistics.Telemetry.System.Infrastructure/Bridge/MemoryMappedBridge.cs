using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using Microsoft.Extensions.Logging;

namespace EasyLogistics.Telemetry.System.Infrastructure.Bridge;

/// <summary>
/// High-performance IPC bridge using Windows Shared Memory.
/// Implementation aligned with IFleetBridge contract.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class MemoryMappedBridge : IFleetBridge
{
    private const string MapName = "TruckTelemetryBridge";
    private const int MaxTrucks = 50;
    private readonly int _bufferSize;
    private readonly ILogger<MemoryMappedBridge> _logger;
    private MemoryMappedFile? _mmf;

    public MemoryMappedBridge(ILogger<MemoryMappedBridge> logger)
    {
        // Requires TruckTelemetry to be a [StructLayout(LayoutKind.Sequential, Pack = 1)] struct
        _bufferSize = Marshal.SizeOf<TruckTelemetry>() * MaxTrucks;
        _logger = logger;
        InitializeBridge();
    }

    private void InitializeBridge()
    {
        try
        {
            _mmf = MemoryMappedFile.CreateOrOpen(MapName, _bufferSize, MemoryMappedFileAccess.ReadWrite);
            _logger.LogInformation("MMF Bridge Initialized: {Name} ({Size} bytes)", MapName, _bufferSize);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to initialize MemoryMappedFile.");
        }
    }

    /// <summary>
    /// Reads the fleet snapshot from shared memory. Aligned with IFleetBridge.ReadFleet().
    /// </summary>
    public TruckTelemetry[] ReadFleet()
    {
        if (_mmf == null) return Array.Empty<TruckTelemetry>();

        var fleet = new TruckTelemetry[MaxTrucks];
        try
        {
            using var accessor = _mmf.CreateViewAccessor(0, _bufferSize, MemoryMappedFileAccess.Read);

            // --- DIAGNOSTIC: READ RAW BYTES ---
            // Checking the first 16 bytes to see if Python has written anything.
            // This prevents processing uninitialized or zeroed memory.
            byte[] raw = new byte[16];
            accessor.ReadArray(0, raw, 0, 16);

            if (raw.All(b => b == 0))
            {
                // Signal "No Data" to the FleetWorker
                return Array.Empty<TruckTelemetry>();
            }

            // High-speed block copy from RAM into the C# managed array
            accessor.ReadArray(0, fleet, 0, MaxTrucks);

            // Filtering ensures we only return trucks that have been properly initialized by the engine
            return fleet.Where(t => t.TruckId > 0).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MMF Read Failed");
            return Array.Empty<TruckTelemetry>();
        }
    }

    /// <summary>
    /// Writes data back to shared memory. Aligned with IFleetBridge.WriteFleet().
    /// </summary>
    public void WriteFleet(TruckTelemetry[] fleet)
    {
        if (_mmf == null || fleet == null) return;

        try
        {
            using var accessor = _mmf.CreateViewAccessor(0, _bufferSize, MemoryMappedFileAccess.Write);
            accessor.WriteArray(0, fleet, 0, Math.Min(fleet.Length, MaxTrucks));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Write to MemoryMappedFile failed.");
        }
    }

    public void Dispose()
    {
        _mmf?.Dispose();
        _logger.LogInformation("MMF Bridge Disposed.");
        GC.SuppressFinalize(this);
    }
}