#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Nexus PM - Avtomatik GitHub Push
.DESCRIPTION
    Bütün faylları GitHub-a avtomatik göndərir
#>

$ErrorActionPreference = "Stop"

Write-Host @"
╔══════════════════════════════════════════════════════════════════╗
║                                                                  ║
║              NEXUS PM - GITHUB AUTO PUSH                         ║
║                                                                  ║
╚══════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Cyan

$projectPath = "C:\Users\Sahil\Desktop\Proqrams\PM\Nexus.ProjectManagement"
$repoUrl = "https://github.com/sahilaziz/NexusPM.git"

Set-Location $projectPath

# Git yoxlama
Write-Host "`n[1/8] Git yoxlanılır..." -ForegroundColor Yellow
try {
    $gitVersion = git --version 2>$null
    Write-Host "   ✓ Git tapıldı: $gitVersion" -ForegroundColor Green
} catch {
    Write-Host "   ✗ Git quraşdırılmayıb!" -ForegroundColor Red
    Write-Host "   https://git-scm.com/download/win yükləyin" -ForegroundColor Yellow
    Read-Host "Çıxmaq üçün Enter"
    exit 1
}

# Git init
Write-Host "`n[2/8] Git initialize edilir..." -ForegroundColor Yellow
if (-not (Test-Path ".git")) {
    git init | Out-Null
    Write-Host "   ✓ Git initialize edildi" -ForegroundColor Green
} else {
    Write-Host "   ✓ Artıq initialize edilib" -ForegroundColor Green
}

# .gitignore yarat
Write-Host "`n[3/8] .gitignore yoxlanılır..." -ForegroundColor Yellow
$gitignoreContent = @"
# .NET
bin/
obj/
*.dll
*.exe
*.pdb
*.user
*.suo
.vs/

# Flutter
mobile/.dart_tool/
mobile/.packages
mobile/build/
mobile/.flutter-plugins
mobile/.flutter-plugins-dependencies

# IDE
.idea/
.vscode/
*.swp
*.swo

# OS
.DS_Store
Thumbs.db

# Secrets
appsettings.Development.json
appsettings.Local.json
*.key
*.pfx
"@

if (-not (Test-Path ".gitignore")) {
    $gitignoreContent | Out-File -FilePath ".gitignore" -Encoding UTF8
    Write-Host "   ✓ .gitignore yaradıldı" -ForegroundColor Green
} else {
    Write-Host "   ✓ .gitignore artıq var" -ForegroundColor Green
}

# Remote əlavə et
Write-Host "`n[4/8] GitHub remote əlavə edilir..." -ForegroundColor Yellow
git remote remove origin 2>$null
git remote add origin $repoUrl 2>$null
Write-Host "   ✓ Remote əlavə edildi: $repoUrl" -ForegroundColor Green

# Faylları əlavə et
Write-Host "`n[5/8] Bütün fayllar əlavə edilir..." -ForegroundColor Yellow
$files = git status --porcelain 2>$null | Measure-Object | Select-Object -ExpandProperty Count
if ($files -eq 0) {
    Write-Host "   ⚠ Bütün fayllar artıq track edilib" -ForegroundColor Yellow
} else {
    git add . | Out-Null
    Write-Host "   ✓ $files fayl əlavə edildi" -ForegroundColor Green
}

# Commit
Write-Host "`n[6/8] Commit edilir..." -ForegroundColor Yellow
$hasChanges = git status --porcelain 2>$null
if ($hasChanges) {
    git commit -m "🚀 Nexus PM v1.0.0 - Initial Release

✅ Backend API (55+ endpoints)
   - CQRS + Clean Architecture
   - Authentication (JWT + AD)
   - Task Dependencies, Labels, Time Tracking
   - Gantt, Kanban, Dashboard

✅ Mobile App (Flutter)
   - 8 screens with Riverpod
   - Real-time timer
   - Offline support ready

✅ Infrastructure
   - GitHub Actions CI/CD
   - Docker support
   - Hybrid messaging/monitoring

✅ Documentation
   - Complete API docs
   - Deployment guide
   - Architecture diagrams" | Out-Null
    Write-Host "   ✓ Commit yaradıldı" -ForegroundColor Green
} else {
    Write-Host "   ⚠ Dəyişiklik yoxdur" -ForegroundColor Yellow
}

# Branch
Write-Host "`n[7/8] Branch yoxlanılır..." -ForegroundColor Yellow
git branch -M main 2>$null
Write-Host "   ✓ Branch: main" -ForegroundColor Green

# Push
Write-Host "`n[8/8] GitHub-a göndərilir..." -ForegroundColor Yellow
Write-Host "   ⚠ Sizdən username və password (token) istəyəcək..." -ForegroundColor Cyan
Write-Host "   💡 Token yaratmaq: https://github.com/settings/tokens" -ForegroundColor Cyan
Write-Host ""

try {
    git push -u origin main 2>&1 | ForEach-Object {
        if ($_ -match "error|fatal") {
            Write-Host "   ✗ $_" -ForegroundColor Red
        } elseif ($_ -match "Enumerating|Counting|Compressing|Writing|Resolving|Branch") {
            Write-Host "   ⏳ $_" -ForegroundColor Gray
        } else {
            Write-Host "   $_" -ForegroundColor White
        }
    }
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "`n" -NoNewline
        Write-Host @"
╔══════════════════════════════════════════════════════════════════╗
║                                                                  ║
║              ✅ UĞURLU! BÜTÜN FAYLLAR GÖNDƏRİLDİ!               ║
║                                                                  ║
║   Link: https://github.com/sahilaziz/NexusPM                     ║
║                                                                  ║
║   Yoxlamaq üçün:                                                 ║
║   1. Browser-də açın                                             ║
║   2. Actions tab-ına baxın                                       ║
║   3. Yaşıl ✅ gözləyin                                           ║
║                                                                  ║
╚══════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Green
    } else {
        throw "Push failed"
    }
} catch {
    Write-Host "`n" -NoNewline
    Write-Host @"
╔══════════════════════════════════════════════════════════════════╗
║                                                                  ║
║              ❌ XƏTA BAŞ VERDİ                                    ║
║                                                                  ║
║   Mümkün səbəblər:                                               ║
║   1. GitHub repo yaradılmayıb                                    ║
║   2. Username/Password yanlışdır                                 ║
║   3. Token yetkisi yoxdur                                        ║
║                                                                  ║
║   Həll:                                                          ║
║   1. https://github.com/new - repo yaradın                       ║
║   2. https://github.com/settings/tokens - token yaradın          ║
║   3. Scopes-da ✅ 'repo' seçin                                   ║
║                                                                  ║
╚══════════════════════════════════════════════════════════════════╝
"@ -ForegroundColor Red
}

Write-Host "`nÇıxmaq üçün Enter basın..." -ForegroundColor Cyan
Read-Host
