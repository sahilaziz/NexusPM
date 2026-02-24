# Nexus PM - SQL Server Installation Script
# Supports both SQL Server Express and Full Edition installation

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("EXPRESS", "DEVELOPER", "STANDARD", "ENTERPRISE")]
    [string]$Edition = "EXPRESS",
    
    [Parameter(Mandatory=$false)]
    [string]$InstanceName = "NEXUSPM",
    
    [Parameter(Mandatory=$true)]
    [string]$SaPassword,
    
    [Parameter(Mandatory=$false)]
    [string]$DataPath = "D:\\SQLData",
    
    [Parameter(Mandatory=$false)]
    [string]$LogPath = "D:\\SQLLogs",
    
    [Parameter(Mandatory=$false)]
    [switch]$Silent
)

$ErrorActionPreference = "Stop"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "[$timestamp] [$Level] $Message"
    Write-Host $logMessage -ForegroundColor $(if($Level -eq "ERROR"){"Red"}elseif($Level -eq "WARN"){"Yellow"}else{"Green"})
}

function Test-SQLInstalled {
    param([string]$InstanceName)
    
    $services = Get-Service | Where-Object { $_.Name -like "*MSSQL*$InstanceName*" -or $_.Name -eq "MSSQLSERVER" }
    return $services.Count -gt 0
}

function Get-SQLDownloadUrl {
    param([string]$Edition)
    
    switch ($Edition) {
        "EXPRESS" { 
            return @{
                Url = "https://go.microsoft.com/fwlink/?linkid=2215158"
                FileName = "SQLEXPR_x64_ENU.exe"
                Arguments = "/ACTION=Install /QUIET /IACCEPTSQLSERVERLICENSETERMS /FEATURES=SQLENGINE /INSTANCENAME=$InstanceName /SQLSVCACCOUNT='NT AUTHORITY\\SYSTEM' /SQLSYSADMINACCOUNTS='BUILTIN\\ADMINISTRATORS' /SECURITYMODE=SQL /SAPWD='$SaPassword' /SQLUSERDBDIR='$DataPath' /SQLUSERDBLOGDIR='$LogPath'"
            }
        }
        "DEVELOPER" {
            return @{
                Url = "https://go.microsoft.com/fwlink/?linkid=2215159"
                FileName = "SQLDEV_x64_ENU.exe"
                Arguments = "/ACTION=Install /QUIET /IACCEPTSQLSERVERLICENSETERMS /FEATURES=SQLENGINE /INSTANCENAME=$InstanceName /SQLSVCACCOUNT='NT AUTHORITY\\SYSTEM' /SQLSYSADMINACCOUNTS='BUILTIN\\ADMINISTRATORS' /SECURITYMODE=SQL /SAPWD='$SaPassword' /SQLUSERDBDIR='$DataPath' /SQLUSERDBLOGDIR='$LogPath'"
            }
        }
        default {
            # For Standard/Enterprise, assume ISO/media is provided
            return $null
        }
    }
}

