using System.Runtime.InteropServices;

namespace EasyLogistics.Core.Models;

[StructLayout(LayoutKind.Explicit, Size = 36)] // Force total size to match Python 36 bytes
public struct TruckTelemetry
{
    [FieldOffset(0)] public int Id;
    [FieldOffset(4)] public double Lat;
    [FieldOffset(12)] public double Lng;
    [FieldOffset(20)] public double Speed;
    [FieldOffset(28)] public long Timestamp;
};