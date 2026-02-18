using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Versioning; // Added for platform attribute
using EasyLogistics.Core.Interfaces;
using EasyLogistics.Core.Models;
using Microsoft.Extensions.Logging;

namespace EasyLogistics.Infrastructure.Bridge;

[SupportedOSPlatform("windows")] // Fixed Warning: Tells compiler we know this is Windows-only
public class MemoryMappedBridge : IFleetBridge
{
    private const string MapName = "Global_Fleet_Stream";
    private const int MaxTrucks = 50;
    private readonly int _bufferSize;
    private readonly ILogger<MemoryMappedBridge> _logger;
    private MemoryMappedFile? _mmf;

    public MemoryMappedBridge(ILogger<MemoryMappedBridge> logger)
    {
        _bufferSize = Marshal.SizeOf<TruckTelemetry>() * MaxTrucks;
        _logger = logger;
        InitializeBridge();
    }

    private void InitializeBridge()
    {
        try
        {
            _mmf = MemoryMappedFile.CreateOrOpen(MapName, _bufferSize, MemoryMappedFileAccess.ReadWrite);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to initialize MemoryMappedFile.");
        }
    }

    public TruckTelemetry[] ReadFleet()
    {
        if (_mmf == null) return Array.Empty<TruckTelemetry>();

        var fleet = new TruckTelemetry[MaxTrucks];
        try
        {
            using var accessor = _mmf.CreateViewAccessor(0, _bufferSize, MemoryMappedFileAccess.Read);
            accessor.ReadArray(0, fleet, 0, MaxTrucks);
        }
        catch (IOException)
        {
            _logger.LogWarning("Memory map busy. Skipping frame.");
        }
        return fleet;
    }

    public void WriteFleet(TruckTelemetry[] fleet) => throw new NotImplementedException();
    public void Dispose() => _mmf?.Dispose();
}