using EasyLogistics.Core.ViewModels;

namespace EasyLogistics.Core.Interfaces;

public interface IFleetStateService
{
    void UpdateFleet(IEnumerable<EasyLogistics.Core.Models.TruckTelemetry> data);
    List<TruckDisplayVm> GetFormattedFleet();
}