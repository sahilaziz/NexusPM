# Nexus PM - Server Konfiqurasiya Təlimatı

## 1. Server Tələbləri

### Minimum Tələblər (10-50 istifadəçi)
- **OS**: Windows Server 2019/2022 Standard
- **CPU**: 4 Core (Intel Xeon E5 və ya bənzəri)
- **RAM**: 16 GB
- **Disk**: 
  - C: (System) - 100 GB SSD
  - D: (Data) - 500 GB+ SSD
- **Şəbəkə**: 1 Gbps

### Tövsiyə Olunan (50-200+ istifadəçi)
- **OS**: Windows Server 2022 Datacenter
- **CPU**: 8+ Core
- **RAM**: 32 GB+
- **Disk**:
  - C: (System) - 200 GB SSD
  - D: (Data/DB) - 1 TB+ SSD/NVMe
  - E: (Backup) - 2 TB+ HDD
- **Şəbəkə**: 10 Gbps

## 2. Windows Server Quraşdırma

### 2.1 Rolların Quraşdırılması
```powershell
# PowerShell ilə IIS və .NET quraşdırma
Install-WindowsFeature -Name Web-Server, Web-Mgmt-Tools, Web-Mgmt-Console
Install-WindowsFeature -Name Web-Asp-Net45, Web-Net-Ext45

# .NET 9 Hosting Bundle yüklə
# https://dotnet.microsoft.com/download/dotnet/9.0
# Yüklə: ASP.NET Core Runtime 9.0 - Windows Hosting Bundle
```

### 2.2 Disk Konfiqurasiya
```powershell
# D: disk yarat (Data üçün)
New-Partition -DiskNumber 1 -UseMaximumSize | Format-Volume -FileSystem NTFS -NewFileSystemLabel "Data"

# E: disk yarat (Backup üçün)
New-Partition -DiskNumber 2 -UseMaximumSize | Format-Volume -FileSystem NTFS -NewFileSystemLabel "Backup"

# Qovluq strukturu yarat
New-Item -Path "D:\NexusPM" -ItemType Directory
New-Item -Path "D:\NexusPM\Data" -ItemType Directory
New-Item -Path "D:\NexusPM\Documents" -ItemType Directory
New-Item -Path "D:\NexusPM\Logs" -ItemType Directory
New-Item -Path "E:\NexusPM\Backup" -ItemType Directory
```

### 2.3 Folder İcazələri
```powershell
# IIS App Pool üçün icazələr
$appPoolIdentity = "IIS AppPool\NexusPM"

# Data qovluğu üçün tam icazə
icacls "D:\NexusPM" /grant "$appPoolIdentity:(OI)(CI)F"
icacls "D:\NexusPM\Documents" /grant "$appPoolIdentity:(OI)(CI)F"
icacls "D:\NexusPM\Logs" /grant "$appPoolIdentity:(OI)(CI)F"
```

## 3. SQL Server 2022 Quraşdırma

### 3.1 SQL Server İnstallasiya
1. SQL Server 2022 Developer/Standard Edition yüklə
2. Features:
   - Database Engine Services
   - SQL Server Replication
   - Full-Text and Semantic Extractions
   - Data Quality Services

### 3.2 Konfiqurasiya
```sql
-- Master database default path-ləri dəyiş
USE master;
GO

EXEC xp_instance_regwrite 
    N'HKEY_LOCAL_MACHINE', 
    N'Software\Microsoft\MSSQLServer\MSSQLServer', 
    N'DefaultData', 
    REG_SZ, 
    N'D:\NexusPM\Data'

EXEC xp_instance_regwrite 
    N'HKEY_LOCAL_MACHINE', 
    N'Software\Microsoft\MSSQLServer\MSSQLServer', 
    N'DefaultLog', 
    REG_SZ, 
    N'D:\NexusPM\Data'
```

