# Nexus Project Management - Project Summary

## 🎯 Project Overview

**Nexus Project Management** - neft və qaz sənayesi üçün sənəd və layihə idarəetmə sistemi.

**Əsas Xüsusiyyət:** Universal sənəd identifikator sistemi - həm daxil olan məktubları (xarici nömrə ilə), həm də daxili layihələri (avtomatik nömrə ilə) idarə edir.

---

## ✅ Completed Features

### 1. Sənəd Identifikator Sistemi (YENI)

#### Daxil olan məktublar (Incoming Letters)
- İstifadəçi sənəd nömrəsini daxil edir
- **Format dəstəyi:** `1-4-8\3-2-1243\2026`, `45-а\123\2026`, və s.
- Xüsusi simvollar saxlanılır (\, -, /)
- Unikal nömrə yoxlanışı

#### Daxili layihələr (Internal Projects)
- Sistem avtomatik nömrə yaradır
- **Format:** `PRJ-{İDARƏ}-{İL}-{SAY}`
- Nümunə: `PRJ-AZNEFT_IB-2026-0001`

### 2. Smart Axtarış (YENI)

#### Normalization Algorithm
```
Original:     1-4-8\3-2-1243\2026
Normalized:   1 4 8 3 2 1243 2026

Axtarış:      "1 4 2026"
Nəticə:       1-4-8\3-2-1243\2026  ✓
```

- Simvollar ignor edilir: `-`, `\`, `/`, `.`, `_`
- Full-text search
- Partial matching dəstəyi

### 3. Backend (ASP.NET Core 9)

#### Core Architecture
- Clean Architecture (Domain → Application → Infrastructure → API)
- JWT Authentication
- SignalR Real-time

#### Database Schema
```sql
DocumentNodes
├── DocumentNumber           -- Original: 1-4-8\3-2-1243\2026
├── NormalizedDocumentNumber -- Search: 1 4 8 3 2 1243 2026
├── ExternalDocumentNumber   -- Xarici nömrə (əgər varsa)
└── SourceType               -- IncomingLetter/InternalProject/ExternalDocument
```

#### API Endpoints
| Endpoint | Method | Description |
|----------|--------|-------------|
| `/documents/create-incoming-letter` | POST | Daxil olan məktub yarat |
| `/documents/create-internal-project` | POST | Daxili layihə yarat |
| `/documents/check-document-number` | GET | Nömrə unikallığını yoxla |
| `/documents/search-by-number` | GET | Smart axtarış |
| `/documents/tree` | GET | Qovluq ağacı |
| `/documents/search` | GET | Ümumi axtarış |

### 4. Frontend (Flutter 3.22+)

#### Yeni UI
- **Sənəd növü seçimi:** Daxil olan məktub / Daxili layihə
- **Nömrə yoxlanışı:** Real-time unikal yoxlama
- **Smart search:** Simvolları ignor edən axtarış

#### Models
```dart
enum DocumentSourceType {
  incomingLetter,   // Daxil olan məktub
  internalProject,  // Daxili layihə
  externalDocument, // Xarici sənəd
}

DocumentNode
├── documentNumber              // Original
├── normalizedDocumentNumber    // Search index
├── externalDocumentNumber      // Xarici nömrə
└── sourceType                  // Type
```

### 5. Smart Foldering (Mövcud)

```
Idare → Quyu → Menteqe → Sənəd
Azneft İB → 20 saylı quyu → 1 nömrəli məntəqə → Sənəd
```

- Avtomatik hierarchy yaratma
- Duplicate folder qarşısını alma
- Closure Table Pattern

---

## 📊 Sənəd Nömrə Formatları

| Tip | Format | Nümunə |
|-----|--------|--------|
| Daxil olan məktub | İstifadəçi daxil edir | `1-4-8\3-2-1243\2026` |
| Daxili layihə | Avtomatik | `PRJ-AZNEFT_IB-2026-0001` |
| Xarici sənəd | Avtomatik | `EXT-AZNEFT_IB-2026-0001` |

---

## 🔍 Smart Axtarış Nümunələri

```sql
-- Axtarış: "1 4 2026"
-- Nəticə: 1-4-8\3-2-1243\2026

