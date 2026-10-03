param([string]$BusinessLayerPath = 'C:\Quantower\TradingPlatform\v1.146.18\bin\TradingPlatform.BusinessLayer.dll', [switch]$InstallSdk)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot
$version = (Get-Content global.json -Raw | ConvertFrom-Json).sdk.version
$sdkRoot = Join-Path $repoRoot '.local\dotnet'
$dotnetExe = Join-Path $sdkRoot 'dotnet.exe'
if (-not (Test-Path $dotnetExe)) {
    $systemDotnet = Get-Command dotnet -ErrorAction SilentlyContinue
    if ($systemDotnet) { $dotnetExe = $systemDotnet.Source }
}
if ($InstallSdk) {
    if ($env:PROCESSOR_ARCHITECTURE -ne 'AMD64') { throw 'This installer targets Windows x64; select an official matching SDK on other architectures.' }
    $metadata = Invoke-RestMethod 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json'
    $sdks = foreach ($release in $metadata.releases) { if ($release.sdks) { $release.sdks } else { $release.sdk } }
    $sdk = $sdks | Where-Object { $_.version -eq $version } | Select-Object -First 1
    if (-not $sdk) { throw "Pinned SDK $version not found in official metadata." }
    $artifact = $sdk.files | Where-Object { $_.rid -eq 'win-x64' -and $_.name.EndsWith('.zip') } | Select-Object -First 1
    if (-not $artifact) { throw 'Official Windows x64 ZIP not found.' }
    New-Item -ItemType Directory -Force (Join-Path $repoRoot '.local') | Out-Null
    $archive = Join-Path $repoRoot '.local\sdk.zip'
    Invoke-WebRequest $artifact.url -OutFile $archive
    if ((Get-FileHash $archive -Algorithm SHA512).Hash -ne $artifact.hash) { throw 'SDK SHA-512 mismatch; extraction refused.' }
    Expand-Archive -LiteralPath $archive -DestinationPath $sdkRoot -Force
    Remove-Item -LiteralPath $archive
    $dotnetExe = Join-Path $sdkRoot 'dotnet.exe'
}
if (-not (Test-Path $dotnetExe)) { throw 'No SDK found. Run this script with -InstallSdk to install the checksum-verified SDK locally.' }
$env:DOTNET_CLI_HOME = Join-Path $repoRoot '.local\cli'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
New-Item -ItemType Directory -Force artifacts | Out-Null
& $dotnetExe build VolumeIntelligence.sln -c Release --nologo -m:2 2>&1 | Tee-Object artifacts\windows-core-build.log
if ($LASTEXITCODE -ne 0) { throw 'Core build failed.' }
& $dotnetExe run --project tests\VolumeIntelligence.Tests -c Release --no-build 2>&1 | Tee-Object artifacts\windows-tests.log
if ($LASTEXITCODE -ne 0) { throw 'Core tests failed.' }
if (-not (Test-Path $BusinessLayerPath)) { throw 'Core checks finished, but the actual Quantower API DLL was not found.' }
Get-FileHash $BusinessLayerPath -Algorithm SHA256 | Format-List | Out-File artifacts\quantower-api-hash.txt
& $dotnetExe build src\VolumeIntelligence.Quantower -c Release --nologo "-p:BusinessLayerPath=$BusinessLayerPath" 2>&1 | Tee-Object artifacts\windows-quantower-build.log
if ($LASTEXITCODE -ne 0) { throw 'Quantower build failed. Return the build log; do not install any DLL.' }
& $dotnetExe build src\VolumeIntelligence.Quantower\Indicators -c Release --nologo "-p:BusinessLayerPath=$BusinessLayerPath" 2>&1 | Tee-Object artifacts\windows-indicator-build.log
if ($LASTEXITCODE -ne 0) { throw 'Quantower indicator build failed. Return the build log; do not install the indicator DLL.' }
Write-Host 'Build and core tests passed locally. Nothing was installed into Quantower. Follow docs/WINDOWS_SMOKE_TEST_FA.md.'