### 3.3 Database Yaratma
```sql
-- Database yarat
CREATE DATABASE NexusDB
    COLLATE Cyrillic_General_CI_AS
    ON PRIMARY (
        NAME = N'NexusDB_Data',
        FILENAME = N'D:\NexusPM\Data\NexusDB.mdf',
        SIZE = 100MB,
        MAXSIZE = 10GB,
        FILEGROWTH = 100MB
    )
    LOG ON (
        NAME = N'NexusDB_Log',
        FILENAME = N'D:\NexusPM\Data\NexusDB.ldf',
        SIZE = 50MB,
        MAXSIZE = 2GB,
        FILEGROWTH = 50MB
    );
GO

-- App user yarat
CREATE LOGIN nexus_app WITH PASSWORD = 'StrongP@ssw0rd123!';
CREATE USER nexus_app FOR LOGIN nexus_app;
ALTER ROLE db_datareader ADD MEMBER nexus_app;
ALTER ROLE db_datawriter ADD MEMBER nexus_app;
GRANT EXECUTE TO nexus_app;
GO
```

## 4. IIS Konfiqurasiya

### 4.1 Application Pool Yarat
```powershell
Import-Module WebAdministration

# App Pool yarat
New-Item -Path IIS:\AppPools\NexusPM
Set-ItemProperty -Path IIS:\AppPools\NexusPM -Name "managedRuntimeVersion" -Value ""
Set-ItemProperty -Path IIS:\AppPools\NexusPM -Name "managedPipelineMode" -Value "Integrated"
Set-ItemProperty -Path IIS:\AppPools\NexusPM -Name "processModel.identityType" -Value "ApplicationPoolIdentity"

# Recycle settings
Set-ItemProperty -Path IIS:\AppPools\NexusPM -Name "recycling.periodicRestart.time" -Value "00:00:00"
Set-ItemProperty -Path IIS:\AppPools\NexusPM -Name "processModel.idleTimeout" -Value "00:00:00"
```

### 4.2 Website Yarat
```powershell
# Website yarat
New-Website -Name "NexusPM" -Port 80 -PhysicalPath "D:\NexusPM\API" -ApplicationPool "NexusPM"

# HTTPS binding əlavə et (sertifikatdan sonra)
New-WebBinding -Name "NexusPM" -Protocol "https" -Port 443
```

### 4.3 Web.config
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <handlers>
      <add name="aspNetCore" path="*" verb="*" modules="AspNetCoreModuleV2" resourceType="Unspecified"/>
    </handlers>
    <aspNetCore processPath="dotnet" arguments=".\Nexus.API.dll" stdoutLogEnabled="true" stdoutLogFile=".\logs\stdout" hostingModel="inprocess"/>
    <security>
      <requestFiltering>
        <requestLimits maxAllowedContentLength="2147483648"/> <!-- 2GB -->
      </requestFiltering>
    </security>
  </system.webServer>
  <system.web>
    <httpRuntime maxRequestLength="2097152" executionTimeout="3600"/> <!-- 2GB, 1 saat -->
  </system.web>
</configuration>
```

## 5. Firewall Konfiqurasiya

### 5.1 Windows Firewall
```powershell
# HTTP/HTTPS port-ları aç
New-NetFirewallRule -DisplayName "NexusPM-HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
New-NetFirewallRule -DisplayName "NexusPM-HTTPS" -Direction Inbound -Protocol TCP -LocalPort 443 -Action Allow

# SQL Server port-u aç (əgər uzaqdan bağlantı lazımdırsa)
New-NetFirewallRule -DisplayName "SQLServer" -Direction Inbound -Protocol TCP -LocalPort 1433 -Action Allow -RemoteAddress @("10.0.0.0/24", "192.168.1.0/24")

