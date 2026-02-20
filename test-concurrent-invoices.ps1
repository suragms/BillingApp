# Concurrent Invoice Creation Test
# Tests invoice number generation race condition fix

$baseUrl = "http://localhost:5000"
$concurrentRequests = 20  # Number of simultaneous requests

# Login and get token
$loginBody = @{
    email = "owner@test.com"  # Update with your test user
    password = "password123"
} | ConvertTo-Json

try {
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType "application/json"
    $token = $loginResponse.data.token
    Write-Host "✅ Logged in successfully" -ForegroundColor Green
} catch {
    Write-Host "❌ Login failed: $_" -ForegroundColor Red
    exit 1
}

$headers = @{ Authorization = "Bearer $token" }

# Create invoice function
function Create-Invoice {
    param($index)
    
    $body = @{
        customerId = $null
        items = @(@{ productId = 1; qty = 1; unitPrice = 100 })
        discount = 0
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/sales" -Method POST -Body $body -ContentType "application/json" -Headers $headers
        return @{ Success = $true; InvoiceNo = $response.data.invoiceNo; Index = $index }
    } catch {
        return @{ Success = $false; Error = $_.Exception.Message; Index = $index }
    }
}

Write-Host "=== Concurrent Invoice Creation Test ===" -ForegroundColor Cyan
Write-Host "Creating $concurrentRequests invoices simultaneously..." -ForegroundColor Yellow
Write-Host ""

# Create invoices concurrently
$jobs = @()
for ($i = 1; $i -le $concurrentRequests; $i++) {
    $jobs += Start-Job -ScriptBlock ${function:Create-Invoice} -ArgumentList $i
}

# Wait for all jobs to complete
$results = $jobs | Wait-Job | Receive-Job
$jobs | Remove-Job

# Analyze results
$successful = $results | Where-Object { $_.Success }
$failed = $results | Where-Object { -not $_.Success }
$invoiceNumbers = $successful | ForEach-Object { $_.InvoiceNo } | Sort-Object

Write-Host "Results:" -ForegroundColor Cyan
Write-Host "  Successful: $($successful.Count)" -ForegroundColor Green
Write-Host "  Failed: $($failed.Count)" -ForegroundColor $(if ($failed.Count -eq 0) { "Green" } else { "Red" })
Write-Host ""

# Check for duplicates
$uniqueNumbers = $invoiceNumbers | Select-Object -Unique
$duplicates = $invoiceNumbers.Count - $uniqueNumbers.Count

if ($duplicates -eq 0) {
    Write-Host "✅ PASS: No duplicate invoice numbers found" -ForegroundColor Green
    Write-Host "  All $($successful.Count) invoices have unique numbers" -ForegroundColor Gray
} else {
    Write-Host "❌ FAIL: Found $duplicates duplicate invoice number(s)!" -ForegroundColor Red
    Write-Host "  This indicates a race condition in invoice number generation" -ForegroundColor Red
}

Write-Host ""
Write-Host "Invoice Numbers Created:" -ForegroundColor Cyan
$invoiceNumbers | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }

if ($failed.Count -gt 0) {
    Write-Host ""
    Write-Host "Failed Requests:" -ForegroundColor Red
    $failed | ForEach-Object { Write-Host "  Index $($_.Index): $($_.Error)" -ForegroundColor Red }
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Cyan
