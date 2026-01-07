# scripts/dev.ps1
# Start ARIS in development mode with hot-reload.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\scripts\dev.ps1
#
# This script starts both backend and frontend, then opens the browser.
# Press Ctrl+C to stop.

param(
  [switch]$NoBrowser,
  [switch]$BackendOnly,
  [switch]$FrontendOnly
)

$ErrorActionPreference = "Stop"

# Resolve paths
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Resolve-Path (Join-Path $scriptDir "..")
$backendDir = Join-Path $repoRoot "src\Aris.Hosting"
$frontendDir = Join-Path $repoRoot "frontend"

$backendUrl = "http://localhost:5000"
$frontendUrl = "http://localhost:5173"

# Track processes for cleanup
$script:backendProcess = $null
$script:frontendProcess = $null

function Stop-DevProcesses {
  Write-Host "`n==> Stopping dev servers..." -ForegroundColor Yellow

  if ($script:backendProcess -and !$script:backendProcess.HasExited) {
    Write-Host "    Stopping backend (PID $($script:backendProcess.Id))..."
    Stop-Process -Id $script:backendProcess.Id -Force -ErrorAction SilentlyContinue
  }

  if ($script:frontendProcess -and !$script:frontendProcess.HasExited) {
    Write-Host "    Stopping frontend (PID $($script:frontendProcess.Id))..."
    Stop-Process -Id $script:frontendProcess.Id -Force -ErrorAction SilentlyContinue
    # Also kill any node processes that might be children
    Get-Process -Name "node" -ErrorAction SilentlyContinue | Where-Object {
      $_.StartTime -gt (Get-Date).AddMinutes(-5)
    } | Stop-Process -Force -ErrorAction SilentlyContinue
  }

  Write-Host "==> Done" -ForegroundColor Green
}

# Cleanup on exit
$null = Register-EngineEvent -SourceIdentifier PowerShell.Exiting -Action { Stop-DevProcesses }
trap { Stop-DevProcesses; break }

function Stop-ProcessOnPort {
  param([int]$Port)
  $connections = Get-NetTCPConnection -LocalPort $Port -ErrorAction SilentlyContinue
  foreach ($conn in $connections) {
    $proc = Get-Process -Id $conn.OwningProcess -ErrorAction SilentlyContinue
    if ($proc) {
      Write-Host "    Killing existing process on port $Port (PID $($proc.Id), $($proc.ProcessName))..." -ForegroundColor Yellow
      Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
      Start-Sleep -Milliseconds 500
    }
  }
}

try {
  Push-Location $repoRoot

  # Kill any existing processes on our ports
  Write-Host "==> Checking for processes on required ports..." -ForegroundColor Cyan
  Stop-ProcessOnPort -Port 5000
  Stop-ProcessOnPort -Port 5173

  # Install frontend deps if needed
  if (-not $BackendOnly -and -not (Test-Path (Join-Path $frontendDir "node_modules"))) {
    Write-Host "==> Installing frontend dependencies..." -ForegroundColor Cyan
    Push-Location $frontendDir
    & npm install
    if ($LASTEXITCODE -ne 0) { throw "npm install failed" }
    Pop-Location
  }

  if (-not $FrontendOnly) {
    # Start backend
    Write-Host "==> Starting backend..." -ForegroundColor Cyan
    $script:backendProcess = Start-Process -FilePath "dotnet" `
      -ArgumentList "run", "--urls", $backendUrl `
      -WorkingDirectory $backendDir `
      -PassThru `
      -NoNewWindow

    # Wait for backend to be ready
    Write-Host "    Waiting for backend at $backendUrl..." -ForegroundColor Gray
    $maxWait = 30
    $waited = 0
    while ($waited -lt $maxWait) {
      Start-Sleep -Seconds 1
      $waited++
      try {
        $response = Invoke-WebRequest -Uri "$backendUrl/api/health" -UseBasicParsing -TimeoutSec 2 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
          Write-Host "    Backend ready!" -ForegroundColor Green
          break
        }
      } catch {
        # Not ready yet
        Write-Host "    ..." -ForegroundColor Gray
      }

      if ($script:backendProcess.HasExited) {
        throw "Backend process exited unexpectedly"
      }
    }

    if ($waited -ge $maxWait) {
      Write-Host "    Backend may still be starting..." -ForegroundColor Yellow
    }
  }

  if (-not $BackendOnly) {
    # Start frontend
    Write-Host "==> Starting frontend..." -ForegroundColor Cyan
    $script:frontendProcess = Start-Process -FilePath "cmd" `
      -ArgumentList "/c", "npm run dev" `
      -WorkingDirectory $frontendDir `
      -PassThru `
      -NoNewWindow

    Start-Sleep -Seconds 3
  }

  # Open browser
  if (-not $NoBrowser -and -not $BackendOnly) {
    Write-Host "==> Opening browser..." -ForegroundColor Cyan
    Start-Process $frontendUrl
  }

  # Show status
  Write-Host ""
  Write-Host "========================================" -ForegroundColor Green
  Write-Host "  ARIS Development Server Running" -ForegroundColor Green
  Write-Host "========================================" -ForegroundColor Green
  Write-Host ""
  if (-not $FrontendOnly) {
    Write-Host "  Backend:  $backendUrl" -ForegroundColor White
  }
  if (-not $BackendOnly) {
    Write-Host "  Frontend: $frontendUrl (with hot-reload)" -ForegroundColor White
  }
  Write-Host ""
  Write-Host "  Press Ctrl+C to stop" -ForegroundColor Yellow
  Write-Host ""

  # Keep script running until Ctrl+C
  while ($true) {
    Start-Sleep -Seconds 1

    # Check if processes died
    if (-not $FrontendOnly -and $script:backendProcess.HasExited) {
      Write-Host "Backend stopped unexpectedly!" -ForegroundColor Red
      break
    }
    if (-not $BackendOnly -and $script:frontendProcess.HasExited) {
      Write-Host "Frontend stopped unexpectedly!" -ForegroundColor Red
      break
    }
  }
}
finally {
  Stop-DevProcesses
  Pop-Location
}