-- Axtarış: "45 2026"
-- Nəticə: 45-а\123\2026

-- Axtarış: "PRJ AZNEFT 0001"
-- Nəticə: PRJ-AZNEFT_IB-2026-0001
```

---

## 🚀 Quick Start

### 1. Daxil olan məktub əlavə et
```bash
curl -X POST http://localhost:5000/api/v1/documents/create-incoming-letter \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "idareCode": "AZNEFT_IB",
    "documentNumber": "1-4-8\\3-2-1243\\2026",
    "subject": "Qazma işləri"
  }'
```

### 2. Daxili layihə yarat
```bash
curl -X POST http://localhost:5000/api/v1/documents/create-internal-project \
  -H "Authorization: Bearer {token}" \
  -d '{
    "idareCode": "AZNEFT_IB",
    "projectName": "Yeni Quyu Layihəsi"
  }'
```

### 3. Smart axtarış
```bash
curl "http://localhost:5000/api/v1/documents/search-by-number?number=1-4-8-2026" \
  -H "Authorization: Bearer {token}"
```

---

## 📝 Fayl Adlandırma

```
{YYYY-MM-DD} - {DocumentNumber} - {Subject}.pdf

Examples:
2026-02-24 - 1-4-8\3-2-1243\2026 - Qazma işlərinin təhvil-təslimi.pdf
2026-02-24 - PRJ-AZNEFT_IB-2026-0001 - Yeni Quyu Layihəsi.pdf
```

---

## 🎯 Key Features

1. **Universal Identifikator Sistemi**
   - Daxil olan məktublar (manual nömrə)
   - Daxili layihələr (avtomatik nömrə)
   
2. **Smart Axtarış**
   - Simvolları ignor et
   - Full-text search
   
3. **Unikal Nömrə Yoxlanışı**
   - Real-time validation
   - Duplicate qarşısını alma
   
4. **Smart Foldering**
   - Avtomatik hierarchy
   - Idare → Quyu → Menteqe → Sənəd

---

## 📁 Project Structure

```
Backend/
├── Domain/
│   ├── DocumentNode.cs              # +NormalizedDocumentNumber, SourceType
│   └── DocumentSourceType.cs        # Enum: IncomingLetter, InternalProject
├── Application/
│   ├── DocumentIdentifierService.cs # YENI: ID generation & search
│   └── DocumentService.cs           # Updated
├── Infrastructure/
│   └── DocumentRepository.cs        # +SearchByNormalizedNumber
└── API/
    └── DocumentsController.cs       # +create-incoming-letter, +create-internal-project

Frontend/
├── Models/
│   ├── document_node.dart           # +normalizedDocumentNumber, sourceType
│   └── document_upload_screen.dart  # YENI: Source type selector
└── Services/
    └── api_service.dart             # Updated endpoints
```

---

## ✅ Status

**Tam hazırdır!**

- ✅ Universal sənəd identifikator sistemi
- ✅ Smart axtarış (simvol ignor)
- ✅ Daxil olan məktub / Daxili layihə dəstəyi
- ✅ Unikal nömrə yoxlanışı
- ✅ Smart Foldering
- ✅ JWT Auth + SignalR

**Lahiyə istifadəyə hazırdır!** 🚀

---

## 🖥️ Server Deployment

### Server Tələbləri
| Komponent | Minimum | Tövsiyə Olunan |
|-----------|---------|----------------|
| **OS** | Windows Server 2019 | Windows Server 2022 |
| **CPU** | 4 Core | 8+ Core |
| **RAM** | 16 GB | 32+ GB |
| **Disk C:** | 100 GB SSD | 200 GB SSD |
| **Disk D:** | 500 GB SSD | 1 TB+ NVMe (Data) |
| **Disk E:** | - | 2 TB+ (Backup) |

### Deployment Üsulları

#### 1. PowerShell Auto-Installation (Windows Server)
```powershell
# Bir əmr ilə bütün konfiqurasiya
.\Scripts\Install-NexusPM.ps1 `
  -Environment "Production" `
  -DataDrive "D:" `
  -BackupDrive "E:" `
  -DbPassword "StrongP@ssw0rd123!"

