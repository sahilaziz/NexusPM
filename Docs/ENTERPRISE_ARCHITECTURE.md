# NexusPM Enterprise Architecture Guide

## Məqsəd: 5000+ Concurrent User, 99.9% Uptime, Horizontal Scale

## 1. ARXITEKTUR DƏYİŞİKLİYİ: Monolith → Modular Monolith → Microservices

### Mövcud Hal (Monolith)
```
[API] → [Database]
  ↓
[SignalR]
```

### Enterprise Hal (Modular Monolith + CQRS)
```
[API Gateway]
     ↓
[Auth Module] ← → [User DB]
     ↓
[Document Module] ← → [Doc DB]
     ↓
[Task Module] ← → [Task DB]
     ↓
[Notification Service] ← → [Redis/NCache]
     ↓
[Event Bus] (Azure Service Bus)
```

## 2. CQRS + EVENT SOURCING (Əsas Dəyişiklik)

### Nədir?
- **Command** (yazma) və **Query** (oxuma) ayrılır
- Hər dəyişiklik event olaraq saxlanılır
- Audit log avtomatik olur

### Implementasiya
```csharp
// Command (Yazma)
public class CreateDocumentCommand : IRequest<DocumentDto>
{
    public string Title { get; set; }
    public long ParentId { get; set; }
}

// Query (Oxuma - optimize edilmiş)
public class GetDocumentQuery : IRequest<DocumentDto>
{
    public long DocumentId { get; set; }
}

// Event Store (Azure Cosmos DB və ya SQL Server)
public class DocumentCreatedEvent
{
    public Guid EventId { get; set; }
    public long DocumentId { get; set; }
    public string Title { get; set; }
    public DateTime Timestamp { get; set; }
    public string UserId { get; set; }
}
```

## 3. DISTRIBUTED CACHING (Redis əvəzinə)

### NCache (Commercial, Windows-native)
```csharp
// appsettings.json
"NCache": {
  "CacheName": "NexusPMCache",
  "Servers": "server1:9800,server2:9800",
  "EnableClientLogs": false
}

// Service registration
services.AddNCacheDistributedCache(configuration);
```

**Niyə NCache?**
- ✅ Windows-native (sizin stack-a uyğun)
- ✅ Active-Active multi-site replication
- ✅ SQL Server integration
- ✅ Native .NET object caching (serialization lazım deyil)

## 4. DATABASE STRATEGIYASI

### Mövcud: Single SQL Server
```
[Single SQL Server] ← 5000 user
```

### Enterprise: SQL Server Always On + Read Replicas
```sql
-- Primary: Writes + Critical Reads
-- Secondary 1: Report queries (Read-only)
-- Secondary 2: Analytics (Read-only)
-- Secondary 3: Backup/DR
```

### Database per Service (Microservices üçün)
```
[Auth DB]         - SQL Server (User data)
[Document DB]     - SQL Server (File metadata)
[Task DB]         - SQL Server (Task data)
[Event Store]     - Azure Cosmos DB (Events)
[Cache]           - NCache
[Search]          - Azure Cognitive Search
```

## 5. MESSAGE QUEUE (Event-Driven)

### Azure Service Bus (Commercial)
```csharp
// Event publishing
public class DocumentUploadedEvent : IEvent
{
    public long DocumentId { get; set; }
    public string UploadedBy { get; set; }
    public DateTime Timestamp { get; set; }
}

// Handler
public class DocumentUploadedHandler : IEventHandler<DocumentUploadedEvent>
{
    public async Task Handle(DocumentUploadedEvent @event)
    {
        // 1. Send notification
        // 2. Update search index
        // 3. Generate thumbnail
        // 4. Audit log
    }
}
```

**Avantajları:**
- Guaranteed delivery
- Dead-letter queue (xəta halları üçün)
- Scheduled messages
- Transaction support

## 6. API GATEWAY

