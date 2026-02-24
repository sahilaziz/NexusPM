# 🗺️ NEXUS PROJECT MANAGEMENT - İŞ PLANI

> **Status:** Foundation qurulub (30%)  
> **Hədəf:** Production-ready MVP (100%)  
> **Müddət:** 6-9 ay (tək developer)  
> **Başlama:** Bu gün

---

## 📍 FAZ 0: BUGÜNKÜ DURUM (Realitet)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│  ❌ PRODUKSIYA HAZIR DEYIL!                                                 │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ✅ Var olan (Foundation):                                                  │
│     • Backend API (CQRS, Auth, Document)                                   │
│     • Database schema                                                      │
│     • Infrastructure (Azure/Private switch)                                │
│                                                                             │
│  ❌ Çatışan (Core PM):                                                      │
│     • Mobile app tam deyil                                                 │
│     • Task dependencies yoxdur                                             │
│     • Time tracking yoxdur                                                 │
│     • Reporting yoxdur                                                     │
│     • Email notifications yoxdur                                           │
│     • Gantt/Calendar yoxdur                                                │
│                                                                             │
│  🎯 Nəticə: Sistem İNDI test üçün hazırdır, istifadə üçün YOX!            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 🚀 FAZ 1: CORE PM MVP (Ay 1-3)

**Hədəf:** İstifadə edilə bilən minimal məhsul

### Ay 1: Task Management Core
```
┌────────────────────────────────────────────────────────────────────┐
│  HƏDƏF: Task sistemi tam işlək                                     │
├────────────────────────────────────────────────────────────────────┤
│  Həftə 1-2: Task Dependencies & Relations                          │
│  ├── Database: TaskDependencies table yarat                        │
│  ├── Domain: TaskRelation entity (Parent, Child, Related)          │
│  ├── API: AddDependency, RemoveDependency endpoints                │
│  └── Logic: Circular dependency check (DFS algorithm)              │
│                                                                     │
│  Həftə 3: Task Labels/Tags                                          │
│  ├── Label entity (Id, Name, Color, ProjectId)                     │
│  ├── TaskLabel many-to-many                                        │
│  ├── API: CRUD for labels                                          │
│  └── Filter by label                                               │
│                                                                     │
│  Həftə 4: Task Attachments (tam)                                   │
│  ├── File upload API (chunked)                                     │
│  ├── Storage abstraction (Local/Azure)                             │
│  ├── File preview (thumbnail generation)                           │
│  └── Virus scanning (ClamAV integration)                           │
└────────────────────────────────────────────────────────────────────┘
```

### Ay 2: Time Tracking & Activity
```
┌────────────────────────────────────────────────────────────────────┐
│  HƏDƏF: Vaxt izləmə sistemi                                         │
├────────────────────────────────────────────────────────────────────┤
│  Həftə 1-2: Time Tracking Core                                      │
│  ├── TimeEntry entity (TaskId, UserId, StartTime, EndTime, Note)   │
│  ├── API: StartTimer, StopTimer, ManualEntry                       │
│  ├── Running timer state (Redis/cache)                             │
│  └── Daily/Weekly time summary                                     │
│                                                                     │
│  Həftə 3: Activity Log                                              │
│  ├── Activity entity (Who, What, When, Where)                      │
│  ├── Automatic activity tracking (Mediator pipeline)               │
│  ├── Activity feed API                                             │
│  └── Recent activity widget                                        │
│                                                                     │
│  Həftə 4: Task Comments (tam)                                      │
│  ├── Rich text comments (Markdown support)                         │
│  ├── Comment threading (reply to reply)                            │
│  ├── @mentions (@username notifications)                           │
│  └── File attachments in comments                                  │
└────────────────────────────────────────────────────────────────────┘
```

