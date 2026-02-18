# ========================================
# PRODUCTS MIGRATION SCRIPT
# Purpose: Run COMPLETE_PRODUCTS_MIGRATION.sql on PostgreSQL database
# Author: AI Assistant
# Date: 2026-02-17
# ========================================

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "PRODUCTS MIGRATION SCRIPT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Get script directory
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$sqlScriptPath = Join-Path $scriptDir "COMPLETE_PRODUCTS_MIGRATION.sql"

if (-not (Test-Path $sqlScriptPath)) {
    Write-Host "❌ SQL script not found: $sqlScriptPath" -ForegroundColor Red
    exit 1
}

# Try to get connection string from environment variable first
$connectionString = $env:ConnectionStrings__DefaultConnection
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    $connectionString = $env:DATABASE_URL
}

# If still not found, try to read from appsettings.json
if ([string]::IsNullOrWhiteSpace($connectionString)) {
    $appSettingsPath = Join-Path (Split-Path -Parent (Split-Path -Parent $scriptDir)) "appsettings.json"
    if (Test-Path $appSettingsPath) {
        $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
        $connectionString = $appSettings.ConnectionStrings.DefaultConnection
    }
}

# Check if it's PostgreSQL
$isPostgreSQL = $false
$host = ""
$port = ""
$database = ""
$username = ""
$password = ""

if (-not [string]::IsNullOrWhiteSpace($connectionString)) {
    if ($connectionString -match "postgres://([^:]+):([^@]+)@([^:]+):(\d+)/(.+)") {
        # DATABASE_URL format
        $username = $matches[1]
        $password = $matches[2]
        $host = $matches[3]
        $port = $matches[4]
        $database = $matches[5]
        $isPostgreSQL = $true
    }
    elseif ($connectionString -match "Host=([^;]+)") {
        # Npgsql connection string format
        $isPostgreSQL = $true
        if ($connectionString -match "Host=([^;]+)") { $host = $matches[1] }
        if ($connectionString -match "Port=([^;]+)") { $port = $matches[1] } else { $port = "5432" }
        if ($connectionString -match "Database=([^;]+)") { $database = $matches[1] }
        if ($connectionString -match "Username=([^;]+)") { $username = $matches[1] }
        if ($connectionString -match "Password=([^;]+)") { $password = $matches[1] }
        if ($connectionString -match "User Id=([^;]+)") { $username = $matches[1] }
        if ($connectionString -match "Pwd=([^;]+)") { $password = $matches[1] }
    }
    elseif ($connectionString -match "Data Source=") {
        Write-Host "⚠️  Detected SQLite database. This migration is for PostgreSQL only." -ForegroundColor Yellow
        Write-Host "   For SQLite, please use Entity Framework migrations:" -ForegroundColor Yellow
        Write-Host "   dotnet ef migrations add AddProductsFeatures" -ForegroundColor Gray
        Write-Host "   dotnet ef database update" -ForegroundColor Gray
        exit 0
    }
}

if (-not $isPostgreSQL) {
    Write-Host "❌ PostgreSQL connection string not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please provide PostgreSQL connection details:" -ForegroundColor Yellow
    Write-Host ""
    $host = Read-Host "Host (e.g., localhost)"
    $port = Read-Host "Port (default: 5432)"
    if ([string]::IsNullOrWhiteSpace($port)) { $port = "5432" }
    $database = Read-Host "Database name"
    $username = Read-Host "Username"
    $password = Read-Host "Password" -AsSecureString
    $password = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))
}

Write-Host ""
Write-Host "✅ Connection details:" -ForegroundColor Green
Write-Host "   Host: $host" -ForegroundColor Gray
Write-Host "   Port: $port" -ForegroundColor Gray
Write-Host "   Database: $database" -ForegroundColor Gray
Write-Host "   Username: $username" -ForegroundColor Gray
Write-Host ""

# Check if psql is installed
try {
    $psqlVersion = & psql --version 2>&1
    Write-Host "✅ PostgreSQL Client: $psqlVersion" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "❌ psql command not found!" -ForegroundColor Red
    Write-Host "Please install PostgreSQL client tools:" -ForegroundColor Yellow
    Write-Host "  Windows: https://www.postgresql.org/download/windows/" -ForegroundColor Gray
    Write-Host "  Or install via chocolatey: choco install postgresql" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Alternatively, you can run the SQL script manually using:" -ForegroundColor Yellow
    Write-Host "  psql -h $host -p $port -U $username -d $database -f `"$sqlScriptPath`"" -ForegroundColor Gray
    exit 1
}

# Confirm execution
Write-Host "⚠️  This script will modify your database!" -ForegroundColor Yellow
Write-Host "   Make sure you have a backup before proceeding." -ForegroundColor Yellow
Write-Host ""
$confirm = Read-Host "Do you want to continue? (yes/no)"

if ($confirm -ne "yes") {
    Write-Host "❌ Operation cancelled by user" -ForegroundColor Red
    exit 0
}

Write-Host ""
Write-Host "🚀 Executing products migration..." -ForegroundColor Cyan
Write-Host ""

# Set PGPASSWORD environment variable
$env:PGPASSWORD = $password

# Execute SQL script
try {
    $result = & psql -h $host -p $port -U $username -d $database -f $sqlScriptPath 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host ""
        Write-Host "========================================" -ForegroundColor Green
        Write-Host "✅ MIGRATION COMPLETED SUCCESSFULLY!" -ForegroundColor Green
        Write-Host "========================================" -ForegroundColor Green
        Write-Host ""
        Write-Host "Migration output:" -ForegroundColor Yellow
        Write-Host $result
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor Cyan
        Write-Host "1. Restart your backend service" -ForegroundColor Gray
        Write-Host "2. Test the Products page to ensure it loads correctly" -ForegroundColor Gray
        Write-Host "3. Try creating a product category and uploading a product image" -ForegroundColor Gray
    } else {
        Write-Host ""
        Write-Host "❌ MIGRATION FAILED!" -ForegroundColor Red
        Write-Host "Error output:" -ForegroundColor Yellow
        Write-Host $result
        exit 1
    }
} catch {
    Write-Host ""
    Write-Host "❌ Error executing SQL script: $_" -ForegroundColor Red
    exit 1
} finally {
    # Clear password from environment
    $env:PGPASSWORD = ""
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
