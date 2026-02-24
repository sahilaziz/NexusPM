# 🚀 Build & Test Təlimatı

## 📋 Mündəricat
- [GitHub Actions ilə Avtomatik Build](#github-actions-ilə-avtomatik-build)
- [Lokal Build](#lokal-build)
- [Test Strukturu](#test-strukturu)
- [Xətralara Baxış](#xətalara-baxış)

---

## GitHub Actions ilə Avtomatik Build

### 🔄 Workflow İşlədiyində

Hər `push` və `pull_request`-də avtomatik işləyir:

```yaml
✅ Backend Build & Test (.NET 9)
✅ Docker Image Build
✅ Code Quality Checks
```

### 📊 Workflow Status

| Job | Təsvir |
|-----|--------|
| **Backend** | .NET 9 restore, build, unit & integration tests |
| **Docker** | Dockerfile build yoxlaması |
| **Code Quality** | Formatting checks |

---

## Lokal Build

### Tələblər
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/products/docker-desktop) (opsional)

### 🏗️ Addımlar

#### 1. Dependencies Restore
```bash
cd Backend
dotnet restore
```

#### 2. Build
```bash
dotnet build --no-restore --configuration Release
```

#### 3. Unit Tests
```bash
cd tests/Nexus.UnitTests
dotnet test --verbosity normal
```

#### 4. Integration Tests
```bash
cd tests/Nexus.IntegrationTests
dotnet test --verbosity normal
```

#### 5. Docker Build (Opsional)
```bash
cd Backend
docker build -t nexus-pm:latest .
```

---

## Test Strukturu

### 📁 Test Layihələri

```
Backend/tests/
├── Nexus.UnitTests/              # Unit testlər
│   ├── Commands/
│   │   └── AddTaskDependencyCommandTests.cs    # 8 test
│   └── Repositories/
│       └── TaskDependencyRepositoryTests.cs    # 22 test
│
└── Nexus.IntegrationTests/       # Integration testlər
    └── Controllers/
        └── ProjectsControllerTests.cs
```

### 🧪 Test Sayı

| Layihə | Test Sayı | Status |
|--------|-----------|--------|
| Nexus.UnitTests | 22+ | ✅ Aktiv |
| Nexus.IntegrationTests | 3+ | ✅ Aktiv |
| **Ümumi** | **25+** | ✅ |

### 📝 Unit Test Nümunələri

```csharp
[Fact]
public async Task Handle_ValidDependency_AddsAndReturnsSuccess()
{
    // Arrange
    var command = new AddTaskDependencyCommand(
        TaskId: 2,
        DependsOnTaskId: 1,
        Type: DependencyType.FinishToStart
    );

    // Act
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert
    result.ShouldNotBeNull();
    result.IsValid.ShouldBeTrue();
}
```

### 🔍 Əsas Test Kategoriyaları

#### Task Dependencies (Asılılıqlar)
- ✅ Valid dependency əlavə etmə
- ❌ Self-dependency (özünə asılılıq) bloklanması
- ❌ Fərqli layihələrdəki tapşırıqlara asılılıq bloklanması
- ❌ Dairəvi asılılıq (circular dependency) aşkarlanması
- ❌ Duplicate asılılıq bloklanması
- ⚠️ Tamamlanmamış asılılıq xəbərdarlığı
- ✅ Bütün 4 asılılıq tipi (FS, SS, FF, SF)

#### Repository Tests
- ✅ Get by ID
- ✅ Get dependencies/dependents
- ✅ Exists check
- ✅ Circular dependency detection (DFS alqoritmi)
- ✅ IsBlocked logic
- ✅ CanStart logic
- ✅ Add/Delete operations

---

## Xətalara Baxış

### Build Xətaları

#### "error CS0246: The type or namespace name 'X' could not be found"
**Səbəb:** Dependency əskikdir
**Həll:**
```bash
dotnet restore
```

#### "error NU1101: Unable to find package"
**Səbəb:** NuGet package tapılmadı
**Həll:**
```bash
dotnet nuget locals all --clear
dotnet restore
```

### Test Xətaları

#### Testlər işləmirsə
```bash
# Detallı log
dotnet test --verbosity diagnostic

# Specific test filter ilə
dotnet test --filter "FullyQualifiedName~AddTaskDependency"
```

---

## 🎯 CI/CD Pipeline

```
Push/PR
    │
    ▼
┌─────────────────┐
│  Restore        │
│  Packages       │
└────────┬────────┘
         │
    ▼
┌─────────────────┐
│  Build          │
│  (Release)      │
└────────┬────────┘
         │
    ▼
┌─────────────────┐     ┌─────────────────┐
│  Unit Tests     │────▶│  ✅ 22 tests    │
└────────┬────────┘     └─────────────────┘
         │
    ▼
┌─────────────────┐     ┌─────────────────┐
│  Integration    │────▶│  ✅ 3+ tests    │
│  Tests          │     └─────────────────┘
└────────┬────────┘
         │
    ▼
┌─────────────────┐
│  Docker Build   │
│  Check          │
└────────┬────────┘
         │
    ▼
   ✅ Done!
```

---

## 📞 Dəstək

Problemlər varsa:
1. GitHub Actions logs-ı yoxlayın
2. Lokalda `dotnet test --verbosity diagnostic` işlədin
3. Dockerfile-ı yoxlayın: `docker build --no-cache -t nexus-pm:test .`