# SignalR/WebSocket üçün
New-NetFirewallRule -DisplayName "NexusPM-SignalR" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
```

### 5.2 Antivirus İstisnaları
```powershell
# Windows Defender istisnaları
Add-MpPreference -ExclusionPath "D:\NexusPM"
Add-MpPreference -ExclusionPath "D:\NexusPM\Documents"
Add-MpPreference -ExclusionProcess "dotnet.exe"
Add-MpPreference -ExclusionProcess "sqlservr.exe"
```

## 6. SSL/TLS Sertifikat

### 6.1 Let's Encrypt (Pulsuz)
```powershell
# win-acme istifadə et
# https://www.win-acme.com/

# Yüklə və quraşdır
# Yüklə: https://github.com/win-acme/win-acme/releases

# Sertifikat yarat (interactive)
wacs.exe --target iis --siteid 1 --installation iis
```

### 6.2 Ticari Sertifikat
```powershell
# PFX import et
$password = ConvertTo-SecureString "P@ssw0rd" -AsPlainText -Force
Import-PfxCertificate -FilePath "C:\certs\nexus.pfx" -CertStoreLocation Cert:\LocalMachine\WebHosting -Password $password

# IIS binding update et
Import-Module WebAdministration
$cert = Get-ChildItem -Path Cert:\LocalMachine\WebHosting | Where-Object {$_.Subject -like "*nexus.yourdomain.com*"}
$binding = Get-WebBinding -Name "NexusPM" -Protocol "https"
$binding.AddSslCertificate($cert.Thumbprint, "WebHosting")
```

## 7. Environment Variables

### 7.1 Sistem Environment Variables
```powershell
# Database connection
[Environment]::SetEnvironmentVariable("NEXUS_ConnectionStrings__DefaultConnection", "Server=localhost;Database=NexusDB;User Id=nexus_app;Password=StrongP@ssw0rd123!;TrustServerCertificate=True;", "Machine")

# JWT Secret
[Environment]::SetEnvironmentVariable("NEXUS_JwtSettings__SecretKey", "YourSuperSecretKeyMin32CharsLong!!!", "Machine")

# Storage path
[Environment]::SetEnvironmentVariable("NEXUS_Storage__DefaultPath", "D:\NexusPM\Documents", "Machine")

# Environment
[Environment]::SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production", "Machine")
[Environment]::SetEnvironmentVariable("ASPNETCORE_URLS", "http://+:80", "Machine")
```

### 7.2 appsettings.Production.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=NexusDB;User Id=nexus_app;Password=StrongP@ssw0rd123!;TrustServerCertificate=True;MultipleActiveResultSets=true;"
  },
  "JwtSettings": {
    "SecretKey": "YourSuperSecretKeyMin32CharsLong!!!",
    "Issuer": "NexusPM",
    "Audience": "NexusPM-Clients",
    "ExpiryMinutes": 480
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft.AspNetCore": "Warning",
      "Nexus": "Information"
    },
    "File": {
      "Path": "D:\\NexusPM\\Logs\\nexus-.log",
      "RollingInterval": "Day",
      "RetainedFileCountLimit": 30
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Limits": {
      "MaxRequestBodySize": 2147483648
    }
  }
}
```

## 8. Backup Strategiyası

### 8.1 SQL Server Backup
```sql
-- Full backup job
BACKUP DATABASE NexusDB 
TO DISK = 'E:\NexusPM\Backup\NexusDB_Full.bak'
WITH COMPRESSION, CHECKSUM;

-- Differential backup
BACKUP DATABASE NexusDB 
TO DISK = 'E:\NexusPM\Backup\NexusDB_Diff.bak'
WITH DIFFERENTIAL, COMPRESSION;

-- Log backup
BACKUP LOG NexusDB 
TO DISK = 'E:\NexusPM\Backup\NexusDB_Log.trn'
WITH COMPRESSION;
```

### 8.2 File Backup Script
```powershell
# Robocopy ilə document backup
robocopy "D:\NexusPM\Documents" "E:\NexusPM\Backup\Documents" /MIR /R:3 /W:10 /MT:8 /LOG:"E:\NexusPM\Backup\Logs\backup.log"

# API files backup
robocopy "D:\NexusPM\API" "E:\NexusPM\Backup\API" /MIR /R:3 /W:10
```

