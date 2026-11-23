@echo off
setlocal
cd /d "%~dp0"

where node >nul 2>nul
if errorlevel 1 (
    echo [ERROR] Node.js was not found on PATH.
    echo Please install Node.js 18+ from https://nodejs.org/ and re-run this launcher.
    pause
    exit /b 1
)

echo Launching RAKDA Ltd. ^(Group A-10^) 2045 Digital Twin at http://localhost:8170 ...
start "" "http://localhost:8170"
node serve.mjs
endlocal
