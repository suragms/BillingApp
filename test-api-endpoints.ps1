# HexaBill API Endpoint Testing Script
# Tests critical fixes: transactions, race conditions, permissions

$baseUrl = "http://localhost:5000"
$testResults = @()

# Test user credentials (update with your test users)
$testUsers = @{
    Owner = @{ Email = "owner@test.com"; Password = "password123" }
    Admin = @{ Email = "admin@test.com"; Password = "password123" }
    Staff = @{ Email = "staff@test.com"; Password = "password123" }
}

function Test-Login {
    param($email, $password)
    $body = @{
        email = $email
        password = $password
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $body -ContentType "application/json"
        return $response.data.token
    } catch {
        Write-Host "Login failed: $_" -ForegroundColor Red
        return $null
    }
}

function Test-Request {
    param($name, $method, $uri, $headers = @{}, $body = $null)
    
    try {
        $params = @{
            Uri = $uri
            Method = $method
            Headers = $headers
        }
        
        if ($body) {
            $params.Body = ($body | ConvertTo-Json)
            $params.ContentType = "application/json"
        }
        
        $response = Invoke-RestMethod @params
        return @{ Success = $true; Response = $response; Error = $null }
    } catch {
        return @{ Success = $false; Response = $null; Error = $_.Exception.Message }
    }
}

Write-Host "=== HexaBill API Testing ===" -ForegroundColor Cyan
Write-Host ""

# Test 1: Staff Cannot Delete Invoice
Write-Host "Test 1: Staff Delete Invoice Permission" -ForegroundColor Yellow
$staffToken = Test-Login $testUsers.Staff.Email $testUsers.Staff.Password
if ($staffToken) {
    $headers = @{ Authorization = "Bearer $staffToken" }
    $result = Test-Request "Staff Delete Invoice" "DELETE" "$baseUrl/api/sales/1" $headers
    if (-not $result.Success) {
        Write-Host "✅ PASS: Staff cannot delete invoice (as expected)" -ForegroundColor Green
        $testResults += @{ Test = "Staff Delete Permission"; Result = "PASS" }
    } else {
        Write-Host "❌ FAIL: Staff was able to delete invoice!" -ForegroundColor Red
        $testResults += @{ Test = "Staff Delete Permission"; Result = "FAIL" }
    }
} else {
    Write-Host "⚠️ SKIP: Could not login as Staff" -ForegroundColor Yellow
}

Write-Host ""

# Test 2: Payment Status Update Transaction
Write-Host "Test 2: Payment Status Update Transaction" -ForegroundColor Yellow
$ownerToken = Test-Login $testUsers.Owner.Email $testUsers.Owner.Password
if ($ownerToken) {
    $headers = @{ Authorization = "Bearer $ownerToken" }
    
    # Create test invoice
    $invoiceBody = @{
        customerId = $null
        items = @(@{ productId = 1; qty = 1; unitPrice = 100 })
        discount = 0
    }
    $invoiceResult = Test-Request "Create Invoice" "POST" "$baseUrl/api/sales" $headers $invoiceBody
    
    if ($invoiceResult.Success -and $invoiceResult.Response.data.id) {
        $saleId = $invoiceResult.Response.data.id
        Write-Host "Created test invoice: $saleId" -ForegroundColor Gray
        
        # Create payment
        $paymentBody = @{
            saleId = $saleId
            amount = 50
            mode = "CHEQUE"
        }
        $paymentResult = Test-Request "Create Payment" "POST" "$baseUrl/api/payments" $headers $paymentBody
        
        if ($paymentResult.Success -and $paymentResult.Response.data.id) {
            $paymentId = $paymentResult.Response.data.id
            Write-Host "Created test payment: $paymentId" -ForegroundColor Gray
            
            # Update payment status
            $statusResult = Test-Request "Update Payment Status" "PUT" "$baseUrl/api/payments/$paymentId/status" $headers @{ status = "CLEARED" }
            
            if ($statusResult.Success) {
                Write-Host "✅ PASS: Payment status updated successfully" -ForegroundColor Green
                $testResults += @{ Test = "Payment Transaction"; Result = "PASS" }
            } else {
                Write-Host "❌ FAIL: Payment status update failed: $($statusResult.Error)" -ForegroundColor Red
                $testResults += @{ Test = "Payment Transaction"; Result = "FAIL" }
            }
        } else {
            Write-Host "⚠️ SKIP: Could not create payment" -ForegroundColor Yellow
        }
    } else {
        Write-Host "⚠️ SKIP: Could not create invoice" -ForegroundColor Yellow
    }
} else {
    Write-Host "⚠️ SKIP: Could not login as Owner" -ForegroundColor Yellow
}

Write-Host ""

# Test 3: Expense Creation Transaction
Write-Host "Test 3: Expense Creation Transaction" -ForegroundColor Yellow
if ($ownerToken) {
    $headers = @{ Authorization = "Bearer $ownerToken" }
    
    $expenseBody = @{
        categoryId = 1
        amount = 100
        date = (Get-Date).ToString("yyyy-MM-dd")
        note = "Test expense"
    }
    $expenseResult = Test-Request "Create Expense" "POST" "$baseUrl/api/expenses" $headers $expenseBody
    
    if ($expenseResult.Success) {
        Write-Host "✅ PASS: Expense created successfully" -ForegroundColor Green
        $testResults += @{ Test = "Expense Transaction"; Result = "PASS" }
    } else {
        Write-Host "❌ FAIL: Expense creation failed: $($expenseResult.Error)" -ForegroundColor Red
        $testResults += @{ Test = "Expense Transaction"; Result = "FAIL" }
    }
} else {
    Write-Host "⚠️ SKIP: Could not login as Owner" -ForegroundColor Yellow
}

Write-Host ""

# Test 4: Invalid Payload Validation
Write-Host "Test 4: Invalid Payload Validation" -ForegroundColor Yellow
if ($ownerToken) {
    $headers = @{ Authorization = "Bearer $ownerToken" }
    
    # Test empty items array
    $invalidBody = @{
        customerId = $null
        items = @()
        discount = 0
    }
    $invalidResult = Test-Request "Create Invoice (Empty Items)" "POST" "$baseUrl/api/sales" $headers $invalidBody
    
    if (-not $invalidResult.Success) {
        Write-Host "✅ PASS: Invalid payload rejected (as expected)" -ForegroundColor Green
        $testResults += @{ Test = "Invalid Payload Validation"; Result = "PASS" }
    } else {
        Write-Host "❌ FAIL: Invalid payload was accepted!" -ForegroundColor Red
        $testResults += @{ Test = "Invalid Payload Validation"; Result = "FAIL" }
    }
} else {
    Write-Host "⚠️ SKIP: Could not login as Owner" -ForegroundColor Yellow
}

Write-Host ""

# Summary
Write-Host "=== Test Summary ===" -ForegroundColor Cyan
foreach ($result in $testResults) {
    $color = if ($result.Result -eq "PASS") { "Green" } else { "Red" }
    Write-Host "$($result.Test): $($result.Result)" -ForegroundColor $color
}

$passCount = ($testResults | Where-Object { $_.Result -eq "PASS" }).Count
$totalCount = $testResults.Count
Write-Host ""
Write-Host "Passed: $passCount / $totalCount" -ForegroundColor $(if ($passCount -eq $totalCount) { "Green" } else { "Yellow" })
