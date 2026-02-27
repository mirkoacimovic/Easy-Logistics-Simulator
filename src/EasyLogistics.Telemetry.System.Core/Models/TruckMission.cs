using System;

namespace EasyLogistics.Telemetry.System.Core.Models
{
    public enum MissionState { Northbound, Southbound }

    public class TruckMission
    {
        public int TruckId { get; set; }
        public double StartLat { get; set; }
        public double StartLng { get; set; }
        public double EndLat { get; set; }
        public double EndLng { get; set; }
        public double Progress { get; set; }
        public MissionState State { get; set; }

        // Added to track stateful analytics within the C# side if needed
        public double CurrentFuel { get; set; }
        public double TotalDistance { get; set; }

        public TruckTelemetry CreateTelemetrySnapshot(double speed)
        {
            double currentLat, currentLng;

            // 1. Calculate linear interpolation for position
            if (State == MissionState.Northbound)
            {
                currentLat = StartLat + (EndLat - StartLat) * Progress;
                currentLng = StartLng + (EndLng - StartLng) * Progress;
            }
            else
            {
                currentLat = EndLat + (StartLat - EndLat) * Progress;
                currentLng = EndLng + (StartLng - EndLng) * Progress;
            }

            // 2. Return the populated Struct
            return new TruckTelemetry
            {
                TruckId = this.TruckId,
                Latitude = currentLat + (Random.Shared.NextDouble() * 0.001),
                Longitude = currentLng + (Random.Shared.NextDouble() * 0.001),
                Speed = speed,

                // --- MUST INCLUDE THE ANALYTICS FIELDS ---
                FuelConsumed = this.CurrentFuel,
                DistanceTraveled = this.TotalDistance,
                TotalCost = this.TotalDistance * 1.45, // Consistent with our cost logic

                // --- MUST MATCH THE 'long' TimestampTicks ---
                Timestamp = DateTime.UtcNow.Ticks
            };
        }
    }
}