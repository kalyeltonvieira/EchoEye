param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot
$ServiceName = "EchoEyeProtection"
$InstallDir = Join-Path $env:ProgramFiles "EchoEye X"
$ServiceExe = Join-Path $InstallDir "EchoEye.Protection.exe"
$DotNet = if ($env:DOTNET_EXE) {
    $env:DOTNET_EXE
} else {
    (Get-Command dotnet -ErrorAction Stop).Source
}

if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run install.ps1 from an elevated PowerShell session."
}

& $DotNet publish (Join-Path $ScriptRoot "src\EchoEye.Protection\EchoEye.Protection.csproj") `
    --configuration $Configuration `
    --runtime win-x64 `
    --self-contained false `
    --output $InstallDir

New-Item -Path (Join-Path $env:ProgramData "EchoEye") -ItemType Directory -Force | Out-Null

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

New-Service `
    -Name $ServiceName `
    -BinaryPathName "`"$ServiceExe`"" `
    -DisplayName "EchoEye X Protection Service" `
    -Description "EchoEye X ransomware resilience protection service" `
    -StartupType Automatic

Start-Service -Name $ServiceName
Write-Host "EchoEye X installed and started as $ServiceName." -ForegroundColor Green
