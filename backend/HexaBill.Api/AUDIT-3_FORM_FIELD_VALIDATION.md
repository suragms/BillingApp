# AUDIT-3: Form Field Validation Audit

**Status:** 🔄 IN PROGRESS  
**Date:** 2026-02-18

---

## AUDIT METHODOLOGY

For each form:
1. List required fields
2. Check client-side validation (react-hook-form, manual validation)
3. Check server-side validation (DTO attributes: [Required], [Range], [MaxLength], etc.)
4. Verify null handling
5. Check numeric parsing and decimal precision
6. Verify VAT calculation correctness
7. Check date timezone handling
8. Verify route/branch assignment logic
9. Check auto-selection logic for POS branch

---

## FINDINGS

### ✅ **GOOD PATTERNS FOUND:**

#### **1. Backend DTOs Have Strong Validation**

**Location:** `backend/HexaBill.Api/Models/DTOs.cs`

**Examples:**
- ✅ `CreateProductRequest`: `[Required]`, `[MaxLength(200)]`, `[Range(0, 99999999.99)]`
- ✅ `LoginRequest`: `[Required]`, `[EmailAddress]`
- ✅ `CreateUserRequest`: `[Required]`, `[EmailAddress]`, `[MinLength(6)]`, `[MaxLength(100)]`
- ✅ `SaleItemRequest`: `[Required]`, `[Range(0.0001, 999999.99)]` for Qty
- ✅ `PurchaseItemRequest`: `[Required]`, `[Range(0.0001, 999999.99)]` for Qty

**Status:** ✅ **EXCELLENT** - Comprehensive server-side validation

---

#### **2. Client-Side Validation Patterns**

**Location:** `frontend/hexabill-ui/src/pages/`

**Examples:**
- ✅ `SignupPage.jsx`: Manual validation with error messages
- ✅ `UsersPage.jsx`: Uses `react-hook-form` with validation rules
- ✅ `ProductsPage.jsx`: Client-side checks before API calls

**Status:** ✅ **GOOD** - Most forms have client-side validation

---

#### **3. VAT Calculation Logic**

**Location:** `backend/HexaBill.Api/Modules/Purchases/PurchaseService.cs` (lines 242-261)

**VAT Calculation:**
```csharp
if (includesVat)
{
    // Cost includes VAT - extract VAT amount
    unitCostExclVat = item.UnitCost / (1 + (vatPercent / 100));
    itemVatAmount = unitCostInclVat - unitCostExclVat;
}
else
{
    // Cost excludes VAT - add VAT
    unitCostExclVat = item.UnitCost;
    itemVatAmount = unitCostExclVat * (vatPercent / 100);
    unitCostInclVat = unitCostExclVat + itemVatAmount;
}
```

**Status:** ✅ **CORRECT** - Proper VAT calculation logic

---

### 🔴 **CRITICAL ISSUES FOUND:**

#### **BUG #1: ProductForm Missing Client-Side Validation**

**Location:** `frontend/hexabill-ui/src/components/ProductForm.jsx`

**Issue:**
- Form submits without validating required fields (`nameEn`, `sku`)
- No validation for negative prices
- No validation for invalid `conversionToBase` (must be > 0)
- No validation for `sellPrice` < `costPrice` (business logic check)

**Impact:** Invalid data can be sent to backend (though backend will reject it)

**Fix Required:**
```javascript
const handleSubmit = async (e) => {
  e.preventDefault()
  
  // VALIDATION MISSING:
  if (!formData.nameEn || !formData.nameEn.trim()) {
    toast.error('Product name is required')
    return
  }
  if (!formData.sku || !formData.sku.trim()) {
    toast.error('SKU is required')
    return
  }
  if (formData.costPrice < 0 || formData.sellPrice < 0) {
    toast.error('Prices cannot be negative')
    return
  }
  if (formData.conversionToBase <= 0) {
    toast.error('Conversion to base must be greater than 0')
    return
  }
  
  // ... rest of submit logic
}
```

---

#### **BUG #2: Missing Decimal Precision Validation**

**Location:** Multiple forms

**Issue:**
- No client-side validation for decimal precision (max 2 decimal places for currency)
- Backend uses `decimal(18,2)` but frontend doesn't enforce this

**Impact:** User might enter prices with more than 2 decimal places, causing rounding issues

**Fix Required:** Add decimal precision validation:
```javascript
const validateDecimal = (value, maxDecimals = 2) => {
  const parts = value.toString().split('.')
  return parts.length === 1 || parts[1].length <= maxDecimals
}
```

---

#### **BUG #3: Date Timezone Handling**

**Location:** `PosPage.jsx`, `PurchasesPage.jsx`, `ExpensesPage.jsx`

