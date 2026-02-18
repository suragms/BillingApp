# AUDIT-9: Branch & Route Logic Audit

**Status:** ✅ COMPLETED  
**Date:** 2026-02-18

## Audit Scope

This audit validates:
1. Route belongs to Branch (FK constraint and validation)
2. Customer belongs to Route (validation logic)
3. POS auto-selects branch/route for staff
4. Staff cannot select wrong branch/route
5. Branch assignment consistency validation
6. Branch-based report filtering

---

## ✅ **EXCELLENT PATTERNS FOUND:**

### 1. **Database Schema - Route.BranchId Foreign Key**
**Location:** `AppDbContext.cs` (Line 569)
**Implementation:** ✅ Route has required `BranchId` FK to Branch table
```csharp
entity.HasOne(e => e.Branch).WithMany(b => b.Routes).HasForeignKey(e => e.BranchId);
```
**Status:** ✅ **EXCELLENT** - Database enforces relationship at schema level

### 2. **Route Creation Validation**
**Location:** `RouteService.cs` - `CreateRouteAsync` (Lines 122-129)
**Implementation:** ✅ Validates Branch exists and belongs to tenant before creating route
```csharp
var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId && b.TenantId == tenantId);
if (branch == null) 
    throw new InvalidOperationException($"Branch with ID {request.BranchId} not found or does not belong to your tenant.");
```
**Status:** ✅ **EXCELLENT**

### 3. **Sale Route-Branch Consistency Validation**
**Location:** `SaleService.cs` - `CreateSaleAsync` (Lines 412-418)
**Implementation:** ✅ Validates Route.BranchId matches Sale.BranchId
```csharp
if (request.BranchId.HasValue && route.BranchId != request.BranchId.Value)
{
    throw new InvalidOperationException(
        $"Sale belongs to Branch {request.BranchId.Value}, but Route {request.RouteId.Value} belongs to Branch {route.BranchId}. " +
        "Sale and Route must belong to the same Branch.");
}
```
**Status:** ✅ **EXCELLENT**

### 4. **Sale Customer-Route Consistency Validation**
**Location:** `SaleService.cs` - `CreateSaleAsync` (Lines 420-426)
**Implementation:** ✅ Validates Customer.RouteId matches Sale.RouteId
```csharp
if (customer != null && customer.RouteId.HasValue && customer.RouteId.Value != request.RouteId.Value)
{
    throw new InvalidOperationException(
        $"Customer {customer.Id} belongs to Route {customer.RouteId.Value}, but Sale is being assigned to Route {request.RouteId.Value}. " +
        "Customer and Sale must belong to the same Route.");
}
```
**Status:** ✅ **EXCELLENT**

### 5. **Customer Route-Branch Consistency Validation**
**Location:** `CustomerService.cs` - `CreateCustomerAsync` (Lines 263-269)
**Implementation:** ✅ Validates Route.BranchId matches Customer.BranchId
```csharp
if (customer.BranchId.HasValue && route.BranchId != customer.BranchId.Value)
{
    throw new InvalidOperationException(
        $"Customer belongs to Branch {customer.BranchId.Value}, but Route {request.RouteId.Value} belongs to Branch {route.BranchId}. " +
        "Customer and Route must belong to the same Branch.");
}
```
**Status:** ✅ **EXCELLENT**

### 6. **Route Customer Assignment Validation**
**Location:** `RouteService.cs` - `AssignCustomerToRouteAsync` (Lines 218-224)
**Implementation:** ✅ Validates Route.BranchId matches Customer.BranchId when assigning customer to route
```csharp
if (customer.BranchId.HasValue && route.BranchId != customer.BranchId.Value)
{
    throw new InvalidOperationException(
        $"Customer {customerId} belongs to Branch {customer.BranchId.Value}, but Route {routeId} belongs to Branch {route.BranchId}. " +
        "Customer and Route must belong to the same Branch.");
}
```
**Status:** ✅ **EXCELLENT**

