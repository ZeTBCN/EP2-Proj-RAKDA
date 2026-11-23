@echo off
setlocal
set "NATIVE_DIR=%~dp0"
set "ROOT_DIR=%NATIVE_DIR%.."
set "CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if not exist "%CSC%" (
    echo [ERROR] Required .NET Framework 4.x x64 csc.exe not found at %CSC%
    exit /b 1
)

echo Compiling native\PhysicsTests.exe ...
"%CSC%" /nologo /platform:x64 /optimize+ /target:exe /r:System.dll /r:System.Core.dll "/out:%NATIVE_DIR%PhysicsTests.exe" "%NATIVE_DIR%Physics.cs" "%NATIVE_DIR%PhysicsTests.cs"
if errorlevel 1 exit /b %errorlevel%

echo Running native\PhysicsTests.exe ...
"%NATIVE_DIR%PhysicsTests.exe"
if errorlevel 1 exit /b %errorlevel%

echo Compiling %ROOT_DIR%\RAKDA_3D_Sim.exe ...
"%CSC%" /nologo /platform:x64 /optimize+ /unsafe /target:winexe /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll "/out:%ROOT_DIR%\RAKDA_3D_Sim.exe" "%NATIVE_DIR%Math3D.cs" "%NATIVE_DIR%GlNative.cs" "%NATIVE_DIR%Physics.cs" "%NATIVE_DIR%Renderer.cs" "%NATIVE_DIR%SubterraneanScene.cs" "%NATIVE_DIR%SurfaceScene.cs" "%NATIVE_DIR%FlowsScene.cs" "%NATIVE_DIR%MainForm.cs" "%NATIVE_DIR%Program.cs"
if errorlevel 1 exit /b %errorlevel%

echo Build succeeded: %ROOT_DIR%\RAKDA_3D_Sim.exe
endlocal
