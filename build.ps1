# EchoEye X Build Script
# Builds all modules and prepares deployment artifacts

param(
    [switch]$Clean,
    [switch]$Test,
    [switch]$Package
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot

function Test-DotNetSdk {
    param([string]$Path)

    if (-not (Test-Path $Path)) {
        return $false
    }

    & $Path --info *> $null
    return $LASTEXITCODE -eq 0
}

function Resolve-DotNet {
    $candidates = @(
        $env:DOTNET_EXE,
        "D:\dotnet\dotnet.exe",
        (Get-Command dotnet -ErrorAction SilentlyContinue).Source
    ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

    foreach ($candidate in $candidates) {
        if (Test-DotNetSdk $candidate) {
            return $candidate
        }
    }

    throw "No working .NET SDK found. Install .NET 8 SDK or set DOTNET_EXE to a working dotnet.exe."
}

$DotNet = Resolve-DotNet

Write-Host "EchoEye X Build Script" -ForegroundColor Cyan
Write-Host "========================" -ForegroundColor Cyan

# Clean build artifacts if requested
if ($Clean) {
    Write-Host "Cleaning build artifacts..." -ForegroundColor Yellow
    Remove-Item -Path "$ScriptRoot\build" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$ScriptRoot\bin" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -Path "$ScriptRoot\obj" -Recurse -Force -ErrorAction SilentlyContinue
}

# Create build directory
$BuildDir = "$ScriptRoot\build"
New-Item -Path $BuildDir -ItemType Directory -Force | Out-Null

# Build each module
$Modules = @(
    "EchoEye.Core",
    "EchoEye.Installer",
    "EchoEye.Protection",
    "EchoEye.Decoy",
    "EchoEye.Recovery",
    "EchoEye.Intelligence",
    "EchoEye.Management",
    "EchoEye.Dashboard"
)

foreach ($Module in $Modules) {
    $ModulePath = "$ScriptRoot\src\$Module"
    if (Test-Path $ModulePath) {
        Write-Host "Building $Module..." -ForegroundColor Green
        & $DotNet build "$ModulePath\$Module.csproj" --configuration Release --output "$BuildDir\$Module"
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to build $Module" -ForegroundColor Red
            exit 1
        }
    }
}

# Run tests if requested
if ($Test) {
    Write-Host "Running tests..." -ForegroundColor Green
    if (-not (Test-Path "$ScriptRoot\tests")) {
        Write-Host "No tests directory found." -ForegroundColor Yellow
        exit 1
    }

    $TestProjects = Get-ChildItem -Path "$ScriptRoot\tests" -Filter "*.csproj" -Recurse
    if (-not $TestProjects) {
        Write-Host "No test projects found." -ForegroundColor Yellow
        exit 1
    }

    foreach ($TestProject in $TestProjects) {
        & $DotNet test $TestProject.FullName --configuration Release
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Tests failed for $($TestProject.Name)" -ForegroundColor Red
            exit 1
        }
    }
}

# Package if requested
if ($Package) {
    Write-Host "Creating deployment package..." -ForegroundColor Green
    $PackageDir = "$BuildDir\package"
    New-Item -Path $PackageDir -ItemType Directory -Force | Out-Null
    
    # Copy all built assemblies
    Copy-Item -Path "$BuildDir\*\*.dll" -Destination $PackageDir -Recurse -Force
    Copy-Item -Path "$BuildDir\*\*.exe" -Destination $PackageDir -Recurse -Force
    
    # Copy configuration files
    Copy-Item -Path "$ScriptRoot\config\*" -Destination "$PackageDir\config" -Recurse -Force -ErrorAction SilentlyContinue
    
    # Copy installation scripts
    Copy-Item -Path "$ScriptRoot\install.ps1" -Destination $PackageDir -Force
    Copy-Item -Path "$ScriptRoot\uninstall.ps1" -Destination $PackageDir -Force
    
    # Create ZIP archive
    $ZipPath = "$BuildDir\EchoEye-X-$((Get-Date).ToString('yyyyMMdd-HHmmss')).zip"
    Compress-Archive -Path "$PackageDir\*" -DestinationPath $ZipPath -Force
    Write-Host "Package created: $ZipPath" -ForegroundColor Green
}

Write-Host "Build completed successfully!" -ForegroundColor Green
