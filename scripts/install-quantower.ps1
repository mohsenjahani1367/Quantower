param([Parameter(Mandatory=$true)][string]$Destination, [ValidateSet('Strategy','Indicator')][string]$Component = 'Strategy')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
if ($Component -eq 'Strategy') {
    $buildDir = Join-Path $repoRoot 'src\VolumeIntelligence.Quantower\bin\Release\net10.0-windows'
    $assemblyName = 'VolumeIntelligence.Quantower.Native.dll'
} else {
    $buildDir = Join-Path $repoRoot 'src\VolumeIntelligence.Quantower\Indicators\bin\Release\net10.0-windows'
    $assemblyName = 'VolumeIntelligence.Quantower.Indicators.dll'
}
# Only the two project assemblies are installed; never replace Quantower's own API DLL.
$files = @($assemblyName,'VolumeIntelligence.Core.dll')
foreach ($name in $files) { if (-not (Test-Path (Join-Path $buildDir $name))) { throw "Missing successful build output: $name" } }
if (-not (Test-Path $Destination)) { throw 'Choose an existing, verified Quantower Scripts directory.' }
$backup = Join-Path $repoRoot ('artifacts\install-backup-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
foreach ($name in $files) {
    $target = Join-Path $Destination $name
    if (Test-Path $target) { New-Item -ItemType Directory -Force $backup | Out-Null; Copy-Item $target (Join-Path $backup $name) }
    Copy-Item (Join-Path $buildDir $name) $target
}
Write-Host 'Copied project DLLs only. Open Quantower, UPDATE ALL, and perform the documented read-only smoke test.'
