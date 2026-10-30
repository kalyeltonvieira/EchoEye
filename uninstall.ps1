$ErrorActionPreference = "Stop"
$ServiceName = "EchoEyeProtection"
$InstallDir = Join-Path $env:ProgramFiles "EchoEye X"

if (-not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw "Run uninstall.ps1 from an elevated PowerShell session."
}

$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existing) {
    Stop-Service -Name $ServiceName -ErrorAction SilentlyContinue
    sc.exe delete $ServiceName | Out-Null
}

if (Test-Path $InstallDir) {
    Remove-Item -LiteralPath $InstallDir -Recurse -Force
}

Write-Host "EchoEye X service removed." -ForegroundColor Green