### 8.3 Scheduled Task
```powershell
# Gündəlik backup task-ı yarat
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-File D:\NexusPM\Scripts\backup.ps1"
$trigger = New-ScheduledTaskTrigger -Daily -At "02:00"
$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount
$settings = New-ScheduledTaskSettingsSet -ExecutionTimeLimit (New-TimeSpan -Hours 2)

Register-ScheduledTask -TaskName "NexusPM-Backup" -Action $action -Trigger $trigger -Principal $principal -Settings $settings
```

## 9. Monitoring

### 9.1 Performance Counters
```powershell
# Performance counter-lar yarat
New-ItemProperty -Path "HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib" -Name "DisablePerformanceCounters" -Value 0

# Yaddaş monitorinqi
logman create counter "NexusPM-Memory" -c "\Memory\Available MBytes" "\Process(dotnet)\Working Set" -si 60 -max 100 -o "D:\NexusPM\Logs\perf-memory.blg"

# Disk monitorinqi
logman create counter "NexusPM-Disk" -c "\LogicalDisk(D:)\% Free Space" "\LogicalDisk(D:)\Free Megabytes" -si 300 -max 100
```

### 9.2 Health Check Endpoint
```bash
# Health check
GET http://server/health

# Response:
{
  "status": "Healthy",
  "checks": {
    "database": "Connected",
    "storage": "Accessible",
    "diskSpace": "OK (85% free)"
  }
}
```

## 10. Troubleshooting

### 10.1 Ən Çox Rast Gəlinən Problemlər

**Problem**: "502.5 - ANCM Out-Of-Process Startup Failure"
```powershell
# Həll: .NET Hosting Bundle quraşdır
# Yoxla: Event Viewer > Windows Logs > Application
# Log yoxla: D:\NexusPM\API\logs\stdout_*.log
```

**Problem**: SQL Connection timeout
```sql
-- Həll: Connection string-də timeout artır
-- Default: 15 saniyə
Server=localhost;Database=NexusDB;...;Connect Timeout=60;
```

**Problem**: Fayl yükləmə xətası (həcmli fayllar)
```powershell
# Həll: IIS request limits artır
# web.config:
<requestLimits maxAllowedContentLength="2147483648"/>
```

### 10.2 Log Analizi
```powershell
# Son 50 xətanı göstər
Get-Content "D:\NexusPM\Logs\nexus-.log" | Select-Object -Last 50

# Xüsusi xətaları filtrə
Select-String -Path "D:\NexusPM\Logs\*.log" -Pattern "Error|Exception|Fatal"
```

## 11. Yeniləmə Prosesi

### 11.1 Sıfırdan Yeniləmə
```powershell
# 1. App Pool stop
Stop-WebAppPool -Name "NexusPM"

# 2. Backup al
robocopy "D:\NexusPM\API" "E:\NexusPM\Backup\API-$(Get-Date -Format yyyyMMdd)" /MIR

# 3. Yeni versiyanı kopyala
robocopy "\\DeployServer\NexusPM\Latest" "D:\NexusPM\API" /MIR

# 4. App Pool start
Start-WebAppPool -Name "NexusPM"

# 5. Health check
Invoke-RestMethod -Uri "http://localhost/health" -Method GET
```

## 12. Təhlükəsizlik Checklist

- [ ] Windows Updates quraşdırılıb
- [ ] Antivirus aktivdir və istisnalar düzgün konfiqurasiya edilib
- [ ] Firewall qaydaları aktivdir
- [ ] SSL sertifikatı quraşdırılıb və yenilənib
- [ ] SQL Server authentication aktivdir
- [ ] Zəif şifrələr qadağandır
- [ ] Backup avtomatlaşdırılıb və test edilib
- [ ] Log rotasiya konfiqurasiya edilib
- [ ] Monitorinq aktivdir
- [ ] Disaster recovery planı hazırdır
