# Monitoring Sistemləri - Application Insights vs Alternativlər

## Application Insights Nədir?

**Tərif:** Azure-un tətbiq izləmə (application monitoring) xidmətidir.

### Nə İzləyir?

```
┌─────────────────────────────────────────────────────────┐
│                    Application Insights                  │
├─────────────────────────────────────────────────────────┤
│  📊 Request-lər: Response time, status code              │
│  🐛 Xətalar: Exception-lar, stack trace                  │
│  🗄️ Database: SQL query vaxtları                        │
│  🔌 External API: Çağırış vaxtları                       │
│  👥 Users: Neçə nəfər aktivdir                           │
│  💾 Performance: CPU, Memory istifadəsi                  │
└─────────────────────────────────────────────────────────┘
```

### Real Nümunə

```csharp
// Sizin kodunuz
public async Task GetDocument(int id)
{
    // Application Insights avtomatik bunları izləyir:
    // 1. Request başladı: GET /api/documents/123
    // 2. Database sorğusu: 50ms
    // 3. Xəta baş verdi (əgər varsa)
    // 4. Request bitdi: 200 OK, 200ms
}
```

**Nəticə:** Dashboard-da görürsünüz:
- "5000 user sistəmdədir"
- "Ortaq response time: 150ms"
- "Son 1 saatda 3 xəta baş verib"

---

## Qiymət Niyə Belədir?

### Application Insights Qiymət Strukturu

| Metrik | Qiymət | Sizin Halınız |
|--------|--------|---------------|
| **Data ingestion** | $2.40/GB | ~100GB/ay = **$240/ay** |
| **Data retention** | $0.12/GB/ay | 90 gün saxlama |
| **Live metrics** | Pulsuz | Real-time izləmə |
| **Alerts** | Pulsuz | Email/SMS bildirişlər |

**Sizin hesabladığınız:** $2,400 = **1 il üçün** ($240 × 12 ay) və ya çox yüklənmə halı.

---

## PULSUZ ALTERNATİVLƏR (Tövsiyə Olunur)

### Variant 1: SQL Server + Custom Dashboard (Pulsuz)

```csharp
// Öz audit log sisteminiz (hazırdır)
public class PerformanceLogger
{
    private readonly AppDbContext _db;

    public async Task LogRequestAsync(string endpoint, long durationMs, bool success)
    {
        _db.PerformanceLogs.Add(new PerformanceLog
        {
            Endpoint = endpoint,
            DurationMs = durationMs,
            Success = success,
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
```

**Grafana ilə Dashboard:**
```sql
-- Orta response time
SELECT Endpoint, AVG(DurationMs) as AvgTime
FROM PerformanceLogs
WHERE Timestamp > DATEADD(HOUR, -1, GETUTCDATE())
GROUP BY Endpoint;

-- Xətalar
SELECT COUNT(*) as ErrorCount
FROM PerformanceLogs
WHERE Success = 0 
  AND Timestamp > DATEADD(HOUR, -1, GETUTCDATE());
```

**Üstünlükləri:**
- ✅ Tamamilə pulsuz
- ✅ Verilənlər sizin SQL Server-də
- ✅ İstədiyiniz query yazarsınız
- ❌ Real-time deyil (1-5 dəqiqə gecikmə)

---

### Variant 2: Serilog + Seq (Aşağı büdcə)

```csharp
// NuGet: Serilog, Serilog.Sinks.Seq

// Program.cs
Log.Logger = new LoggerConfiguration()
    .WriteTo.Seq("http://localhost:5341") // Öz serverinizdə
    .WriteTo.SQLite("logs.db") // Və ya SQLite
    .CreateLogger();

// İstifadə
Log.Information("Request {Endpoint} completed in {Duration}ms", 
    endpoint, duration);
```

**Qiymət:** 
- Seq Single Server: $0 (development)
- Seq Enterprise: $1,500/il (production)
- **Yəni $0 ilə başlaya bilərsiniz!**

---

### Variant 3: Prometheus + Grafana (Tam Pulsuz)

