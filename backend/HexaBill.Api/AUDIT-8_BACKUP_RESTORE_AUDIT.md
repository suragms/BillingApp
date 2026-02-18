# AUDIT-8: Backup & Restore Audit

**Status:** ✅ COMPLETED  
**Date:** 2026-02-18

---

## AUDIT SCOPE

Checked:
- ✅ Per-tenant backup isolation
- ✅ Restore validation
- ✅ Schema validation
- ✅ Transaction safety
- ✅ CSV import mapping
- ✅ Payment status preservation
- ✅ Paid amount preservation
- ✅ Balance recalculation after restore

---

## FINDINGS

### 🔴 **CRITICAL SECURITY ISSUES FOUND:**

#### **ISSUE #1: Backup is System-Wide, Not Per-Tenant**

**Location:** `ComprehensiveBackupService.cs` - Multiple methods

**Problem:**
```csharp
// Line 617: BackupCsvExportsAsync
var customers = await _context.Customers.ToListAsync(); // ❌ NO TenantId filter

// Line 622: BackupCsvExportsAsync
var sales = await _context.Sales
    .Include(s => s.Items)
    .Where(s => !s.IsDeleted)
    .ToListAsync(); // ❌ NO TenantId filter

// Line 531: ExportPostgreSQLViaEfCoreAsync
await ExportTableAsync(writer, "Products", async () => await _context.Products.ToListAsync()); // ❌ NO TenantId filter

// Line 766: BackupUsersAsync
var users = await _context.Users.ToListAsync(); // ❌ NO TenantId filter

// Line 784: BackupSettingsAsync
var settings = await _context.Settings.ToListAsync(); // ❌ NO TenantId filter
```

**Issues:**
- ❌ **Backup includes ALL tenants' data** - Security risk
- ❌ **No TenantId filtering** - Multi-tenant data leakage
- ❌ **Backup can be accessed by any tenant** - Data exposure risk

**Impact:** 🔴 **CRITICAL SECURITY RISK** - Backup contains all tenants' data

**Risk Scenario:**
- Tenant A creates backup → Backup contains Tenant B, C, D data
- Tenant A downloads backup → Can see all other tenants' data
- Backup file stored on server → Other tenants could potentially access it

**Recommendation:**
```csharp
public async Task<string> CreateFullBackupAsync(int tenantId, bool exportToDesktop = false, ...)
{
    // Filter all queries by TenantId
    var customers = await _context.Customers
        .Where(c => c.TenantId == tenantId)
        .ToListAsync();
    
    var sales = await _context.Sales
        .Where(s => s.TenantId == tenantId && !s.IsDeleted)
        .ToListAsync();
    
    // ... filter all other entities by TenantId
}
```

**Priority:** 🔴 **CRITICAL** - Must fix immediately

---

#### **ISSUE #2: Restore Doesn't Validate Tenant Isolation**

**Location:** `ComprehensiveBackupService.cs` - `RestoreFromBackupAsync` (Line 1101)

**Problem:**
```csharp
public async Task<bool> RestoreFromBackupAsync(string backupFilePath, string? uploadedFilePath = null)
{
    // ... extract backup ...
    
    // Restore database - NO tenant validation
    await RestoreDatabaseAsync(dbFile); // ❌ Can restore ANY tenant's data
    
    // Restore storage files - NO tenant validation
    await CopyDirectoryAsync(storageSource, storageDest); // ❌ Can overwrite other tenants' files
}
```

**Issues:**
- ❌ **No tenant validation** - Can restore backup from different tenant
- ❌ **Can override other tenants' data** - Data corruption risk
- ❌ **No tenant ID check** - Restore doesn't verify backup belongs to current tenant

**Impact:** 🔴 **CRITICAL SECURITY RISK** - Restore can corrupt other tenants' data

**Risk Scenario:**
- Tenant A uploads Tenant B's backup
- Restore overwrites Tenant B's data with Tenant A's data
- Tenant B's data is lost/corrupted

