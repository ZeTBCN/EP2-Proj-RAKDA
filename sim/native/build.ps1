param(
    [switch]$TestsOnly,
    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'

$nativeDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $nativeDir
$cscPath = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'

if (-not (Test-Path $cscPath)) {
    throw "Required .NET Framework 4.x x64 compiler not found at $cscPath"
}

$physicsCs = Join-Path $nativeDir 'Physics.cs'
$physicsTestsCs = Join-Path $nativeDir 'PhysicsTests.cs'
$physicsTestsExe = Join-Path $nativeDir 'PhysicsTests.exe'

if (-not $SkipTests) {
    Write-Host "Compiling native\PhysicsTests.exe ..."
    & $cscPath /nologo /platform:x64 /optimize+ /target:exe /r:System.dll /r:System.Core.dll "/out:$physicsTestsExe" $physicsCs $physicsTestsCs
    if ($LASTEXITCODE -ne 0) {
        throw "PhysicsTests.exe compilation failed with exit code $LASTEXITCODE"
    }

    Write-Host "Running native\PhysicsTests.exe ..."
    & $physicsTestsExe
    if ($LASTEXITCODE -ne 0) {
        throw "PhysicsTests.exe failed with exit code $LASTEXITCODE"
    }
}

if ($TestsOnly) {
    exit 0
}

$appSources = @(
    'Math3D.cs',
    'GlNative.cs',
    'Physics.cs',
    'Renderer.cs',
    'SubterraneanScene.cs',
    'SurfaceScene.cs',
    'FlowsScene.cs',
    'MainForm.cs',
    'Program.cs'
) | ForEach-Object { Join-Path $nativeDir $_ }

foreach ($src in $appSources) {
    if (-not (Test-Path $src)) {
        throw "Missing required source file: $src"
    }
}

$outputExe = Join-Path $rootDir 'RAKDA_3D_Sim.exe'
Write-Host "Compiling $outputExe (100% Native Windows OpenGL 3.3 Desktop App) ..."
& $cscPath /nologo /platform:x64 /optimize+ /unsafe /target:winexe `
    /r:System.dll /r:System.Core.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
    "/out:$outputExe" @appSources

if ($LASTEXITCODE -ne 0) {
    throw "RAKDA_3D_Sim.exe compilation failed with exit code $LASTEXITCODE"
}

$item = Get-Item $outputExe
Write-Host ("Build succeeded: {0} ({1:N0} bytes)" -f $item.FullName, $item.Length)
