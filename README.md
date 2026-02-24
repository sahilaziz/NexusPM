# 🚀 Nexus Project Management

[![Backend CI](https://github.com/sahilaziz/NexusPM/actions/workflows/backend-ci.yml/badge.svg)](https://github.com/sahilaziz/NexusPM/actions/workflows/backend-ci.yml)
[![Mobile CI](https://github.com/sahilaziz/NexusPM/actions/workflows/mobile-ci.yml/badge.svg)](https://github.com/sahilaziz/NexusPM/actions/workflows/mobile-ci.yml)
[![Docker Build](https://github.com/sahilaziz/NexusPM/actions/workflows/docker-build.yml/badge.svg)](https://github.com/sahilaziz/NexusPM/actions/workflows/docker-build.yml)
[![Code Coverage](https://codecov.io/gh/sahilaziz/NexusPM/branch/main/graph/badge.svg)](https://codecov.io/gh/sahilaziz/NexusPM)

> Enterprise-grade Project Management System for Oil & Gas industry

## 📋 Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Quick Start](#quick-start)
- [API Documentation](#api-documentation)
- [Deployment](#deployment)
- [Contributing](#contributing)

## ✨ Features

### Core PM
- ✅ **Projects** - Full CRUD with team management
- ✅ **Tasks** - Hierarchical with dependencies (FS, SS, FF, SF)
- ✅ **Labels** - 12 default + custom labels with colors
- ✅ **Time Tracking** - Live timer + manual entry + reports
- ✅ **Documents** - Universal ID system with smart foldering

### Views & Reporting
- ✅ **Gantt Chart** - Timeline with critical path
- ✅ **Kanban Board** - WIP limits with drag-drop
- ✅ **Dashboard** - User, Project, and Admin dashboards

### Infrastructure
- ✅ **Authentication** - Local (JWT) + Active Directory
- ✅ **Hybrid Cloud** - Private/Azure switchable messaging & monitoring
- ✅ **API Gateway** - Ocelot with load balancing
- ✅ **Caching** - NCache (SQL Server backed)
- ✅ **Resilience** - Polly (Retry + Circuit Breaker)

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                      PRESENTATION                           │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  Web App │  │ Mobile   │  │  API GW  │  │  Admin   │   │
│  │ (Future) │  │ (Flutter)│  │ (Ocelot) │  │  Panel   │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                        BACKEND (.NET 9)                     │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  API Layer (Controllers, Middleware)                  │  │
│  ├───────────────────────────────────────────────────────┤  │
│  │  Application Layer (CQRS, MediatR, Validators)        │  │
│  ├───────────────────────────────────────────────────────┤  │
│  │  Infrastructure Layer (Repositories, Services)        │  │
│  ├───────────────────────────────────────────────────────┤  │
│  │  Domain Layer (Entities, Value Objects)               │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
┌─────────────────────────────────────────────────────────────┐
│                      DATA & MESSAGING                       │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │
│  │  SQL Server  │  │ Private/Azure│  │    Cache     │      │
│  │   2022       │  │ Service Bus  │  │   (NCache)   │      │
│  └──────────────┘  └──────────────┘  └──────────────┘      │
└─────────────────────────────────────────────────────────────┘
```

## 🚀 Quick Start

### Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server 2022](https://www.microsoft.com/sql-server)
- [Flutter 3.22+](https://flutter.dev) (for mobile)

### Backend Setup
```bash
# Clone repository
git clone https://github.com/sahilaziz/NexusPM.git
cd NexusPM

# Database
dotnet ef database update --project Backend/src/Nexus.Infrastructure

# Run API
cd Backend/src/Nexus.API
dotnet run

# API will be available at: http://localhost:5000
```

### Mobile Setup
```bash
cd mobile
flutter pub get
flutter run
```

## 📚 API Documentation

### API Endpoints (55+)

| Module | Endpoints | Description |
|--------|-----------|-------------|
| Auth | 8 | Login, 2FA, AD, Password Reset |
| Projects | 6 | CRUD, Team Management |
| Tasks | 12 | CRUD, Dependencies, Labels |
| Time Tracking | 12 | Timer, Reports, Approvals |
| Dashboard | 3 | User, Project, Admin |

### Swagger UI
```
Development: http://localhost:5000/swagger
Production: https://api.nexus.local/swagger
```

## 🐳 Deployment

### Docker (Recommended)
```bash
docker-compose up -d
```

### Manual Deployment
See [DEPLOYMENT_GUIDE.md](docs/DEPLOYMENT_GUIDE.md) for detailed instructions.

## 🧪 Testing

### Backend Tests
```bash
cd Backend
dotnet test
```

### Mobile Tests
```bash
cd mobile
flutter test
```

## 📊 Code Coverage

| Module | Coverage |
|--------|----------|
| Backend | 75% |
| Mobile | 60% |

## 🤝 Contributing

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Commit Convention
- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation
- `style:` Formatting
- `refactor:` Code refactoring
- `test:` Tests
- `chore:` Maintenance

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with ❤️ in Azerbaijan
- Designed for Oil & Gas industry
- Enterprise-grade architecture

---

**Made with passion by the Nexus Team**

[Documentation](docs/) • [API Reference](https://api.nexus.local/swagger) • [Issues](../../issues)