**Issue:**
- Dates are converted to ISO string: `today.toISOString().split('T')[0]`
- No explicit timezone handling
- Backend might receive dates in different timezone

**Impact:** Date mismatches between frontend and backend

**Status:** ⚠️ **NEEDS VERIFICATION** - Check if backend handles UTC correctly

---

#### **BUG #4: Route/Branch Auto-Selection Logic**

**Location:** `PosPage.jsx` (lines 74-77)

**Issue:**
- `selectedBranchId` and `selectedRouteId` state variables exist
- Need to verify auto-selection logic when customer is selected
- Need to verify branch/route validation before saving sale

**Status:** ⚠️ **NEEDS VERIFICATION** - Check auto-selection implementation

---

#### **BUG #5: Missing Null Checks in ProductForm**

**Location:** `ProductForm.jsx` (line 16)

**Issue:**
```javascript
expiryDate: product?.expiryDate ? product.expiryDate.split('T')[0] : '',
```

**Potential Issue:** If `expiryDate` is null but not undefined, this might fail

**Fix:** Use optional chaining more safely:
```javascript
expiryDate: product?.expiryDate?.split('T')[0] || '',
```

---

### 🟡 **MEDIUM PRIORITY ISSUES:**

#### **ISSUE #1: Inconsistent Validation Patterns**

**Location:** Multiple forms

**Issue:**
- Some forms use `react-hook-form` (UsersPage)
- Some forms use manual validation (SignupPage, ProductsPage)
- Some forms have minimal validation (ProductForm)

**Recommendation:** Standardize on `react-hook-form` for consistency

---

#### **ISSUE #2: Missing Business Logic Validation**

**Location:** ProductForm, SaleForm

**Issue:**
- No check: `sellPrice` should be >= `costPrice` (or warn if not)
- No check: Stock cannot be negative
- No check: Credit limit cannot exceed reasonable maximum

**Recommendation:** Add business logic validation

---

#### **ISSUE #3: Error Messages Not User-Friendly**

**Location:** Backend DTOs

**Issue:**
- Some validation errors return technical messages
- Frontend might expose raw error messages to users

**Example:**
```csharp
[Range(0.0001, 999999.99, ErrorMessage = "ConversionToBase must be greater than 0")]
```

**Status:** ⚠️ **ACCEPTABLE** - Error messages are clear

---

## FORM-BY-FORM AUDIT

### **1. ProductForm**

**Required Fields:**
- ✅ `nameEn` - Backend: `[Required]`, Frontend: ❌ Missing validation
- ✅ `sku` - Backend: `[Required]`, Frontend: ❌ Missing validation
- ✅ `unitType` - Backend: `[Required]`, Frontend: ❌ Missing validation
- ✅ `conversionToBase` - Backend: `[Range(0.0001, 999999.99)]`, Frontend: ❌ Missing validation
- ✅ `costPrice` - Backend: `[Range(0, 99999999.99)]`, Frontend: ❌ Missing validation
- ✅ `sellPrice` - Backend: `[Range(0, 99999999.99)]`, Frontend: ❌ Missing validation

**Status:** 🔴 **NEEDS FIX** - Add client-side validation

---

### **2. CustomerForm**

**Required Fields:**
- ✅ `name` - Backend: `[Required]`, Frontend: ✅ Has validation
- ⚠️ `phone` - Backend: Optional, Frontend: Optional
- ⚠️ `email` - Backend: `[EmailAddress]` if provided, Frontend: ✅ Has validation
- ⚠️ `trn` - Backend: Optional, Frontend: Optional

**Status:** ✅ **GOOD** - Validation exists

---

### **3. SaleForm (POS)**

**Required Fields:**
- ✅ `Items` - Backend: `[Required]`, `[MinLength(1)]`, Frontend: ✅ Checks cart not empty
- ✅ `CustomerId` - Backend: Optional, Frontend: Optional (Cash sales)
- ✅ `PaymentMethod` - Backend: `[Required]`, Frontend: ✅ Has validation

**Status:** ✅ **GOOD** - Validation exists

---

### **4. PurchaseForm**

**Required Fields:**
- ✅ `SupplierName` - Backend: `[Required]`, Frontend: ✅ Has validation
- ✅ `InvoiceNo` - Backend: `[Required]`, Frontend: ✅ Has validation
- ✅ `PurchaseDate` - Backend: `[Required]`, Frontend: ✅ Has validation
- ✅ `Items` - Backend: `[Required]`, `[MinLength(1)]`, Frontend: ✅ Has validation

**Status:** ✅ **GOOD** - Validation exists

---

### **5. ExpenseForm**

**Required Fields:**
- ✅ `CategoryId` - Backend: `[Required]`, Frontend: ✅ Has validation
- ✅ `Amount` - Backend: `[Required]`, Frontend: ✅ Has validation
- ✅ `Date` - Backend: `[Required]`, Frontend: ✅ Has validation

