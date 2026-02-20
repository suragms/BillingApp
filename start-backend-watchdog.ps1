# HexaBill Backend Watchdog Script
# Automatically restarts the backend if it crashes
# Usage: .\start-backend-watchdog.ps1

$ErrorActionPreference = "Continue"
$backendPath = Join-Path $PSScriptRoot "backend\HexaBill.Api"
$maxRestarts = 999 # Allow unlimited restarts
$restartCount = 0
$restartDelay = 2 # seconds to wait before restarting (reduced for faster recovery)
$healthCheckUrl = "http://localhost:5000/api/health"
$healthCheckTimeout = 30 # seconds to wait for backend to become healthy after start

Write-Host "`n🔄 HexaBill Backend Watchdog Started" -ForegroundColor Cyan
Write-Host "   This script will automatically restart the backend if it crashes`n" -ForegroundColor Gray

function Test-BackendHealth {
    param([int]$MaxAttempts = 10, [int]$DelaySeconds = 2)
    
    for ($i = 1; $i -le $MaxAttempts; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $healthCheckUrl -Method GET -TimeoutSec 3 -UseBasicParsing -ErrorAction Stop
            if ($response.StatusCode -eq 200) {
                Write-Host "✅ Backend is healthy!" -ForegroundColor Green
                return $true
            }
        }
        catch {
            if ($i -lt $MaxAttempts) {
                Write-Host "⏳ Waiting for backend to start... (attempt $i/$MaxAttempts)" -ForegroundColor Yellow
                Start-Sleep -Seconds $DelaySeconds
            }
        }
    }
    return $false
}

while ($restartCount -lt $maxRestarts) {
    $restartCount++
    Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray
    Write-Host "🚀 Starting Backend (Attempt $restartCount)..." -ForegroundColor Cyan
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor DarkGray
    
    Set-Location -Path $backendPath
    
    # Start backend in background
    $process = Start-Process -FilePath "dotnet" -ArgumentList "run" -NoNewWindow -PassThru
    
    # Wait a moment for process to start
    Start-Sleep -Seconds 3
    
    # Check if process is still running
    if ($process.HasExited) {
        Write-Host "❌ Backend process exited immediately with code: $($process.ExitCode)" -ForegroundColor Red
        Write-Host "⏳ Waiting $restartDelay seconds before restarting...`n" -ForegroundColor Yellow
        Start-Sleep -Seconds $restartDelay
        continue
    }
    
    # Wait for backend to become healthy
    Write-Host "🔍 Checking backend health..." -ForegroundColor Cyan
    $isHealthy = Test-BackendHealth -MaxAttempts 15 -DelaySeconds 2
    
    if (-not $isHealthy) {
        Write-Host "❌ Backend failed to become healthy. Stopping process..." -ForegroundColor Red
        try {
            Stop-Process -Id $process.Id -Force -ErrorAction SilentlyContinue
        }
        catch {
            # Process may have already exited
        }
        Write-Host "⏳ Waiting $restartDelay seconds before restarting...`n" -ForegroundColor Yellow
        Start-Sleep -Seconds $restartDelay
        continue
    }
    
    # Monitor the process - wait for it to exit
    Write-Host "👀 Monitoring backend process (PID: $($process.Id))..." -ForegroundColor Gray
    $process.WaitForExit()
    
    $exitCode = $process.ExitCode
    Write-Host "`n⚠️  Backend process exited with code: $exitCode" -ForegroundColor Yellow
    
    if ($exitCode -eq 0) {
        Write-Host "✅ Backend stopped normally (exit code 0)" -ForegroundColor Green
        break
    }
    
    Write-Host "⏳ Waiting $restartDelay seconds before restarting...`n" -ForegroundColor Yellow
    Start-Sleep -Seconds $restartDelay
}

Write-Host "`n🛑 Watchdog stopped.`n" -ForegroundColor Red
