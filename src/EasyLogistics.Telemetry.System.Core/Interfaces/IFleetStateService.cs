using EasyLogistics.Telemetry.System.Core.ViewModels;

namespace EasyLogistics.Telemetry.System.Core.Interfaces;

public interface IFleetStateService
{
    void UpdateFleet(IEnumerable<EasyLogistics.Telemetry.System.Core.Models.TruckTelemetry> data);
    List<TruckDisplayVm> GetFormattedFleet();
}