### Ocelot (Open source amma production-ready) və ya Azure API Management
```csharp
// ocelot.json
{
  "Routes": [
    {
      "DownstreamPathTemplate": "/api/documents/{id}",
      "DownstreamScheme": "https",
      "DownstreamHostAndPorts": [
        { "Host": "document-service", "Port": 80 }
      ],
      "UpstreamPathTemplate": "/api/documents/{id}",
      "UpstreamHttpMethod": ["GET", "POST"],
      "RateLimitOptions": {
        "ClientWhitelist": [],
        "EnableRateLimiting": true,
        "Period": "1s",
        "PeriodTimespan": 1,
        "Limit": 10
      }
    }
  ]
}
```

**Gateway funksiyaları:**
- Authentication/Authorization
- Rate Limiting
- Request/Response transformation
- Load balancing
- Circuit Breaker

## 7. CIRCUIT BREAKER PATTERN (Resilience)

### Polly Library
```csharp
// Service registration
services.AddHttpClient("DocumentService")
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy());

// Retry policy
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}

// Circuit Breaker
static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
}
```

## 8. DISTRIBUTED TRACING & MONITORING

### Azure Application Insights (Commercial)
```csharp
// Program.cs
builder.Services.AddApplicationInsightsTelemetry();

// Custom telemetry
public class DocumentService
{
    private readonly TelemetryClient _telemetry;
    
    public async Task UploadDocumentAsync(...)
    {
        using var operation = _telemetry.StartOperation<RequestTelemetry>("UploadDocument");
        try
        {
            // Upload logic
            _telemetry.TrackMetric("DocumentUpload.Size", fileSize);
        }
        catch (Exception ex)
        {
            _telemetry.TrackException(ex);
            throw;
        }
    }
}
```

### Health Checks (Enterprise səviyyə)
```csharp
// Deep health checks
services.AddHealthChecks()
    .AddSqlServer(connectionString, name: "sql-primary")
    .AddSqlServer(connectionStringReadReplica, name: "sql-replica")
    .AddNCache("NexusPMCache", "cache")
    .AddAzureServiceBusQueue("queue")
    .AddCheck<StorageHealthCheck>("storage");
```

## 9. FEATURE FLAGS (Azure App Configuration)

```csharp
// Yeni feature yavaş-yavaş açmaq üçün
if (await _featureManager.IsEnabledAsync("NewSearchAlgorithm"))
{
    // Yeni kod
}
else
{
    // Köhnə kod
}
```

## 10. BLUE-GREEN DEPLOYMENT

```
[Load Balancer]
     ↓
[Blue Environment] ← Live (v1.0)
[Green Environment] ← Idle (v1.1)

// Deploy:
1. Green-i v1.1 ilə update et
2. Health check et
3. Load Balancer-ı Green-ə çevir
4. Blue idle qalır (rollback üçün)
```

## 11. SAGA PATTERN (Distributed Transactions)

Sənəd upload + Task yaratma + Notification göndərmə = Atomic transaction

```csharp
public class DocumentUploadSaga : Saga<DocumentUploadSagaData>
{
    public async Task Handle(UploadDocumentCommand message)
    {
        // Step 1: Save to storage
        await Send(new SaveToStorageCommand { ... });
    }
    
    public async Task Handle(StorageSavedEvent message)
    {
        // Step 2: Create DB record
        await Send(new CreateDocumentRecordCommand { ... });
    }
    
    public async Task Handle(DocumentCreatedEvent message)
    {
        // Step 3: Send notification
        await Send(new SendNotificationCommand { ... });
        MarkAsComplete();
    }
    
    // Compensation (rollback)
    public async Task Handle(DocumentCreationFailedEvent message)
    {
        await Send(new DeleteFromStorageCommand { ... });
        await Send(new SendFailureNotificationCommand { ... });
    }
}
```

## 12. DATA SHARDING (5000+ user üçün)

