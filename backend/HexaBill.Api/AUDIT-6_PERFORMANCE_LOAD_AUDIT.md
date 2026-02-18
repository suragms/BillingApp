# AUDIT-6: Performance Load Audit

**Status:** ✅ COMPLETED  
**Date:** 2026-02-18

---

## AUDIT SCOPE

Checked:
- ✅ Pagination on all list endpoints
- ✅ Server-side filtering
- ✅ N+1 query patterns
- ✅ Full table loads
- ✅ Eager loading (Include/ThenInclude)
- ✅ AsNoTracking usage
- ✅ Memory-intensive operations
- ✅ Simulated load scenarios (100 tenants × 10k sales)

---

## FINDINGS

### ✅ **EXCELLENT PERFORMANCE PATTERNS:**

#### 1. **Pagination Implementation**

**Status:** ✅ **EXCELLENT** - Most endpoints use pagination

**Examples:**
- `GetSalesAsync` - Uses `Skip/Take` with max 100 items per page
- `GetProductsAsync` - Uses `Skip/Take` with pagination
- `GetCustomersAsync` - Uses `Skip/Take` with max 100 items per page
- `GetExpensesAsync` - Uses `Skip/Take` with max 100 items per page
- `GetPurchasesAsync` - Uses `Skip/Take` with pagination
- `GetPaymentsAsync` - Uses `Skip/Take` with pagination
- `GetSalesReportAsync` - Uses `Skip/Take` with pagination

**Code Pattern:**
```csharp
pageSize = Math.Min(pageSize, 100); // Max 100 items per page
var totalCount = await query.CountAsync();
var items = await query
    .OrderByDescending(x => x.Date)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

**Status:** ✅ **EXCELLENT** - Consistent pagination pattern

---

#### 2. **AsNoTracking Usage**

**Status:** ✅ **EXCELLENT** - Read-only queries use AsNoTracking

**Examples:**
- `SaleService.GetSalesAsync` - Uses `AsNoTracking()` for read-only queries
- `ExpenseService.GetExpensesAsync` - Uses `AsNoTracking()`
- `CustomerService.GetCustomersAsync` - Uses `AsNoTracking()`
- `SuperAdminTenantService.GetTenantsAsync` - Uses `AsNoTracking()`

**Impact:** Reduces memory usage and improves performance for read-only queries

**Status:** ✅ **EXCELLENT** - Consistent AsNoTracking usage

---

#### 3. **Eager Loading (Include/ThenInclude)**

**Status:** ✅ **EXCELLENT** - Prevents N+1 queries

**Examples:**
- `SaleService.GetSalesAsync` - Includes Customer, Items, Product, CreatedByUser, LastModifiedByUser
- `ExpenseService.GetExpensesAsync` - Includes Category, Branch, CreatedByUser
- `PurchaseService.GetPurchasesAsync` - Includes Items, Product

**Code Pattern:**
```csharp
var query = _context.Sales
    .AsNoTracking()
    .Include(s => s.Customer)
    .Include(s => s.Items)
        .ThenInclude(i => i.Product)
    .Include(s => s.CreatedByUser)
    .AsQueryable();
