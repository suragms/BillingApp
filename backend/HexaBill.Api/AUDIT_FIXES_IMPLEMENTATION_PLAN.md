# Audit Fixes Implementation Plan

**Date:** 2026-02-18  
**Status:** 🔄 IN PROGRESS

## Priority Order (Critical First)

### 🔴 **CRITICAL PRIORITY (Security & Data Integrity)**

#### ✅ **FIX-1: Backup Per-Tenant Filtering** - PARTIALLY FIXED
**Status:** ✅ **MOSTLY COMPLETE** - Need to fix file backup methods
- ✅ `CreateFullBackupAsync` - Takes tenantId parameter
- ✅ `ExportPostgreSQLViaEfCoreAsync` - Filters by tenantId
- ✅ `BackupCsvExportsAsync` - Filters by tenantId
- ✅ `BackupUsersAsync` - Filters by tenantId
- ✅ `BackupSettingsAsync` - Filters by tenantId
- ✅ `CreateManifestAsync` - Includes tenantId
- ❌ `BackupInvoicesAsync` - Needs tenantId filtering for invoice files
- ❌ `BackupUploadedFilesAsync` - Needs tenantId filtering for storage files
- ❌ `BackupCustomerStatementsAsync` - Needs tenantId filtering
- ❌ `BackupMonthlySalesLedgerAsync` - Uses tenantId=0, should use actual tenantId
- ❌ `BackupReportsAsync` - Needs tenantId filtering

**Action Required:** Add tenantId parameter to file backup methods and filter files by tenant

---

#### ✅ **FIX-2: Restore Tenant Validation** - FIXED
**Status:** ✅ **COMPLETE**
- ✅ `RestoreFromBackupAsync` - Takes tenantId parameter
- ✅ Validates manifest.TenantId matches tenantId
- ✅ Throws UnauthorizedAccessException if mismatch

**Action Required:** None - Already fixed

---

#### ✅ **FIX-3: Restore Transaction Wrapping** - FIXED
**Status:** ✅ **COMPLETE**
- ✅ `RestoreFromBackupAsync` - Wrapped in transaction
- ✅ Rollback on failure
- ✅ Commit on success

**Action Required:** None - Already fixed

---

#### ✅ **FIX-4: UpsertTableDataAsync TenantId Filter** - FIXED
**Status:** ✅ **COMPLETE**
- ✅ `UpsertTableDataAsync` - Takes tenantId parameter
- ✅ Validates TenantId before upsert
- ✅ Uses FirstOrDefaultAsync with TenantId filter
- ✅ Skips records with TenantId mismatch

**Action Required:** None - Already fixed

---

#### ✅ **FIX-5: Balance Recalculation After Restore** - FIXED
**Status:** ✅ **COMPLETE**
- ✅ Calls `RecalculateAllCustomerBalancesAsync` after restore
- ✅ Executes within transaction

**Action Required:** None - Already fixed

---

### 🟡 **MEDIUM PRIORITY (Performance & Data Consistency)**

#### **FIX-6: Add Pagination to GetLowStockProductsAsync**
**Status:** ❌ **PENDING**
**Location:** `ProductService.cs` - Line 546
**Action:** Add pagination parameters and return PagedResponse

---

#### **FIX-7: Add Pagination to GetOutstandingCustomersAsync**
**Status:** ❌ **PENDING**
**Location:** `ReportService.cs` - Line 943
**Action:** Add pagination parameters and return PagedResponse

---

#### **FIX-8: Add Pagination to GetChequeReportAsync**
**Status:** ❌ **PENDING**
**Location:** `ReportService.cs` - Line 986
**Action:** Add pagination parameters and return PagedResponse

---

#### **FIX-9: Add Pagination to GetPendingBillsAsync**
**Status:** ❌ **PENDING**
**Location:** `ReportService.cs` - Line 1201
**Action:** Add pagination parameters and return PagedResponse

---

#### **FIX-10: Route Branch Change Validation**
**Status:** ❌ **PENDING**
**Location:** `RouteService.cs` - `UpdateRouteAsync` (Line 158)
**Action:** Add validation to prevent branch change if route has customers/sales

---

## Implementation Status Summary

### ✅ **COMPLETED FIXES (5/10)**
1. ✅ Restore Tenant Validation
2. ✅ Restore Transaction Wrapping
3. ✅ UpsertTableDataAsync TenantId Filter
4. ✅ Balance Recalculation After Restore
5. ✅ Backup Per-Tenant Filtering (Database & CSV) - PARTIAL

### ❌ **PENDING FIXES (5/10)**
1. ❌ Backup File Methods TenantId Filtering (Invoices, Uploads, Statements, Reports)
2. ❌ GetLowStockProductsAsync Pagination
3. ❌ GetOutstandingCustomersAsync Pagination
4. ❌ GetChequeReportAsync Pagination
5. ❌ GetPendingBillsAsync Pagination
6. ❌ Route Branch Change Validation

---

## Next Steps

1. Fix remaining backup file methods (tenantId filtering)
2. Add pagination to 4 report endpoints
3. Add route branch change validation
4. Test all fixes
5. Update production documentation