**Recommendation:**
```csharp
public async Task<bool> RestoreFromBackupAsync(int tenantId, string backupFilePath, ...)
{
    // 1. Read manifest and validate TenantId
    var manifest = ReadManifest(backupFilePath);
    if (manifest.TenantId != tenantId)
    {
        throw new UnauthorizedAccessException("Backup does not belong to this tenant");
    }
    
    // 2. Wrap restore in transaction
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // 3. Only restore data for this tenant
        await RestoreDatabaseForTenantAsync(dbFile, tenantId);
        
        // 4. Recalculate balances
        await RecalculateAllCustomerBalancesAsync(tenantId);
        
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**Priority:** 🔴 **CRITICAL** - Must fix immediately

---

#### **ISSUE #3: Restore Not Wrapped in Transaction**

**Location:** `ComprehensiveBackupService.cs` - `RestoreFromBackupAsync` (Line 1101)

**Problem:**
```csharp
public async Task<bool> RestoreFromBackupAsync(string backupFilePath, string? uploadedFilePath = null)
{
    // ... extract backup ...
    
    // Restore database - NO transaction
    await RestoreDatabaseAsync(dbFile); // ❌ No rollback on failure
    
    // Restore storage files - NO transaction
    await CopyDirectoryAsync(storageSource, storageDest); // ❌ Partial restore possible
    
    // Restore settings - NO transaction
    await RestoreSettingsAsync(settingsFile); // ❌ Partial restore possible
}
```

**Issues:**
- ❌ **No transaction wrapping** - Partial restore possible
- ❌ **No rollback on failure** - Data corruption risk
- ❌ **Database and files restored separately** - Inconsistent state possible

**Impact:** 🔴 **CRITICAL** - Data corruption risk if restore fails mid-way

**Risk Scenario:**
- Restore database successfully
- Restore storage files fails
- Database restored but files missing → Inconsistent state
- No way to rollback → Data corruption

**Recommendation:**
```csharp
using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // Backup current database first
    await BackupCurrentDatabaseAsync();
    
    // Restore database
    await RestoreDatabaseAsync(dbFile);
    
    // Restore storage files
    await CopyDirectoryAsync(storageSource, storageDest);
    
    // Restore settings
    await RestoreSettingsAsync(settingsFile);
    
    // Recalculate balances
    await RecalculateAllCustomerBalancesAsync(tenantId);
    
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    // Restore previous database backup
    await RestorePreviousDatabaseBackupAsync();
    throw;
}
```

**Priority:** 🔴 **CRITICAL** - Must add transaction wrapping

---

#### **ISSUE #4: No Schema Validation Before Restore**

**Location:** `ComprehensiveBackupService.cs` - `RestoreFromBackupAsync` (Line 1101)

**Problem:**
```csharp
public async Task<bool> RestoreFromBackupAsync(string backupFilePath, string? uploadedFilePath = null)
{
    // ... extract backup ...
    
    // Restore database - NO schema validation
    await RestoreDatabaseAsync(dbFile); // ❌ No version check
    
    // No check if backup schema matches current schema
}
```

**Issues:**
- ❌ **No schema version check** - Can restore incompatible backup
- ❌ **No migration validation** - Restore may fail or corrupt data
- ⚠️ **PreviewImportAsync checks schema** - But restore doesn't use it

**Impact:** 🟡 **MEDIUM** - Restore may fail or corrupt data

**Recommendation:**
```csharp
// Check schema version before restore
var manifest = ReadManifest(backupFilePath);
var currentSchemaVersion = "1.0"; // Get from migrations

if (manifest.SchemaVersion != currentSchemaVersion)
{
    throw new InvalidOperationException(
        $"Schema version mismatch. Backup: {manifest.SchemaVersion}, Current: {currentSchemaVersion}");
}

