using EasyLogistics.Telemetry.System.Core.Configuration;
using EasyLogistics.Telemetry.System.Core.Interfaces;
using EasyLogistics.Telemetry.System.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace EasyLogistics.Telemetry.System.Infrastructure.Bridge;

/// <summary>
/// Infrastructure bridge that reads telemetry data from a shared binary file.
/// Uses RandomAccess for high-performance, exact-length reads from Docker volumes.
/// </summary>
public sealed class MemoryMappedBridge : IFleetBridge, IDisposable
{
    private readonly FleetSettings _settings;
    private readonly ILogger<MemoryMappedBridge> _logger;
    private readonly string _bridgePath;
    private readonly int _structSize;

    public MemoryMappedBridge(ILogger<MemoryMappedBridge> logger, IOptions<FleetSettings> settings)
    {
        _logger = logger;
        _settings = settings.Value;
        _bridgePath = _settings.SharedMemoryPath ?? "/app/data/TruckTelemetryBridge.bin";
        _structSize = Marshal.SizeOf<TruckTelemetry>();
    }

    /// <summary>
    /// Reads the current state of the fleet from the binary bridge file.
    /// Handles IO contention with the Python engine via FileShare and IOException catches.
    /// </summary>
    public TruckTelemetry[] ReadFleet()
    {
        if (!File.Exists(_bridgePath))
        {
            return Array.Empty<TruckTelemetry>();
        }

        try
        {
            using SafeFileHandle handle = File.OpenHandle(
                _bridgePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

            long fileLength = RandomAccess.GetLength(handle);
            if (fileLength == 0 || fileLength < _structSize)
            {
                return Array.Empty<TruckTelemetry>();
            }

            byte[] buffer = new byte[fileLength];

            int bytesRead = RandomAccess.Read(handle, buffer, 0);

            if (bytesRead < _structSize)
            {
                return Array.Empty<TruckTelemetry>();
            }

            int truckCount = bytesRead / _structSize;
            var fleet = new TruckTelemetry[truckCount];

            for (int i = 0; i < truckCount; i++)
            {
                byte[] structBuffer = new byte[_structSize];
                Buffer.BlockCopy(buffer, i * _structSize, structBuffer, 0, _structSize);

                GCHandle gcHandle = GCHandle.Alloc(structBuffer, GCHandleType.Pinned);
                
                try
                {
                    fleet[i] = Marshal.PtrToStructure<TruckTelemetry>(gcHandle.AddrOfPinnedObject());
                }
                finally
                {
                    gcHandle.Free();
                }
            }

            // Return only valid trucks (filter out empty binary slots)
            return fleet.Where(t => t.TruckId > 0).ToArray();
        }
        catch (IOException)
        {
            return Array.Empty<TruckTelemetry>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telemetry Bridge Read Failure on path: {Path}", _bridgePath);
            return Array.Empty<TruckTelemetry>();
        }
    }

    public void WriteFleet(TruckTelemetry[] fleet)
    {
        // One-way bridge: Python produces, .NET consumes.
    }

    public void Dispose()
    {
        // File handles are managed by 'using' blocks; no persistent resources to release.
    }
}
