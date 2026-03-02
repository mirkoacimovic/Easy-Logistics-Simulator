import mmap
import struct
import time
import math
import random

# --- PHASE 3 ALIGNMENT: Must match appsettings.json and FleetSettings.cs ---
SHM_NAME = "TruckTelemetryBridge"
MAX_TRUCKS = 50

# STRUCT_FORMAT Details: 
# <   : Little-endian (Windows standard)
# i   : TruckId (int32)
# i   : Id (redundant/legacy, int32)
# d   : Latitude (double)
# d   : Longitude (double)
# d   : Speed (double)
# d   : FuelConsumed (double)
# d   : DistanceTraveled (double)
# d   : TotalCost (double)
# q   : Timestamp (int64 / Windows FileTime)
STRUCT_FORMAT = "<iiddddddq" 
STRUCT_SIZE = struct.calcsize(STRUCT_FORMAT)
TOTAL_SIZE = STRUCT_SIZE * MAX_TRUCKS

# --- SYNCED HUBS (Matches your FleetSettings.cs) ---
EUROPE_HUBS = [
    ("Belgrade", 44.8186, 20.4689), ("Berlin", 52.5200, 13.4050),
    ("Paris", 48.8566, 2.3522), ("Vienna", 48.2082, 16.3738),
    ("Warsaw", 52.2297, 21.0122), ("Budapest", 47.4979, 19.0402),
    ("Prague", 50.0755, 14.4378), ("Munich", 48.1351, 11.5820),
    ("Milan", 45.4642, 9.1900)
]

def run_simulation():
    print(f"🚛 Python Engine: Simulating {MAX_TRUCKS} trucks...")
    print(f"🔗 Shared Memory: {SHM_NAME} ({TOTAL_SIZE} bytes)")
    
    fleet = []
    for i in range(1, MAX_TRUCKS + 1):
        h1 = random.choice(EUROPE_HUBS)
        h2 = random.choice([h for h in EUROPE_HUBS if h != h1])
        fleet.append({
            "id": i,
            "origin": (h1[1], h1[2]),
            "dest": (h2[1], h2[2]),
            "speed_base": random.uniform(70.0, 95.0),
            "progress": random.uniform(0, 1.0),
            "direction": 1,
            "state": "DRIVING",
            "wait_until": 0,
            "fuel": random.uniform(50, 100),
            "dist": random.uniform(100, 500)
        })

    try:
        # Create or Open the Shared Memory
        # Note: If C# app is already running, it opens. If not, Python creates it.
        shm = mmap.mmap(-1, TOTAL_SIZE, tagname=SHM_NAME, access=mmap.ACCESS_WRITE)
    except Exception as e:
        print(f"❌ SHM Error: {e}. Ensure you are on Windows.")
        return

    last_time = time.time()

    try:
        while True:
            now = time.time()
            delta = now - last_time
            last_time = now
            buffer = bytearray()
            
            for truck in fleet:
                active_speed = 0.0
                
                # State Machine: WAITING vs DRIVING
                if truck["state"] == "WAITING":
                    if now >= truck["wait_until"]:
                        truck["state"] = "DRIVING"
                        truck["direction"] *= -1 
                    else:
                        active_speed = 0.0
                
                if truck["state"] == "DRIVING":
                    # Simulate variable speed (hills, traffic)
                    active_speed = truck["speed_base"] + (math.sin(now * 0.2) * 5)
                    # (Speed / Earth Circumference/360) simplified progress
                    step = (active_speed / 400000) * delta 
                    truck["progress"] += step * truck["direction"]

                    # Hub Arrival Check
                    if truck["progress"] >= 1.0 or truck["progress"] <= 0:
                        truck["progress"] = 1.0 if truck["progress"] >= 1.0 else 0.0
                        truck["state"] = "WAITING"
                        truck["wait_until"] = now + random.uniform(10, 30) # Wait 10-30s at hub
                        active_speed = 0.0

                # Linear Interpolation between Hubs
                p = truck["progress"]
                lat = truck["origin"][0] + (truck["dest"][0] - truck["origin"][0]) * p
                lng = truck["origin"][1] + (truck["dest"][1] - truck["origin"][1]) * p
                
                # Logic: Apply a subtle "Road Curve" offset so they aren't perfect lines
                curve_offset = math.sin(p * math.pi) * 0.02
                lat += curve_offset
                
                # Telemetry Increments
                if active_speed > 0:
                    truck["dist"] += (active_speed / 3600) * delta
                    truck["fuel"] += (0.008) * delta 
                
                # Windows FileTime (100-nanosecond intervals since Jan 1, 1601)
                # Essential for C# DateTime.FromFileTime()
                ticks = int((time.time() + 11644473600) * 10000000)

                # Packing the bytes to match [StructLayout(LayoutKind.Sequential, Pack = 1)]
                buffer.extend(struct.pack(
                    STRUCT_FORMAT,
                    truck["id"],        # TruckId
                    truck["id"],        # Legacy Id
                    lat,                # Latitude
                    lng,                # Longitude
                    active_speed,       # Speed
                    truck["fuel"],      # FuelConsumed
                    truck["dist"],      # DistanceTraveled
                    truck["fuel"] * 2.1,# TotalCost
                    ticks               # Timestamp
                ))

            # Atomic-like update to Shared Memory
            shm.seek(0)
            shm.write(buffer)
            
            # 2Hz update rate (Matches delay in FleetWorker)
            time.sleep(0.5) 

    except KeyboardInterrupt:
        print("\n🛑 Simulation Stopped.")
    finally:
        shm.close()

if __name__ == "__main__":
    run_simulation()