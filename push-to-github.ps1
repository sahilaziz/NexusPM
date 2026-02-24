# Nexus PM - GitHub Push Script
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   Nexus PM - GitHub Push Script" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$projectPath = "C:\Users\Sahil\Desktop\Proqrams\PM\Nexus.ProjectManagement"
Set-Location $projectPath

# Check if git is initialized
if (-not (Test-Path ".git")) {
    Write-Host "[1/6] Git initialize edilir..." -ForegroundColor Yellow
    git init
    
    Write-Host "[2/6] Remote əlavə edilir..." -ForegroundColor Yellow
    git remote add origin https://github.com/sahilaziz/NexusPM.git
} else {
    Write-Host "[1/6] Git artıq initialize edilib ✓" -ForegroundColor Green
}

# Check remote
Write-Host "[2/6] Remote yoxlanılır..." -ForegroundColor Yellow
$remote = git remote -v 2>$null
if (-not $remote) {
    Write-Host "      Remote əlavə edilir..." -ForegroundColor Yellow
    git remote add origin https://github.com/sahilaziz/NexusPM.git
}

# Add files
Write-Host "[3/6] Fayllar əlavə edilir..." -ForegroundColor Yellow
git add .

# Commit
Write-Host "[4/6] Commit edilir..." -ForegroundColor Yellow
git commit -m "Initial commit: Nexus PM v1.0.0

Backend:
- 55+ API endpoints
- CQRS + Clean Architecture
- Authentication (Local + AD)
- Task Dependencies, Labels, Time Tracking

Mobile:
- Flutter app with 8 screens
- Riverpod state management
- Real-time timer

Infrastructure:
- GitHub Actions CI/CD
- Docker support
- Hybrid messaging/monitoring

Documentation complete."

# Push
Write-Host "[5/6] GitHub-a push edilir..." -ForegroundColor Yellow
try {
    git push -u origin main
    if ($LASTEXITCODE -eq 0) {
        Write-Host "[6/6] ✅ UĞURLU! GitHub-a göndərildi!" -ForegroundColor Green
        Write-Host ""
        Write-Host "Link: https://github.com/sahilaziz/NexusPM" -ForegroundColor Cyan
    } else {
        throw "Push failed"
    }
} catch {
    Write-Host "[XƏTA] Push uğursuz oldu!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Mümkün səbəblər:" -ForegroundColor Yellow
    Write-Host "1. GitHub repo yaradılmayıb" -ForegroundColor Yellow
    Write-Host "2. Authentication xətası" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Həll:" -ForegroundColor Green
    Write-Host "1. https://github.com/new keçin və repo yaradın" -ForegroundColor White
    Write-Host "2. Token yaradın: GitHub → Settings → Developer settings → Tokens" -ForegroundColor White
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Read-Host "Çıxmaq üçün Enter basın"
