@echo off
chcp 65001 >nul
title Nexus PM - GitHub'a Gonder

:: Administrator yoxlama
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo [XƏTA] Administrator kimi işlədin!
    echo.
    echo PowerShell-i Administrator kimi açın və yenidən işlədin:
    echo powershell -ExecutionPolicy Bypass -File "C:\Users\Sahil\Desktop\Proqrams\PM\Nexus.ProjectManagement\AUTO_PUSH.ps1"
    echo.
    pause
    exit /b 1
)

echo.
echo ==========================================
echo    NEXUS PM - GITHUB'A GONDER
echo ==========================================
echo.

cd /d "C:\Users\Sahil\Desktop\Proqrams\PM\Nexus.ProjectManagement"

echo [1/6] Git yoxlanilir...
git --version >nul 2>&1
if errorlevel 1 (
    echo [XƏTA] Git tapilmadi!
    echo Git yukleyin: https://git-scm.com/download/win
    pause
    exit /b 1
)
echo     ✓ Git tapildi

echo.
echo [2/6] Git initialize edilir...
if not exist ".git" (
    git init >nul 2>&1
    echo     ✓ Git initialize edildi
) else (
    echo     ✓ Artiq initialize edilib
)

echo.
echo [3/6] .gitignore yaradilir...
if not exist ".gitignore" (
    (
        echo # .NET
        echo bin/
        echo obj/
        echo *.dll
        echo *.exe
        echo .vs/
        echo.
        echo # Flutter
        echo mobile/.dart_tool/
        echo mobile/build/
        echo.
        echo # Secrets
        echo appsettings.Development.json
        echo *.key
    ) > .gitignore
    echo     ✓ .gitignore yaradildi
) else (
    echo     ✓ .gitignore artiq var
)

echo.
echo [4/6] GitHub remote elave edilir...
git remote remove origin >nul 2>&1
git remote add origin https://github.com/sahilaziz/NexusPM.git >nul 2>&1
echo     ✓ Remote elave edildi

echo.
echo [5/6] Fayllar elave edilir...
git add . >nul 2>&1
echo     ✓ Fayllar elave edildi

echo.
echo [6/6] Commit ve Push...
git commit -m "Nexus PM v1.0.0 - Initial Release" >nul 2>&1
git branch -M main >nul 2>&1

echo.
echo ------------------------------------------
echo  INDI GITHUB HESABINIZI SORACAQ...
echo ------------------------------------------
echo.
echo Username: sahilaziz
echo Password: TOKEN (yaratdiginiz)
echo.
echo Token yaratmaq:
echo https://github.com/settings/tokens
echo.

git push -u origin main 2>&1

if %errorlevel% equ 0 (
    echo.
    echo ==========================================
    echo    ✅ UGURLU! GitHub-a gonderildi!
    echo ==========================================
    echo.
    echo Link: https://github.com/sahilaziz/NexusPM
    echo.
) else (
    echo.
    echo ==========================================
    echo    ❌ XETA BAS VERDI
    echo ==========================================
    echo.
    echo 1. https://github.com/new kecid edin
    echo 2. Repo yaradin: NexusPM
    echo 3. Token yaradin: https://github.com/settings/tokens
    echo 4. Scopes-da 'repo' secin
    echo.
)

pause