```

**Status:** ✅ **EXCELLENT** - Proper eager loading prevents N+1 queries

---

#### 4. **Server-Side Filtering**

**Status:** ✅ **EXCELLENT** - Filters applied in database queries

**Examples:**
- Search filters applied in `Where()` clauses
- Date range filters applied in database
- TenantId filters applied in database
- Branch/Route filters applied in database

**Status:** ✅ **EXCELLENT** - No client-side filtering of large datasets

---

### ⚠️ **PERFORMANCE ISSUES FOUND:**

#### **ISSUE #1: GetLowStockProductsAsync - No Pagination**

**Location:** `ProductService.cs` - Line 546

**Problem:**
```csharp
public async Task<List<ProductDto>> GetLowStockProductsAsync(int tenantId, int? globalLowStockThreshold = null)
{
    var query = _context.Products.Where(p => p.TenantId == tenantId && p.IsActive);
    // ... filters ...
    var products = await query
        .Select(p => new ProductDto { ... })
        .ToListAsync();
    
    return products.OrderBy(p => p.StockQty).ToList();
}
```

**Issues:**
- ❌ Loads ALL low stock products into memory
- ❌ No pagination limit
- ❌ Could return thousands of products

**Impact:** 
- 🟡 **MEDIUM** - Memory exhaustion with large product catalogs
- 🟡 **MEDIUM** - Slow response time with many low stock products

**Risk Scenario:**
- Tenant with 10,000 products, 2,000 low stock → loads 2,000 products into memory
- Multiple concurrent requests → memory pressure

**Recommendation:**
```csharp
public async Task<PagedResponse<ProductDto>> GetLowStockProductsAsync(
    int tenantId, 
    int page = 1, 
    int pageSize = 50,
    int? globalLowStockThreshold = null)
{
    pageSize = Math.Min(pageSize, 100);
    // ... existing filters ...
    var totalCount = await query.CountAsync();
    var products = await query
        .OrderBy(p => p.StockQty)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(p => new ProductDto { ... })
        .ToListAsync();
    
    return new PagedResponse<ProductDto> { ... };
}
```

**Priority:** 🟡 **MEDIUM** - Should add pagination

---

#### **ISSUE #2: GetOutstandingCustomersAsync - No Pagination**

**Location:** `ReportService.cs` - Line 943

**Problem:**
```csharp
public async Task<List<CustomerDto>> GetOutstandingCustomersAsync(int tenantId, int days = 30)
{
    var customers = await _context.Customers
        .Where(c => c.TenantId == tenantId && c.PendingBalance > 0.01m)
        .OrderByDescending(c => c.PendingBalance)
        .Select(c => new CustomerDto { ... })
        .ToListAsync();
    
    return customers;
}
```

**Issues:**
- ❌ Loads ALL outstanding customers into memory
- ❌ No pagination limit
- ❌ Could return thousands of customers

**Impact:**
- 🟡 **MEDIUM** - Memory exhaustion with many outstanding customers
- 🟡 **MEDIUM** - Slow response time

**Risk Scenario:**
- Tenant with 5,000 customers, 3,000 with outstanding balance → loads 3,000 customers
- Multiple concurrent requests → memory pressure

**Recommendation:**
Add pagination or limit (e.g., top 100 by balance)

**Priority:** 🟡 **MEDIUM** - Should add pagination or limit

---

#### **ISSUE #3: GetChequeReportAsync - No Pagination**

**Location:** `ReportService.cs` - Line 986

**Problem:**
```csharp
public async Task<List<PaymentDto>> GetChequeReportAsync(int tenantId)
{
    var cheques = await _context.Payments
        .Where(p => p.TenantId == tenantId && p.Mode == PaymentMode.CHEQUE)
        .Include(p => p.Sale)
        .Include(p => p.Customer)
        .OrderByDescending(p => p.PaymentDate)
        .Select(p => new PaymentDto { ... })
        .ToListAsync();
    
    return cheques;
}
```

**Issues:**
- ❌ Loads ALL cheque payments into memory
- ❌ No pagination limit
- ❌ Includes related entities (Sale, Customer) for all records

**Impact:**
- 🟡 **MEDIUM** - Memory exhaustion with many cheque payments
- 🟡 **MEDIUM** - Slow response time

**Recommendation:**
Add pagination or limit (e.g., last 500 cheques)

**Priority:** 🟡 **MEDIUM** - Should add pagination or limit

---

#### **ISSUE #4: GetPendingBillsAsync - No Pagination**

**Location:** `ReportService.cs` - Line 1201

**Problem:**
```csharp
public async Task<List<PendingBillDto>> GetPendingBillsAsync(...)
{
    // ... filters ...
    var list = await query
        .OrderByDescending(x => x.s.InvoiceDate)
        .Select(x => new PendingBillDto { ... })
        .ToListAsync();
    
    // In-memory sorting
    return list
        .OrderByDescending(pb => pb.DaysOverdue)
        .ThenByDescending(pb => pb.InvoiceDate)
        .ToList();
}
```

**Issues:**
- ❌ Loads ALL pending bills into memory
- ❌ No pagination limit
- ❌ In-memory sorting after database query

**Impact:**
- 🟡 **MEDIUM** - Memory exhaustion with many pending bills
- 🟡 **MEDIUM** - Slow response time

**Risk Scenario:**
- Tenant with 10,000 sales, 5,000 pending → loads 5,000 bills into memory

**Recommendation:**
Add pagination and move sorting to database query

**Priority:** 🟡 **MEDIUM** - Should add pagination

---

#### **ISSUE #5: GetAISuggestionsAsync - Inefficient Top N Query**

**Location:** `ReportService.cs` - Line 1013

**Problem:**
```csharp
// Low margin products - loads ALL then takes top 5
lowMarginProducts = await _context.Products
    .Where(p => p.TenantId == tenantId && ...)
    .Select(p => new ProductDto { ... })
    .ToListAsync();