try {
    Write-Log "Starting SQL Server $Edition Installation"
    Write-Log "Instance Name: $InstanceName"
    Write-Log "Data Path: $DataPath"
    Write-Log "Log Path: $LogPath"

    # Check if already installed
    if (Test-SQLInstalled -InstanceName $InstanceName) {
        Write-Log "SQL Server instance '$InstanceName' already installed" "WARN"
        
        if (-not $Silent) {
            $response = Read-Host "Do you want to skip installation? (Y/N)"
            if ($response -eq 'Y' -or $response -eq 'y') {
                Write-Log "Skipping installation"
                exit 0
            }
        } else {
            exit 0
        }
    }

    # Create directories
    @($DataPath, $LogPath) | ForEach-Object {
        if (!(Test-Path $_)) {
            New-Item -Path $_ -ItemType Directory -Force | Out-Null
            Write-Log "Created directory: $_"
        }
    }

    $sqlInfo = Get-SQLDownloadUrl -Edition $Edition
    
    if ($sqlInfo -eq $null -and $Edition -in @("STANDARD", "ENTERPRISE")) {
        # For Standard/Enterprise, look for installation media
        $mediaPath = "${env:SystemDrive}\SQLServer2022"
        if (Test-Path "$mediaPath\setup.exe") {
            Write-Log "Using local installation media: $mediaPath"
            $installer = "$mediaPath\setup.exe"
            $arguments = "/ACTION=Install /QUIET /IACCEPTSQLSERVERLICENSETERMS /FEATURES=SQLENGINE /INSTANCENAME=$InstanceName /SQLSVCACCOUNT='NT AUTHORITY\\SYSTEM' /SQLSYSADMINACCOUNTS='BUILTIN\\ADMINISTRATORS' /SECURITYMODE=SQL /SAPWD='$SaPassword' /SQLUSERDBDIR='$DataPath' /SQLUSERDBLOGDIR='$LogPath'"
        } else {
            throw "Installation media not found for $Edition. Please mount SQL Server ISO or copy installation files to $mediaPath"
        }
    } else {
        # Download installer
        $tempPath = "$env:TEMP\SQLInstall"
        if (!(Test-Path $tempPath)) {
            New-Item -Path $tempPath -ItemType Directory -Force | Out-Null
        }

        $installer = "$tempPath\$($sqlInfo.FileName)"
        
        if (!(Test-Path $installer)) {
            Write-Log "Downloading SQL Server $Edition..."
            Invoke-WebRequest -Uri $sqlInfo.Url -OutFile $installer -UseBasicParsing
            Write-Log "Download complete"
        }

        $arguments = $sqlInfo.Arguments
    }

    # Install SQL Server
    Write-Log "Installing SQL Server... This may take 10-30 minutes."
    Write-Log "Command: $installer $arguments"
    
    $process = Start-Process -FilePath $installer -ArgumentList $arguments -Wait -PassThru -NoNewWindow
    
    if ($process.ExitCode -ne 0) {
        throw "SQL Server installation failed with exit code: $($process.ExitCode)"
    }

    Write-Log "SQL Server installation completed successfully"

    # Configure SQL Server
    Write-Log "Configuring SQL Server..."
    
    # Enable TCP/IP
    $instancePath = if ($InstanceName -eq "MSSQLSERVER") { "MSSQLSERVER" } else { "MSSQL`$$InstanceName" }
    $smo = 'Microsoft.SqlServer.Management.Smo'
    [System.Reflection.Assembly]::LoadWithPartialName($smo) | Out-Null
    
    $wmi = New-Object ($smo + '.Wmi.ManagedComputer')
    $tcp = $wmi.ServerInstances[$InstanceName].ServerProtocols['Tcp']
    $tcp.IsEnabled = $true
    $tcp.Alter()
    
    # Restart service
    $serviceName = if ($InstanceName -eq "MSSQLSERVER") { "MSSQLSERVER" } else { "MSSQL`$$InstanceName" }
    Restart-Service -Name $serviceName -Force
    
    Write-Log "SQL Server configured successfully"
    
    # Create Nexus database user
    Write-Log "Creating Nexus database user..."
    
    $connectionString = "Server=localhost\$InstanceName;Database=master;User Id=sa;Password=$SaPassword;TrustServerCertificate=True;"
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    $command = $connection.CreateCommand()
    $command.CommandText = @"
IF NOT EXISTS (SELECT * FROM sys.sql_logins WHERE name = 'nexus_app')
BEGIN
    CREATE LOGIN nexus_app WITH PASSWORD = '$SaPassword', CHECK_POLICY = OFF;
    ALTER SERVER ROLE sysadmin ADD MEMBER nexus_app;
END
"@
    $command.ExecuteNonQuery() | Out-Null
    $connection.Close()
    
    Write-Log "Nexus database user created"
    
    # Output connection info
    Write-Log "========================================"
    Write-Log "SQL Server Installation Complete"
    Write-Log "========================================"
    Write-Log "Instance: localhost\$InstanceName"
    Write-Log "SA Username: sa"
    Write-Log "App Username: nexus_app"
    Write-Log "Connection String:"
    Write-Log "Server=localhost\$InstanceName;Database=NexusDB;User Id=nexus_app;Password=***;TrustServerCertificate=True;"
    Write-Log "========================================"

} catch {
    Write-Log "Installation failed: $($_.Exception.Message)" "ERROR"
    throw
}