### 7. **POS Auto-Selection for Staff**
**Location:** `PosPage.jsx` (Lines 191-208)
**Implementation:** ✅ Auto-selects branch/route when staff has only one option
```javascript
// Auto-select Branch if only 1 is available
if (branches.length === 1 && !selectedBranchId) {
  setSelectedBranchId(String(branches[0].id))
}

// Auto-select Route if only 1 is available (considering branch filter)
const availableRoutes = selectedBranchId
  ? routes.filter(r => r.branchId === parseInt(selectedBranchId))
  : routes

if (availableRoutes.length === 1 && !selectedRouteId) {
  setSelectedRouteId(String(availableRoutes[0].id))
}
```
**Status:** ✅ **EXCELLENT**

### 8. **POS Staff Route Lock**
**Location:** `SaleService.cs` - `CreateSaleAsync` (Lines 429-451)
**Implementation:** ✅ Prevents Staff from creating invoices for routes not assigned to them
```csharp
if (request.RouteId.HasValue && tenantId > 0 &&
    userRole.Equals("Staff", StringComparison.OrdinalIgnoreCase))
{
    var allowedRouteIds = await _routeScopeService.GetRestrictedRouteIdsAsync(userId, tenantId, userRole);
    if (allowedRouteIds != null && allowedRouteIds.Length > 0)
    {
        if (!allowedRouteIds.Contains(request.RouteId.Value))
        {
            throw new UnauthorizedAccessException(
                "You can only create invoices for routes assigned to you. The selected route is not in your assigned routes.");
        }
    }
}
```
**Status:** ✅ **EXCELLENT**

### 9. **POS Branch/Route Dropdown Filtering**
**Location:** `PosPage.jsx` (Lines 1836-1838)
**Implementation:** ✅ Routes filtered by selected branch
```javascript
{routes
  // Filter routes by selected branch if a branch is selected
  .filter(r => !selectedBranchId || r.branchId === parseInt(selectedBranchId))
  .map(r => (
    <option key={r.id} value={r.id}>{r.name}</option>
  ))}
```
**Status:** ✅ **EXCELLENT**

### 10. **POS Staff Dropdown Disabling**
**Location:** `PosPage.jsx` (Lines 1814, 1832)
**Implementation:** ✅ Disables branch/route dropdowns for staff when only one option (prevents manual selection)
```javascript
disabled={branches.length === 0 || (!isAdminOrOwner(user) && branches.length === 1)}
disabled={routes.length === 0 || !selectedBranchId || (!isAdminOrOwner(user) && (selectedBranchId ? routes.filter(r => r.branchId === parseInt(selectedBranchId, 10)) : routes).length <= 1)}
```
**Status:** ✅ **EXCELLENT**

### 11. **Report Branch/Route Filtering**
**Location:** `ReportService.cs` - `GetSummaryReportAsync` (Lines 95-96)
**Implementation:** ✅ Reports filter by branchId and routeId
```csharp
if (branchId.HasValue) salesQuery = salesQuery.Where(s => s.BranchId == branchId.Value);
if (routeId.HasValue) salesQuery = salesQuery.Where(s => s.RouteId == routeId.Value);
```
**Status:** ✅ **EXCELLENT**

### 12. **Customer Form Route Filtering**
**Location:** `CustomersPage.jsx` (Line 1054)
**Implementation:** ✅ Route dropdown filtered by selected branch
```javascript
{(selectedBranchId ? routes.filter(r => r.branchId === parseInt(selectedBranchId, 10)) : []).map(r => (
```
**Status:** ✅ **EXCELLENT**

---

## ⚠️ **MEDIUM PRIORITY ISSUES FOUND:**

### **ISSUE #1: Route Branch Change Doesn't Validate Existing Customers/Sales**
**Location:** `RouteService.cs` - `UpdateRouteAsync` (Lines 158-195)
**Problem:** When Route.BranchId is changed, the system validates the new branch exists and belongs to tenant, but does NOT check if:
- Route has customers assigned (via RouteCustomers or Customer.RouteId)
- Route has sales records (via Sale.RouteId)

**Impact:** 🟡 **MEDIUM** - Moving a route to a different branch could create data inconsistency:
- Customers assigned to the route would have `Customer.BranchId` that doesn't match `Route.BranchId`
- Sales records would have `Sale.BranchId` that doesn't match `Route.BranchId`
- Reports filtering by branch/route would show incorrect data

