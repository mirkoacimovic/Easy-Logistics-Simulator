import struct
import time
import math
import random
import os

BRIDGE_PATH = "/app/data/TruckTelemetryBridge.bin"
MAX_TRUCKS = 50

# < = Little Endian | i = int(4) | 4x = pad(4) | d = double(8) x 6 | q = long(8)
STRUCT_FORMAT = "<i4xddddddq" 
STRUCT_SIZE = struct.calcsize(STRUCT_FORMAT)

EUROPE_HUBS = [
    ("Belgrade", 44.8186, 20.4689), ("Berlin", 52.5200, 13.4050),
    ("Paris", 48.8566, 2.3522), ("Warsaw", 52.2297, 21.0122),
    ("Budapest", 47.4979, 19.0402), ("Vienna", 48.2082, 16.3738),
    ("Rome", 41.9028, 12.4964), ("Madrid", 40.4168, -3.7038)
]

def run_simulation():
    os.makedirs(os.path.dirname(BRIDGE_PATH), exist_ok=True)

    fleet = []
    for i in range(MAX_TRUCKS):
        start = random.choice(EUROPE_HUBS)
        dest = random.choice([h for h in EUROPE_HUBS if h != start])
        fleet.append({
            "id": i + 1,
            "lat": start[1], "lng": start[2],
            "dest_lat": dest[1], "dest_lng": dest[2],
            "speed": 0.0, "dist": 0.0, "fuel": 0.0
        })

    print(f"🚛 ENGINE ONLINE: {MAX_TRUCKS} trucks simulated at {BRIDGE_PATH}")

    try:
        while True:
            now = time.time()
            ticks = int((now + 62135596800.0) * 10000000)
            payload = bytearray()
            
            for truck in fleet:
                d_lat = truck["dest_lat"] - truck["lat"]
                d_lng = truck["dest_lng"] - truck["lng"]
                dist_to_target = math.sqrt(d_lat**2 + d_lng**2)

                if dist_to_target < 0.001:
                    new_dest = random.choice(EUROPE_HUBS)
                    truck["dest_lat"], truck["dest_lng"] = new_dest[1], new_dest[2]
                    dist_to_target = 0.001

                truck["speed"] = 70.0 + random.uniform(0, 25)
                step = (truck["speed"] / 360000)
                truck["lat"] += (d_lat / dist_to_target) * step
                truck["lng"] += (d_lng / dist_to_target) * step
                
                truck["dist"] += (truck["speed"] / 3600) * 0.5
                truck["fuel"] += (truck["speed"] * 0.00005)
                total_cost = truck["fuel"] * 1.65

                # Binary Packing
                data = struct.pack(STRUCT_FORMAT, 
                    truck["id"], truck["lat"], truck["lng"], 
                    truck["speed"], truck["fuel"], truck["dist"], 
                    total_cost, ticks)
                payload.extend(data)

            # Write to .tmp then rename
            temp_path = BRIDGE_PATH + ".tmp"
            try:
                with open(temp_path, "wb") as f:
                    f.write(payload)
                os.replace(temp_path, BRIDGE_PATH)
            except Exception as e:
                print(f"Write Contention: {e}")
            
            time.sleep(0.5)
            
    except KeyboardInterrupt:
        print("🛑 Engine stopping...")

if __name__ == "__main__":
    run_simulation()