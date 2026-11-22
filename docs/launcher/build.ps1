$ErrorActionPreference = 'Stop'

$launcherDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $launcherDir
$sourceFile = Join-Path $launcherDir 'Program.cs'
$outputExe = Join-Path $rootDir 'RAKDA_3D_Sim.exe'

$cscCandidates = @(
    'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe',
    'C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe'
)

$cscPath = $cscCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $cscPath) {
    throw 'Could not locate .NET Framework 4.x csc.exe on this machine.'
}

Write-Host "Compiling $sourceFile -> $outputExe using $cscPath ..."
& $cscPath /nologo /optimize+ /platform:anycpu /target:exe "/out:$outputExe" $sourceFile
if ($LASTEXITCODE -ne 0) {
    throw "csc.exe failed with exit code $LASTEXITCODE"
}

$item = Get-Item $outputExe
Write-Host ("Build succeeded: {0} ({1:N0} bytes)" -f $item.FullName, $item.Length)
