# Öz Monitoring Sisteminiz - Admin Paneldən İdarə

## Xüsusiyyətlər

✅ **Tam Pulsuz** - SQL Server istifadə edir, əlavə xərc yoxdur
✅ **Enable/Disable Switch** - Admin paneldən aktiv/deaktiv etmək
✅ **Real-time Dashboard** - Request, error, performance metrics
✅ **Configurable** - Threshold-ları admin paneldən dəyişmək

---

## Admin Panel API-ləri

### 1. Monitoring Status-unu Yoxla
```http
GET /api/admin/monitoring/status
Authorization: Bearer {admin-token}
```

**Cavab:**
```json
{
  "isEnabled": true,
  "logRequests": true,
  "logErrors": true,
  "trackPerformance": true,
  "retentionDays": 30,
  "currentMetrics": {
    "totalRequests": 1523,
    "errorCount": 12,
    "averageResponseTime": 145.5
  }
}
```

---

### 2. Monitoring-i Aç/Bağla (Switch)
```http
POST /api/admin/monitoring/toggle
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "enable": false
}
```

**Cavab:**
```json
{
  "message": "Monitoring disabled",
  "isEnabled": false,
  "timestamp": "2026-02-24T09:30:00Z"
}
```

---

### 3. Konfiqurasiyanı Yoxla
```http
GET /api/admin/monitoring/config
Authorization: Bearer {admin-token}
```

**Cavab:**
```json
{
  "configId": 1,
  "isEnabled": true,
  "logRequests": true,
  "logErrors": true,
  "trackPerformance": true,
  "logDatabaseQueries": false,
  "minimumLogLevel": "Information",
  "retentionDays": 30,
  "slowRequestThresholdMs": 1000,
  "alertEmail": null,
  "cpuAlertThreshold": 80,
  "memoryAlertThreshold": 85,
  "errorRateAlertThreshold": 5
}
```

---

### 4. Konfiqurasiyanı Yenilə
```http
PUT /api/admin/monitoring/config
Authorization: Bearer {admin-token}
Content-Type: application/json

{
  "isEnabled": true,
  "logRequests": true,
  "logErrors": true,
  "trackPerformance": true,
  "slowRequestThresholdMs": 500,
  "retentionDays": 14
}
```

---

### 5. Dashboard Məlumatları
```http
GET /api/admin/monitoring/dashboard?hours=1
Authorization: Bearer {admin-token}
```

**Cavab:**
```json
{
  "period": "01:00:00",
  "totalRequests": 1523,
  "errorCount": 12,
  "errorRate": 0.79,
  "averageResponseTime": 145.5,
  "slowRequests": 23,
  "recentErrors": [
    {
      "timestamp": "2026-02-24T09:25:00Z",
      "message": "Timeout error",
      "endpoint": "/api/documents/upload"
    }
  ],
  "topEndpoints": [
    {
      "endpoint": "/api/documents",
      "count": 523,
      "avgDuration": 120.5
    }
  ]
}
```

---

## SQL ilə Manual Yoxlama

### Son 100 log
```sql
SELECT TOP 100 * FROM SystemLogs
ORDER BY Timestamp DESC;
```

### Son 1 saatda neçə request olub
```sql
SELECT COUNT(*) as RequestCount
FROM SystemLogs
WHERE Category = 'Request'
  AND Timestamp > DATEADD(HOUR, -1, GETUTCDATE());
```

### Orta response time
```sql
SELECT AVG(DurationMs) as AvgResponseTime
FROM SystemLogs
WHERE Category = 'Request'
  AND Timestamp > DATEADD(HOUR, -1, GETUTCDATE());
```

### Səhv əksər olan endpoint-lər
```sql
SELECT Endpoint, COUNT(*) as ErrorCount
FROM SystemLogs
WHERE Level >= 3 -- Error, Critical
  AND Timestamp > DATEADD(HOUR, -1, GETUTCDATE())
GROUP BY Endpoint
ORDER BY ErrorCount DESC;
```

### Dashboard View
```sql
SELECT * FROM vw_MonitoringDashboard;
```

---

## Konfiqurasiya Variantları

### Variant 1: Minimal (Yalnız Error-lar)
```json
{
  "isEnabled": true,
  "logRequests": false,
  "logErrors": true,
  "trackPerformance": false,
  "minimumLogLevel": "Error"
}
```
**Nəticə:** Yalnız xətalar yazılır, database kiçik qalır.

---

### Variant 2: Normal (Request + Error)
```json
{
  "isEnabled": true,
  "logRequests": true,
  "logErrors": true,
  "trackPerformance": false,
  "slowRequestThresholdMs": 1000,
  "retentionDays": 14
}
```
**Nəticə:** Request-lər və error-lar, 14 gün saxlanılır.

---

### Variant 3: Full (Hər şey)
```json
{
  "isEnabled": true,
  "logRequests": true,
  "logErrors": true,
  "trackPerformance": true,
  "logDatabaseQueries": true,
  "slowRequestThresholdMs": 500,
  "retentionDays": 30
}
```
**Nəticə:** Hər şey izlənilir, daha çox disk tutumu.

---

## Maintenance (Təmizlik)

### Manual təmizlik
```sql
-- 7 gündən köhnə log-ları sil
EXEC sp_CleanupOldMonitoringData @RetentionDays = 7;
```

### Avtomatik təmizlik
Sistem hər gecə avtomatik təmizlik edir (RetentionDays əsasında).

---

## Nə Zaman Bağlamaq Olar?

### Monitoring-i bağlayın əgər:
- Disk yeriniz azalıbsa
- Performance problemi varsa (log yazmaq da vaxt aparır)
- Debug prosesini bitirmisinizsə

### Nə vaxt açın:
- Production problemləri araşdırmaq lazımdırsa
- Performance analizi aparmaq istəyirsinizsə
- User activity izləmək lazımdırsa

---

## XÜLASƏ

| Xüsusiyyət | Status |
|------------|--------|
| **Qiymət** | $0 (Pulsuz) |
| **Enable/Disable** | ✅ Admin paneldən |
| **Real-time** | ✅ 1-2 saniyə gecikmə |
| **Data saxlama** | SQL Server-də (sizin nəzarətinizdə) |
| **Xarici dependency** | ❌ Yoxdur |

**Nəticə:** Azure Application Insights-ə ehtiyac yoxdur, özünüzün tam nəzarətinizdədir! 🎉
