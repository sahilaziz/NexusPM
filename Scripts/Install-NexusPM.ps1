# Nexus PM - Automated Server Installation Script
# Run as Administrator

param(
    [Parameter(Mandatory=$true)]
    [string]$Environment = "Production",
    
    [Parameter(Mandatory=$true)]
    [string]$DataDrive = "D:",
    
    [Parameter(Mandatory=$false)]
    [string]$BackupDrive = "E:",
    
    [Parameter(Mandatory=$true)]
    [string]$DbPassword,
    
    [Parameter(Mandatory=$false)]
    [string]$Domain = ""
)

#Requires -RunAsAdministrator

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Nexus PM Server Installation" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Configuration
$Config = @{
    AppPoolName = "NexusPM"
    WebsiteName = "NexusPM"
    DataPath = "$DataDrive\NexusPM"
    ApiPath = "$DataDrive\NexusPM\API"
    DocumentsPath = "$DataDrive\NexusPM\Documents"
    LogsPath = "$DataDrive\NexusPM\Logs"
    BackupPath = if ($BackupDrive) { "$BackupDrive\NexusPM\Backup" } else { "$DataDrive\NexusPM\Backup" }
    DbName = "NexusDB"
    DbUser = "nexus_app"
    JwtSecret = -join ((33..126) | Get-Random -Count 64 | ForEach-Object {[char]$_})
}

try {
    # 1. Create Directories
    Write-Host "`n1. Creating directory structure..." -ForegroundColor Yellow
    @($Config.DataPath, $Config.ApiPath, $Config.DocumentsPath, $Config.LogsPath, $Config.BackupPath) | ForEach-Object {
        if (!(Test-Path $_)) {
            New-Item -Path $_ -ItemType Directory -Force | Out-Null
            Write-Host "   Created: $_" -ForegroundColor Green
        }
    }
    
    # 2. Install IIS Features
    Write-Host "`n2. Installing IIS features..." -ForegroundColor Yellow
    $features = @("Web-Server", "Web-Asp-Net45", "Web-Mgmt-Console")
    foreach ($feature in $features) {
        $installed = Get-WindowsFeature -Name $feature -ErrorAction SilentlyContinue
        if ($installed -and !$installed.Installed) {
            Install-WindowsFeature -Name $feature -IncludeManagementTools | Out-Null
            Write-Host "   Installed: $feature" -ForegroundColor Green
        } else {
            Write-Host "   Already installed: $feature" -ForegroundColor Gray
        }
    }
    
    # 3. Create App Pool
    Write-Host "`n3. Creating Application Pool..." -ForegroundColor Yellow
    Import-Module WebAdministration
    if (Test-Path "IIS:\AppPools\$($Config.AppPoolName)") {
        Remove-WebAppPool -Name $Config.AppPoolName
    }
    New-WebAppPool -Name $Config.AppPoolName -Force | Out-Null
    Set-ItemProperty -Path "IIS:\AppPools\$($Config.AppPoolName)" -Name "managedRuntimeVersion" -Value ""
    Write-Host "   Created: $($Config.AppPoolName)" -ForegroundColor Green
    
    # 4. Create Website
    Write-Host "`n4. Creating Website..." -ForegroundColor Yellow
    if (Test-Path "IIS:\Sites\$($Config.WebsiteName)") {
        Remove-Website -Name $Config.WebsiteName
    }
    New-Website -Name $Config.WebsiteName -Port 80 -PhysicalPath $Config.ApiPath -ApplicationPool $Config.AppPoolName -Force | Out-Null
    Write-Host "   Created: http://localhost" -ForegroundColor Green
    
    # 5. Set Permissions
    Write-Host "`n5. Setting permissions..." -ForegroundColor Yellow
    $identity = "IIS AppPool\$($Config.AppPoolName)"
    @($Config.DataPath, $Config.DocumentsPath, $Config.LogsPath) | ForEach-Object {
        icacls $_ /grant "$($identity):(OI)(CI)F" /T | Out-Null
        Write-Host "   Permissions set: $_" -ForegroundColor Green
    }
    
    # 6. Firewall Rules
    Write-Host "`n6. Configuring firewall..." -ForegroundColor Yellow
    @(80, 443) | ForEach-Object {
        $rule = Get-NetFirewallRule -DisplayName "NexusPM-Port$_" -ErrorAction SilentlyContinue
        if (!$rule) {
            New-NetFirewallRule -DisplayName "NexusPM-Port$_" -Direction Inbound -Protocol TCP -LocalPort $_ -Action Allow | Out-Null
            Write-Host "   Rule created: Port $_" -ForegroundColor Green
        }
    }
    
    # 7. Create Config File
    Write-Host "`n7. Creating configuration..." -ForegroundColor Yellow
    $appSettings = @"
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=$($Config.DbName);User Id=$($Config.DbUser);Password=$DbPassword;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "$($Config.JwtSecret)",
    "Issuer": "NexusPM",
    "Audience": "NexusPM-$Environment",
    "ExpiryMinutes": 480
  }
}
"@
    $appSettings | Out-File -FilePath "$($Config.ApiPath)\appsettings.Production.json" -Encoding UTF8
    Write-Host "   Config created" -ForegroundColor Green
    
    # Summary
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Installation Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "API Path: $($Config.ApiPath)"
    Write-Host "Documents: $($Config.DocumentsPath)"
    Write-Host "Backup: $($Config.BackupPath)"
    Write-Host "`nNext Steps:"
    Write-Host "1. Copy Nexus.API.dll to: $($Config.ApiPath)"
    Write-Host "2. Create SQL database: $($Config.DbName)"
    Write-Host "3. Start website in IIS Manager"
    Write-Host "========================================"
    
} catch {
    Write-Host "`nERROR: $($_.Exception.Message)" -ForegroundColor Red
    throw
}
