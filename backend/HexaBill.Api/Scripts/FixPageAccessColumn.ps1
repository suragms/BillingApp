# Fix PageAccess column for SQLite database
# Run this if you get "no such column: u.PageAccess" error

$dbPath = "hexabill.db"
$fullPath = Join-Path $PSScriptRoot "..\$dbPath"

if (-not (Test-Path $fullPath)) {
    Write-Host "Database file not found at: $fullPath" -ForegroundColor Red
    Write-Host "Please run this script from the Scripts folder or update the path." -ForegroundColor Yellow
    exit 1
}

Write-Host "Adding PageAccess column to SQLite database..." -ForegroundColor Cyan
Write-Host "Database: $fullPath" -ForegroundColor Gray

# Check if column already exists
$checkColumn = sqlite3 $fullPath "PRAGMA table_info(Users);" | Select-String "PageAccess"

if ($checkColumn) {
    Write-Host "✅ PageAccess column already exists!" -ForegroundColor Green
    exit 0
}

# Add the column
try {
    sqlite3 $fullPath "ALTER TABLE Users ADD COLUMN PageAccess TEXT NULL;"
    Write-Host "✅ Successfully added PageAccess column!" -ForegroundColor Green
    
    # Verify
    $verify = sqlite3 $fullPath "PRAGMA table_info(Users);" | Select-String "PageAccess"
    if ($verify) {
        Write-Host "✅ Column verified successfully!" -ForegroundColor Green
    } else {
        Write-Host "⚠️ Warning: Column added but verification failed" -ForegroundColor Yellow
    }
} catch {
    Write-Host "❌ Error adding column: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Done! You can now login again." -ForegroundColor Green