**Current Code:**
```csharp
// PROD-12: Validate Branch exists and belongs to tenant if BranchId is being changed
if (request.BranchId != route.BranchId)
{
    var branch = await _context.Branches.FirstOrDefaultAsync(b => b.Id == request.BranchId && b.TenantId == tenantId);
    if (branch == null)
        throw new InvalidOperationException($"Branch with ID {request.BranchId} not found or does not belong to your tenant.");
    
    // PROD-12: Validate Route.BranchId matches Branch.TenantId
    if (branch.TenantId != tenantId)
        throw new InvalidOperationException($"Branch {request.BranchId} does not belong to tenant {tenantId}.");
}

route.BranchId = request.BranchId; // ⚠️ No validation for existing customers/sales
```

**Recommendation:** Add validation to prevent branch change if route has customers or sales:
```csharp
if (request.BranchId != route.BranchId)
{
    // ... existing branch validation ...
    
    // Check if route has customers
    var hasCustomers = await _context.RouteCustomers.AnyAsync(rc => rc.RouteId == id) ||
                       await _context.Customers.AnyAsync(c => c.RouteId == id && c.TenantId == tenantId);
    
    // Check if route has sales
    var hasSales = await _context.Sales.AnyAsync(s => s.RouteId == id && s.TenantId == tenantId && !s.IsDeleted);
    
    if (hasCustomers || hasSales)
    {
        throw new InvalidOperationException(
            $"Cannot change route branch. Route has {(hasCustomers ? "customers" : "")} " +
            $"{(hasCustomers && hasSales ? "and " : "")}{(hasSales ? "sales records" : "")} assigned. " +
            "Please reassign customers and sales to another route before changing branch.");
    }
}
```

**Alternative (Less Restrictive):** If branch change is allowed, update all related records:
```csharp
if (request.BranchId != route.BranchId)
{
    // ... existing branch validation ...
    
    // Update all customers assigned to this route
    var customersToUpdate = await _context.Customers
        .Where(c => c.RouteId == id && c.TenantId == tenantId)
        .ToListAsync();
    foreach (var customer in customersToUpdate)
    {
        customer.BranchId = request.BranchId;
    }
    
    // Update all sales assigned to this route
    var salesToUpdate = await _context.Sales
        .Where(s => s.RouteId == id && s.TenantId == tenantId && !s.IsDeleted)
        .ToListAsync();
    foreach (var sale in salesToUpdate)
    {
        sale.BranchId = request.BranchId;
    }
    
    await _context.SaveChangesAsync(); // Save customer/sale updates before route update
}
```

**Priority:** 🟡 **MEDIUM** - Data consistency risk, but may be intentional business logic

---

## ✅ **SUMMARY:**

### **Excellent Protections:**
- ✅ Database FK constraint enforces Route → Branch relationship
- ✅ Route creation validates branch exists and belongs to tenant
- ✅ Sale creation validates Route.BranchId matches Sale.BranchId
- ✅ Sale creation validates Customer.RouteId matches Sale.RouteId
- ✅ Customer creation validates Route.BranchId matches Customer.BranchId
- ✅ Route customer assignment validates branch consistency
- ✅ POS auto-selects branch/route for staff
- ✅ Staff route lock prevents unauthorized route selection
- ✅ Frontend route dropdowns filtered by selected branch
- ✅ Reports filter by branchId and routeId

### **Minor Improvements Needed:**
- 🟡 Route branch change should validate existing customers/sales (or update them atomically)

### **Overall Assessment:**
✅ **EXCELLENT** - Branch and Route logic is well-implemented with comprehensive validation at multiple layers (database, backend service, frontend). The only minor gap is route branch change validation, which may be intentional business logic.

---

## **Recommendations:**

1. **Add Route Branch Change Validation** (Medium Priority):
   - Option A: Prevent branch change if route has customers/sales (safer, prevents data inconsistency)
   - Option B: Allow branch change but update all related customers/sales atomically (more flexible, requires careful testing)

2. **Consider Adding Audit Log** (Low Priority):
   - Log when route branch is changed
   - Log when customers are reassigned between routes
   - Helps track data changes for compliance

3. **Consider Adding Route Deletion Validation** (Low Priority):
   - Currently `DeleteRouteAsync` doesn't check for existing customers/sales
   - Should prevent deletion if route has active customers or sales records

---

**Next Steps:**
- ✅ Audit completed
- 🟡 Consider implementing route branch change validation
- 🟢 System is production-ready for branch/route logic
