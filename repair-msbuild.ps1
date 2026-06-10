# Chay PowerShell (Run as Administrator)
# Sua file MSBuild bi thieu trong Visual Studio va .NET SDK

$ErrorActionPreference = 'Stop'

$sdk  = 'C:\Program Files\dotnet\sdk\10.0.300'
$backup = 'C:\Users\ASUS\dotnet-sdk\sdk\10.0.300'

if (-not (Test-Path $sdk)) {
    Write-Error "Khong tim thay .NET SDK tai $sdk"
}

$vsBin   = 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin'
$vsAmd64 = Join-Path $vsBin 'amd64'

$files = @(
    'Microsoft.Common.CurrentVersion.targets',
    'Microsoft.DotNet.Cli.Definitions.xml',
    'NuGet.targets'
)

function Copy-ToDir($dir) {
    if (-not (Test-Path $dir)) {
        Write-Warning "Bo qua: $dir"
        return
    }
    foreach ($f in $files) {
        $src = Join-Path $sdk $f
        if (-not (Test-Path $src) -and (Test-Path $backup)) {
            $src = Join-Path $backup $f
        }
        $dst = Join-Path $dir $f
        Copy-Item $src $dst -Force
        Write-Host "  OK [$dir]: $f"
    }
}

Write-Host '1/3 Sua .NET SDK (neu thieu)...'
$extraSdk = @(
    'DotnetTools\dotnet-watch\10.0.300\tools\net10.0\any\Microsoft.DotNet.Cli.Definitions.xml',
    'runtimes\any\native\NuGet.targets'
)
foreach ($f in ($files + $extraSdk)) {
    $src = Join-Path $backup $f
    if (-not (Test-Path $src)) { $src = Join-Path $sdk $f }
    if (-not (Test-Path $src)) { continue }
    $dst = Join-Path $sdk $f
    $dir = Split-Path $dst -Parent
    if (-not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    if (-not (Test-Path $dst)) {
        Copy-Item $src $dst -Force
        Write-Host "  OK [sdk]: $f"
    }
}

Write-Host '2/3 Sua Visual Studio MSBuild...'
Copy-ToDir $vsBin
Copy-ToDir $vsAmd64

Write-Host '3/3 Kiem tra build...'
$msbuild = Join-Path $vsBin 'MSBuild.exe'
& $msbuild "$PSScriptRoot\HealthPath.API\HealthPath.API.csproj" /restore /v:minimal
if ($LASTEXITCODE -ne 0) { Write-Error 'VS MSBuild van loi.' }

& 'C:\Program Files\dotnet\dotnet.exe' build "$PSScriptRoot\HealthPath.API\HealthPath.API.csproj"
if ($LASTEXITCODE -ne 0) { Write-Error 'dotnet build van loi.' }

Write-Host ''
Write-Host 'THANH CONG. Dong Visual Studio roi mo lai HealthPath.slnx.'
