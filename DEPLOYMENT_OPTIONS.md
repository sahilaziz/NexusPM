# Nexus PM - Deployment Options

## 1. Professional Installer (Setup.exe) ⭐ RECOMMENDED

### For Windows Server Environments

**File**: `NexusPM-Setup.exe` (150-300 MB)

**Includes:**
- ✅ SQL Server 2022 Express (auto-download & install)
- ✅ IIS Application Pool & Website configuration
- ✅ Active Directory integration setup
- ✅ Automatic folder structure creation
- ✅ Permission configuration
- ✅ Firewall rules
- ✅ SSL Certificate setup (Let's Encrypt)

### Installation Types

#### A. Interactive Installation (Wizard)
```
1. Double-click NexusPM-Setup.exe
2. Follow wizard steps:
   - License Agreement
   - Installation Path (D:\NexusPM)
   - SQL Server Options:
     * Install SQL Express (recommended)
     * Use existing SQL Server
   - Authentication Mode:
     * Active Directory (Windows Auth)
     * Local Users (JWT)
     * Mixed Mode
   - IIS Configuration
   - Storage Backend
3. Click Install
4. Done! (10-30 minutes)
```

#### B. Silent Installation (Enterprise)
```powershell
# Install with SQL Express + AD
NexusPM-Setup.exe /S `
  /INSTALLPATH="D:\NexusPM" `
  /SQLSERVER="INSTALL" `
  /SQLEDITION="EXPRESS" `
  /SQLPASSWORD="SecurePass123!" `
  /AUTHMODE="AD" `
  /DOMAIN="CORP"

# Or use config file
NexusPM-Setup.exe /S /CONFIG="install.config.json"
```

### Configuration File Example
```json
{
  "Installation": {
    "Path": "D:\\NexusPM",
    "CreateDesktopShortcut": true
  },
  "SQLServer": {
    "Mode": "InstallNew",
    "Edition": "Express",
    "InstanceName": "NEXUSPM",
    "DataPath": "D:\\SQLData",
    "SaPassword": "SecurePass123!",
    "AppPassword": "AppPass123!"
  },
  "ActiveDirectory": {
    "Enabled": true,
    "Domain": "CORP.COMPANY.COM",
    "UserGroup": "NexusPM_Users",
    "AdminGroup": "NexusPM_Admins"
  },
  "IIS": {
    "WebsiteName": "NexusPM",
    "Port": 80,
    "EnableSsl": true
  },
  "Storage": {
    "Type": "LocalDisk",
    "Path": "D:\\NexusPM\\Documents"
  }
}
```

---

## 2. Docker Deployment

### For Linux/Cloud Environments

**File**: `docker-compose.yml`

```bash
# Development
docker-compose up -d

# Production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

**Includes:**
- ✅ SQL Server 2022 Linux container
- ✅ Nexus API container (2 replicas)
- ✅ Nginx reverse proxy
- ✅ SSL termination
- ✅ Health checks
- ✅ Automatic restart

### Production Docker Stack
```yaml
version: '3.9'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=YourStrong@Passw0rd
    volumes:
      - sql_data:/var/opt/mssql/data
    
  api:
    image: nexuspm/api:latest
    deploy:
      replicas: 2
    environment:
      - ConnectionStrings__DefaultConnection=Server=sqlserver;...
    
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
```

---

## 3. Manual Deployment

### For Advanced Users

**Steps:**
1. Install SQL Server manually
2. Install IIS with ASP.NET
3. Copy API files
4. Configure appsettings.json
5. Set permissions
6. Create database

See: `Docs/SERVER_SETUP.md`

---

## Feature Comparison

| Feature | Setup.exe | Docker | Manual |
|---------|-----------|--------|--------|
| SQL Server Install | ✅ Auto | ✅ Container | ❌ Manual |
| IIS Config | ✅ Auto | ✅ Nginx | ❌ Manual |
| AD Integration | ✅ Wizard | ⚠️ Config | ⚠️ Config |
| SSL Setup | ✅ Auto/Let's Encrypt | ✅ Config | ❌ Manual |
| Backup Config | ✅ Auto | ⚠️ Config | ❌ Manual |
| Silent Install | ✅ Yes | ✅ Yes | ❌ No |
| Offline Install | ✅ Yes (with ISO) | ⚠️ Images | ✅ Yes |
| Upgrade Support | ✅ Yes | ✅ Yes | ⚠️ Script |

---

## Recommended Deployment Scenarios

### Small Office (5-20 users)
**Use**: Setup.exe
```
Server: Windows Server 2019+
SQL: Express (auto-install)
Storage: Local Disk
Time: 15 minutes
```

### Medium Enterprise (20-100 users)
**Use**: Setup.exe with existing SQL
```
Server: Windows Server 2022
SQL: Standard/Enterprise (existing)
Storage: Network SAN
AD: Full integration
Time: 30 minutes
```

### Large Enterprise (100+ users)
**Use**: Setup.exe + Load Balancer
```
Servers: 2x Web + 1x SQL Cluster
SQL: Enterprise with AlwaysOn
Storage: Enterprise SAN
AD: SSO with Kerberos
Load Balancer: Hardware/Software
Time: 2-4 hours
```

### Cloud/Azure
**Use**: Docker or Azure Templates
```
Azure SQL Managed Instance
Azure Container Instances
Azure Files (storage)
Azure AD (authentication)
```

---

## Pre-Installation Checklist

### Server Requirements
- [ ] Windows Server 2019/2022 (for Setup.exe)
- [ ] 4+ CPU cores
- [ ] 16+ GB RAM
- [ ] D: Drive 500GB+ (data)
- [ ] E: Drive 1TB+ (backup)
- [ ] Static IP address
- [ ] Domain joined (for AD auth)

### Software Prerequisites
- [ ] .NET 9 Runtime
- [ ] IIS Installed (for Setup.exe)
- [ ] SQL Server (or use installer)
- [ ] Firewall configured
- [ ] Antivirus exclusions set

### Information Needed
- [ ] Domain name (if AD)
- [ ] SQL Server credentials
- [ ] SSL certificate (or use Let's Encrypt)
- [ ] Storage path
- [ ] Admin email

---

## Post-Installation

### Immediate Tasks
1. Change default admin password
2. Configure backup schedule
3. Test AD authentication
4. Upload SSL certificate (if not auto)
5. Configure email notifications

### Health Check
```powershell
# Verify all components
& "C:\Program Files\SOCAR\NexusPM\Scripts\health-check.ps1"

# Should output:
# ✓ Database: Connected
# ✓ Storage: Accessible  
# ✓ AD: Authenticated
# ✓ IIS: Running
```

---

## Support

**Installation Issues:**
- 📧 support@azneft.az
- 📞 +994 12 123 45 67
- 🌐 https://support.azneft.az

**Logs Location:**
```
%TEMP%\NexusPM-Install-*.log
C:\ProgramData\NexusPM\Logs\
```
