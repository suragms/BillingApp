# Race Conditions Audit Report

**Date:** 2026-02-18  
**Task:** PROD-19 - Audit all race conditions (concurrent updates, signup, stock adjustments)

---

## Summary

Comprehensive audit of potential race conditions in the application. Identified and documented all areas where concurrent operations could cause data corruption or inconsistent state.

---

## Race Conditions Found

### ✅ **1. Invoice Number Generation** - PROTECTED

**Location:** `InvoiceNumberService.cs`, `SaleService.cs`  
**Risk Level:** LOW (Already Protected)

**Current Protection:**
- ✅ SemaphoreSlim for process-level locking
- ✅ PostgreSQL advisory locks (`pg_advisory_lock`) for database-level locking
- ✅ Retry logic with exponential backoff (50 retries)
- ✅ Serializable isolation level in `CreateSaleInternalAsync`
- ✅ Retry logic in `CreateSaleAsync` (5 retries) for duplicate key violations
- ✅ Unique constraint on `(OwnerId, InvoiceNo)` with filter

**Status:** ✅ **WELL PROTECTED** - Multiple layers of protection prevent race conditions

---

### ✅ **2. User Signup** - PROTECTED

**Location:** `SignupService.cs`  
**Risk Level:** LOW (Already Protected)

**Current Protection:**
- ✅ Transaction wrapping entire signup process
- ✅ Unique constraint on `Users.Email`
- ✅ Unique constraint on `Tenants.Name`
- ✅ Try-catch for PostgreSQL unique violation (23505)
- ✅ Rollback on duplicate detection

**Status:** ✅ **PROTECTED** - Database constraints + transaction prevent duplicate signups

---

### ⚠️ **3. Stock Updates** - NEEDS FIX

**Location:** Multiple files:
- `SaleService.cs` - Stock decremented during sale creation
- `PurchaseService.cs` - Stock incremented during purchase
- `StockAdjustmentService.cs` - Stock adjusted manually
- `ProductService.cs` - Stock adjusted via API

**Risk Level:** HIGH

**Issue:**
Stock updates are done directly without proper locking:
```csharp
product.StockQty -= baseQty;  // Direct update - race condition possible
```

**Race Condition Scenario:**
1. Two concurrent sales for same product
2. Both read current stock (e.g., 100)
3. Both calculate new stock (e.g., 100 - 50 = 50)
4. Both save (both save 50, but should be 0)
5. Result: Stock becomes 50 instead of 0 (lost update)

**Current Protection:**
- ✅ Transactions wrap stock updates
- ✅ Stock validation before update
- ❌ **NO ROW-LEVEL LOCKING** - Multiple transactions can read same stock value
- ❌ **NO OPTIMISTIC CONCURRENCY** - No RowVersion on Product model

**Fix Required:**
- Add RowVersion to Product model for optimistic concurrency
- Use `SELECT FOR UPDATE` (pessimistic locking) for stock updates
- Or use atomic SQL update: `UPDATE Products SET StockQty = StockQty - @qty WHERE Id = @id`

**Status:** ⚠️ **NEEDS FIX** - Race condition possible with concurrent sales/purchases

---

### ✅ **4. Sale Updates** - PROTECTED

**Location:** `SaleService.cs`  
**Risk Level:** LOW (Already Protected)

**Current Protection:**
- ✅ RowVersion field for optimistic concurrency
- ✅ Serializable isolation level
- ✅ Version check before update
- ✅ Throws exception if RowVersion mismatch

**Status:** ✅ **PROTECTED** - Optimistic concurrency prevents lost updates

---

### ⚠️ **5. Customer Balance Updates** - NEEDS REVIEW

**Location:** `BalanceService.cs`, `SaleService.cs`, `PaymentService.cs`  
**Risk Level:** MEDIUM

**Issue:**
Customer balance is updated in multiple places:
- Sale creation: `customer.Balance += grandTotal`
- Payment creation: `customer.Balance -= amount`
- Balance recalculation: Recalculates from all transactions

**Current Protection:**
- ✅ Transactions wrap balance updates
- ✅ Balance recalculation method exists (`RecalculateCustomerBalanceAsync`)
- ❌ **NO ROW-LEVEL LOCKING** - Concurrent payments could cause incorrect balance
- ❌ **NO OPTIMISTIC CONCURRENCY** - No RowVersion on Customer model

**Risk:**
- Concurrent payments could cause balance to be incorrect
- Balance recalculation helps but doesn't prevent race condition

**Status:** ⚠️ **NEEDS REVIEW** - Balance recalculation mitigates risk but doesn't prevent race condition

---

### ✅ **6. Purchase Invoice Number** - PROTECTED

**Location:** `PurchaseService.cs`  
**Risk Level:** LOW (Already Protected)

**Current Protection:**
- ✅ Unique constraint on `(OwnerId, InvoiceNo)`
- ✅ Transaction wrapping purchase creation
- ✅ Duplicate check before insert

**Status:** ✅ **PROTECTED** - Database constraint prevents duplicates

---

## Recommendations

### High Priority

1. **Fix Stock Update Race Condition** ⚠️
   - Add RowVersion to Product model
   - Use pessimistic locking (`SELECT FOR UPDATE`) for stock updates
   - Or use atomic SQL update: `UPDATE Products SET StockQty = StockQty - @qty WHERE Id = @id AND StockQty >= @qty`

### Medium Priority

2. **Review Customer Balance Updates** ⚠️
   - Consider adding RowVersion to Customer model
   - Use pessimistic locking for balance updates
   - Or always use balance recalculation instead of direct updates

### Low Priority

3. **Add RowVersion to Other Critical Models**
   - Product (for stock updates)
   - Customer (for balance updates)
   - Purchase (for concurrent updates)

---

## Testing Recommendations

1. **Load Testing:**
   - Simulate 100+ concurrent sales for same product
   - Verify stock is correctly decremented
   - Verify no negative stock occurs

2. **Concurrent Payment Testing:**
   - Simulate multiple payments for same customer simultaneously
   - Verify customer balance is correct
   - Verify balance recalculation works correctly

3. **Concurrent Signup Testing:**
   - Simulate multiple signups with same email simultaneously
   - Verify only one succeeds
   - Verify proper error handling

---

## Files to Review

1. `backend/HexaBill.Api/Models/Product.cs` - Add RowVersion
2. `backend/HexaBill.Api/Modules/Billing/SaleService.cs` - Fix stock update race condition
3. `backend/HexaBill.Api/Modules/Purchases/PurchaseService.cs` - Fix stock update race condition
4. `backend/HexaBill.Api/Modules/Inventory/StockAdjustmentService.cs` - Fix stock update race condition
5. `backend/HexaBill.Api/Models/Customer.cs` - Consider adding RowVersion
