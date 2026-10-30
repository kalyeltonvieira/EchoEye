param(
    [int]$Port = 8080
)

$ErrorActionPreference = "Stop"
$ScriptRoot = $PSScriptRoot

function Test-PortAvailable {
    param([int]$CandidatePort)

    $listener = $null
    try {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Parse("127.0.0.1"), $CandidatePort)
        $listener.Start()
        return $true
    }
    catch {
        return $false
    }
    finally {
        if ($listener) {
            $listener.Stop()
        }
    }
}

$selectedPort = $Port
while (-not (Test-PortAvailable -CandidatePort $selectedPort)) {
    $selectedPort++
}

$node = Get-Command node -ErrorAction SilentlyContinue
if (-not $node) {
    throw "Node.js is required for the local runner, but node was not found on PATH."
}

$logDir = Join-Path $ScriptRoot "build"
New-Item -Path $logDir -ItemType Directory -Force | Out-Null
$stdoutLogPath = Join-Path $logDir "echoeye-x-local-runner.out.log"
$stderrLogPath = Join-Path $logDir "echoeye-x-local-runner.err.log"

$serverPath = Join-Path $ScriptRoot "runtime\server.mjs"
$process = Start-Process -FilePath $node.Source `
    -ArgumentList @($serverPath, $selectedPort) `
    -WorkingDirectory $ScriptRoot `
    -WindowStyle Hidden `
    -RedirectStandardOutput $stdoutLogPath `
    -RedirectStandardError $stderrLogPath `
    -PassThru

Start-Sleep -Milliseconds 700

Write-Host "EchoEye X local runner started." -ForegroundColor Green
Write-Host "PID: $($process.Id)"
Write-Host "URL: http://127.0.0.1:$selectedPort"
Write-Host "Stdout log: $stdoutLogPath"
Write-Host "Stderr log: $stderrLogPath"