```csharp
// NuGet: prometheus-net

// Metrics
public static class AppMetrics
{
    public static readonly Counter RequestCount = Metrics
        .CreateCounter("nexus_requests_total", "Total requests");
    
    public static readonly Histogram RequestDuration = Metrics
        .CreateHistogram("nexus_request_duration_seconds", "Request duration");
}

// İstifadə
AppMetrics.RequestCount.Inc();
using (AppMetrics.RequestDuration.NewTimer())
{
    await ProcessRequestAsync();
}
```

**Üstünlükləri:**
- ✅ Tamamilə pulsuz
- ✅ Industry standard
- ✅ Real-time monitoring
- ✅ Alerting var
- ❌ Qurulum çətin (Docker lazımdır)

---

## TÖVSİYƏ (Sizin üçün)

### İndi (Development + İlk Production)
```csharp
// Pulsuz variant - SQL Server ilə
// Hazırda sisteminizdə var:
// - Health Checks (/health endpoint)
// - Logs (appsettings.json configured)
// - Performance tracking (CQRS ilə)

// Əlavə edin: Basit Performance Middleware
app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    logger.LogInformation("Request {Method} {Path} started", 
        context.Request.Method, 
        context.Request.Path);
    
    await next();
    
    stopwatch.Stop();
    logger.LogInformation("Request {Method} {Path} completed in {Duration}ms - {StatusCode}",
        context.Request.Method,
        context.Request.Path,
        stopwatch.ElapsedMilliseconds,
        context.Response.StatusCode);
});
```

**Xərc:** $0

---

### Gələcəkdə (Scale edəndə)

| Mərhələ | User Sayı | Tövsiyə | Xərc |
|---------|-----------|---------|------|
| **İndi** | < 1000 | SQL Logs + Health Checks | $0 |
| **Mərhələ 2** | 1000-5000 | Seq Single Server | $0 |
| **Mərhələ 3** | 5000+ | Seq Enterprise və ya App Insights | $1,500-2,400/il |

---

## İNDİ NƏ ETMƏLİ?

### Step 1: Pulsuz Monitoring Aktiv Et

```csharp
// Program.cs - Əlavə edin
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database")
    .AddCheck<DiskSpaceHealthCheck>("disk");

// Request logging middleware
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        await next();
        stopwatch.Stop();
        
        logger.LogInformation(
            "Request {Method} {Path} completed in {Duration}ms - Status {StatusCode}",
            context.Request.Method,
            context.Request.Path,
            stopwatch.ElapsedMilliseconds,
            context.Response.StatusCode);
    }
    catch (Exception ex)
    {
        stopwatch.Stop();
        logger.LogError(ex,
            "Request {Method} {Path} failed after {Duration}ms",
            context.Request.Method,
            context.Request.Path,
            stopwatch.ElapsedMilliseconds);
        throw;
    }
});
```

**Xərc:** $0 ✅

### Step 2: Basit Dashboard Yarat

```sql
-- Günlük report üçün view yaradın
CREATE VIEW vw_DailyStats AS
SELECT 
    CAST(Timestamp AS DATE) as Date,
    COUNT(*) as TotalRequests,
    AVG(DurationMs) as AvgResponseTime,
    SUM(CASE WHEN Success = 0 THEN 1 ELSE 0 END) as ErrorCount
FROM PerformanceLogs
GROUP BY CAST(Timestamp AS DATE);
```

**Xərc:** $0 ✅

---

## XÜLASƏ

| Monitoring Tipi | Qiymət | Sizin Üçün? |
|----------------|--------|-------------|
| **Application Insights** | $2,400/il | ❌ İndi lazım deyil |
| **SQL Server Logs** | $0 | ✅ İndi istifadə edin |
| **Seq** | $0-1,500/il | ⚠️ Gələcəkdə baxın |
| **Prometheus+Grafana** | $0 | ⚠️ DevOps komandası varsa |

**Nəticə:** İndi pulsuz variantlardan istifadə edin, pul xərcləməyin! 🎉