# Bu skript avtomatik olaraq:
# - IIS feature-larını quraşdırır
# - App Pool və Website yaradır
# - Qovluq strukturunu yaradır (D:\NexusPM)
# - İcazələri konfiqurasiya edir
# - Firewall qaydaları əlavə edir
# - appsettings.Production.json yaradır
```

#### 2. Docker Compose (Linux/Windows)
```bash
# Development
docker-compose up -d

# Production
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d
```

#### 3. Manual Deployment
Ətraflı təlimat üçün: `Docs/SERVER_SETUP.md`

### Qovluq Strukturu
```
D:\NexusPM                    # Əsas qovluq
├── API\                      # API faylları
│   ├── Nexus.API.dll
│   ├── appsettings.Production.json
│   └── web.config
├── Documents\                # Sənəd faylları
│   ├── AZNEFT_IB\
│   │   └── QUYU_020\
│   │       └── MNT_001\
│   │           └── *.pdf
├── Logs\                     # Log faylları
└── Scripts\                  # Backup və maintenance
    ├── backup.ps1
    └── health-check.ps1

E:\NexusPM\Backup             # Backup qovluğu
├── SQL\                      # Database backup
├── Documents\                # Fayl backup
└── API\                      # API backup
```

### Konfiqurasiya Faylları

#### appsettings.Production.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=NexusDB;User Id=nexus_app;Password=***;TrustServerCertificate=True;"
  },
  "JwtSettings": {
    "SecretKey": "64-char-random-secret-key",
    "Issuer": "NexusPM",
    "Audience": "NexusPM-Production",
    "ExpiryMinutes": 480
  },
  "Storage": {
    "DefaultPath": "D:\\NexusPM\\Documents"
  }
}
```

### SSL Sertifikat (HTTPS)
```powershell
# Let's Encrypt ilə pulsuz sertifikat
wacs.exe --target iis --siteid 1 --installation iis

# Və ya təcili sertifikat yüklə
Import-PfxCertificate -FilePath "cert.pfx" -CertStoreLocation Cert:\LocalMachine\WebHosting
```

### Backup Strategiyası
```powershell
# Gündəlik avtomatik backup
schtasks /create /tn "NexusPM-Backup" /tr "powershell.exe -File D:\NexusPM\Scripts\backup.ps1" /sc daily /st 02:00

# Backup skripti:
# 1. SQL Database full backup
# 2. Document faylların backup
# 3. 30 gündən köhnə backup-ları sil
```

### Health Check & Monitoring
```bash
# Health check endpoint
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

---

## 📚 Əlavə Sənədlər

| Sənəd | Təsvir |
|-------|--------|
| `Docs/SERVER_SETUP.md` | Ətraflı server quraşdırma |
| `Docs/STORAGE.md` | Multi-storage konfiqurasiya |
| `Docs/API.md` | API dokumentasiyası |
| `Scripts/Install-NexusPM.ps1` | Auto-install skripti |
| `docker-compose.prod.yml` | Production Docker |

---

## ✅ Final Status

**Nexus PM tam hazırdır!** 🎉

### Backend (100%)
- ✅ Universal sənəd identifikator sistemi
- ✅ Smart axtarış (simvolları ignor edir)
- ✅ Multi-storage (Local/FTP/OneDrive)
- ✅ Smart Foldering
- ✅ JWT Authentication
- ✅ SignalR real-time
- ✅ SQL Server + EF Core

### Frontend (100%)
- ✅ Flutter Windows + Android
- ✅ Offline sync
- ✅ Document upload
- ✅ Smart search UI

### DevOps (100%)
- ✅ PowerShell auto-installation
- ✅ Docker support
- ✅ IIS configuration
- ✅ SSL/HTTPS
- ✅ Backup automation
- ✅ Health monitoring

### Server (100%)
- ✅ Windows Server deployment guide
- ✅ Automated installation script
- ✅ Production configuration
- ✅ Security hardening
- ✅ Backup strategy

**🚀 Layihə istehsalata hazırdır!**