// Check if migrations are up to date
var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
if (pendingMigrations.Any())
{
    throw new InvalidOperationException(
        $"Pending migrations detected. Apply migrations before restore.");
}
```

**Priority:** 🟡 **MEDIUM** - Should add schema validation

---

#### **ISSUE #5: UpsertTableDataAsync Doesn't Filter by TenantId**

**Location:** `ComprehensiveBackupService.cs` - `UpsertTableDataAsync` (Line 1401)

**Problem:**
```csharp
private async Task UpsertTableDataAsync(string tableName, string jsonData)
{
    switch (tableName)
    {
        case "Products":
            var products = JsonSerializer.Deserialize<List<Product>>(jsonData);
            foreach (var item in products)
            {
                var existing = await _context.Products.FindAsync(item.Id);
                if (existing != null)
                {
                    _context.Entry(existing).CurrentValues.SetValues(item); // ❌ Can overwrite other tenant's product
                }
                else
                {
                    _context.Products.Add(item); // ❌ Can add product with wrong TenantId
                }
            }
            break;
        // ... same for all tables
    }
}
```

**Issues:**
- ❌ **No TenantId validation** - Can overwrite other tenants' data
- ❌ **Uses FindAsync** - Doesn't check TenantId before update
- ❌ **Can add records with wrong TenantId** - Data leakage risk

**Impact:** 🔴 **CRITICAL SECURITY RISK** - Can corrupt other tenants' data

**Recommendation:**
```csharp
private async Task UpsertTableDataAsync(string tableName, string jsonData, int tenantId)
{
    switch (tableName)
    {
        case "Products":
            var products = JsonSerializer.Deserialize<List<Product>>(jsonData);
            foreach (var item in products)
            {
                // Validate TenantId matches
                if (item.TenantId != tenantId)
                {
                    throw new UnauthorizedAccessException(
                        $"Product {item.Id} belongs to tenant {item.TenantId}, not {tenantId}");
                }
                
                // Use FirstOrDefaultAsync with TenantId filter
                var existing = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == item.Id && p.TenantId == tenantId);
                
                if (existing != null)
                {
                    _context.Entry(existing).CurrentValues.SetValues(item);
                }
                else
                {
                    _context.Products.Add(item);
                }
            }
            break;
    }
}
```

**Priority:** 🔴 **CRITICAL** - Must add TenantId validation

---

#### **ISSUE #6: Balance Recalculation Not Called After Restore**

**Location:** `ComprehensiveBackupService.cs` - `RestoreFromBackupAsync` (Line 1101)

**Problem:**
```csharp
public async Task<bool> RestoreFromBackupAsync(string backupFilePath, string? uploadedFilePath = null)
{
    // ... restore database ...
    
    // ❌ NO balance recalculation
    // Customer balances may be incorrect after restore
}
```

**Issues:**
- ❌ **No balance recalculation** - Customer balances may be stale
- ❌ **No invoice status recalculation** - PaymentStatus may be incorrect
- ❌ **No PaidAmount recalculation** - PaidAmount may be incorrect

**Impact:** 🟡 **MEDIUM** - Data inconsistency after restore

**Recommendation:**
```csharp
// After restore, recalculate all customer balances
var customerService = scope.ServiceProvider.GetRequiredService<ICustomerService>();
var customers = await _context.Customers
    .Where(c => c.TenantId == tenantId)
    .Select(c => c.Id)
    .ToListAsync();

foreach (var customerId in customers)
{
    await customerService.RecalculateCustomerBalanceAsync(customerId, tenantId);
    await customerService.RecalculateCustomerInvoiceStatusesAsync(customerId, tenantId);
}
```

**Priority:** 🟡 **MEDIUM** - Should recalculate balances after restore

---

### ✅ **GOOD PATTERNS FOUND:**

#### 1. **CSV Import Uses Transaction**

**Location:** `SalesLedgerImportService.cs` - `ApplyImportAsync` (Line 151)

**Implementation:**
```csharp
await using var transaction = await _context.Database.BeginTransactionAsync();
try
{
    // ... import data ...
    await transaction.CommitAsync();
}
catch (Exception ex)
{
    await transaction.RollbackAsync();
    // ... handle error ...
}
```

**Status:** ✅ **EXCELLENT** - Proper transaction wrapping

---

#### 2. **CSV Import Validates Mapping**

**Location:** `SalesLedgerImportService.cs` - `ApplyImportAsync` (Line 155)

**Implementation:**
```csharp
if (!map.TryGetValue("invoiceNo", out var invCol) || !map.TryGetValue("customerName", out var custCol))
{
    res.Errors.Add("Column mapping must include invoiceNo and customerName.");
    return res;
}
```

**Status:** ✅ **EXCELLENT** - Validates required columns

---

#### 3. **CSV Import Preserves Payment Status**

**Location:** `SalesLedgerImportService.cs` - `ApplyImportAsync` (Line 239-245)

**Implementation:**
```csharp
var isCash = paymentType.Contains("CASH") && !paymentType.Contains("CREDIT");
if (isCash)
{
    sale.PaidAmount = grandTotal;
    sale.PaymentStatus = SalePaymentStatus.Paid;
    sale.LastPaymentDate = invoiceDate;
}
```

**Status:** ✅ **GOOD** - Preserves payment status correctly

---

#### 4. **CSV Import Updates Customer Balance**

**Location:** `SalesLedgerImportService.cs` - `ApplyImportAsync` (Line 285-288)

**Implementation:**
```csharp
customer.Balance += sale.GrandTotal;
if (sale.PaidAmount > 0)
    customer.Balance -= sale.PaidAmount;