**Status:** ✅ **GOOD** - Validation exists

---

### **6. UserForm**

**Required Fields:**
- ✅ `Name` - Backend: `[Required]`, Frontend: ✅ Uses react-hook-form
- ✅ `Email` - Backend: `[Required]`, `[EmailAddress]`, Frontend: ✅ Uses react-hook-form
- ✅ `Password` - Backend: `[Required]`, `[MinLength(6)]`, Frontend: ✅ Uses react-hook-form
- ✅ `Role` - Backend: `[Required]`, Frontend: ✅ Uses react-hook-form

**Status:** ✅ **EXCELLENT** - Uses react-hook-form with proper validation

---

## CALCULATION VERIFICATION

### **VAT Calculations**

**Status:** ✅ **VERIFIED CORRECT**

**Purchase VAT:**
- ✅ Includes VAT: `unitCostExclVat = unitCost / (1 + vatPercent/100)`
- ✅ Excludes VAT: `unitCostInclVat = unitCostExclVat + (unitCostExclVat * vatPercent/100)`

**Sale VAT:**
- ✅ VAT calculated correctly on line items
- ✅ Total VAT aggregated correctly

---

### **Stock Calculations**

**Status:** ✅ **VERIFIED CORRECT**

- ✅ Base quantity: `baseQty = qty * conversionToBase`
- ✅ Stock updates: Atomic SQL updates (PROD-19 fix)
- ✅ Stock validation: Checks availability before sale

---

### **Balance Calculations**

**Status:** ✅ **VERIFIED CORRECT**

- ✅ Customer balance: Updated atomically
- ✅ Payment calculations: Correct
- ✅ Ledger recalculation: Proper

---

## NULL HANDLING AUDIT

### **Frontend Null Handling:**

- ✅ Uses optional chaining: `product?.expiryDate`
- ✅ Uses nullish coalescing: `product?.sku || ''`
- ⚠️ Some places might need more defensive checks

### **Backend Null Handling:**

- ✅ DTOs use nullable types: `string?`, `int?`, `DateTime?`
- ✅ Database allows NULL for optional fields
- ✅ Queries handle nulls correctly

---

## DATE TIMEZONE HANDLING

### **Current Implementation:**

**Frontend:**
```javascript
const today = new Date()
return today.toISOString().split('T')[0] // YYYY-MM-DD format
```

**Backend:**
- Uses `DateTime` with `.ToUtcKind()` in some places
- Need to verify all date handling uses UTC

**Status:** ⚠️ **NEEDS VERIFICATION** - Check all date operations use UTC

---

## ROUTE/BRANCH ASSIGNMENT LOGIC

### **POS Auto-Selection:**

**Location:** `PosPage.jsx`

**Status:** ⚠️ **NEEDS VERIFICATION** - Check if:
1. Customer's route/branch is auto-selected
2. Staff's assigned branch/route is used as default
3. Validation ensures route belongs to branch

---

## RECOMMENDATIONS

### **Priority 1: Critical Fixes**

1. **Add ProductForm Validation:**
   - Required fields: nameEn, sku, unitType
   - Numeric validation: prices >= 0, conversionToBase > 0
   - Business logic: sellPrice >= costPrice (warning)

2. **Add Decimal Precision Validation:**
   - Max 2 decimal places for currency fields
   - Round to 2 decimals before sending to backend

3. **Verify Date Timezone Handling:**
   - Ensure all dates use UTC
   - Verify backend converts correctly

### **Priority 2: Improvements**

1. **Standardize Validation:**
   - Use `react-hook-form` for all forms
   - Create reusable validation schemas

2. **Add Business Logic Validation:**
   - Stock cannot be negative
   - Credit limit reasonable maximum
   - Sell price >= cost price warning

3. **Improve Error Messages:**
   - User-friendly error messages
   - Field-specific error display

---

## SUMMARY

### **Overall Status:**

- ✅ **Backend Validation:** EXCELLENT (comprehensive DTO validation)
- ✅ **VAT Calculations:** CORRECT
- ✅ **Stock Calculations:** CORRECT
- ⚠️ **Client-Side Validation:** INCONSISTENT (some forms missing validation)
- ⚠️ **Date Timezone:** NEEDS VERIFICATION
- ⚠️ **Route/Branch Logic:** NEEDS VERIFICATION

### **Critical Issues:**

1. 🔴 ProductForm missing client-side validation
2. 🔴 Missing decimal precision validation
3. 🟡 Date timezone handling needs verification
4. 🟡 Route/branch auto-selection needs verification

---

**Last Updated:** 2026-02-18  
**Next Steps:** Fix ProductForm validation, add decimal precision checks, verify date/timezone handling
