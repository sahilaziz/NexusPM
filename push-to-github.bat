@echo off
chcp 65001 >nul
echo ==========================================
echo   Nexus PM - GitHub Push Script
echo ==========================================
echo.

cd /d "C:\Users\Sahil\Desktop\Proqrams\PM\Nexus.ProjectManagement"

echo [1/5] Git status yoxlanılır...
git status
if errorlevel 1 (
    echo [XƏTA] Git tapılmadı! Git quraşdırılmayıb.
    pause
    exit /b 1
)

echo.
echo [2/5] Fayllar əlavə edilir...
git add .

echo.
echo [3/5] Commit edilir...
git commit -m "Initial commit: Nexus PM v1.0.0 - Full system implementation with CI/CD"

echo.
echo [4/5] GitHub-a push edilir...
git push -u origin main

if errorlevel 1 (
    echo.
    echo [XƏTA] Push uğursuz oldu!
    echo Əgər repo yoxdursa, əvvəlcə yaradın:
    echo https://github.com/new
    echo.
    echo Və remote əlavə edin:
    echo git remote add origin https://github.com/sahilaziz/NexusPM.git
    pause
    exit /b 1
)

echo.
echo ==========================================
echo   ✅ UĞURLU! GitHub-a göndərildi!
echo ==========================================
echo.
echo Link: https://github.com/sahilaziz/NexusPM
echo.
pause
