# Messaging Sistemləri - Azure Service Bus vs Alternativlər

## 1. Event Bus (Message Queue) Nədir?

### Real Həyat Nümunəsi
```
📝 Sənəd Yükləndi
    ↓
📧 Email Göndər (5 saniyə)
🔔 Bildiriş Göndər (2 saniyə)
🔍 Axtarış İndeksini Yenilə (10 saniyə)
📊 Audit Log Yaz (1 saniyə)
    ↓
✅ Cəmi: 18 saniyə GÖZLƏMƏ
```

**PROBLEM:** İstifadəçi 18 saniyə gözləməli olur!

### Event Bus ilə (Asinxron)
```
📝 Sənəd Yükləndi
    ↓
📮 Event Bus-a Göndər (100ms)
    ↓
✅ İstifadəçiyə Cavab: "Yükləndi!"
    ↓
📧 Email, 🔔 Bildiriş, 🔍 İndeks, 📊 Log 
    (Arxa planda işləyir, istifadəçi gözləmir)
```

**FAYDA:** İstifadəçi 100ms-də cavab alır, digər işlər arxa planda!

---

## 2. Azure Service Bus Nədir?

**Tərif:** Microsoft-un cloud-based message queue xidmətidir.

**Lazım olan səbəblər:**

### A. Guaranteed Delivery (Təminatlı Çatdırılma)
```
1. Sənəd yükləndi
2. Event Bus-a göndərildi
3. Server çökdü 😱
4. Server yenidən başladı
5. Event avtomatik yenidən işləndi ✅

Əgər Event Bus olmasaydı:
4. Bildiriş heç vaxt göndərilməyəcəkdi ❌
```

### B. Decoupling (Bağımsızlıq)
```
Sənəd Servisi ← → Bildiriş Servisi
      ↓
   Event Bus
      ↓
Email Servisi ← → SMS Servisi ← → Push Servisi

Əlaqə yoxdur! Hər biri müstəqil işləyir.
```

### C. Load Leveling (Yük Bölünməsi)
```
Ani yüklənmə: 1000 bildiriş/saniyə
    ↓
Event Bus queue-da saxlayır
    ↓
Bildiriş servisi yavaş-yavaş işləyir: 100/saniyə
    ↓
Sistem çökmür! ✅
```

### D. Retry Policy (Avtomatik Yenidən Cəhd)
```
1. Email servisi çökdü
2. Event Bus 5 dəfə avtomatik cəhd edir
3. 1 saat sonra yenə çəhd edir
4. Uğursuz olarsa "Dead Letter Queue"-ya atılır
5. Admin baxıb manual işləyə bilər
```

---

## 3. Azure Service Bus Ödənişləri

### Qiymətlər (2024)

| Tier | Qiymət | Xüsusiyyətlər |
|------|--------|---------------|
| **Basic** | ~$10/ay | 13M messages/ay, Queue sadəcə |
| **Standard** | ~$10 + $0.015/million | Topics, Sessions, Transactions |
| **Premium** | ~$700/ay | Dedicated resources, 1M+ msg/s |

### Sizin üçün nə lazımdır?

**5000 user üçün:**
- Günlük ~50,000 event
- Aylıq ~1.5M event
- **Standard Tier**: ~$10 + $22 = **$32/ay**

---

## 4. AZURE-SİZ ALTERNATİVLƏR (Pulsuz/Lisenziyalı)

### A. RabbitMQ (Open Source - Pulsuz)
```csharp
// Implementasiya
public class RabbitMQEventBus : IEventBus
{
    // Öz serverinizdə qurursunuz
    // Windows/Linux dəstəyi var
}
```

**Üstünlükləri:**
- ✅ Tamamilə pulsuz
- ✅ Windows-da işləyir
- ✅ Çox güclü
- ✅ 1 milyon+ msg/saniyə

**Mənfi tərəfləri:**
- ❌ Özünüz qurmalısınız
- ❌ Maintenance sizdədir
- ❌ Backup/HA siz qurmalısınız

### B. SQL Server Service Broker (Pulsuz - Sizin stack)
```csharp
// SQL Server-in özündə var!
// Əlavə heç nə quraşdırmaq lazım deyil

// Database-də enable et:
// ALTER DATABASE NexusPM SET ENABLE_BROKER;
```

**Üstünlükləri:**
- ✅ SQL Server ilə gəlir (pulsuz)
- ✅ Windows-native
- ✅ Transaction dəstəyi
- ✅ Sizin büdcənizə uyğun

**Mənfi tərəfləri:**
- ❌ Daha yavaş (Azure SB ilə müqayisədə)
- ❌ Complex configuration
- ❌ Limited features

### C. MSMQ (Microsoft Message Queue - Pulsuz)
```csharp
// Windows-un özündə var
// .NET 9 ilə işləyir
```

**Üstünlükləri:**
- ✅ Windows-un hissəsidir
- ✅ Çox sürətli
- ✅ Transaction dəstəyi

**Mənfi tərəfləri:**
- ❌ Cloud-da işləmir (on-premise only)
- ❌ Scale çətindir
- ❌ Legacy texnologiya

---

## 5. TÖVSİYƏ (Sizin üçün)

### Mərhələ 1: İndi (Development)
```csharp
// InMemoryEventBus istifadə edin (hazırdır)
// Pulsuzdur, test üçün idealdır
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();
```

### Mərhələ 2: Production (Aşağı büdcə)
```csharp
// SQL Server Service Broker
// və ya RabbitMQ öz serverinizdə
// ~$0 əlavə xərc
```

### Mərhələ 3: Scale (Gələcəkdə)
```csharp
// Azure Service Bus
// Yalnız scale etməyə başlayanda
// ~$30-50/ay
```

---

## 6. İNDİ NƏ ETMƏLİSİNİZ?

### Variant 1: Pulsuz (Tövsiyə olunur)
```csharp
// Program.cs
// Azure Service Bus YOX, InMemory istifadə edin
builder.Services.AddSingleton<IEventBus, InMemoryEventBus>();

// Gələcəkdə dəyişmək asandır:
// builder.Services.AddSingleton<IEventBus, AzureServiceBus>();
```

### Variant 2: SQL Server Service Broker
```sql
-- Database-də enable et
ALTER DATABASE NexusPM SET ENABLE_BROKER;

-- Queue yarad
CREATE QUEUE DocumentEventQueue;
CREATE SERVICE DocumentEventService ON QUEUE DocumentEventQueue;
```

---

## XÜLASƏ

| Variant | Qiymət | Maintenance | Scale | Tövsiyə |
|---------|--------|-------------|-------|---------|
| **InMemory** | $0 | Asan | Yalnız 1 server | ✅ İndi üçün |
| **SQL Service Broker** | $0 | Orta | Orta | ✅ Production (az büdcə) |
| **RabbitMQ** | $0 | Çətin | Yaxşı | ⚠️ Əgər DevOps komandanız varsa |
| **Azure Service Bus** | $30/ay | Yoxdur | Əla | ⚠️ Gələcəkdə |

**Son qərar:** İndi `InMemoryEventBus` istifadə edin, gələcəkdə Azure Service Bus-a keçin. Kod hazırdır, yalnız DI dəyişmək lazımdır.