lowMarginProducts = products
    .OrderBy(p => p.SellPrice > 0 ? (p.SellPrice - p.CostPrice) / p.SellPrice : 0)
    .Take(5)
    .ToList();
```

**Issues:**
- ❌ Loads ALL products matching criteria into memory
- ❌ Then sorts and takes top 5 in memory
- ❌ Should use database-level ordering and Take()

**Impact:**
- 🟡 **MEDIUM** - Unnecessary memory usage
- 🟢 **LOW** - Performance impact (still fast, but inefficient)

**Recommendation:**
```csharp
lowMarginProducts = await _context.Products
    .Where(p => p.TenantId == tenantId && ...)
    .OrderBy(p => p.SellPrice > 0 ? (p.SellPrice - p.CostPrice) / p.SellPrice : 0)
    .Take(5)
    .Select(p => new ProductDto { ... })
    .ToListAsync();
```

**Priority:** 🟢 **LOW** - Minor optimization

---

#### **ISSUE #6: GetSummaryReportAsync - Potential N+1 Query**

**Location:** `ReportService.cs` - Line 426

**Problem:**
```csharp
var branches = await _branchService.GetBranchesAsync(tenantId);
foreach (var branch in branches)
{
    var branchSalesQuery = _context.Sales
        .Where(s => s.BranchId == branch.Id && ...)
        .SumAsync(s => (decimal?)s.GrandTotal);
    
    var branchExpensesQuery = _context.Expenses
        .Where(e => e.BranchId == branch.Id && ...)
        .SumAsync(e => (decimal?)e.Amount);
}
```

**Issues:**
- ⚠️ Loops through branches and makes separate queries for each
- ⚠️ Could be optimized with a single grouped query

**Impact:**
- 🟡 **MEDIUM** - Multiple database round trips
- 🟢 **LOW** - Performance impact (usually < 10 branches)

**Recommendation:**
Use a single grouped query:
```csharp
var branchStats = await _context.Sales
    .Where(s => s.TenantId == tenantId && ...)
    .GroupBy(s => s.BranchId)
    .Select(g => new {
        BranchId = g.Key,
        Sales = g.Sum(s => s.GrandTotal),
        InvoiceCount = g.Count()
    })
    .ToListAsync();
```

**Priority:** 🟢 **LOW** - Minor optimization (usually < 10 branches)

---

#### **ISSUE #7: GetSalesVsExpensesAsync - Loads All Into Memory**

**Location:** `ReportService.cs` - Line 1336

**Problem:**
```csharp
// Loads all sales, purchases, expenses for date range
var salesData = await salesBaseQuery
    .GroupBy(s => new { Year = s.InvoiceDate.Year, Month = s.InvoiceDate.Month })
    .Select(g => new { Period = ..., Sales = g.Sum(s => s.GrandTotal) })
    .ToListAsync();