### Ay 3: Notifications & Email
```
┌────────────────────────────────────────────────────────────────────┐
│  HƏDƏF: Bildiriş sistemi tam                                       │
├────────────────────────────────────────────────────────────────────┤
│  Həftə 1-2: Email Integration                                       │
│  ├── SMTP/SendGrid configuration                                   │
│  ├── Email templates (Razor/Fluid)                                 │
│  ├── Welcome email                                                 │
│  ├── Task assigned email                                           │
│  └── Daily digest email                                            │
│                                                                     │
│  Həftə 3: Notification Preferences                                  │
│  ├── User notification settings table                              │
│  ├── Email/Website/Push preferences                                │
│  ├── Quiet hours configuration                                     │
│  └── Per-project notification settings                             │
│                                                                     │
│  Həftə 4: Notification Triggers                                     │
│  ├── Event → Notification mapping                                  │
│  ├── Template engine                                               │
│  ├── In-app notification center                                    │
│  └── Mark all as read                                              │
└────────────────────────────────────────────────────────────────────┘
```

**MVP Çıxışı:**
- ✅ Task management (dependencies, labels, time)
- ✅ Email notifications
- ✅ Activity tracking
- ⚠️ Web UI (basic)
- ❌ Mobile hələ yoxdur

---

## 📱 FAZ 2: MOBILE + UX (Ay 4-5)

**Hədəf:** Mobile app tam işlək

### Ay 4: Flutter Core
```
┌────────────────────────────────────────────────────────────────────┐
│  HƏDƏF: Mobile app MVP                                             │
├────────────────────────────────────────────────────────────────────┤
│  Həftə 1-2: Authentication & Navigation                            │
│  ├── Login screen (Local + AD)                                     │
│  ├── 2FA screen                                                    │
│  ├── Bottom navigation (Projects, Tasks, Docs, Profile)            │
│  ├── State management (Riverpod)                                   │
│  └── Offline storage (Hive/SQLite)                                 │
│                                                                     │
│  Həftə 3: Project & Task Lists                                     │
│  ├── Project list with search                                      │
│  ├── Task list with filters (status, priority, assignee)           │
│  ├── Pull to refresh                                               │
│  └── Infinite scroll (pagination)                                  │
│                                                                     │
│  Həftə 4: Task Detail & CRUD                                       │
│  ├── Task detail view                                              │
│  ├── Create task screen                                            │
│  ├── Edit task (title, description, dates)                         │
│  ├── Assign user                                                   │
│  └── Change status (drag or buttons)                               │
└────────────────────────────────────────────────────────────────────┘
```

### Ay 5: Mobile Features + Polish
```
┌────────────────────────────────────────────────────────────────────┐
│  HƏDƏF: Mobile app production-ready                                │
├────────────────────────────────────────────────────────────────────┤
│  Həftə 1-2: Document Access                                        │
│  ├── Folder tree navigation                                        │
│  ├── File list with thumbnails                                     │
│  ├── File download & offline access                                │
│  ├── Document viewer (PDF, images)                                 │
│  └── Share functionality                                           │
│                                                                     │
│  Həftə 3: Notifications & Real-time                                │
│  ├── Push notifications (Firebase)                                 │
│  ├── Notification list                                             │
│  ├── SignalR connection                                            │
│  └── Real-time task updates                                        │
│                                                                     │
│  Həftə 4: Polish & Optimization                                    │
│  ├── Error handling & retry logic                                  │
│  ├── Loading states & skeletons                                    │
│  ├── Animations (smooth transitions)                               │
│  ├── Dark mode support                                             │
│  └── Performance optimization                                      │
└────────────────────────────────────────────────────────────────────┘
```

**Mobile Çıxışı:**
- ✅ Full task management
- ✅ Document access
- ✅ Push notifications
- ✅ Offline support

---

## 📊 FAZ 3: REPORTING & VIEWS (Ay 6-7)

**Hədəf:** Vizual idarəetmə və hesabatlar

### Ay 6: Visualization
```
┌────────────────────────────────────────────────────────────────────┐
│  HƏDƏF: Görünüş müxtəlifliyi                                       │
├────────────────────────────────────────────────────────────────────┤
│  Həftə 1-2: Kanban Board                                           │
│  ├── Column view (Todo, InProgress, Done)                          │
│  ├── Drag & drop (tasks between columns)                           │
│  ├── Swimlanes (by assignee or priority)                           │
│  ├── Quick add task                                                │
│  └── Bulk operations                                               │
│                                                                     │
│  Həftə 3: Calendar View                                            │
│  ├── Month/Week/Day views                                          │
│  ├── Tasks with due dates                                          │
│  ├── Drag to reschedule                                            │
│  └── Color coding (by project/priority)                            │
│                                                                     │
│  Həftə 4: Gantt Chart (basic)                                      │
│  ├── Timeline view                                                 │
│  ├── Task bars with duration                                       │
│  ├── Dependency lines (arrows)                                     │
│  ├── Zoom (day/week/month)                                         │
│  └── Critical path highlighting                                    │
└────────────────────────────────────────────────────────────────────┘
```

