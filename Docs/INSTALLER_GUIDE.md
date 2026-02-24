# Nexus PM - Professional Installer Guide

## Overview

Nexus PM includes a professional Windows Installer (MSI/Setup.exe) that supports:

- ✅ **One-click installation** of all components
- ✅ **SQL Server installation** (Express/Developer/Standard/Enterprise)
- ✅ **Active Directory integration** (Windows Authentication)
- ✅ **IIS automatic configuration**
- ✅ **Multi-storage backend** configuration

## Installer Features

### 1. Installation Modes

#### Silent Installation (Enterprise)
```powershell
# Silent install with configuration file
NexusPM-Setup.exe /S /CONFIG="install.config.json"

# Or with parameters
NexusPM-Setup.exe /S `
  /INSTALLPATH="D:\NexusPM" `
  /SQLSERVER="INSTALL" `
  /SQLEDITION="EXPRESS" `
  /SQLPASSWORD="SecurePass123!" `
  /AUTHMODE="AD" `
  /DOMAIN="CORP" `
  /IISPORT="80"
```

#### Interactive Installation
Double-click `NexusPM-Setup.exe` and follow wizard:
1. Welcome & License Agreement
2. Installation Path Selection
3. SQL Server Configuration
4. Active Directory Settings
5. IIS Configuration
6. Storage Backend Selection
7. Ready to Install
8. Installation Progress
9. Completion

### 2. SQL Server Options

#### Option A: Install SQL Server Express (Recommended for Small Deployments)
- **Automatic download** and installation
- **Pre-configured** for Nexus PM
- **Instance name**: `NEXUSPM` (configurable)
- **Auto-creates**: Database + Application User

#### Option B: Install SQL Server Developer/Standard/Enterprise
- **Requires**: Installation media (ISO)
- **Supports**: All SQL Server editions
- **Custom paths**: Data/Log file locations
- **Enterprise features**: Clustering, AlwaysOn

#### Option C: Use Existing SQL Server
```
Server: sql-server.company.com
Port: 1433
Instance: (optional)
Database: NexusDB (auto-created)
Username: sa (or admin)
Password: ********
```

### 3. Active Directory Integration

#### Automatic AD Configuration
The installer can:
1. **Detect Domain**: Auto-detect current domain
2. **Create AD Groups**: `NexusPM_Users`, `NexusPM_Admins`
3. **Configure Windows Auth**: Enable Kerberos/NTLM
4. **Map Domain Users**: Auto-import domain users

#### Manual AD Configuration
```json
{
  "ActiveDirectory": {
    "Enabled": true,
    "Domain": "CORP.COMPANY.COM",
    "LdapPath": "LDAP://DC=CORP,DC=COMPANY,DC=COM",
    "UserGroups": "NexusPM_Users,Domain Users",
    "AdminGroups": "NexusPM_Admins,Domain Admins",
    "AutoCreateUsers": true
  }
}
```

#### Mixed Mode Authentication
- **Primary**: Active Directory (Windows Auth)
- **Fallback**: Local JWT Authentication
- **External Users**: SQL-based authentication

### 4. IIS Configuration

The installer automatically:
- ✅ Creates Application Pool (`NexusPM`)
- ✅ Creates Website (`NexusPM`)
- ✅ Configures Port 80/443
- ✅ Sets folder permissions
- ✅ Installs URL Rewrite (if needed)
- ✅ Configures request limits (2GB uploads)

#### SSL/HTTPS Setup
```powershell
# With Let's Encrypt (auto)
NexusPM-Setup.exe /SSL="LETSENCRYPT" /DOMAIN="nexus.company.com"

# With existing certificate
NexusPM-Setup.exe /SSL="PFX" /CERTPATH="C:\certs\nexus.pfx" /CERTPASS="***"

# Self-signed (development only)
NexusPM-Setup.exe /SSL="SELF"
```

## Installation Scenarios

### Scenario 1: Small Office (5-20 users)
```yaml
Server: Single Windows Server 2019
SQL: SQL Server Express (auto-install)
Storage: Local Disk (D:)
Auth: Active Directory
IIS: Default port 80
Backup: Daily to E:
```

**Command:**
```powershell
.\NexusPM-Setup.exe /S `
  /SQLSERVER="INSTALL" `
  /SQLEDITION="EXPRESS" `
  /STORAGE="LOCAL" `
  /AUTHMODE="AD"
```

### Scenario 2: Enterprise (100+ users)
```yaml
Server: Windows Server 2022 Datacenter
SQL: SQL Server 2022 Enterprise (existing cluster)
Storage: Network SAN (\\storage\nexus)
Auth: Active Directory with SSO
IIS: HTTPS with Load Balancer
Backup: SQL AlwaysOn + File replication
```

**Command:**
```powershell
.\NexusPM-Setup.exe /S `
  /SQLSERVER="EXISTING" `
  /SQLHOST="sql-cluster.company.com" `
  /SQLUSER="nexus_app" `
  /SQLPASS="***" `
  /STORAGE="NETWORK" `
  /NETWORKPATH="\\storage\nexus" `
  /AUTHMODE="AD" `
  /DOMAIN="CORP" `
  /SSL="PFX" `
  /CERTPATH="C:\certs\star.pfx"
