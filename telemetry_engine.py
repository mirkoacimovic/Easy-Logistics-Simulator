import mmap
import struct
import time
import math
import random

# CONFIGURATION - Must match C# exactly
MAX_TRUCKS = 50
# Struct format: i (int32), d (double), d (double), d (double), q (int64)
# Total size: 4 + 8 + 8 + 8 + 8 = 36 bytes
STRUCT_FORMAT = "=idddq"
STRUCT_SIZE = struct.calcsize(STRUCT_FORMAT)
TOTAL_SIZE = STRUCT_SIZE * MAX_TRUCKS
MAP_NAME = "Global_Fleet_Stream"

class TelemetryEngine:
    def __init__(self):
        print(f"[*] Initializing Telemetry Engine for {MAX_TRUCKS} trucks...")
        # Create a shared memory map (Windows style)
        self.mm = mmap.mmap(-1, TOTAL_SIZE, tagname=MAP_NAME, access=mmap.ACCESS_WRITE)
        
        # Initialize 50 trucks with starting positions around Belgrade
        self.trucks = []
        for i in range(1, MAX_TRUCKS + 1):
            self.trucks.append({
                "id": i,
                "lat": 44.8125 + (random.random() * 0.01),
                "lon": 20.4612 + (random.random() * 0.01),
                "speed": random.uniform(40.0, 95.0) # Some will be speeding!
            })

    def update_positions(self):
        """Simulate movement logic"""
        for truck in self.trucks:
            # Move them slightly North-East
            truck["lat"] += 0.0001 * (truck["speed"] / 50.0)
            truck["lon"] += 0.0001 * (truck["speed"] / 50.0)
            # Random speed jitter
            truck["speed"] = max(0, truck["speed"] + random.uniform(-1, 1))

    def write_to_memory(self):
        """Pack data into raw bytes and write to MMF"""
        buffer = bytearray()
        timestamp = int(time.time())

        for truck in self.trucks:
            # Pack: Id, Lat, Lon, Speed, Timestamp
            packed_data = struct.pack(
                STRUCT_FORMAT, 
                truck["id"], 
                truck["lat"], 
                truck["lon"], 
                truck["speed"], 
                timestamp
            )
            buffer.extend(packed_data)

        self.mm.seek(0)
        self.mm.write(buffer)

    def run(self):
        print(f"[+] Engine running. Writing {TOTAL_SIZE} bytes to '{MAP_NAME}' at 10Hz...")
        try:
            while True:
                self.update_positions()
                self.write_to_memory()
                time.sleep(0.1) # 10Hz matching C# Worker
        except KeyboardInterrupt:
            print("[*] Engine stopped.")
            self.mm.close()

if __name__ == "__main__":
    engine = TelemetryEngine()
    engine.run()