@echo off
setlocal
set "LAUNCHER_DIR=%~dp0"
set "ROOT_DIR=%LAUNCHER_DIR%.."
set "CSC64=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
set "CSC32=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe"

if exist "%CSC64%" (
    set "CSC=%CSC64%"
) else if exist "%CSC32%" (
    set "CSC=%CSC32%"
) else (
    echo [ERROR] .NET Framework 4.x csc.exe not found.
    exit /b 1
)

echo Compiling "%LAUNCHER_DIR%Program.cs" -^> "%ROOT_DIR%\RAKDA_3D_Sim.exe"
"%CSC%" /nologo /optimize+ /platform:anycpu /target:exe "/out:%ROOT_DIR%\RAKDA_3D_Sim.exe" "%LAUNCHER_DIR%Program.cs"
if errorlevel 1 exit /b %errorlevel%

echo Build succeeded: %ROOT_DIR%\RAKDA_3D_Sim.exe
endlocal