```sql
-- Horizontal Partitioning (UserId əsasında)
-- Users 1-1000 → DB1
-- Users 1001-2000 → DB2
-- Users 2001-3000 → DB3

-- Application level routing
public class ShardManager
{
    public string GetConnectionString(long userId)
    {
        var shardId = (userId / 1000) + 1;
        return $"Server=db-server{shardId};...";
    }
}
```

## 13. SEARCH OPTIMIZATION

### Azure Cognitive Search (və ya Elasticsearch Commercial)
```csharp
// Full-text search (SQL Server-dəki LIKE yox)
public async Task<List<Document>> SearchAsync(string query)
{
    var results = await _searchClient.SearchAsync<Document>(query, new SearchOptions
    {
        Filter = $"OrganizationCode eq '{orgCode}'",
        OrderBy = { "CreatedAt desc" },
        IncludeTotalCount = true
    });
    
    return results.Value.GetResults().Select(r => r.Document).ToList();
}
```

## 14. FILE STORAGE STRATEGY

### Azure Blob Storage (və ya local disk + replication)
```csharp
// Tiered storage
- Hot tier: Son 30 günün faylları (SSD)
- Cool tier: 30-365 gün (HDD)
- Archive tier: 365+ gün (tape backup)

// Upload
await _blobClient.UploadAsync(stream, new BlobUploadOptions
{
    Metadata = new Dictionary<string, string>
    {
        ["DocumentNumber"] = docNumber,
        ["OrganizationCode"] = orgCode
    },
    AccessTier = AccessTier.Hot
});
```

## 15. BACKUP STRATEGY (Enterprise)

```
RPO (Recovery Point Objective): 15 dəqiqə
RTO (Recovery Time Objective): 1 saat

1. SQL Server Always On (real-time replication)
2. Transaction log backups (hər 15 dəqiqədə)
3. Full backups (gündəlik)
4. Geo-redundant storage (fərqlidata mərkəzi)
5. Point-in-time recovery (30 gün geriyə)
```

## XÜLASƏ: Enterprise Checklist

| Komponent | Mövcud | Enterprise | Effekt |
|-----------|--------|------------|---------|
| Arxitektur | Monolith | Modular + CQRS | Scale + Maintainability |
| Cache | MemoryCache | NCache (distributed) | Consistency |
| Database | Single | Always On + Sharding | Availability |
| Real-time | SQL Backplane | Redis/NCache | Performance |
| Queue | Direct call | Service Bus | Reliability |
| Monitoring | Basic logs | App Insights | Observability |
| Deployment | Manual | Blue-Green + CI/CD | Zero downtime |
| Search | SQL LIKE | Cognitive Search | Speed |

## BÜDCƏ (Təxmini)

**Lisenziyalı həllər (5000 user üçün):**
- SQL Server Enterprise: ~$15,000/core
- NCache Enterprise: ~$10,000/server
- Azure Service Bus: ~$300/ay
- Azure App Insights: ~$200/ay
- **Ümumi**: ~$50,000 ilkin + $1000/ay

**Alternativ (Open Source amma Support ilə):**
- PostgreSQL + Patroni: $0 (support üçün $5000/il)
- Redis Enterprise: $5000/il
- RabbitMQ: $0 (support üçün $3000/il)
- **Ümumi**: ~$15,000/il

**Sizin halınız (Microsoft stack, lisenziyalı):**
- SQL Server (mövcud)
- NCache (əvəz edin)
- Azure Service Bus (əlavə edin)
- **Əlavə xərc**: ~$25,000

## SON QƏRAR

Sizin kodunuz **yaxşı arxitektura** üzərindədir, amma:
1. **NCache** əlavə edin (Redis əvəzinə)
2. **CQRS** implementasiya edin
3. **Azure Service Bus** əlavə edin
4. **SQL Server Always On** quraşdırın

Bu dəyişikliklər ilə sistem **5000 user-dən 50,000 user-ə** qədər çıxa bilər.