### Ay 7: Reporting
```
┌────────────────────────────────────────────────────────────────────┐
│  HƏDƏF: Hesabat sistemi                                            │
├────────────────────────────────────────────────────────────────────┤
│  Həftə 1-2: Project Dashboard                                      │
│  ├── Project health (on track, at risk, delayed)                   │
│  ├── Task completion rate                                          │
│  ├── Overdue tasks count                                           │
│  ├── Team workload chart                                           │
│  └── Recent activity feed                                          │
│                                                                     │
│  Həftə 3: Time Reports                                               │
│  ├── Personal timesheet                                            │
│  ├── Project time summary                                          │
│  ├── Time by task/category                                         │
│  ├── Export to Excel/PDF                                           │
│  └── Billable hours calculation                                    │
│                                                                     │
│  Həftə 4: Custom Reports                                             │
│  ├── Report builder UI                                             │
│  ├── Filter combinations                                           │
│  ├── Chart types (bar, pie, line)                                  │
│  ├── Save & schedule reports                                       │
│  └── Email report delivery                                         │
└────────────────────────────────────────────────────────────────────┘
```

**Reporting Çıxışı:**
- ✅ Multiple views (List, Kanban, Calendar, Gantt)
- ✅ Project dashboards
- ✅ Time reports
- ✅ Custom report builder

---

## 🏢 FAZ 4: ENTERPRISE (Ay 8-9)

**Hədəf:** Böyük şirkətlər üçün hazır

### Ay 8: Resource & Financial
```
┌────────────────────────────────────────────────────────────────────┐
│  HƏDƏF: Resurs və maliyyə idarəetmə                                │
├────────────────────────────────────────────────────────────────────┤
│  Həftə 1-2: Resource Management                                    │
│  ├── Team management                                               │
│  ├── Resource allocation (who works on what)                       │
│  ├── Workload balancing                                            │
│  ├── Capacity planning                                             │
│  └── Vacation/leave tracking                                       │
│                                                                     │
│  Həftə 3-4: Budget & Finance                                       │
│  ├── Project budget setting                                        │
│  ├── Cost tracking (hourly rates)                                  │
│  ├── Expense logging                                               │
│  ├── Budget vs actual reporting                                    │
│  └── Cost forecasting                                              │
└────────────────────────────────────────────────────────────────────┘
```

### Ay 9: Advanced Features
```
┌────────────────────────────────────────────────────────────────────┐
│  HƏDƏF: Enterprise-grade features                                  │
├────────────────────────────────────────────────────────────────────┤
│  Həftə 1: Risk Management                                          │
│  ├── Risk register                                                 │
│  ├── Risk assessment (probability x impact)                        │
│  ├── Mitigation plans                                              │
│  └── Risk reports                                                  │
│                                                                     │
│  Həftə 2: Change Management                                        │
│  ├── Change request workflow                                       │
│  ├── Approval process                                              │
│  ├── Impact analysis                                               │
│  └── Change log                                                    │
│                                                                     │
│  Həftə 3: Integrations                                             │
│  ├── Microsoft 365 (Teams, Outlook)                                │
│  ├── Email integration (task from email)                           │
│  ├── Calendar sync                                                 │
│  └── Webhook API                                                   │
│                                                                     │
│  Həftə 4: Security & Compliance                                    │
│  ├── Data retention policies                                       │
│  ├── GDPR compliance (data export/delete)                          │
│  ├── Advanced audit logs                                           │
│  ├── IP restrictions                                               │
│  └── SAML/SSO support                                              │
└────────────────────────────────────────────────────────────────────┘
```

**Enterprise Çıxışı:**
- ✅ Resource management
- ✅ Budget tracking
- ✅ Risk management
- ✅ Integrations
- ✅ Compliance

---

## 📅 HƏFTƏLİK İCRA PLANI

