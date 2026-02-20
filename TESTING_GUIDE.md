# HexaBill QA Testing Guide

## Quick Start Testing

### Prerequisites
1. Backend running on `http://localhost:5000` (or configured port)
2. Frontend running on `http://localhost:5173` (or configured port)
3. Database connection configured
4. Test users created (SuperAdmin, Owner, Admin, Staff)

---

## Phase 1: Manual Role Testing

### Test User Setup
Create test users for each role:
- **SuperAdmin**: Can access all tenants
- **Owner**: Tenant owner with full access
- **Admin**: Tenant admin with management access
- **Staff**: Limited access, assigned to routes

### Test Checklist

#### ✅ SuperAdmin Tests
- [ ] Login as SuperAdmin
- [ ] View all tenants in SuperAdmin dashboard
- [ ] Switch between tenants
- [ ] Create new tenant
- [ ] View tenant settings
- [ ] Access tenant's invoices, customers, products

#### ✅ Owner Tests
- [ ] Login as Owner
- [ ] Create invoice
- [ ] Edit invoice (verify RowVersion conflict detection)
- [ ] Delete invoice
- [ ] Create customer
- [ ] Create product
- [ ] Adjust stock
- [ ] View reports
- [ ] Export reports
- [ ] Manage users
- [ ] Update settings

#### ✅ Admin Tests
- [ ] Login as Admin
- [ ] Create invoice
- [ ] Edit invoice
- [ ] Delete invoice
- [ ] Manage users (create, update, delete)
- [ ] View reports
- [ ] Export reports
- [ ] Update settings
- [ ] Manage branches/routes

#### ✅ Staff Tests
- [ ] Login as Staff
- [ ] Create invoice (only for assigned routes)
- [ ] Edit invoice (only for assigned routes)
- [ ] **VERIFY: Cannot delete invoice** (should fail or UI hidden)
- [ ] View customers (only assigned routes)
- [ ] View products
- [ ] Adjust stock
- [ ] View expenses (only assigned routes)
- [ ] **VERIFY: Cannot approve expenses** (should fail)
- [ ] **VERIFY: Cannot manage users** (should fail)
- [ ] **VERIFY: Cannot access settings** (should fail)

---

## Phase 2: Transaction & Race Condition Testing

### Test 1: Payment Status Update Transaction
**Purpose:** Verify payment status updates are atomic

**Steps:**
1. Create an invoice with $1000 total
2. Create a payment of $500 (PENDING status)
3. Update payment status to CLEARED
4. **Verify:**
   - Payment status updated
   - Sale PaidAmount updated correctly
   - Customer Balance updated correctly
   - All changes succeed or all fail together

**Expected Result:** All updates succeed atomically

---

### Test 2: Expense Creation Transaction
**Purpose:** Verify expense and audit log are created atomically

**Steps:**
1. Create an expense
2. **Verify:**
   - Expense created in database
   - Audit log entry created
   - Both succeed or both fail

**Expected Result:** Expense and audit log created together

---

### Test 3: Invoice Number Race Condition
**Purpose:** Verify no duplicate invoice numbers under concurrency

**Steps:**
1. Open two browser tabs/windows
2. Login as same user in both
3. Create invoice simultaneously in both tabs
4. **Verify:**
   - Both invoices created successfully
   - Invoice numbers are unique
   - No duplicate invoice number errors

**Expected Result:** Unique invoice numbers generated even under concurrency

---

### Test 4: Stock Update Race Condition
**Purpose:** Verify stock validation happens before updates

**Steps:**
1. Create product with stock: 100 units
2. Create invoice with 60 units (should succeed)
3. Immediately create another invoice with 50 units (should fail - insufficient stock)
4. **Verify:**
   - First invoice succeeds, stock becomes 40
   - Second invoice fails with "Insufficient stock" error
   - Stock remains at 40 (not negative)

**Expected Result:** Stock validation prevents negative stock

---

### Test 5: Concurrent Invoice Editing Conflict Detection
**Purpose:** Verify RowVersion conflict detection works

**Steps:**
1. Open invoice #100 in two browser tabs
2. Edit invoice in Tab 1, save
3. Edit invoice in Tab 2, save
4. **Verify:**
   - Tab 1 save succeeds
   - Tab 2 save fails with conflict error: "CONFLICT: This invoice was modified by another user"
   - Error message shows current version and last modified info

**Expected Result:** Second save detects conflict and prevents lost update

---

## Phase 3: API Edge Case Testing

### Test 1: Expired JWT Token
**Steps:**
1. Login and get JWT token
2. Manually expire token (set expiration to past date)
3. Make API call with expired token
4. **Verify:**
   - API returns 401 Unauthorized
   - Frontend redirects to login page
   - Toast message: "Authentication failed. Please login again."

**Expected Result:** Graceful handling of expired tokens

---

### Test 2: Invalid Payloads
**Test Cases:**
- [ ] Create invoice with empty items array → Should return 400
- [ ] Create invoice with negative quantity → Should return 400
- [ ] Create invoice with negative price → Should return 400
- [ ] Create payment with amount exceeding invoice total → Should return 400
- [ ] Create customer with empty name → Should return 400

