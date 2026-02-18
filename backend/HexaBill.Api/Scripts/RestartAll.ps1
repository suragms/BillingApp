# ========================================
# RESTART ALL - Backend, Frontend, and Database Migrations
# Purpose: Stop all processes, apply migrations, restart backend and frontend
# Author: AI Assistant
# Date: 2026-02-17
# ========================================

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RESTART ALL - HexaBill Application" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiDir = Split-Path -Parent $scriptDir
$rootDir = Split-Path -Parent (Split-Path -Parent $apiDir)
$frontendDir = Join-Path $rootDir "frontend" "hexabill-ui"

Write-Host "API Directory: $apiDir" -ForegroundColor Gray
Write-Host "Frontend Directory: $frontendDir" -ForegroundColor Gray
Write-Host ""

# STEP 1: Stop all running processes
Write-Host "[1/6] Stopping all running processes..." -ForegroundColor Yellow
try {
    $processes = Get-Process | Where-Object {
        $_.ProcessName -like "*HexaBill*" -or 
        ($_.ProcessName -eq "dotnet" -and $_.Path -like "*HexaBill*") -or
        ($_.ProcessName -eq "node" -and $_.Path -like "*hexabill-ui*")
    }
    
    if ($processes) {
        foreach ($proc in $processes) {
            Write-Host "  Stopping process: $($proc.ProcessName) (PID: $($proc.Id))" -ForegroundColor Gray
            Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
        }
        Start-Sleep -Seconds 2
        Write-Host "✅ All processes stopped" -ForegroundColor Green
    } else {
        Write-Host "✅ No running processes found" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Warning: Could not stop some processes: $_" -ForegroundColor Yellow
}

Write-Host ""

# STEP 2: Build backend
Write-Host "[2/6] Building backend..." -ForegroundColor Yellow
Set-Location $apiDir
try {
    dotnet build --no-incremental 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed!" -ForegroundColor Red
        dotnet build 2>&1 | Select-Object -First 20
        exit 1
    }
    Write-Host "✅ Backend build successful" -ForegroundColor Green
} catch {
    Write-Host "❌ Build error: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# STEP 3: Check and create migration if needed
Write-Host "[3/6] Checking for pending migrations..." -ForegroundColor Yellow
try {
    # Check if ProductCategory migration exists
    $migrationFiles = Get-ChildItem -Path (Join-Path $apiDir "Migrations") -Filter "*AddProductsFeatures*" -ErrorAction SilentlyContinue
    if (-not $migrationFiles) {
        Write-Host "  Creating migration: AddProductsFeatures..." -ForegroundColor Gray
        dotnet ef migrations add AddProductsFeatures --context AppDbContext 2>&1 | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "✅ Migration created successfully" -ForegroundColor Green
        } else {
            Write-Host "⚠️  Migration creation failed (may already exist or DB locked)" -ForegroundColor Yellow
        }
    } else {
        Write-Host "✅ Migration already exists" -ForegroundColor Green
    }
} catch {
    Write-Host "⚠️  Could not create migration: $_" -ForegroundColor Yellow
}

Write-Host ""

# STEP 4: Apply migrations
Write-Host "[4/6] Applying database migrations..." -ForegroundColor Yellow
try {
    dotnet ef database update --context AppDbContext 2>&1 | Out-Null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Migrations applied successfully" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Migration application had issues (check logs above)" -ForegroundColor Yellow
        Write-Host "  You may need to run SQL script manually: Scripts\COMPLETE_PRODUCTS_MIGRATION.sql" -ForegroundColor Gray
    }
} catch {
    Write-Host "⚠️  Could not apply migrations: $_" -ForegroundColor Yellow
    Write-Host "  You may need to run SQL script manually: Scripts\COMPLETE_PRODUCTS_MIGRATION.sql" -ForegroundColor Gray
}

Write-Host ""

# STEP 5: Start backend
Write-Host "[5/6] Starting backend API..." -ForegroundColor Yellow
Write-Host "  Backend will run in background on http://localhost:5000" -ForegroundColor Gray
try {
    $backendJob = Start-Job -ScriptBlock {
        param($apiDir)
        Set-Location $apiDir
        dotnet run
    } -ArgumentList $apiDir
    
    Write-Host "✅ Backend started (Job ID: $($backendJob.Id))" -ForegroundColor Green
    Write-Host "  Waiting 5 seconds for backend to initialize..." -ForegroundColor Gray
    Start-Sleep -Seconds 5
} catch {
    Write-Host "❌ Failed to start backend: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""

# STEP 6: Start frontend
Write-Host "[6/6] Starting frontend..." -ForegroundColor Yellow
Write-Host "  Frontend will run in background on http://localhost:5173" -ForegroundColor Gray
Set-Location $frontendDir
try {
    if (-not (Test-Path "node_modules")) {
        Write-Host "  Installing npm dependencies..." -ForegroundColor Gray
        npm install 2>&1 | Out-Null
    }
    
    $frontendJob = Start-Job -ScriptBlock {
        param($frontendDir)
        Set-Location $frontendDir
        npm run dev
    } -ArgumentList $frontendDir
    
    Write-Host "✅ Frontend started (Job ID: $($frontendJob.Id))" -ForegroundColor Green
    Write-Host "  Waiting 5 seconds for frontend to initialize..." -ForegroundColor Gray
    Start-Sleep -Seconds 5
} catch {
    Write-Host "❌ Failed to start frontend: $_" -ForegroundColor Red
    Write-Host "  Make sure Node.js is installed and npm is available" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "✅ RESTART COMPLETE!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Backend: http://localhost:5000" -ForegroundColor Cyan
Write-Host "Frontend: http://localhost:5173" -ForegroundColor Cyan
Write-Host ""
Write-Host "To stop all processes, run:" -ForegroundColor Yellow
Write-Host "  Stop-Job -Id $($backendJob.Id), $($frontendJob.Id)" -ForegroundColor Gray
Write-Host "  Remove-Job -Id $($backendJob.Id), $($frontendJob.Id)" -ForegroundColor Gray
Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
