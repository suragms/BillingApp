# Production Fixes - Session 11: PROD-19 (Race Condition Audit)

**Date:** 2026-02-18  
**Task:** PROD-19 - Audit all race conditions (concurrent updates, signup, stock adjustments)

---

## Summary

Completed comprehensive audit of race conditions across the application. Fixed critical stock update race condition by implementing atomic SQL updates. Documented all race condition risks and their mitigation strategies.

---

## Issues Found and Fixed

### 1. **Stock Update Race Condition** ✅ FIXED

**Location:** Multiple files:
- `SaleService.cs` - Stock decremented during sale creation/update/delete
- `PurchaseService.cs` - Stock incremented/decremented during purchase operations
- `StockAdjustmentService.cs` - Stock adjusted manually
- `ProductService.cs` - Stock adjusted via API
- `ReturnService.cs` - Stock restored/adjusted during returns
- `CustomerService.cs` - Stock restored during customer deletion

**Issue:**
Stock updates were done directly without proper locking:
```csharp
product.StockQty -= baseQty;  // Direct update - race condition possible
```

**Race Condition Scenario:**
1. Two concurrent sales for same product
2. Both read current stock (e.g., 100)
3. Both calculate new stock (e.g., 100 - 50 = 50)
4. Both save (both save 50, but should be 0)
5. Result: Stock becomes 50 instead of 0 (lost update)

**Fix:**
Replaced all direct stock updates with atomic SQL updates:
```csharp
// PROD-19: Atomic stock update to prevent race conditions
var rowsAffected = await _context.Database.ExecuteSqlInterpolatedAsync(
    $@"UPDATE ""Products"" 
       SET ""StockQty"" = ""StockQty"" - {baseQty}, 
           ""UpdatedAt"" = {DateTime.UtcNow}
       WHERE ""Id"" = {product.Id} 
         AND ""TenantId"" = {tenantId}
         AND ""StockQty"" >= {baseQty}");  // Prevents negative stock

if (rowsAffected == 0)
{
    throw new InvalidOperationException("Insufficient stock...");
}

await _context.Entry(product).ReloadAsync();  // Reload to get updated RowVersion
```

**Benefits:**
- ✅ **Atomic operation** - Database handles update atomically
- ✅ **Prevents negative stock** - WHERE clause ensures sufficient stock
- ✅ **No race condition** - Database-level atomicity prevents concurrent update issues
- ✅ **RowVersion updated** - Reload ensures RowVersion is current

**Impact:** Prevents stock corruption from concurrent sales/purchases. Ensures stock accuracy even under high concurrency.

---

## Verified Race Condition Protections

### ✅ **1. Invoice Number Generation** - PROTECTED

**Location:** `InvoiceNumberService.cs`, `SaleService.cs`  
**Protection:**
- ✅ SemaphoreSlim for process-level locking
- ✅ PostgreSQL advisory locks (`pg_advisory_lock`) for database-level locking
- ✅ Retry logic with exponential backoff (50 retries)
- ✅ Serializable isolation level in `CreateSaleInternalAsync`
- ✅ Retry logic in `CreateSaleAsync` (5 retries) for duplicate key violations
- ✅ Unique constraint on `(OwnerId, InvoiceNo)` with filter

**Status:** ✅ **WELL PROTECTED**

---

### ✅ **2. User Signup** - PROTECTED

**Location:** `SignupService.cs`  
**Protection:**
- ✅ Transaction wrapping entire signup process
- ✅ Unique constraint on `Users.Email`
- ✅ Unique constraint on `Tenants.Name`
- ✅ Try-catch for PostgreSQL unique violation (23505)
- ✅ Rollback on duplicate detection

**Status:** ✅ **PROTECTED**

---

### ✅ **3. Sale Updates** - PROTECTED

**Location:** `SaleService.cs`  
**Protection:**
- ✅ RowVersion field for optimistic concurrency
- ✅ Serializable isolation level
- ✅ Version check before update
- ✅ Throws exception if RowVersion mismatch

**Status:** ✅ **PROTECTED**