**Expected Result:** All invalid payloads return 400 with validation errors

---

### Test 3: Concurrent Operations
**Test Cases:**
- [ ] 10 simultaneous invoice creations → All succeed with unique numbers
- [ ] 2 users editing same invoice → One succeeds, one gets conflict error
- [ ] Rapid payment creation → All succeed, balances calculated correctly

**Expected Result:** System handles concurrency without data corruption

---

## Phase 4: Mobile & Responsive Testing

### Test 1: Table Overflow on Mobile
**Steps:**
1. Open Sales Ledger page on mobile device (or Chrome DevTools mobile view)
2. Set viewport to 375px width (iPhone)
3. **Verify:**
   - Tables scroll horizontally instead of overflowing
   - All columns accessible via horizontal scroll
   - No content cut off

**Expected Result:** Tables scroll horizontally on mobile

---

### Test 2: Tablet View (768px)
**Steps:**
1. Set viewport to 768px width (iPad)
2. **Verify:**
   - Tables display properly
   - Horizontal scroll available if needed
   - No layout breaking

**Expected Result:** Responsive layout works on tablets

---

## Phase 5: Stress Testing

### Test 1: 100 Concurrent Invoice Creation
**Steps:**
1. Use script to create 100 invoices simultaneously
2. **Verify:**
   - All invoices created successfully
   - All invoice numbers unique
   - No duplicate key errors
   - Database remains consistent

**Expected Result:** System handles high concurrency

---

### Test 2: Two Users Editing Same Invoice
**Steps:**
1. User A starts editing invoice #100
2. User B starts editing invoice #100
3. User A saves changes
4. User B tries to save changes
5. **Verify:**
   - User A save succeeds
   - User B gets conflict error
   - Invoice reflects User A's changes

**Expected Result:** Conflict detection prevents lost updates

---

### Test 3: Rapid Delete and Recreate
**Steps:**
1. Create customer "Test Customer"
2. Delete customer immediately
3. Recreate customer with same name
4. **Verify:**
   - Customer deleted successfully
   - Customer recreated successfully
   - No constraint violations

**Expected Result:** Soft delete allows recreation

---

## Test Results Template

```
## Test Execution Results

**Date:** [Date]
**Tester:** [Name]
**Environment:** [Local/Staging/Production]

### Phase 1: Role Testing
- [ ] SuperAdmin: ✅ Pass / ❌ Fail
- [ ] Owner: ✅ Pass / ❌ Fail
- [ ] Admin: ✅ Pass / ❌ Fail
- [ ] Staff: ✅ Pass / ❌ Fail

### Phase 2: Transaction Testing
- [ ] Payment Transaction: ✅ Pass / ❌ Fail
- [ ] Expense Transaction: ✅ Pass / ❌ Fail
- [ ] Invoice Race Condition: ✅ Pass / ❌ Fail
- [ ] Stock Race Condition: ✅ Pass / ❌ Fail
- [ ] Conflict Detection: ✅ Pass / ❌ Fail

### Phase 3: API Testing
- [ ] Expired JWT: ✅ Pass / ❌ Fail
- [ ] Invalid Payloads: ✅ Pass / ❌ Fail
- [ ] Concurrent Operations: ✅ Pass / ❌ Fail

### Phase 4: Mobile Testing
- [ ] Mobile Tables: ✅ Pass / ❌ Fail
- [ ] Tablet View: ✅ Pass / ❌ Fail

### Phase 5: Stress Testing
- [ ] 100 Concurrent Invoices: ✅ Pass / ❌ Fail
- [ ] Concurrent Editing: ✅ Pass / ❌ Fail
- [ ] Rapid Delete/Recreate: ✅ Pass / ❌ Fail

### Issues Found:
1. [Issue description]
2. [Issue description]

### Overall Status: ✅ Ready for Production / ⚠️ Issues Found
```

---

## Quick Test Scripts

### Test Payment Transaction (Postman Collection)
```json
{
  "name": "Test Payment Transaction",
  "requests": [
    {
      "name": "Create Invoice",
      "method": "POST",
      "url": "http://localhost:5000/api/sales",
      "body": {
        "customerId": 1,
        "items": [{"productId": 1, "qty": 10, "unitPrice": 100}]
      }
    },
    {
      "name": "Create Payment (PENDING)",
      "method": "POST",
      "url": "http://localhost:5000/api/payments",
      "body": {
        "saleId": 1,
        "amount": 500,
        "mode": "CHEQUE"
      }
    },
    {
      "name": "Update Payment Status to CLEARED",
      "method": "PUT",
      "url": "http://localhost:5000/api/payments/1/status",
      "body": {
        "status": "CLEARED"
      }
    }
  ]
}
```

---

## Critical Fixes Verification Checklist

- [x] Payment transactions wrapped
- [x] Expense transactions wrapped
- [x] Invoice number generation inside transaction
- [x] Stock validation before updates
- [x] Mobile table overflow fixed
- [x] Staff delete permission restricted
- [x] RowVersion conflict detection verified

---

**Next Steps:**
1. Run all tests in this guide
2. Document any issues found
3. Fix any issues before production deployment
4. Re-test after fixes
