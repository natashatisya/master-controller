# 🎛️ Master Host Controller System

A distributed .NET system for managing, monitoring, and deploying applications across multiple hosts from a central controller.

---

## 📋 Overview

This system consists of two components:

- **Master API** — Central controller that manages all registered hosts, monitors their health, and triggers deployments
- **Client Agent** — Runs on each host, registers itself with the master, sends heartbeats, and executes deployment scripts

---

## 🏗️ Architecture
```
┌─────────────────────────────────────────┐
│           Master Host Controller         │
│         ASP.NET Core 8 Web API          │
│         http://localhost:5000           │
│                                         │
│  ┌─────────┐  ┌─────────┐  ┌────────┐  │
│  │  Hosts  │  │ Deploy  │  │  Web   │  │
│  │   API   │  │ Engine  │  │  UI    │  │
│  └─────────┘  └─────────┘  └────────┘  │
│              SQL Server DB              │
└──────────────────┬──────────────────────┘
                   │ HTTP
        ┌──────────┴──────────┐
        │                     │
┌───────▼────────┐   ┌────────▼───────┐
│  Client Agent  │   │  Client Agent  │
│ .NET Worker    │   │ .NET Worker    │
│ Service        │   │ Service        │
│ port 5100      │   │ port 5100      │
└────────────────┘   └────────────────┘
```

---

## 🛠️ Tech Stack

| Component | Technology |
|---|---|
| Master API | ASP.NET Core 8 |
| Client Agent | .NET 8 Worker Service |
| Database | SQL Server Express |
| ORM | Entity Framework Core 8 |
| API Docs | Swagger / OpenAPI |
| Object Storage | MinIO |
| Authentication | API Key |

---

## ✨ Features

- ✅ **Host Registration** — Agents auto-register with master on startup
- ✅ **Health Monitoring** — Real-time CPU, Memory, Disk usage tracking
- ✅ **Heartbeat System** — Agents send heartbeat every 30 seconds
- ✅ **Offline Detection** — Hosts marked offline if no heartbeat for 2 minutes
- ✅ **Deployment Engine** — Trigger PowerShell script deployments remotely
- ✅ **MinIO Deployment** — Automated MinIO object storage installation
- ✅ **API Key Authentication** — Secure agent-to-master communication
- ✅ **Web UI Dashboard** — Real-time host monitoring and deployment control
- ✅ **Deployment History** — Full audit log of all deployments

---

## 📁 Project Structure
```
MasterControllerSystem/
├── Master.API/                 # Central controller API
│   ├── Controllers/
│   │   ├── HostController.cs       # Host registration & monitoring
│   │   └── DeploymentController.cs # Deployment engine
│   ├── Models/
│   │   ├── HostEntity.cs           # Host database model
│   │   └── Deployment.cs           # Deployment database model
│   ├── DTOs/
│   │   └── HostDtos.cs             # Data transfer objects
│   ├── Services/
│   │   └── HostMonitorService.cs   # Background health monitor
│   ├── Data/
│   │   └── AppDbContext.cs         # Entity Framework context
│   ├── Middleware/
│   │   └── ApiKeyMiddleware.cs     # API key authentication
│   └── wwwroot/
│       └── index.html              # Web UI dashboard
├── Client.Agent/               # Host agent worker service
│   ├── Controllers/
│   │   └── AgentController.cs      # Receives deploy commands
│   ├── Worker.cs                   # Background heartbeat worker
│   └── scripts/
│       └── install-minio.ps1       # MinIO deployment script
├── scripts/
│   └── install-minio.ps1           # MinIO installation script
└── README.md
```

---

## 🚀 Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server Express
- PowerShell 5+

### 1. Clone Repository
```bash
git clone https://github.com/natashatisya/master-controller.git
cd master-controller
```

### 2. Configure Database

Update connection string in `Master.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.\\SQLEXPRESS;Database=NetOrchestrator;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3. Run Master API
```bash
cd Master.API
dotnet run
```

Master API runs at: `http://localhost:5000`

### 4. Run Client Agent
```bash
cd Client.Agent
dotnet run
```

Client Agent runs at: `http://localhost:5100`

### 5. Open Dashboard

Open browser and go to:
```
http://localhost:5000
```

Enter your API key and click **Connect**!

---

## 📡 API Endpoints

### Host Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | /api/hosts/register | Register a new host |
| GET | /api/hosts | Get all hosts |
| GET | /api/hosts/{id} | Get host by ID |
| GET | /api/hosts/status | Get online/offline summary |
| POST | /api/hosts/{id}/heartbeat | Send heartbeat |
| DELETE | /api/hosts/{id} | Remove host |

### Deployment Endpoints

| Method | Endpoint | Description |
|---|---|---|
| POST | /api/deployments/{hostId}/deploy | Trigger deployment |
| GET | /api/deployments | Get all deployments |
| GET | /api/deployments/{id} | Get deployment by ID |

---

## 🔐 Authentication

All endpoints except `/api/hosts/register` require an API key header:
```
X-API-Key: your-api-key-here
```

API keys are automatically generated when a host registers.

---

## 🖥️ Web UI Dashboard

Access the dashboard at `http://localhost:5000`

Features:
- 📊 Live host statistics
- 🖥️ Host list with CPU/Memory/Disk metrics
- 🟢 Online/Offline status indicators
- 🚀 One-click deployment trigger
- 📋 Deployment history log

---

## 🚀 Deployment Flow
```
1. Click Deploy on dashboard
2. Master API receives request
3. Master finds host IP address
4. Master sends HTTP command to Agent
5. Agent runs PowerShell script
6. Script installs application
7. Agent reports result back to Master
8. Dashboard updates with Success/Failed
```

---

## 📦 MinIO Deployment

MinIO is used as a sample deployment target to demonstrate the deployment engine.

After successful deployment:
- **Server URL:** http://localhost:9000
- **Console URL:** http://localhost:9001
- **Username:** minioadmin
- **Password:** minioadmin123

---

## 👩‍💻 Author

Natasha Tisya
GitHub: [@natashatisya](https://github.com/natashatisya)