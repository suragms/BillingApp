# HexaBill Build Check Script
# Run before commits to verify frontend and backend build successfully.
# Usage: .\scripts\build-check.ps1   or   pwsh scripts/build-check.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path $root)) { $root = (Get-Location).Path }

Write-Host "`n=== HexaBill Build Check ===" -ForegroundColor Cyan
Write-Host "Root: $root`n" -ForegroundColor Gray

$failed = $false

# Backend
Write-Host "Building backend (dotnet build)..." -ForegroundColor Yellow
$backendPath = Join-Path $root "backend\HexaBill.Api"
if (Test-Path $backendPath) {
    try {
        Push-Location $backendPath
        dotnet build --no-restore 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            dotnet build 2>&1 | Out-Null
        }
        if ($LASTEXITCODE -ne 0) {
            dotnet build
            $failed = $true
        } else {
            Write-Host "  Backend: OK" -ForegroundColor Green
        }
        Pop-Location
    } catch {
        Write-Host "  Backend: FAILED - $_" -ForegroundColor Red
        $failed = $true
        Pop-Location -ErrorAction SilentlyContinue
    }
} else {
    Write-Host "  Backend path not found: $backendPath" -ForegroundColor Yellow
}

# Frontend
Write-Host "Building frontend (npm run build)..." -ForegroundColor Yellow
$frontendPath = Join-Path $root "frontend\hexabill-ui"
if (Test-Path $frontendPath) {
    try {
        Push-Location $frontendPath
        & npm run build
        if ($LASTEXITCODE -ne 0) {
            $failed = $true
        } else {
            Write-Host "  Frontend: OK" -ForegroundColor Green
        }
        Pop-Location
    } catch {
        Write-Host "  Frontend: FAILED - $_" -ForegroundColor Red
        $failed = $true
        Pop-Location -ErrorAction SilentlyContinue
    }
} else {
    Write-Host "  Frontend path not found: $frontendPath" -ForegroundColor Yellow
}

Write-Host ""
if ($failed) {
    Write-Host "Build check FAILED. Fix errors before committing." -ForegroundColor Red
    exit 1
} else {
    Write-Host "Build check PASSED." -ForegroundColor Green
    exit 0
}