```

### Scenario 3: Cloud Hybrid
```yaml
Server: Azure VM (Windows Server 2022)
SQL: Azure SQL Managed Instance
Storage: Azure Blob Storage
Auth: Azure AD (with on-prem sync)
IIS: Azure Application Gateway
```

## Configuration File

### `install.config.json`
```json
{
  "Installation": {
    "Path": "D:\\NexusPM",
    "CreateDesktopShortcut": true,
    "StartService": true
  },
  "SQLServer": {
    "Mode": "InstallNew",
    "Edition": "Express",
    "InstanceName": "NEXUSPM",
    "DataPath": "D:\\SQLData",
    "LogPath": "D:\\SQLLogs",
    "SaPassword": "SecurePass123!",
    "AppPassword": "AppPass123!"
  },
  "ActiveDirectory": {
    "Enabled": true,
    "Domain": "CORP.COMPANY.COM",
    "UserGroup": "NexusPM_Users",
    "AdminGroup": "NexusPM_Admins",
    "AutoCreateUsers": true
  },
  "IIS": {
    "WebsiteName": "NexusPM",
    "AppPoolName": "NexusPM",
    "Port": 80,
    "SslPort": 443,
    "EnableSsl": true,
    "CertificateSource": "LetsEncrypt"
  },
  "Storage": {
    "Type": "LocalDisk",
    "Path": "D:\\NexusPM\\Documents"
  },
  "Features": {
    "InstallSamples": false,
    "InstallDocumentation": true,
    "EnableAutomaticUpdates": true
  }
}
```

## Silent Installation Examples

### Basic Silent Install
```powershell
NexusPM-Setup.exe /S /SQLPASSWORD="Pass123!"
```

### Full Silent with AD
```powershell
$args = @(
    "/S"
    "/INSTALLPATH=D:\NexusPM"
    "/SQLSERVER=INSTALL"
    "/SQLEDITION=EXPRESS"
    "/SQLINSTANCE=NEXUSPM"
    "/SQLPASSWORD=SecurePass123!"
    "/AUTHMODE=AD"
    "/DOMAIN=CORP"
    "/ADUSERGROUP=NexusPM_Users"
    "/ADADMINGROUP=NexusPM_Admins"
    "/IISPORT=80"
    "/IISSSLPORT=443"
    "/STORAGE=LOCAL"
    "/STORAGEPATH=D:\NexusPM\Documents"
)
Start-Process -FilePath "NexusPM-Setup.exe" -ArgumentList $args -Wait
```

### Unattended with SQL Existing
```powershell
NexusPM-Setup.exe /S `
    /SQLSERVER=EXISTING `
    /SQLHOST=sql01.corp.local `
    /SQLPORT=1433 `
    /SQLUSER=sa `
    /SQLPASSWORD=*** `
    /DBNAME=NexusDB `
    /AUTHMODE=AD `
    /DOMAIN=CORP `
    /NOSTARTMENU
```

## Post-Installation

### 1. Verify Installation
```powershell
# Check services
Get-Service | Where-Object { $_.Name -like "*Nexus*" -or $_.Name -like "*SQL*NEXUS*" }

# Check IIS
Get-Website -Name "NexusPM"
Get-IISAppPool -Name "NexusPM"

# Check database
sqlcmd -S localhost\NEXUSPM -Q "SELECT name FROM sys.databases WHERE name = 'NexusDB'"

# Test API
Invoke-RestMethod -Uri "http://localhost/health" -Method GET
```

### 2. Configure Firewall
```powershell
# Already done by installer, but verify:
Get-NetFirewallRule -DisplayName "NexusPM*"
```

### 3. First Login
1. Open browser: `http://server/` or `http://server/admin`
2. Login with domain credentials (if AD enabled)
3. Or use default admin: `admin` / `admin123` (change immediately!)

### 4. Post-Install Script
```powershell
# Run post-install configuration
& "C:\Program Files\SOCAR\NexusPM\Scripts\PostInstall.ps1" -ConfigureAD -CreateDefaultUsers
```

## Troubleshooting

### Installation Logs
```
%TEMP%\NexusPM-Install-*.log
C:\ProgramData\NexusPM\Logs\install.log
```

### Common Issues

#### Issue: SQL Server installation fails
**Solution:**
```powershell
# Check if .NET Framework installed
Get-WindowsFeature -Name NET-Framework-45-Core

# Install manually if needed
Install-WindowsFeature -Name NET-Framework-45-Core

# Retry installation
NexusPM-Setup.exe /REPAIR
```

#### Issue: IIS binding conflict
**Solution:**
```powershell
# Check existing bindings
netsh http show urlacl

# Change port
NexusPM-Setup.exe /IISPORT=8080
```

#### Issue: AD authentication not working
**Solution:**
```powershell
# Test AD connection
test-computersecurechannel -repair

# Check SPNs
setspn -L domain\nexuspm-svc
```

## Building the Installer

### Prerequisites
- WiX Toolset 3.14+
- Visual Studio 2022
- .NET 9 SDK

### Build Command
```powershell
# Build MSI
msbuild Installer\NexusInstaller\NexusInstaller.wixproj /p:Configuration=Release

# Create Setup.exe (bundled with prerequisites)
Installer\CreateBundle.exe -config installer.config.xml

# Sign installer
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com "NexusPM-Setup.exe"
```

## Support

For installation support:
- 📧 Email: support@azneft.az
- 📞 Phone: +994 12 123 45 67
- 💻 Intranet: https://support.azneft.az
