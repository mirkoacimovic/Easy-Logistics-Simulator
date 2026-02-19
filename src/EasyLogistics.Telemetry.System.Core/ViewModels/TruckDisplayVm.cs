namespace EasyLogistics.Telemetry.System.Core.ViewModels;

public class TruckDisplayVm
{
    // Add this to fix CS7036
    public TruckDisplayVm() { }

    public int Id { get; set; }
    public string Status { get; set; } = "";
    public string Position { get; set; } = "";
    public string SpeedDisplay { get; set; } = "";
    public string LastSeen { get; set; } = "";
};