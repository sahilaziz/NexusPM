# Öz Event Bus-unuzun İstifadəsi

## Quraşdırma

Artıq heç bir əlavə konfiqurasiya lazım deyil! Sistem avtomatik işləyir.

```csharp
// appsettings.json (default olaraq aktivdir)
{
  "Messaging": {
    "UsePrivateEventBus": true  // ✅ Hazırda aktiv
  }
}
```

## Necə İşləyir?

### 1. Event Yaratmaq

```csharp
// Yeni event yarat
public class DocumentUploadedEvent : IEvent
{
    public Guid EventId => Guid.NewGuid();
    public DateTime OccurredOn => DateTime.UtcNow;
    public string EventType => "DocumentUploaded";
    
    public long DocumentId { get; set; }
    public string DocumentName { get; set; }
    public string UploadedBy { get; set; }
}
```

### 2. Event Publish Etmək

```csharp
public class DocumentService
{
    private readonly IEventBus _eventBus;

    public async Task UploadDocumentAsync(Document doc)
    {
        // 1. Sənədi yadda saxla
        await _dbContext.SaveAsync(doc);
        
        // 2. Event-i queue-ya göndər (dərhal)
        await _eventBus.PublishAsync(new DocumentUploadedEvent
        {
            DocumentId = doc.Id,
            DocumentName = doc.Name,
            UploadedBy = doc.CreatedBy
        });
        
        // 3. İstifadəçiyə dərhal cavab ver
        // Email, bildiriş və s. arxa planda gedir
    }
}
```

### 3. Event Handler Yaratmaq

```csharp
// Handler - arxa planda işləyəcək
public class DocumentUploadedEmailHandler : IEventHandler<DocumentUploadedEvent>
{
    private readonly IEmailService _emailService;

    public async Task HandleAsync(DocumentUploadedEvent @event)
    {
        // Email göndər
        await _emailService.SendAsync(
            to: @event.UploadedBy,
            subject: "Sənəd yükləndi",
            body: $"{@event.DocumentName} uğurla yükləndi.");
    }
}

// Program.cs-də qeydiyyat
builder.Services.AddScoped<IEventHandler<DocumentUploadedEvent>, DocumentUploadedEmailHandler>();
```

### 4. Background Processor

Sistem avtomatik işləyir:
```
[Background Service] ← Her saniye queue yoxlayır
         ↓
[MessageQueues table] ← Database
         ↓
[Handler çağırılır] ← Email/Notification/Index
```

## Avantajlar

| Xüsusiyyət | Təsvir |
|------------|--------|
| **Pulsuz** | $0 əlavə xərc |
| **Şəxsi** | Verilənləriniz sizin SQL Server-də qalır |
| **Təhlükəsiz** | Xarici şəbəkəyə çıxmır |
| **Avtomatik Retry** | 3 dəfə avtomatik cəhd |
| **Dead Letter** | Uğursuz message-ləri saxlayır |
| **Priority** | Vacib event-ləri öncə işləyir |
| **Multi-tenant** | Hər təşkilatın öz queue-su |

## Monitorinq

### Queue Status SQL ilə
```sql
-- Gözləyən message-lərin sayı
SELECT QueueName, COUNT(*) as PendingCount
FROM MessageQueues
WHERE Status = 'Pending'
GROUP BY QueueName;

-- Son 1 saatda uğursuz olanlar
SELECT * FROM DeadLetterMessages
WHERE FailedAt > DATEADD(HOUR, -1, GETUTCDATE());

-- Ümumi status
SELECT * FROM vw_QueueStatus;
```

### Cleanup əməliyyatları
```sql
-- Köhnə message-ləri təmizlə (7 gündən köhnə)
EXEC sp_CleanupOldMessages @RetentionDays = 7;

-- Stuck message-ləri reset et (15 dəqiqə işlənməyən)
EXEC sp_ResetStuckMessages @TimeoutMinutes = 15;
```

## Troubleshooting

### Message işlənmirsə?
1. `MessageQueues` table-ına baxın - `Status = 'Pending'` olanlar
2. Logs yoxlayın - error varsa görünəcək
3. `DeadLetterMessages` table-ına baxın

### Queue çox dolursa?
```sql
-- Pending sayını yoxla
SELECT COUNT(*) FROM MessageQueues WHERE Status = 'Pending';

-- Əgər 10000-dən çoxdursa:
-- 1. Processor sayını artırın (server scale)
-- 2. Handler-ləri optimize edin
-- 3. Priority-based processing istifadə edin
```

## Fərqli Implementasiyaların Müqayisəsi

| Xüsusiyyət | Öz Event Bus (SQL) | Azure Service Bus | RabbitMQ |
|------------|-------------------|-------------------|----------|
| **Qiymət** | $0 | ~$30/ay | $0 |
| **Qurulum** | Asan | Asan | Çətin |
| **Maintenance** | Aşağı | Yoxdur | Yüksək |
| **Scale** | Orta | Əla | Əla |
| **Təhlükəsizlik** | Tam nəzarət | Microsoft | Özünüz |
| **Offline işləyir** | ✅ Bəli | ❌ Xeyr | ✅ Bəli |

## Nəticə

**Sizin sisteminizdə artıq:**
- ✅ Tam şəxsi Event Bus var
- ✅ Xarici dependency yoxdur
- ✅ Avtomatik retry və dead letter
- ✅ Background processor
- ✅ Database monitorinq

**Növbəti addım:** Event handler-lərinizi yazmaq! 🎉