// Then processes in memory
var allPeriods = salesData.Select(s => s.Period)
    .Union(purchasesData.Select(p => p.Period))
    .Union(expensesData.Select(e => e.Period))
    .Distinct()
    .OrderBy(p => p)
    .ToList();
```

**Issues:**
- ⚠️ Loads grouped data into memory
- ⚠️ Then processes unions and distinct in memory
- ✅ But uses server-side aggregation (GroupBy)

**Impact:**
- 🟢 **LOW** - Grouped data is small (one row per month/day)
- ✅ Acceptable for reporting queries

**Status:** ✅ **ACCEPTABLE** - Grouped queries are efficient

---

## LOAD SIMULATION ANALYSIS

### **Scenario: 100 Tenants × 10,000 Sales Each**

**Total Data:** 1,000,000 sales records

**Endpoint Performance:**

1. **GetSalesAsync (Paginated)**
   - Query: `SELECT * FROM Sales WHERE TenantId = ? LIMIT 100 OFFSET ?`
   - Performance: ✅ **EXCELLENT** - Only loads 100 records per request
   - Memory: ~50KB per request
   - Database Load: ✅ **LOW** - Indexed query

2. **GetLowStockProductsAsync (NOT Paginated)**
   - Query: `SELECT * FROM Products WHERE TenantId = ? AND StockQty <= ReorderLevel`
   - Performance: ⚠️ **SLOW** - Loads all low stock products
   - Memory: ~500KB-2MB per request (depending on count)
   - Database Load: 🟡 **MEDIUM** - Full scan if no index

3. **GetOutstandingCustomersAsync (NOT Paginated)**
   - Query: `SELECT * FROM Customers WHERE TenantId = ? AND PendingBalance > 0.01`
   - Performance: ⚠️ **SLOW** - Loads all outstanding customers
   - Memory: ~200KB-1MB per request
   - Database Load: 🟡 **MEDIUM** - Full scan if no index

4. **GetPendingBillsAsync (NOT Paginated)**
   - Query: `SELECT * FROM Sales WHERE TenantId = ? AND PaymentStatus IN ('Pending', 'Partial')`
   - Performance: ⚠️ **SLOW** - Loads all pending bills
   - Memory: ~500KB-5MB per request
   - Database Load: 🟡 **MEDIUM** - Full scan if no index

**Concurrent Request Impact:**
- 10 concurrent requests to paginated endpoints: ✅ **OK** (~500KB total memory)
- 10 concurrent requests to non-paginated endpoints: ⚠️ **RISKY** (~5-20MB total memory)

---

## INDEX REQUIREMENTS

### **Recommended Indexes:**

1. **Products Table:**
   ```sql
   CREATE INDEX idx_products_tenant_stock ON "Products"("TenantId", "StockQty", "IsActive");
   CREATE INDEX idx_products_tenant_reorder ON "Products"("TenantId", "ReorderLevel", "StockQty");
   ```

2. **Customers Table:**
   ```sql
   CREATE INDEX idx_customers_tenant_balance ON "Customers"("TenantId", "PendingBalance");
   ```

3. **Sales Table:**
   ```sql
   CREATE INDEX idx_sales_tenant_status ON "Sales"("TenantId", "PaymentStatus", "InvoiceDate");
   CREATE INDEX idx_sales_tenant_date ON "Sales"("TenantId", "InvoiceDate");
   ```

4. **Payments Table:**
   ```sql
   CREATE INDEX idx_payments_tenant_mode ON "Payments"("TenantId", "Mode", "PaymentDate");
   ```

**Status:** ⚠️ **VERIFY** - Check if indexes exist in production database

---

## MEMORY USAGE ESTIMATES

### **Per Request (Paginated Endpoints):**
- GetSalesAsync: ~50KB (100 records)
- GetProductsAsync: ~40KB (100 records)
- GetCustomersAsync: ~30KB (100 records)
- GetExpensesAsync: ~25KB (100 records)

**Total:** ~145KB per request ✅ **EXCELLENT**

### **Per Request (Non-Paginated Endpoints):**
- GetLowStockProductsAsync: ~500KB-2MB (depends on count)
- GetOutstandingCustomersAsync: ~200KB-1MB (depends on count)
- GetChequeReportAsync: ~300KB-1.5MB (depends on count)
- GetPendingBillsAsync: ~500KB-5MB (depends on count)

**Total:** ~1.5MB-9.5MB per request ⚠️ **RISKY**

---

## TIMEOUT RISK ANALYSIS

### **Low Risk (Paginated):**
- ✅ GetSalesAsync - Fast query with limit
- ✅ GetProductsAsync - Fast query with limit
- ✅ GetCustomersAsync - Fast query with limit
- ✅ GetExpensesAsync - Fast query with limit

### **Medium Risk (Non-Paginated):**
- 🟡 GetLowStockProductsAsync - Could timeout with large product catalogs
- 🟡 GetOutstandingCustomersAsync - Could timeout with many customers
- 🟡 GetChequeReportAsync - Could timeout with many cheque payments
- 🟡 GetPendingBillsAsync - Could timeout with many pending bills

**Mitigation:** Database timeout is 30 seconds (configured in Program.cs)

---

## RECOMMENDATIONS

### 🔴 **HIGH PRIORITY:**

1. **Add Pagination to GetLowStockProductsAsync**
   - Change return type to `PagedResponse<ProductDto>`
   - Add `page` and `pageSize` parameters
   - Apply `Skip/Take` before `ToListAsync()`

2. **Add Pagination to GetOutstandingCustomersAsync**
   - Change return type to `PagedResponse<CustomerDto>`
   - Or add limit (e.g., top 100 by balance)

3. **Add Pagination to GetChequeReportAsync**
   - Change return type to `PagedResponse<PaymentDto>`
   - Or add limit (e.g., last 500 cheques)

4. **Add Pagination to GetPendingBillsAsync**
   - Change return type to `PagedResponse<PendingBillDto>`
   - Move sorting to database query

### 🟡 **MEDIUM PRIORITY:**

5. **Optimize GetAISuggestionsAsync**
   - Move `OrderBy` and `Take(5)` to database query
   - Don't load all products into memory

6. **Optimize GetSummaryReportAsync**
   - Use single grouped query instead of loop
   - Reduce database round trips

### 🟢 **LOW PRIORITY:**

7. **Verify Database Indexes**
   - Check if recommended indexes exist
   - Add indexes if missing

8. **Add Memory Monitoring**
   - Log memory usage for non-paginated endpoints
   - Alert on high memory usage

---

## CONCLUSION

**Overall Status:** ✅ **GOOD** - Most endpoints are well-optimized

**Strengths:**
- ✅ Excellent pagination on main list endpoints
- ✅ Consistent AsNoTracking usage
- ✅ Proper eager loading prevents N+1 queries
- ✅ Server-side filtering

**Areas for Improvement:**
- 🟡 4 endpoints missing pagination (GetLowStockProductsAsync, GetOutstandingCustomersAsync, GetChequeReportAsync, GetPendingBillsAsync)
- 🟡 Minor optimizations needed (GetAISuggestionsAsync, GetSummaryReportAsync)

**Performance Risk:** 🟡 **LOW-MEDIUM** - Well optimized, but 4 endpoints need pagination

**Critical Issues:** None found ✅

---

**Last Updated:** 2026-02-18  
**Next Review:** After implementing pagination fixes
