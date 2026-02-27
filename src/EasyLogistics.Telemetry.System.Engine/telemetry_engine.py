import mmap
import struct
import time
import math
import random

# --- BINARY CONFIGURATION (Aligned with C# struct Pack=1) ---
SHM_NAME = "TruckTelemetryBridge"
MAX_TRUCKS = 50

# Format Breakdown: 
# < : Little-endian
# i : Id (int32)
# i : TruckId (int32)
# dddddd : Lat, Lng, Speed, Fuel, Dist, Cost (float64)
# q : Timestamp (int64)
STRUCT_FORMAT = "<iiddddddq" 
STRUCT_SIZE = struct.calcsize(STRUCT_FORMAT)
TOTAL_SIZE = STRUCT_SIZE * MAX_TRUCKS

# --- ADRIATIC GUARD: GEOGRAPHIC BOUNDS ---
LAT_MIN, LAT_MAX = 41.0, 55.0
LNG_MIN, LNG_MAX = -4.0, 22.0

# --- EUROPEAN LOGISTICS HUBS ---
EUROPE_HUBS = [
    ("Belgrade", 44.8186, 20.4689), ("Berlin", 52.5200, 13.4050),
    ("Paris", 48.8566, 2.3522), ("Madrid", 40.4168, -3.7038),
    ("Vienna", 48.2082, 16.3738), ("Warsaw", 52.2297, 21.0122),
    ("Budapest", 47.4979, 19.0402), ("Prague", 50.0755, 14.4378),
    ("Munich", 48.1351, 11.5820), ("Amsterdam", 52.3676, 4.9041),
    ("Brussels", 50.8503, 4.3517), ("Zurich", 47.3769, 8.5417),
    ("Lyon", 45.7640, 4.8357), ("Milan", 45.4642, 9.1900)
]

# --- INITIALIZE FLEET ---
fleet = []
for i in range(1, MAX_TRUCKS + 1):
    start_city = random.choice(EUROPE_HUBS)
    end_city = random.choice([h for h in EUROPE_HUBS if h != start_city])
    
    fleet.append({
        "id": i,
        "origin": (start_city[1], start_city[2]),
        "dest": (end_city[1], end_city[2]),
        "speed_base": random.uniform(75.0, 92.0),
        "progress": random.uniform(0, 1.0),
        "direction": 1,
        "total_dist": random.uniform(500, 1500),
        "total_fuel": random.uniform(100, 300),
        "state": "DRIVING",
        "wait_until": 0
    })

def run_simulation():
    print(f"🌍 Europe Logistics Engine Active. Bounding Box: {LAT_MIN}-{LAT_MAX} Lat.")
    print(f"📦 Mapping {MAX_TRUCKS} trucks. Struct size: {STRUCT_SIZE} bytes.")
    
    try:
        # Connect to existing memory or create new
        shm = mmap.mmap(-1, TOTAL_SIZE, tagname=SHM_NAME, access=mmap.ACCESS_WRITE)
    except Exception as e:
        print(f"❌ SHM Error: {e}. Make sure the C# App is running first!")
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
                
                # State Machine Logic
                if truck["state"] == "WAITING":
                    if now >= truck["wait_until"]:
                        truck["state"] = "DRIVING"
                        truck["direction"] *= -1 
                    else:
                        active_speed = 0.0
                
                if truck["state"] == "DRIVING":
                    # Speed divided by a factor to simulate realistic movement
                    step = (truck["speed_base"] / 300000) * delta 
                    truck["progress"] += step * truck["direction"]
                    active_speed = truck["speed_base"] + (math.sin(now + truck["id"]) * 2)

                    if truck["progress"] >= 1.0 or truck["progress"] <= 0:
                        truck["progress"] = 1.0 if truck["progress"] >= 1.0 else 0.0
                        truck["state"] = "WAITING"
                        truck["wait_until"] = now + random.uniform(10, 30)
                        active_speed = 0.0

                # LERP position calculation
                p = truck["progress"]
                lat = truck["origin"][0] + (truck["dest"][0] - truck["origin"][0]) * p
                lng = truck["origin"][1] + (truck["dest"][1] - truck["origin"][1]) * p
                
                # Geographic Clamping
                lat = max(LAT_MIN, min(LAT_MAX, lat))
                lng = max(LNG_MIN, min(LNG_MAX, lng))

                # Update Accumulators
                if active_speed > 0:
                    fuel_rate = 0.009 + (active_speed / 13000)
                    truck["total_dist"] += (active_speed / 3600) * delta
                    truck["total_fuel"] += fuel_rate * delta
                
                total_cost = truck["total_fuel"] * 1.72
                
                # .NET FileTime Ticks conversion
                ticks = int((time.time() + 62135596800) * 10000000)

                # Pack according to: i(Id) i(TruckId) d(Lat) d(Lng) d(Spd) d(Fuel) d(Dist) d(Cost) q(Time)
                buffer.extend(struct.pack(
                    STRUCT_FORMAT,
                    truck["id"],      # i: Id (PK)
                    truck["id"],      # i: TruckId
                    lat,              # d: Lat
                    lng,              # d: Lng
                    active_speed,     # d: Speed
                    truck["total_fuel"], # d: Fuel
                    truck["total_dist"], # d: Dist
                    total_cost,       # d: Cost
                    ticks             # q: Timestamp
                ))

            # Write the entire fleet block to shared memory
            shm.seek(0)
            shm.write(buffer)
            time.sleep(0.1)

    except KeyboardInterrupt:
        print("\n🛑 Simulation Stopped.")
    finally:
        shm.close()

if __name__ == "__main__":
    run_simulation()