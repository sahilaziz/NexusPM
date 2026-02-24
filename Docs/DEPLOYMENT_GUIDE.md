# Nexus Project Management - Deployment Guide

## 🚀 Sistem Deploymənt

### Ümumi Tələblər

```yaml
Backend:
  - .NET 9 Runtime
  - SQL Server 2022
  - IIS (Windows) və ya Docker
  - 4GB+ RAM
  - 20GB+ Disk

Mobile:
  - Flutter SDK 3.22+
  - Android Studio (Android)
  - Xcode (iOS)
  - 8GB+ RAM
```

---

## 📦 Backend Deployment

### 1. Database Setup

```sql
-- Database yarat
CREATE DATABASE NexusPM;
GO

-- User yarat
CREATE LOGIN nexus_user WITH PASSWORD = 'StrongP@ssw0rd!';
GO

USE NexusPM;
CREATE USER nexus_user FOR LOGIN nexus_user;
ALTER ROLE db_owner ADD MEMBER nexus_user;
GO
```

### 2. Environment Variables

```bash
# appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=NexusPM;User=nexus_user;Password=StrongP@ssw0rd!;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "your-super-secret-key-min-32-chars!",
    "Issuer": "NexusPM",
    "Audience": "NexusPM-Users"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "noreply@nexus.local",
    "Password": "app-specific-password",
    "FromEmail": "noreply@nexus.local"
  }
}
```

### 3. IIS Deployment

```powershell
# Publish
dotnet publish Backend/src/Nexus.API/Nexus.API.csproj -c Release -o ./publish

# IIS App Pool
Import-Module WebAdministration
New-Item -Path IIS:\AppPools\NexusPM
Set-ItemProperty -Path IIS:\AppPools\NexusPM -Name "managedRuntimeVersion" -Value ""

# IIS Site
New-Website -Name "NexusPM" -Port 8080 -PhysicalPath "C:\inetpub\wwwroot\NexusPM" -ApplicationPool "NexusPM"
```

### 4. Docker Deployment

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "Backend/src/Nexus.API/Nexus.API.csproj"
RUN dotnet build "Backend/src/Nexus.API/Nexus.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Backend/src/Nexus.API/Nexus.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nexus.API.dll"]
```

```yaml
# docker-compose.yml
version: '3.8'

services:
  api:
    build: .
    ports:
      - "8080:80"
    environment:
      - ConnectionStrings__DefaultConnection=Server=db;Database=NexusPM;User=sa;Password=Your_password123;TrustServerCertificate=True;
    depends_on:
      - db

  db:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=Your_password123
    ports:
      - "1433:1433"
    volumes:
      - sql_data:/var/opt/mssql

volumes:
  sql_data:
```

---

## 📱 Mobile App Build

### Android Build

```bash
# Development APK
flutter build apk --debug

# Release APK
flutter build apk --release

# App Bundle (Play Store)
flutter build appbundle --release

# Key store yarat
keytool -genkey -v -keystore nexus-pm.keystore -alias nexus -keyalg RSA -keysize 2048 -validity 10000
```

### iOS Build

```bash
# Simulator
flutter build ios --simulator

# Device
flutter build ios --release

# Archive (App Store)
open ios/Runner.xcworkspace
# Xcode-də: Product → Archive
```

### Flutter Web (PWA)

```bash
flutter build web --release
# Output: build/web/
# IIS və ya Nginx ilə host et
```

---

## 🔧 Configuration Files

### Backend appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=NexusPM;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Secret": "your-secret-key-min-32-characters",
    "Issuer": "NexusPM",
    "Audience": "NexusPM-Users",
    "ExpirationMinutes": 60
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "",
    "Password": "",
    "FromEmail": "noreply@nexus.local",
    "FromName": "Nexus PM",
    "EnableSsl": true
  },
  "Cors": {
    "AllowedOrigins": [
      "https://app.nexus.local",
      "http://localhost:3000"
    ]
  }
}
```

### Mobile API Config

```dart
// lib/core/api/api_config.dart
class ApiConfig {
  // Development
  static const String devBaseUrl = 'http://localhost:8080/api';
  
  // Production
  static const String prodBaseUrl = 'https://api.nexus.local/api';
  
  static String get baseUrl {
    if (kDebugMode) return devBaseUrl;
    return prodBaseUrl;
  }
}
```

---

## 🔄 CI/CD Pipeline (GitHub Actions)

```yaml
# .github/workflows/deploy.yml
name: Deploy

on:
  push:
    branches: [ main ]

jobs:
  backend:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      
      - name: Build
        run: dotnet build Backend/Nexus.sln -c Release
      
      - name: Test
        run: dotnet test Backend/Nexus.sln
      
      - name: Publish
        run: dotnet publish Backend/src/Nexus.API -c Release -o ./publish
      
      - name: Deploy to Server
        uses: appleboy/scp-action@master
        with:
          host: ${{ secrets.HOST }}
          username: ${{ secrets.USERNAME }}
          password: ${{ secrets.PASSWORD }}
          source: "./publish/*"
          target: "/var/www/nexus-pm"

  mobile-android:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Flutter
        uses: subosito/flutter-action@v2
        with:
          flutter-version: '3.22.0'
      
      - name: Build APK
        run: |
          cd mobile
          flutter pub get
          flutter build apk --release
      
      - name: Upload to Play Store
        # TODO: Add Play Store upload step
        run: echo "Upload to Play Store"
```

---

## 🛡️ Security Checklist

```
✅ HTTPS istifadə et
✅ JWT Secret güclü olsun (32+ chars)
✅ Database connection string encryption
✅ API Rate limiting aktiv et
✅ CORS policy düzgün konfiqurə et
✅ Input validation
✅ SQL Injection protection (EF Core)
✅ XSS protection
✅ Secure headers (HSTS, CSP)
✅ Regular security updates
```

---

## 📊 Monitoring & Logging

```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// Middleware
app.UseMiddleware<RequestTimingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

```yaml
# docker-compose.monitoring.yml
version: '3.8'

services:
  prometheus:
    image: prom/prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml

  grafana:
    image: grafana/grafana
    ports:
      - "3000:3000"
```

---

## 🆘 Troubleshooting

### Common Issues

| Problem | Solution |
|---------|----------|
| 500 Internal Server Error | Check logs in `/logs` folder |
| Database connection failed | Verify connection string, firewall |
| JWT validation failed | Check secret key, clock sync |
| CORS errors | Update `AllowedOrigins` in config |
| Mobile API 401 | Check token expiration, refresh flow |

---

## 🎉 Deployment Complete!

Sistem artıq işləyir:
- 🌐 API: https://api.nexus.local
- 📱 Web: https://app.nexus.local
- 📲 Android: Play Store
- 🍎 iOS: App Store

**Support**: support@nexus.local