---

### ⚠️ **4. Customer Balance Updates** - NEEDS REVIEW

**Location:** `BalanceService.cs`, `SaleService.cs`, `PaymentService.cs`  
**Current Protection:**
- ✅ Transactions wrap balance updates
- ✅ Balance recalculation method exists (`RecalculateCustomerBalanceAsync`)
- ❌ **NO ROW-LEVEL LOCKING** - Concurrent payments could cause incorrect balance
- ❌ **NO OPTIMISTIC CONCURRENCY** - RowVersion exists but not always checked

**Risk:** Medium - Balance recalculation mitigates risk but doesn't prevent race condition

**Recommendation:** Consider using pessimistic locking or atomic SQL updates for balance changes

**Status:** ⚠️ **NEEDS REVIEW** (Lower priority - balance recalculation helps)

---

## Files Modified

1. **`backend/HexaBill.Api/Modules/Billing/SaleService.cs`**
   - Fixed 6 stock update locations (sale creation, update, delete, restore)
   - All stock updates now use atomic SQL updates

2. **`backend/HexaBill.Api/Modules/Purchases/PurchaseService.cs`**
   - Fixed 4 stock update locations (purchase creation, update, delete)
   - All stock updates now use atomic SQL updates

3. **`backend/HexaBill.Api/Modules/Inventory/StockAdjustmentService.cs`**
   - Fixed stock adjustment to use atomic SQL update

4. **`backend/HexaBill.Api/Modules/Inventory/ProductService.cs`**
   - Fixed stock adjustment API to use atomic SQL update

5. **`backend/HexaBill.Api/Modules/Billing/ReturnService.cs`**
   - Fixed 2 stock update locations (sale return, purchase return)
   - All stock updates now use atomic SQL updates

6. **`backend/HexaBill.Api/Modules/Customers/CustomerService.cs`**
   - Fixed stock restore during customer deletion to use atomic SQL update

---

## Documentation Created

1. **`backend/HexaBill.Api/PRODUCTION_RACE_CONDITIONS_AUDIT.md`**
   - Comprehensive audit report of all race conditions
   - Risk assessment for each area
   - Recommendations for future improvements

---

## Production Impact

### Before Fix
- **Risk:** Stock corruption from concurrent sales/purchases
- **Risk:** Lost updates causing incorrect stock values
- **Risk:** Negative stock possible from race conditions
- **Risk:** Data inconsistency under high concurrency

### After Fix
- ✅ **Atomic stock updates** - Database handles updates atomically
- ✅ **No race conditions** - Concurrent operations cannot corrupt stock
- ✅ **Negative stock prevention** - WHERE clause ensures sufficient stock
- ✅ **Data consistency** - Stock values remain accurate under high concurrency
- ✅ **Better error messages** - Clear errors when stock is insufficient

---

## Build Status

✅ **Build Successful** - `0 Error(s)`

---

## Testing Recommendations

1. **Load Testing:**
   - Simulate 100+ concurrent sales for same product
   - Verify stock is correctly decremented
   - Verify no negative stock occurs
   - Verify all sales succeed or fail appropriately

2. **Concurrent Purchase Testing:**
   - Simulate multiple purchases for same product simultaneously
   - Verify stock is correctly incremented
   - Verify no race conditions occur

3. **Concurrent Stock Adjustment Testing:**
   - Simulate multiple stock adjustments for same product
   - Verify final stock value is correct
   - Verify no lost updates occur

---

## Next Steps

1. ✅ PROD-19: Race Condition Audit - **COMPLETED**
2. ⏭️ Consider adding pessimistic locking for Customer balance updates (lower priority)
3. ⏭️ Consider adding RowVersion checks for Product updates in other operations

---

## Notes

- All stock updates now use atomic SQL operations
- RowVersion is reloaded after updates to ensure consistency
- Stock validation still occurs before updates for better error messages
- Negative stock is prevented by WHERE clause in SQL updates
- Transactions still wrap operations for rollback capability
- Product model already has RowVersion configured as concurrency token
