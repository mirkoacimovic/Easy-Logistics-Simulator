#!/bin/bash
set -e

SHM_FILE="/dev/shm/TruckTelemetryBridge"
# Struct Size (64) * 50 Trucks = 3200
REQUIRED_SIZE=3200

# 1. Clear old bridge to prevent stale data reading
rm -f "$SHM_FILE"

# 2. Pre-allocate and set global read/write permissions
truncate -s $REQUIRED_SIZE "$SHM_FILE"
chmod 666 "$SHM_FILE"

echo "2026-03-16 | 🚛 SHM Bridge Initialized ($REQUIRED_SIZE bytes)."
echo "2026-03-16 | 🐍 Starting Python Engine..."

# 3. Start engine with unbuffered output for Docker logs
exec python -u engine.py