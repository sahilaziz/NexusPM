# Nexus Project Management - Layihə Strukturu

## 📁 Tam Struktur

```
Nexus.ProjectManagement/
│
├── 📄 Nexus.sln                 # Visual Studio Solution
├── 📄 README.md                 # Layihə haqqında əsas məlumat
├── 📄 .gitignore               # Git ignore faylı
│
├── 📁 Backend/                 # .NET 9 Web API
│   ├── 📁 src/
│   │   ├── 📁 Nexus.API/              # ASP.NET Core Web API
│   │   │   ├── 📁 Controllers/
│   │   │   │   ├── ProjectsController.cs
│   │   │   │   ├── TasksController.cs
│   │   │   │   ├── DocumentsController.cs
│   │   │   │   └── AuthController.cs
│   │   │   ├── 📁 Hubs/
│   │   │   │   └── SyncHub.cs        # SignalR real-time
│   │   │   ├── 📁 Middleware/
│   │   │   │   └── ExceptionMiddleware.cs
│   │   │   ├── Program.cs
│   │   │   ├── appsettings.json
│   │   │   └── Nexus.API.csproj
│   │   │
│   │   ├── 📁 Nexus.Domain/           # Core Entities
│   │   │   ├── 📁 Entities/
│   │   │   │   ├── DocumentNode.cs
│   │   │   │   ├── Project.cs
│   │   │   │   ├── Task.cs
│   │   │   │   └── User.cs
│   │   │   ├── 📁 Enums/
│   │   │   └── 📁 ValueObjects/
│   │   │   └── Nexus.Domain.csproj
│   │   │
│   │   ├── 📁 Nexus.Application/      # Business Logic
│   │   │   ├── 📁 Services/
│   │   │   │   ├── DocumentService.cs
│   │   │   │   ├── ProjectService.cs
│   │   │   │   └── TaskService.cs
│   │   │   ├── 📁 Interfaces/
│   │   │   ├── 📁 DTOs/
│   │   │   └── Nexus.Application.csproj
│   │   │
│   │   ├── 📁 Nexus.Infrastructure/   # Data Access
│   │   │   ├── 📁 Data/
│   │   │   │   ├── AppDbContext.cs
│   │   │   │   └── Configurations/
│   │   │   ├── 📁 Repositories/
│   │   │   └── Nexus.Infrastructure.csproj
│   │   │
│   │   └── 📁 Nexus.WorkerServices/   # Background Jobs
│   │       ├── DocumentProcessor.cs
│   │       └── SyncWorker.cs
│   │
│   ├── 📁 tests/
│   ├── 📁 deployment/
│   └── 📁 scripts/
│
├── 📁 Frontend/                # Flutter Multi-Platform
│   ├── 📁 nexus_shared/        # Shared code
│   │   ├── 📁 lib/
│   │   │   ├── 📁 models/
│   │   │   ├── 📁 providers/
│   │   │   └── 📁 services/
│   │   └── pubspec.yaml
│   │
│   ├── 📁 nexus_windows/       # Windows Desktop
│   │   ├── 📁 lib/
│   │   │   ├── main.dart
│   │   │   ├── 📁 screens/
│   │   │   └── 📁 widgets/
│   │   ├── 📁 windows/
│   │   └── pubspec.yaml
│   │
│   └── 📁 nexus_android/       # Android
│       ├── 📁 lib/
│       ├── 📁 android/
│       └── pubspec.yaml
│
├── 📁 Database/                # SQL Server
│   ├── 001_CreateDatabase.sql
│   ├── 002_StoredProcedures.sql
│   ├── 003_SeedData.sql
│   └── 📁 Migrations/
│
├── 📁 Tests/                   # Testlər
│   ├── 📁 UnitTests/
│   └── 📁 IntegrationTests/
│
├── 📁 Docs/                    # Sənədlər
│   ├── ProjectStructure.md     # Bu fayl
│   ├── API.md                  # API dokumentasiyası
│   ├── DatabaseSchema.md       # DB diagramları
│   └── UserGuide.md            # İstifadəçi təlimatı
│
└── 📁 .github/                 # CI/CD
    └── workflows/
        ├── build.yml
        └── deploy.yml
```

## 🎯 Hədəf Struktur (Azneft modeli)

```
Root/
├── 📁 AZNEFT_IB/
│   ├── 📁 QUYU_20/
│   │   ├── 📁 YASAYIS_MENTEQESI_A/
│   │   │   ├── 📄 2024-01-15 - Məktub №123 - [Mövzu].pdf
│   │   │   └── 📄 2024-02-01 - Məktub №125 - [Mövzu].pdf
│   │   └── 📁 QUYU_20_UMUMI/
│   │
│   ├── 📁 QUYU_45/
│   └── 📁 AZNEFT_UMUMI/
│
├── 📁 AZPETROL_IB/
└── 📁 UMUMI/
```

## 🔑 Əsas Komponentlər

### 1. Smart Foldering
- Avtomatik qovluq yaradılması
- Duplicate qovluq yoxlanışı
- Materialized path (performance)

### 2. Task Management
- Layihə və tapşırıqlar
- Gantt chart (timeline)
- Assignment və tracking

### 3. Offline-First Sync
- CRDT-based conflict resolution
- Background sync
- Local database (Isar)

### 4. Real-time
- SignalR hub-lar
- Bildirişlər
- Multi-user collaboration

## 🚀 Başlamaq üçün

1. **Database:** `001_CreateDatabase.sql` işlət
2. **Backend:** `Nexus.sln` aç və run et
3. **Frontend:** `nexus_windows` qovluğunda `flutter run`
