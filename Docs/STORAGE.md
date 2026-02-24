# Multi-Storage System Documentation

## Overview

Nexus PM supports multiple storage backends for document files:

1. **Local Disk** - D:, E: or any local drive
2. **FTP Server** - Remote FTP/FTPS server
3. **OneDrive** - Microsoft OneDrive cloud
4. **Google Drive** - Google Drive cloud (planned)
5. **Network Share** - UNC paths (\\Server\Share)

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                   DocumentFileService                        │
│                   (Business Logic)                           │
└───────────────────────┬─────────────────────────────────────┘
                        │
            ┌───────────┼───────────┐
            ▼           ▼           ▼
    ┌──────────┐ ┌──────────┐ ┌──────────┐
    │  Local   │ │   FTP    │ │ OneDrive │
    │  Disk    │ │  Server  │ │  Cloud   │
    └──────────┘ └──────────┘ └──────────┘
```

## Configuration

### 1. Local Disk Storage

```bash
POST /api/v1/admin/storage/local-disk
{
  "storageName": "Primary Storage",
  "basePath": "D:\\NexusStorage",  # Windows
  "isDefault": true
}
```

Or network UNC path:
```bash
{
  "storageName": "Network Storage",
  "basePath": "\\\\Server\\NexusStorage",
  "isDefault": true
}
```

### 2. FTP Server

```bash
POST /api/v1/admin/storage/ftp
{
  "storageName": "FTP Backup",
  "host": "ftp.example.com",
  "port": 21,
  "username": "nexus",
  "password": "secure_password",
  "basePath": "/documents",
  "useSsl": true,
  "isDefault": false
}
```

### 3. OneDrive

**Prerequisites:**
1. Azure App Registration
2. Client ID and Secret
3. Admin consent for organization

```bash
POST /api/v1/admin/storage/onedrive
{
  "storageName": "Cloud Backup",
  "clientId": "your-app-client-id",
  "clientSecret": "your-app-secret",
  "tenantId": "common",  # or your tenant ID
  "folderId": "root",     # or specific folder ID
  "refreshToken": "oauth-refresh-token",
  "isDefault": false
}
```

## File Storage Path Structure

Files are organized automatically:

```
{StorageRoot}
└── {IdareCode}
    └── {QuyuCode}
        └── {MenteqeCode}
            └── {DocumentNumber}_{SafeFileName}.pdf

Example:
D:\NexusStorage
└── AZNEFT_IB
    └── QUYU_020
        └── MNT_001
            └── 1-4-8_3-2-1243_2026_Project_Document.pdf
```

## API Endpoints

### Admin Endpoints (Authentication Required)

```bash
# List all storages
GET /api/v1/admin/storage

# Add local disk storage
POST /api/v1/admin/storage/local-disk

# Add FTP storage
POST /api/v1/admin/storage/ftp

# Add OneDrive storage
POST /api/v1/admin/storage/onedrive

# Delete storage (soft delete)
DELETE /api/v1/admin/storage/{id}

# Set as default storage
POST /api/v1/admin/storage/{id}/set-default

# Health check
GET /api/v1/admin/storage/health

# Get storage types
GET /api/v1/admin/storage/types
```

### Document File Endpoints

```bash
# Upload file to document
POST /api/v1/documents/{documentId}/files
Content-Type: multipart/form-data
file: [binary data]

# Download file
GET /api/v1/files/{fileId}/download

# Get direct URL (if supported by storage)
GET /api/v1/files/{fileId}/url

# Delete file
DELETE /api/v1/files/{fileId}
```

## Security Considerations

### Local Disk
- Ensure application has write permissions
- Use dedicated service account
- Enable NTFS auditing for sensitive files

### FTP
- Always use FTPS (FTP over SSL)
- Use strong passwords
- Limit IP access on firewall
- Regular credential rotation

### OneDrive
- Store secrets in Azure Key Vault
- Use managed identities when possible
- Implement proper OAuth flow
- Regular token refresh

## Health Monitoring

```json
GET /api/v1/admin/storage/health
{
  "success": true,
  "data": [
    {
      "storageType": "LocalDisk",
      "isHealthy": true,
      "message": "Local disk is accessible: D:\NexusStorage",
      "availableSpace": 107374182400,
      "totalSpace": 536870912000
    },
    {
      "storageType": "FtpServer",
      "isHealthy": true,
      "message": "FTP server is accessible: ftp.example.com:21"
    },
    {
      "storageType": "OneDrive",
      "isHealthy": true,
      "message": "OneDrive connection successful",
      "availableSpace": 5368709120,
      "totalSpace": 1099511627776
    }
  ]
}
```

## Migration Guide

### Moving from Local Disk to Cloud

1. Add new cloud storage:
```bash
POST /api/v1/admin/storage/onedrive
{...}
```

2. Set as default:
```bash
POST /api/v1/admin/storage/{new-id}/set-default
```

3. Old files remain accessible
4. New files go to cloud storage

### Backup Strategy

Recommended: Use multiple storages

1. **Primary**: Local Disk (fast access)
2. **Backup 1**: FTP Server (remote backup)
3. **Backup 2**: OneDrive (cloud redundancy)

Configure application to write to all three or use scheduled sync jobs.

## Troubleshooting

### Local Disk Issues
```
Error: Access denied
Solution: Check NTFS permissions, ensure app pool identity has access
```

### FTP Issues
```
Error: Connection timeout
Solution: Check firewall, verify SSL settings, test with FileZilla
```

### OneDrive Issues
```
Error: Token expired
Solution: Re-authenticate, check Azure app registration status
```

## Best Practices

1. **Always have a local backup** - Cloud is great but keep local copy
2. **Monitor disk space** - Set up alerts for low space
3. **Regular health checks** - Automated storage health monitoring
4. **Test restores** - Regularly test file recovery from each storage
5. **Document paths** - Keep record of which storage each file uses

## Future Enhancements

- [ ] Google Drive support
- [ ] Azure Blob Storage support
- [ ] AWS S3 support
- [ ] Automatic storage tiering (hot/warm/cold)
- [ ] Cross-storage replication
- [ ] Storage usage analytics