### Bu Həftə (Başlama)
```
┌────────────────────────────────────────────────────────────────────┐
│  HƏFTƏ 1: Task Dependencies başlanğıcı                              │
├────────────────────────────────────────────────────────────────────┤
│  Gün 1-2: Database design                                          │
│  └── TaskDependency table                                          │
│      • Id, TaskId, DependsOnTaskId                                 │
│      • DependencyType (FinishToStart, StartToStart, etc.)          │
│      • CreatedAt                                                   │
│                                                                     │
│  Gün 3: Domain layer                                               │
│  ├── TaskDependency entity                                         │
│  ├── Validation rules                                              │
│  └── Circular dependency detection                                 │
│                                                                     │
│  Gün 4: Application layer                                          │
│  ├── Commands: AddDependency, RemoveDependency                     │
│  ├── Queries: GetTaskDependencies, GetBlockedTasks                 │
│  └── Event: DependencyAddedEvent                                   │
│                                                                     │
│  Gün 5: API layer                                                  │
│  └── Controller endpoints                                          │
│      • POST /api/tasks/{id}/dependencies                           │
│      • DELETE /api/tasks/{id}/dependencies/{depId}                 │
│      • GET /api/tasks/{id}/dependencies                            │
│                                                                     │
└────────────────────────────────────────────────────────────────────┘
```

### Növbəti 4 Həftə (Ay 1 Detalları)
[Yuxarıda Ay 1 bölməsində verilib]

---

## 🎯 MƏHSUL YOL XƏRITƏSİ

```
Ay:  1    2    3    4    5    6    7    8    9
     |----|----|    |----|    |----|----|----|
     
     [====FAZ 1====]
     Core PM MVP
     • Dependencies ✓
     • Time Tracking ✓
     • Notifications ✓
          |
          v
          [====FAZ 2====]
          Mobile App
          • Flutter ✓
          • Offline ✓
          • Push ✓
               |
               v
               [====FAZ 3====]
               Reporting
               • Gantt ✓
               • Dashboard ✓
               • Reports ✓
                    |
                    v
                    [====FAZ 4====]
                    Enterprise
                    • Resource ✓
                    • Budget ✓
                    • Risk ✓

LEGEND:
[====] Active development
✓    Deliverable complete
```

---

## ✅ BAŞLAMAQ ÜÇÜN CHECKLIST

### Bugün ediləcək:
```
□ 1. TaskDependency database table yarat
□ 2. TaskDependency entity yaz
□ 3. AddDependencyCommand yaz
□ 4. Circular dependency check algorithm yaz
□ 5. API controller yarat
□ 6. Postman collection update et
□ 7. Git commit: "feat: task dependencies core"
```

### Bu həftə sonuna qədər:
```
□ Task dependencies tam işlək
□ Unit tests yaz
□ Integration test yaz
□ Documentation update
```

---

## ⚠️ RİSKLƏR VƏ PLAN B

| Risk | Ehtimal | Təsir | Plan B |
|------|---------|-------|--------|
| Vaxt çatışmazlığı | Orta | Yüksək | Scope azalt (Gantt Phase 3-dən Phase 4-ə keçir) |
| Texniki çətinlik | Aşağı | Orta | Simplified versiya (məs: dependency yalnız FinishToStart) |
| Mobile delay | Orta | Yüksək | PWA (web app) ilə başlayaq |
| Performance issue | Aşağı | Yüksək | Caching artır, read replicas istifadə et |

---

## 🎯 UĞUR MEYARLARI (KPI)

### Faz 1 sonu (Ay 3):
- [ ] Task with dependencies create/edit/delete
- [ ] Time tracking start/stop/report
- [ ] Email notifications working
- [ ] 100 tasks without performance issue

### Faz 2 sonu (Ay 5):
- [ ] Mobile app published (TestFlight/Internal)
- [ ] Offline mode working
- [ ] Push notifications received
- [ ] 50 beta users testing

### Faz 4 sonu (Ay 9):
- [ ] Production deployment ready
- [ ] Security audit passed
- [ ] Documentation complete
- [ ] First paying customer

---

**BAŞLAYAQMI? 🚀**

İlk iş: `TaskDependency` table yaratmaq. Başlayaq?
