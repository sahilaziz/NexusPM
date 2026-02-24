# Nexus Project Management - Deployment Guide

## 🚀 Quick Start (Docker)

```bash
# 1. Clone repository
git clone <repository-url>
cd Nexus.ProjectManagement

# 2. Start all services
docker-compose up -d

# 3. Check status
docker-compose ps

# 4. View logs
docker-compose logs -f api
```

## 📋 Prerequisites

### Development
- .NET 9 SDK
- Flutter 3.22+
- SQL Server 2022 / Docker
- Node.js 18+ (for any tooling)

### Production
- Docker & Docker Compose
- Windows Server 2019+ (for on-premise)
- SSL certificates
- Network file share (for document storage)

## 🏗️ Deployment Options

### Option 1: Docker Compose (Recommended)

```yaml
# docker-compose.yml already configured for:
# - SQL Server 2022
# - .NET 9 API
# - Persistent volumes
# - Health checks
```

```bash
# Production deployment
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

### Option 2: Manual Windows Deployment

#### Backend

```powershell
# 1. Publish
CD Backend\src
dotnet publish Nexus.API\Nexus.API.csproj -c Release -o C:\Nexus\API

# 2. Configure appsettings.Production.json
# 3. Setup IIS or run as Windows Service
# 4. Configure SQL Server connection

# Run as Windows Service
sc create NexusAPI binPath= "C:\Nexus\API\Nexus.API.exe"
sc start NexusAPI
```

#### Frontend

```powershell
# 1. Build Windows app
CD Frontend\nexus_app
flutter build windows --release

# 2. Copy to network share
XCOPY build\windows\x64\runner\Release \\Server\Nexus\Client\ /S /E

# 3. Create desktop shortcut for users
```

### Option 3: Azure Deployment

```bash
# Azure Container Instances
az container create \
  --resource-group nexus-rg \
  --name nexus-api \
  --image nexusregistry.azurecr.io/nexus-api:latest \
  --ports 5000 \
  --environment-variables \
    ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__DefaultConnection=<connection-string>

# Azure SQL Database
az sql db create \
  --resource-group nexus-rg \
  --server nexus-sql \
  --name NexusDB \
  --service-objective S2
```

## ⚙️ Configuration

### Backend Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment | `Production` |
| `ASPNETCORE_URLS` | API endpoints | `http://+:5000` |
| `ConnectionStrings__DefaultConnection` | SQL Server | - |
| `SignalR__Enabled` | Real-time sync | `true` |
| `FileStorage__Path` | Document storage | `\\Server\Nexus\Documents` |
| `OpenText__ApiUrl` | OpenText integration | - |
| `OpenText__ApiKey` | OpenText credentials | - |

### Frontend Configuration

```dart
// lib/config/app_config.dart
class AppConfig {
  static const String apiBaseUrl = 'http://your-server:5000/api/v1/';
  static const String signalRUrl = 'http://your-server:5000/hubs/sync';
  static const bool enableLogging = false;
}
```

## 🔐 Security

### 1. SQL Server Security

```sql
-- Create dedicated app user
CREATE LOGIN nexus_app WITH PASSWORD = 'StrongPassword123!';
CREATE USER nexus_app FOR LOGIN nexus_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::dbo TO nexus_app;
```

### 2. API Security (Production)

```csharp
// Add to Program.cs for production
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        // Configure JWT
    });

builder.Services.AddAuthorization(options => {
    options.AddPolicy("RequireAuthenticated", policy => 
        policy.RequireAuthenticatedUser());
});
```

### 3. File System Security

```powershell
# Grant access to app pool identity
icacls "\\Server\Nexus\Documents" /grant "IIS AppPool\NexusAPI:(OI)(CI)M"
```

## 📊 Monitoring

### Health Checks

```bash
# API health
curl http://localhost:5000/health

# Database health (via API)
curl http://localhost:5000/api/v1/health/db
```

### Logging

```csharp
// appsettings.Production.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Serilog": {
    "WriteTo": [
      { "Name": "File", "Args": { "path": "logs/nexus-.log" } }
    ]
  }
}
```

## 🔄 Backup Strategy

### Database

```bash
# Automated SQL backup
sqlcmd -S localhost -U sa -P 'password' -Q "
BACKUP DATABASE [NexusDB] 
TO DISK = 'D:\\Backups\\NexusDB_$(Get-Date -Format yyyyMMdd).bak'
WITH FORMAT, COMPRESSION"

# Or use Docker
 docker exec nexus-sqlserver /opt/mssql-tools/bin/sqlcmd \
   -S localhost -U SA -P 'YourStrong@Passw0rd' \
   -Q "BACKUP DATABASE [NexusDB] TO DISK = '/var/opt/mssql/backup/NexusDB.bak'"
```

### Documents

```powershell
# Robocopy backup
robocopy "\\Server\Nexus\Documents" "\\Backup\Nexus\Documents" /MIR /R:3 /W:10
```

## 🆘 Troubleshooting

### API won't start

```bash
# Check logs
docker-compose logs api

# Verify database connection
docker exec -it nexus-sqlserver /opt/mssql-tools/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong@Passw0rd' \
  -Q "SELECT name FROM sys.databases"
```

### Sync not working

```bash
# Check SignalR connections
curl http://localhost:5000/hubs/sync

# Verify network connectivity
# Windows Firewall: Allow port 5000
netsh advfirewall firewall add rule name="Nexus API" dir=in action=allow protocol=TCP localport=5000
```

### Database migration issues

```bash
# Reset migrations (development only!)
dotnet ef database drop --force
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## 📈 Performance Tuning

### SQL Server

```sql
-- Index maintenance
EXEC sp_updatestats;

-- Rebuild fragmented indexes
ALTER INDEX ALL ON DocumentNodes REBUILD;

-- Update closure table statistics
UPDATE STATISTICS NodePaths;
```

### API

```csharp
// Enable response caching
builder.Services.AddResponseCaching();
app.UseResponseCaching();

// Configure Kestrel
builder.WebHost.ConfigureKestrel(options => {
    options.Limits.MaxConcurrentConnections = 100;
    options.Limits.MaxConcurrentUpgradedConnections = 100;
});
```

## 📝 Post-Deployment Checklist

- [ ] API health check passes
- [ ] Database migrations applied
- [ ] SignalR hub accessible
- [ ] File storage path writable
- [ ] Flutter client connects
- [ ] Offline sync works
- [ ] Backups configured
- [ ] Monitoring enabled
- [ ] SSL certificates installed (production)
- [ ] Firewall rules configured

## 🎯 Next Steps

1. Configure Active Directory integration
2. Setup OpenText API credentials
3. Import existing documents
4. Train end users
5. Setup monitoring alerts
