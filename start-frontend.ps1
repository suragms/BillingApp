# HexaBill Frontend Startup Script
# Usage: .\start-frontend.ps1

Write-Host "`n🚀 Starting HexaBill Frontend..." -ForegroundColor Cyan
Write-Host "📁 Directory: frontend/hexabill-ui`n" -ForegroundColor Gray

# Change to frontend directory
$frontendPath = Join-Path $PSScriptRoot "frontend\hexabill-ui"
if (-not (Test-Path $frontendPath)) {
    Write-Host "❌ ERROR: Frontend directory not found at: $frontendPath" -ForegroundColor Red
    exit 1
}

Set-Location -Path $frontendPath

# Check if node is available
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "❌ ERROR: Node.js not found!" -ForegroundColor Red
    Write-Host "   Please install Node.js: https://nodejs.org/" -ForegroundColor Yellow
    exit 1
}

# Check if npm is available
if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Host "❌ ERROR: npm not found!" -ForegroundColor Red
    Write-Host "   Please install Node.js (npm comes with it): https://nodejs.org/" -ForegroundColor Yellow
    exit 1
}

# Check if node_modules exists, if not, install dependencies
if (-not (Test-Path "node_modules")) {
    Write-Host "📦 Installing dependencies..." -ForegroundColor Yellow
    npm install
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ ERROR: Failed to install dependencies!" -ForegroundColor Red
        exit 1
    }
    Write-Host ""
}

# Check if port 5173 is already in use
$portInUse = netstat -ano | findstr ":5173"
if ($portInUse) {
    Write-Host "⚠️  WARNING: Port 5173 is already in use!" -ForegroundColor Yellow
    Write-Host "   You may need to stop the existing process first.`n" -ForegroundColor Yellow
}

Write-Host "✅ Starting frontend development server..." -ForegroundColor Green
Write-Host "   Frontend will be available at: http://localhost:5173" -ForegroundColor Gray
Write-Host "   Press Ctrl+C to stop the server`n" -ForegroundColor Yellow

# Start frontend
npm run dev
