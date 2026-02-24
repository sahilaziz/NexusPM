# Nexus Project Management - API Documentation

## Base URL
```
Development: http://localhost:5000/api/v1/
Production: https://your-domain.com/api/v1/
SignalR: ws://localhost:5000/hubs/sync
```

## Authentication

All endpoints require JWT Bearer token.

### Login
```http
POST /api/v1/auth/login
Content-Type: application/json

{
  "username": "john.doe",
  "password": "securePassword123"
}
```

## Documents API

### Create Incoming Letter (Daxil olan məktub)
İstifadəçi sənəd nömrəsini daxil edir. Format: `1-4-8\3-2-1243\2026`

```http
POST /api/v1/documents/create-incoming-letter
Authorization: Bearer {token}
Content-Type: application/json

{
  "idareCode": "AZNEFT_IB",
  "idareName": "Azneft İB",
  "quyuCode": "QUYU_020",
  "quyuName": "20 saylı quyu",
  "menteqeCode": "MNT_001",
  "menteqeName": "1 nömrəli məntəqə",
  "documentDate": "2026-02-24T00:00:00Z",
  "documentNumber": "1-4-8\\3-2-1243\\2026",
  "subject": "Qazma işlərinin təhvil-təslimi"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "nodeId": 123,
    "documentNumber": "1-4-8\\3-2-1243\\2026",
    "normalizedDocumentNumber": "1 4 8 3 2 1243 2026",
    "entityName": "2026-02-24 - 1-4-8\\3-2-1243\\2026 - Qazma işlərinin təhvil-təslimi",
    "sourceType": "IncomingLetter"
  }
}
```

### Create Internal Project (Daxili layihə)
Sistem avtomatik nömrə yaradır: `PRJ-{İDARƏ}-{İL}-{SAY}`

```http
POST /api/v1/documents/create-internal-project
Authorization: Bearer {token}
Content-Type: application/json

{
  "idareCode": "AZNEFT_IB",
  "idareName": "Azneft İB",
  "quyuCode": "QUYU_020",
  "quyuName": "20 saylı quyu",
  "menteqeCode": "MNT_001",
  "menteqeName": "1 nömrəli məntəqə",
  "documentDate": "2026-02-24T00:00:00Z",
  "projectName": "Yeni Quyu Layihəsi"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "nodeId": 124,
    "documentNumber": "PRJ-AZNEFT_IB-2026-0001",
    "entityName": "2026-02-24 - PRJ-AZNEFT_IB-2026-0001 - Yeni Quyu Layihəsi",
    "sourceType": "InternalProject"
  }
}
```

### Check Document Number
Sənəd nömrəsinin unikal olduğunu yoxla

```http
GET /api/v1/documents/check-document-number?number=1-4-8\3-2-1243\2026
Authorization: Bearer {token}
```

**Response:**
```json
{
  "isUnique": true,
  "original": "1-4-8\\3-2-1243\\2026",
  "normalized": "1 4 8 3 2 1243 2026",
  "message": "Bu nömrə istifadə edilə bilər"
}
```

### Smart Search by Document Number
Simvolları ignor edərək axtarış (\, -, / və s.)

```http
GET /api/v1/documents/search-by-number?number=1-4-8-3-2-1243-2026
Authorization: Bearer {token}
```

**Response:**
```json
{
  "success": true,
  "count": 1,
  "searchTerm": "1-4-8-3-2-1243-2026",
  "normalized": "1 4 8 3 2 1243 2026",
  "data": [
    {
      "nodeId": 123,
      "documentNumber": "1-4-8\\3-2-1243\\2026",
      "normalizedDocumentNumber": "1 4 8 3 2 1243 2026"
    }
  ]
}
```

### General Search
```http
GET /api/v1/documents/search?term=qazma&idareCode=AZNEFT_IB&dateFrom=2026-01-01
Authorization: Bearer {token}
```

### Get Document Tree
```http
GET /api/v1/documents/tree?parentId=1
Authorization: Bearer {token}
```

## Smart Search Algorithm

### Normalization
Sənəd nömrələri axtarış üçün normalize edilir:

| Original | Normalized |
|----------|------------|
| `1-4-8\3-2-1243\2026` | `1 4 8 3 2 1243 2026` |
| `45-а\123\2026` | `45 А 123 2026` |
| `PRJ-AZNEFT-2026-0001` | `PRJ AZNEFT 2026 0001` |

### Axtarış qaydaları:
1. Simvollar ignor edilir: `-`, `\`, `/`, `.`, `_`
2. Böyük/kiçik hərf fərqi yoxdur
3. Boşluqlar normalize edilir
4. Hər bir rəqəm/hərf qrupu ayrıca axtarılır

### Nümunə axtarışlar:
```
Axtarış: "1 4 8 2026"
Tapılacaq: "1-4-8\3-2-1243\2026"

Axtarış: "45 2026"
Tapılacaq: "45-а\123\2026"

Axtarış: "PRJ 2026 0001"
Tapılacaq: "PRJ-AZNEFT_IB-2026-0001"
```

## Document Source Types

| Type | Description | ID Format |
|------|-------------|-----------|
| `IncomingLetter` | Daxil olan məktub | İstifadəçi daxil edir: `1-4-8\3-2-1243\2026` |
| `InternalProject` | Daxili layihə | Avtomatik: `PRJ-AZNEFT_IB-2026-0001` |
| `ExternalDocument` | Xarici sənəd | Avtomatik: `EXT-AZNEFT_IB-2026-0001` |

## Error Responses

### Duplicate Document Number
```json
{
  "success": false,
  "message": "Bu sənəd nömrəsi artıq istifadə olunur: 1-4-8\\3-2-1243\\2026"
}
```

## File Naming Convention

```
Format: {YYYY-MM-DD} - {DocumentNumber} - {Subject}.pdf

Examples:
2026-02-24 - 1-4-8\3-2-1243\2026 - Qazma işlərinin təhvil-təslimi.pdf
2026-02-24 - PRJ-AZNEFT_IB-2026-0001 - Yeni Quyu Layihəsi.pdf
```
