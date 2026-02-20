# HexaBill Backend Startup Script
# Usage: .\start-backend.ps1

Write-Host "`n🚀 Starting HexaBill Backend API..." -ForegroundColor Cyan
Write-Host "📁 Directory: backend/HexaBill.Api`n" -ForegroundColor Gray

# Change to backend directory
$backendPath = Join-Path $PSScriptRoot "backend\HexaBill.Api"
if (-not (Test-Path $backendPath)) {
    Write-Host "❌ ERROR: Backend directory not found at: $backendPath" -ForegroundColor Red
    exit 1
}

Set-Location -Path $backendPath

# Check if dotnet is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "❌ ERROR: dotnet CLI not found!" -ForegroundColor Red
    Write-Host "   Please install .NET SDK: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# Check if project file exists
if (-not (Test-Path "HexaBill.Api.csproj")) {
    Write-Host "❌ ERROR: HexaBill.Api.csproj not found!" -ForegroundColor Red
    Write-Host "   Current directory: $(Get-Location)" -ForegroundColor Yellow
    exit 1
}

# Check if port 5000 is already in use
$portInUse = netstat -ano | findstr ":5000"
if ($portInUse) {
    Write-Host "⚠️  WARNING: Port 5000 is already in use!" -ForegroundColor Yellow
    Write-Host "   You may need to stop the existing process first.`n" -ForegroundColor Yellow
}

Write-Host "✅ Starting backend server..." -ForegroundColor Green
Write-Host "   Backend will be available at: http://localhost:5000" -ForegroundColor Gray
Write-Host "   Health check: http://localhost:5000/api/health" -ForegroundColor Gray
Write-Host "   Swagger UI: http://localhost:5000/swagger (Development only)`n" -ForegroundColor Gray
Write-Host "   Press Ctrl+C to stop the server`n" -ForegroundColor Yellow

# Start backend
dotnet run
