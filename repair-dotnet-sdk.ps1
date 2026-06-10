# Chay script nay bang PowerShell (Run as Administrator)
# Sua 5 file SDK bi thieu trong Program Files

$ErrorActionPreference = 'Stop'

$pf   = 'C:\Program Files\dotnet\sdk\10.0.300'
$user = 'C:\Users\ASUS\dotnet-sdk\sdk\10.0.300'

if (-not (Test-Path $user)) {
    Write-Error "Khong tim thay SDK sach tai $user. Chay lai dotnet-install.ps1 truoc."
}

$files = @(
    'Microsoft.Common.CurrentVersion.targets',
    'Microsoft.DotNet.Cli.Definitions.xml',
    'NuGet.targets',
    'DotnetTools\dotnet-watch\10.0.300\tools\net10.0\any\Microsoft.DotNet.Cli.Definitions.xml',
    'runtimes\any\native\NuGet.targets'
)

Write-Host 'Dang sua SDK trong Program Files...'
foreach ($f in $files) {
    $src = Join-Path $user $f
    $dst = Join-Path $pf $f
    $dir = Split-Path $dst -Parent
    if (-not (Test-Path $dir)) {
        New-Item -ItemType Directory -Path $dir -Force | Out-Null
    }
    Copy-Item $src $dst -Force
    Write-Host "  OK: $f"
}

# Xoa bien moi truong tam thoi gay loi app he thong
[Environment]::SetEnvironmentVariable('DOTNET_ROOT', $null, 'User')

$userPath = [Environment]::GetEnvironmentVariable('PATH', 'User')
if ($userPath -like '*dotnet-sdk*') {
    $cleaned = ($userPath -split ';' | Where-Object { $_ -and $_ -notlike '*dotnet-sdk*' }) -join ';'
    [Environment]::SetEnvironmentVariable('PATH', $cleaned, 'User')
    Write-Host 'Da xoa dotnet-sdk khoi PATH (User).'
}

Write-Host ''
Write-Host 'Kiem tra build...'
& 'C:\Program Files\dotnet\dotnet.exe' build "$PSScriptRoot\HealthPath.API\HealthPath.API.csproj"
if ($LASTEXITCODE -eq 0) {
    Write-Host ''
    Write-Host 'THANH CONG. Dong Visual Studio / Cursor roi mo lai.'
} else {
    Write-Error 'Build van loi. Thu: winget install Microsoft.DotNet.SDK.10'
}