await _context.SaveChangesAsync();
```

**Status:** ✅ **GOOD** - Updates customer balance

**Note:** ⚠️ But should use `RecalculateCustomerBalanceAsync` instead of manual calculation

---

#### 5. **PreviewImportAsync Validates Schema**

**Location:** `ComprehensiveBackupService.cs` - `PreviewImportAsync` (Line 862)

**Implementation:**
```csharp
// Check schema compatibility
var currentSchemaVersion = "1.0";
preview.IsCompatible = preview.Manifest.SchemaVersion == currentSchemaVersion;
if (!preview.IsCompatible)
{
    preview.CompatibilityMessage = $"Schema version mismatch...";
}
```

**Status:** ✅ **GOOD** - Validates schema version

**Note:** ⚠️ But restore doesn't use this validation

---

## RESTORE FLOW ANALYSIS

### **Current Restore Flow:**

1. Extract ZIP file ✅
2. Read manifest ✅
3. Restore database ❌ (No tenant validation, no transaction)
4. Restore storage files ❌ (No tenant validation)
5. Restore settings ❌ (No tenant validation)
6. Create audit log ✅
7. **Missing:** Balance recalculation ❌
8. **Missing:** Transaction rollback ❌

### **Recommended Restore Flow:**

1. Extract ZIP file ✅
2. Read manifest ✅
3. **Validate TenantId** 🔴 (MISSING)
4. **Validate schema version** 🟡 (MISSING)
5. **Backup current database** 🟡 (MISSING)
6. **Begin transaction** 🔴 (MISSING)
7. Restore database (filtered by TenantId) 🔴 (MISSING)
8. Restore storage files (filtered by TenantId) 🔴 (MISSING)
9. Restore settings (filtered by TenantId) 🔴 (MISSING)
10. **Recalculate customer balances** 🟡 (MISSING)
11. **Recalculate invoice statuses** 🟡 (MISSING)
12. **Commit transaction** 🔴 (MISSING)
13. Create audit log ✅

---

## BACKUP FLOW ANALYSIS

### **Current Backup Flow:**

1. Create ZIP archive ✅
2. Backup database (ALL tenants) 🔴 (SECURITY RISK)
3. Backup CSV exports (ALL tenants) 🔴 (SECURITY RISK)
4. Backup invoices (ALL tenants) 🔴 (SECURITY RISK)
5. Backup storage files (ALL tenants) 🔴 (SECURITY RISK)
6. Backup settings (ALL tenants) 🔴 (SECURITY RISK)
7. Create manifest ✅
8. **Missing:** TenantId in manifest 🔴 (MISSING)

### **Recommended Backup Flow:**

1. **Validate TenantId** 🔴 (MISSING)
2. Create ZIP archive ✅
3. Backup database (filtered by TenantId) 🔴 (MISSING)
4. Backup CSV exports (filtered by TenantId) 🔴 (MISSING)
5. Backup invoices (filtered by TenantId) 🔴 (MISSING)
6. Backup storage files (filtered by TenantId) 🔴 (MISSING)
7. Backup settings (filtered by TenantId) 🔴 (MISSING)
8. Create manifest ✅
9. **Include TenantId in manifest** 🔴 (MISSING)

---

## CSV IMPORT ANALYSIS

### **CSV Import Flow:**

1. Parse file ✅
2. Validate column mapping ✅
3. **Begin transaction** ✅
4. Create/update customers ✅
5. Create sales ✅
6. Create payments ✅
7. Update customer balance ✅
8. **Commit transaction** ✅

**Status:** ✅ **EXCELLENT** - Proper transaction handling

**Minor Issues:**
- ⚠️ Manual balance calculation instead of `RecalculateCustomerBalanceAsync`
- ⚠️ Should recalculate all customer balances after import completes

---

## RECOMMENDATIONS

### 🔴 **CRITICAL PRIORITY:**

1. **Add TenantId Parameter to Backup Methods**
   ```csharp
   public async Task<string> CreateFullBackupAsync(int tenantId, bool exportToDesktop = false, ...)
   {
       // Filter ALL queries by TenantId
       var customers = await _context.Customers
           .Where(c => c.TenantId == tenantId)
           .ToListAsync();
       // ... filter all other entities
   }
   ```

2. **Add TenantId Validation to Restore**
   ```csharp
   public async Task<bool> RestoreFromBackupAsync(int tenantId, string backupFilePath, ...)
   {
       // Validate backup belongs to tenant
       var manifest = ReadManifest(backupFilePath);
       if (manifest.TenantId != tenantId)
           throw new UnauthorizedAccessException("Backup does not belong to this tenant");
       
       // Restore only this tenant's data
   }
   ```

3. **Wrap Restore in Transaction**
   ```csharp
   using var transaction = await _context.Database.BeginTransactionAsync();
   try
   {
       // Restore operations
       await transaction.CommitAsync();
   }
   catch
   {
       await transaction.RollbackAsync();
       throw;
   }
   ```

4. **Add TenantId Filter to UpsertTableDataAsync**
   ```csharp
   private async Task UpsertTableDataAsync(string tableName, string jsonData, int tenantId)
   {
       // Validate and filter by TenantId
   }
   ```

### 🟡 **MEDIUM PRIORITY:**

5. **Add Schema Validation Before Restore**
   - Check schema version matches
   - Check pending migrations
   - Validate backup compatibility

6. **Recalculate Balances After Restore**
   - Call `RecalculateCustomerBalanceAsync` for all customers
   - Call `RecalculateCustomerInvoiceStatusesAsync` for all customers
   - Ensure PaidAmount is correct

7. **Add TenantId to Manifest**
   - Include TenantId in backup manifest
   - Validate TenantId on restore

### 🟢 **LOW PRIORITY:**

8. **Improve CSV Import Balance Calculation**
   - Use `RecalculateCustomerBalanceAsync` instead of manual calculation
   - Recalculate all balances after import completes

9. **Add Dry-Run Restore Preview**
   - Preview what will be restored
   - Show conflicts before restore
   - Allow user to resolve conflicts

---

## CONCLUSION

**Overall Status:** 🔴 **CRITICAL ISSUES FOUND**

**Strengths:**
- ✅ CSV import uses transactions
- ✅ CSV import validates mapping
- ✅ CSV import preserves payment status
- ✅ PreviewImportAsync validates schema

**Critical Issues:**
- 🔴 Backup is system-wide, not per-tenant (SECURITY RISK)
- 🔴 Restore doesn't validate tenant isolation (SECURITY RISK)
- 🔴 Restore not wrapped in transaction (DATA CORRUPTION RISK)
- 🔴 UpsertTableDataAsync doesn't filter by TenantId (SECURITY RISK)
- 🟡 No balance recalculation after restore (DATA INCONSISTENCY)
- 🟡 No schema validation before restore (COMPATIBILITY RISK)

**Security Risk:** 🔴 **CRITICAL** - Backup/restore can leak or corrupt multi-tenant data

**Data Integrity Risk:** 🔴 **CRITICAL** - Restore can corrupt data without rollback

**Critical Issues:** 4 found 🔴

---

**Last Updated:** 2026-02-18  
**Next Review:** After implementing critical fixes
