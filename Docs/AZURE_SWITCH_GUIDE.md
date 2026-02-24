# Azure ↔ Öz Sistem Switch Qaydaları

## Ümumi Məlumat

**Default:** Hər iki sistem də öz sistemdir (Pulsuz)
- Messaging: SQL Server Message Queue
- Monitoring: SQL Server Monitoring

**Lazım olanda:** Admin paneldən Azure-a keçə bilərsiniz (Ödənişli)

---

## Admin Paneldən Switch Etmə

### 1. Cari Status-u Yoxla
```http
GET /api/admin/server-config/status
Authorization: Bearer {super-admin-token}
```

**Cavab:**
```json
{
  "messaging": {
    "currentMode": "Private",
    "isPrivate": true,
    "isAzure": false,
    "status": "Running (SQL Server)",
    "canSwitch": true
  },
  "monitoring": {
    "currentMode": "Private",
    "isPrivate": true,
    "isAzure": false,
    "status": "Running (SQL Server)",
    "canSwitch": true
  },
  "costs": {
    "current": "$0/ay (Pulsuz)",
    "privateOnly": "$0/ay",
    "azureMessagingOnly": "$30/ay",
    "azureMonitoringOnly": "$200/ay",
    "fullAzure": "$230/ay"
  }
}
```

---

### 2. Messaging Sistemini Dəyiş (Private → Azure)

**Addım 1:** Azure Service Bus connection string əlavə et
```http
PUT /api/admin/server-config/azure/servicebus-connection
Authorization: Bearer {super-admin-token}
Content-Type: application/json

{
  "connectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key"
}
```

**Addım 2:** Switch et
```http
POST /api/admin/server-config/messaging/switch
Authorization: Bearer {super-admin-token}
Content-Type: application/json

{
  "mode": "Azure"
}
```

**Cavab:**
```json
{
  "message": "Messaging mode changed to Azure",
  "warning": "Server restart required for changes to take effect",
  "newMode": "Azure",
  "oldMode": "Private",
  "timestamp": "2026-02-24T10:00:00Z",
  "changedBy": "admin@nexus.local"
}
```

**Addım 3:** Server-i restart et

---

### 3. Monitoring Sistemini Dəyiş (Private → Azure)

**Addım 1:** Azure Application Insights connection string əlavə et
```http
PUT /api/admin/server-config/azure/appinsights-connection
Authorization: Bearer {super-admin-token}
Content-Type: application/json

{
  "connectionString": "InstrumentationKey=your-key;IngestionEndpoint=https://your-region.in.applicationinsights.azure.com/"
}
```

**Addım 2:** Switch et
```http
POST /api/admin/server-config/monitoring/switch
Authorization: Bearer {super-admin-token}
Content-Type: application/json

{
  "mode": "Azure"
}
```

**Addım 3:** Server-i restart et

---

### 4. Hər İkisini Birdən Dəyiş
```http
POST /api/admin/server-config/switch-all
Authorization: Bearer {super-admin-token}
Content-Type: application/json

{
  "messagingMode": "Azure",
  "monitoringMode": "Azure"
}
```

---

### 5. Geri Qayıtmaq (Azure → Private)

```http
POST /api/admin/server-config/switch-all
Authorization: Bearer {super-admin-token}
Content-Type: application/json

{
  "messagingMode": "Private",
  "monitoringMode": "Private"
}
```

**Cavab:**
```json
{
  "message": "All systems mode changed",
  "warning": "Server restart required for changes to take effect",
  "newConfig": {
    "messaging": "Private",
    "monitoring": "Private"
  },
  "oldConfig": {
    "messaging": "Azure",
    "monitoring": "Azure"
  },
  "timestamp": "2026-02-24T10:00:00Z",
  "changedBy": "admin@nexus.local"
}
```

Server restart edin və pulsuz sistemə qayıtmış olacaqsınız!

---

## Konfiqurasiya Faylı (appsettings.json)

### Default (Pulsuz)
```json
{
  "Messaging": {
    "Mode": "Private",
    "AzureServiceBus": {
      "ConnectionString": ""
    }
  },
  "Monitoring": {
    "Mode": "Private",
    "ApplicationInsights": {
      "ConnectionString": ""
    }
  }
}
```

### Azure (Ödənişli)
```json
{
  "Messaging": {
    "Mode": "Azure",
    "AzureServiceBus": {
      "ConnectionString": "Endpoint=sb://..."
    }
  },
  "Monitoring": {
    "Mode": "Azure",
    "ApplicationInsights": {
      "ConnectionString": "InstrumentationKey=..."
    }
  }
}
```

---

## Xərc Müqayisəsi

| Kombinasiya | Aylıq Xərc | Nə vaxt istifadə et |
|-------------|-----------|---------------------|
| **Private + Private** | **$0** | ✅ Default, 5000 user-ə qədər kifayət |
| Private + Azure | $200 | Monitoring çox məlumat yığılanda |
| Azure + Private | $30 | Message traffic çox olanda |
| **Azure + Azure** | **$230** | 10,000+ user, enterprise scale |

---

## Nə Zaman Azure-a Keçmək Lazımdır?

### Messaging (Azure Service Bus) lazımdır əgər:
- 10,000+ message/saniyə
- Multi-region deployment
- Zero message loss tələb olunur
- Geo-replication lazımdır

### Monitoring (Application Insights) lazımdır əgər:
- 100GB+ log/ay
- Advanced analytics (AI-based)
- Real-time alerting (SMS)
- Distributed tracing
- Live metrics stream

---

## TÖVSİYƏ

### İndi (Development / 1000 user)
```
Messaging:  Private ✅ ($0)
Monitoring: Private ✅ ($0)
Toplam:     $0/ay
```

### Gələcəkdə (5000+ user)
```
Messaging:  Private ✅ ($0) - kifayət edir
Monitoring: Private ✅ ($0) - kifayət edir
Toplam:     $0/ay
```

### Scale (10,000+ user)
```
Messaging:  Azure ($30/ay) - message traffic çox olarsa
Monitoring: Private ($0)   - əgər yetərlidirsə
Toplam:     $30/ay
```

---

## XÜLASƏ

✅ **Default:** Pulsuz öz sistemlər işləyir
✅ **Admin Panel:** Bir kliklə Azure-a keçə bilərsiniz
✅ **Geri Qayıtma:** Bir kliklə pulsuz sistemə qayıda bilərsiniz
✅ **Heç bir risk yoxdur:** Hər iki sistem hazırdır, istədiyiniz vaxt switch edin

**Sizin qərarınız:** İndi pulsuz işlədin, gələcəkdə lazım olanda Azure-a keçin! 🎉
