🚛 TruckSimulator: Real-Time Telemetry & Identity Suite
A High-Performance Polyglot Architecture for Fleet Orchestration.
This project demonstrates a production-grade approach to real-time data ingestion and identity management, bridging the gap between a .NET 9 (C# 14) core and an asynchronous Python telemetry engine.

🏗️ Architectural Overview
The system is designed for High-Availability (HA) and Low-Latency streaming:
The Gateway (.NET 9): Handles SignalR orchestration, Identity Cleanup (Core), and the RESTful API surface. Utilizing the latest C# 14 features for memory efficiency.
The Engine (Python): An asynchronous worker service that simulates high-frequency telemetry data (Fuel, GPS, RPM) and streams it back to the Gateway.
The Infrastructure: Containerized via Docker with multi-stage builds optimized for cloud-native deployment (Railway/Vercel).

🚦 Live Demo & Access
The application is currently deployed and optimized for the Belgrade telemetry set.

Live URL: https://logistics-web-production-ee69.up.railway.app/

🔐 Technical Audit Credentials
For recruiters and architects performing a system walkthrough:
user : admin@trucksim.com
password : Trucker123!

🛠️ Tech Stack & Patterns
Backend: .NET 9, C# 14, Entity Framework Core (PostgreSQL).
Real-time: SignalR (WebSockets) for live telemetry updates.
Worker: Python 3.12 (Asyncio) for high-frequency data simulation.
DevOps: Docker, Multi-stage builds, Environment Injection.
Principles: SOLID, Clean Architecture, Repository Pattern.

📈 Roadmap
✅ Phase 1: Identity Cleanup (Core Identity Logic).
🚧 Phase 2: Plumbing Cleanup (Infrastructure & Dependency Injection).
📅 Phase 3: Environment Cleanup (Config & Secret Management).
📅 Phase 4: Worker Cleanup (The Telemetry Loop).

🚀 Installation & Local Development
The system is fully containerized. To spin up the entire telemetry stack locally, ensure you have Docker Desktop and the .NET 9 SDK installed.

1. Clone the Repository
  git clone https://github.com/mirkoacimovic/Easy-Logistics-Simulator.git
  cd TruckSimulator

2. Orchestration (The "Sovereign" Boot)
The entire stack—including the .NET 9 Gateway, the Python Telemetry Engine, and the SQLite Store—is initialized with a single command:
  docker-compose up --build

3. Verification & Access
Once the containers are healthy, you can monitor the real-time telemetry stream and verify the SQLite handshake:
Web Dashboard: Open http://localhost:8080 in your browser.
Database Audit: The EasyLogistics.db file will be automatically created and persisted in the ./infra/data (or designated host) folder.
Health Check: Verify the SignalR connection is active by checking the browser console for live telemetry updates from the Python worker.

👨‍💻 Developed By
Mirko Acimovic 2026
