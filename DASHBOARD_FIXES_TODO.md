# Dashboard Fixes — TODO List & Implementation Plan

**Page:** DashboardTally.jsx  
**Backend:** ReportService.GetSummaryReportAsync  
**Priority:** HIGH (Dashboard is first thing users see)

---

## 🔴 CRITICAL ISSUES (Fix First)

### ISSUE 1: Profit Today Calculation is Wrong

**Problem:**
- Current: `profitToday = (salesToday - purchasesToday) - expensesToday`
- Uses `purchasesToday` (cash flow) instead of COGS (Cost of Goods Sold from actual sales)
- If client buys AED 10,000 stock at start of month and sells AED 500 today → profit shows NEGATIVE
- Label says "Profit Today" but shows cash flow, not gross margin

**Root Cause:**
- `ReportService.cs` line 158: `var grossProfit = salesToday - purchasesToday;`
- Should calculate COGS from SaleItems × Product.CostPrice for TODAY's sales only

**Fix Required:**

**Backend (`ReportService.cs`):**
```csharp
// OLD (WRONG):
var grossProfit = salesToday - purchasesToday;
var profitToday = grossProfit - expensesToday;

// NEW (CORRECT):
// Calculate COGS from TODAY's sales only
var cogsToday = await (from si in _context.SaleItems
    join s in _context.Sales on si.SaleId equals s.Id
    join p in _context.Products on si.ProductId equals p.Id
    where s.TenantId == tenantId 
        && !s.IsDeleted 
        && s.InvoiceDate >= from 
        && s.InvoiceDate < to
    select new { si.Qty, p.CostPrice, si.UnitType, p.ConversionToBase })
    .ToListAsync();

var cogsTodayAmount = cogsToday.Sum(x => {
    var baseQty = x.Qty * x.ConversionToBase; // Convert to base units
    return baseQty * x.CostPrice; // COGS = base qty × cost per base unit
});

var grossProfit = salesToday - cogsTodayAmount; // Sales - COGS
var profitToday = grossProfit - expensesToday; // Gross Profit - Expenses
```

**Frontend (`DashboardTally.jsx`):**
- No change needed (just displays backend value)
- Consider renaming label to "Gross Profit Today" if needed

**TODO:**
- [ ] Update `ReportService.GetSummaryReportAsync` to calculate COGS from SaleItems
- [ ] Add `CogsToday` field to `SummaryReportDto`
- [ ] Test: Buy stock yesterday, sell today → profit should be positive
- [ ] Test: No sales today → profit should be 0 (not negative from purchases)

---

### ISSUE 2: Pending Bills Count Loads ALL Sales into RAM

**Problem:**
- `ReportService.cs` line 227: `var allSales = await allSalesQuery.ToListAsync();`
- Loads ALL sales into memory, then filters in C#: `pendingSales = allSales.Where(...)`
- At 10,000 invoices per tenant → slow dashboard load, high memory usage
- Owner sees spinner on every dashboard load

**Root Cause:**
- Database query doesn't filter by balance, filters in C# after loading all rows

**Fix Required:**

**Backend (`ReportService.cs`):**
```csharp
// OLD (WRONG - loads all sales):
var allSalesQuery = _context.Sales.Where(s => !s.IsDeleted);
if (tenantId > 0) allSalesQuery = allSalesQuery.Where(s => s.TenantId == tenantId);
var allSales = await allSalesQuery.ToListAsync();
var pendingSales = allSales.Where(s => (s.GrandTotal - s.PaidAmount) > 0.01m).ToList();

// NEW (CORRECT - filter in database):
var pendingBillsQuery = _context.Sales
    .Where(s => !s.IsDeleted 
        && (s.GrandTotal - s.PaidAmount) > 0.01m); // Filter in SQL
if (tenantId > 0) pendingBillsQuery = pendingBillsQuery.Where(s => s.TenantId == tenantId);
var pendingBillsCount = await pendingBillsQuery.CountAsync(); // Count in database
var pendingBillsAmount = await pendingBillsQuery.SumAsync(s => s.GrandTotal - s.PaidAmount) ?? 0m;

var paidBillsQuery = _context.Sales
    .Where(s => !s.IsDeleted 
        && (s.GrandTotal - s.PaidAmount) <= 0.01m);
if (tenantId > 0) paidBillsQuery = paidBillsQuery.Where(s => s.TenantId == tenantId);
var paidBillsCount = await paidBillsQuery.CountAsync();
var paidBillsAmount = await paidBillsQuery.SumAsync(s => s.GrandTotal) ?? 0m;
```

**TODO:**
- [ ] Replace `allSales.ToListAsync()` with database-level filtering
- [ ] Use `.CountAsync()` and `.SumAsync()` instead of loading all rows
- [ ] Test with 10,000+ invoices → should load in < 1 second
- [ ] Remove `pendingInvoices` list if not needed (or load separately with pagination)

---

## 🟡 HIGH PRIORITY ISSUES

### ISSUE 3: Auto-Refresh is 2 Minutes (Too Slow)

**Problem:**
- `DashboardTally.jsx` line 88-92: `setInterval(..., 120000)` = 2 minutes
- If staff creates invoice, owner's dashboard won't update for 2 minutes
- Should be 30 seconds or event-driven

**Current Code:**
```javascript
interval = setInterval(() => {
    if (document.visibilityState === 'visible' && !isFetchingRef.current) {
        fetchStatsThrottled()
    }
}, 120000) // 2 minutes
```

**Fix Required:**

**Option A: Reduce to 30 seconds**
```javascript
interval = setInterval(() => {
    if (document.visibilityState === 'visible' && !isFetchingRef.current) {
        fetchStatsThrottled()
    }
}, 30000) // 30 seconds
```

**Option B: Event-Driven (Better)**
- Already has event listeners: `dataUpdated`, `paymentCreated`, `customerCreated`
- Add event dispatch from POS when invoice created:
```javascript
// In PosPage.jsx after successful sale:
window.dispatchEvent(new CustomEvent('invoiceCreated', { detail: { saleId } }));
```

- Update DashboardTally.jsx:
```javascript
window.addEventListener('invoiceCreated', handleDataUpdate);
window.addEventListener('paymentCreated', handleDataUpdate);
window.addEventListener('expenseCreated', handleDataUpdate);
```

**TODO:**
- [ ] Reduce auto-refresh to 30 seconds OR
- [ ] Add `invoiceCreated` event dispatch from POS
- [ ] Add `expenseCreated` event dispatch from Expenses page
- [ ] Test: Create invoice → dashboard updates within 5 seconds (debounce)

---

### ISSUE 4: No Date Selector on Dashboard

**Problem:**
- Dashboard only shows TODAY's data (hardcoded `todayStr`)
- Owner cannot switch to "this week" or "this month" without going to Reports
- Competitors show weekly/monthly toggle on main dashboard

**Current Code:**
```javascript
const today = new Date()
const todayStr = today.toISOString().split('T')[0]
const response = await reportsAPI.getSummaryReport({
    fromDate: todayStr,
    toDate: todayStr
})
```

**Fix Required:**

**Frontend (`DashboardTally.jsx`):**
1. Add date range selector (Today / This Week / This Month / Custom)
2. Add state for selected period
3. Update `fetchStats` to use selected period

```javascript
const [dateRange, setDateRange] = useState('today') // 'today' | 'week' | 'month' | 'custom'
const [customFromDate, setCustomFromDate] = useState('')
const [customToDate, setCustomToDate] = useState('')

const getDateRange = () => {
    const today = new Date()
    switch(dateRange) {
        case 'today':
            const todayStr = today.toISOString().split('T')[0]
            return { from: todayStr, to: todayStr }
        case 'week':
            const weekStart = new Date(today)
            weekStart.setDate(today.getDate() - today.getDay()) // Start of week (Sunday)
            return { from: weekStart.toISOString().split('T')[0], to: today.toISOString().split('T')[0] }
        case 'month':
            const monthStart = new Date(today.getFullYear(), today.getMonth(), 1)
            return { from: monthStart.toISOString().split('T')[0], to: today.toISOString().split('T')[0] }
        case 'custom':
            return { from: customFromDate, to: customToDate }
        default:
            const todayStr2 = today.toISOString().split('T')[0]
            return { from: todayStr2, to: todayStr2 }
    }
}

const fetchStats = async () => {
    const { from, to } = getDateRange()
    const response = await reportsAPI.getSummaryReport({
        fromDate: from,
        toDate: to
    })
    // ... rest of code
}
```

**UI Addition:**
```jsx
<div className="flex items-center gap-2 mb-4">
    <button onClick={() => setDateRange('today')} className={dateRange === 'today' ? 'active' : ''}>Today</button>
    <button onClick={() => setDateRange('week')} className={dateRange === 'week' ? 'active' : ''}>This Week</button>
    <button onClick={() => setDateRange('month')} className={dateRange === 'month' ? 'active' : ''}>This Month</button>
    <button onClick={() => setDateRange('custom')}>Custom</button>
    {dateRange === 'custom' && (
        <>
            <input type="date" value={customFromDate} onChange={e => setCustomFromDate(e.target.value)} />
            <span>to</span>
            <input type="date" value={customToDate} onChange={e => setCustomToDate(e.target.value)} />
        </>
    )}
</div>
```

**TODO:**
- [ ] Add date range selector UI (Today / Week / Month / Custom)
- [ ] Update `fetchStats` to use selected date range
- [ ] Update card labels: "Sales Today" → "Sales" (or "Sales (Today)" when period selected)
- [ ] Test: Switch to "This Week" → shows weekly totals
- [ ] Test: Switch to "This Month" → shows monthly totals

---

### ISSUE 5: Expenses Today Shows Only Admin/Owner Role

**Problem:**
- Expenses card only visible to `isAdminOrOwner(user)`
- If owner delegates to manager (Admin role), manager sees expenses
- But branch manager who is "Owner" of sub-tenant can't see their branch expenses specifically
- Only shows total company expenses, not branch-specific

**Current Code:**
```javascript
{isAdminOrOwner(user) && (
    <div className="bg-white rounded-lg border border-neutral-200 p-4">
        <p className="text-sm text-neutral-500">Expenses Today</p>
        <p className="text-lg font-semibold text-neutral-900">{formatCurrency(stats.expensesToday)}</p>
    </div>
)}
```

**Fix Required:**

**Backend (`ReportService.cs`):**
- Already supports `branchId` parameter in `GetSummaryReportAsync`
- Need to pass branchId from frontend if user is branch-scoped

**Frontend (`DashboardTally.jsx`):**
1. Check if user is Staff with assigned branches
2. If single branch assigned → show branch-specific expenses
3. If multiple branches → show combined or allow branch selector

```javascript
const { user } = useAuth()
const [selectedBranchId, setSelectedBranchId] = useState(null)

// Get user's assigned branches (if Staff)
useEffect(() => {
    if (user && user.role?.toLowerCase() === 'staff') {
        // Fetch assigned branches from getMyAssignedRoutes()
        usersAPI.getMyAssignedRoutes().then(res => {
            const branchIds = res?.data?.assignedBranchIds || []
            if (branchIds.length === 1) {
                setSelectedBranchId(branchIds[0]) // Auto-select single branch
            }
        })
    }
}, [user])

const fetchStats = async () => {
    const { from, to } = getDateRange()
    const response = await reportsAPI.getSummaryReport({
        fromDate: from,
        toDate: to,
        branchId: selectedBranchId || undefined // Pass branchId if selected
    })
    // ... rest
}
```

**UI Update:**
```jsx
{isAdminOrOwner(user) || (user?.role?.toLowerCase() === 'staff' && selectedBranchId) ? (
    <div className="bg-white rounded-lg border border-neutral-200 p-4">
        <p className="text-sm text-neutral-500">
            Expenses {selectedBranchId ? `(Branch)` : 'Today'}
        </p>
        <p className="text-lg font-semibold text-neutral-900">
            {formatCurrency(stats.expensesToday)}
        </p>
    </div>
) : null}
```

**TODO:**
- [ ] Check if Staff user has assigned branches
- [ ] If single branch → auto-select and show branch expenses
- [ ] If multiple branches → add branch selector dropdown
- [ ] Update backend to filter expenses by branchId when provided
- [ ] Test: Staff with single branch → sees only their branch expenses
- [ ] Test: Owner → sees all company expenses (no branch filter)

---

### ISSUE 6: No Branch Breakdown on Dashboard

**Problem:**
- Dashboard shows combined total only (all branches combined)
- If company has 3 branches, owner needs "Branch A: 5000, Branch B: 3000, Branch C: 2000" at a glance
- No way to see branch-wise performance without going to Reports

**Fix Required:**

**Backend (`ReportService.cs`):**
- Add `BranchBreakdown` field to `SummaryReportDto`:
```csharp
public class SummaryReportDto
{
    // ... existing fields
    public List<BranchSummaryDto> BranchBreakdown { get; set; } = new();
}

public class BranchSummaryDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; }
    public decimal Sales { get; set; }
    public decimal Expenses { get; set; }
    public decimal Profit { get; set; }
    public int InvoiceCount { get; set; }
}
```

- In `GetSummaryReportAsync`, add branch breakdown:
```csharp
var branchBreakdown = await (from b in _context.Branches
    where b.TenantId == tenantId
    select new {
        BranchId = b.Id,
        BranchName = b.Name,
        Sales = _context.Sales
            .Where(s => s.BranchId == b.Id 
                && !s.IsDeleted 
                && s.InvoiceDate >= from 
                && s.InvoiceDate < to)
            .Sum(s => (decimal?)s.GrandTotal) ?? 0m,
        Expenses = _context.Expenses
            .Where(e => e.BranchId == b.Id 
                && e.Date >= from 
                && e.Date < to)
            .Sum(e => (decimal?)e.Amount) ?? 0m,
        InvoiceCount = _context.Sales
            .Where(s => s.BranchId == b.Id 
                && !s.IsDeleted 
                && s.InvoiceDate >= from 
                && s.InvoiceDate < to)
            .Count()
    })
    .ToListAsync();

var branchBreakdownDto = branchBreakdown.Select(b => new BranchSummaryDto
{
    BranchId = b.BranchId,
    BranchName = b.BranchName,
    Sales = b.Sales,
    Expenses = b.Expenses,
    Profit = b.Sales - b.Expenses, // Simplified (no COGS per branch in this view)
    InvoiceCount = b.InvoiceCount
}).OrderByDescending(b => b.Sales).ToList();

result.BranchBreakdown = branchBreakdownDto;
```

**Frontend (`DashboardTally.jsx`):**
```jsx
{data.branchBreakdown && data.branchBreakdown.length > 0 && (
    <div className="bg-white rounded-lg border border-neutral-200 p-4">
        <h3 className="text-sm font-medium text-neutral-700 mb-3">Branch Breakdown</h3>
        <div className="space-y-2">
            {data.branchBreakdown.map(branch => (
                <div key={branch.branchId} className="flex items-center justify-between text-sm">
                    <span className="font-medium">{branch.branchName}:</span>
                    <span className="text-neutral-900">{formatCurrency(branch.sales)}</span>
                    <span className="text-neutral-500">({branch.invoiceCount} invoices)</span>
                </div>
            ))}
        </div>
    </div>
)}
```

**TODO:**
- [ ] Add `BranchBreakdown` to `SummaryReportDto`
- [ ] Calculate branch-wise sales, expenses, profit in backend
- [ ] Add branch breakdown card to dashboard UI
- [ ] Show top 5 branches (or all if < 5)
- [ ] Make branch names clickable → link to `/branches/{branchId}`
- [ ] Test: Company with 3 branches → shows 3 rows
- [ ] Test: Company with 1 branch → shows 1 row or hides card

---

## 🟢 MEDIUM PRIORITY ISSUES

### ISSUE 7: No Weekly/Monthly Sales Trend Mini-Chart

**Problem:**
- Backend already returns `invoicesWeekly` and `invoicesMonthly` counts
- But no amount trend (daily sales for last 7 days)
- A simple 7-bar sparkline showing daily sales would be valuable

**Fix Required:**

**Backend (`ReportService.cs`):**
- Add `DailySalesTrend` field to `SummaryReportDto`:
```csharp
public class DailySalesDto
{
    public string Date { get; set; } // YYYY-MM-DD
    public decimal Sales { get; set; }
    public int InvoiceCount { get; set; }
}

public class SummaryReportDto
{
    // ... existing fields
    public List<DailySalesDto> DailySalesTrend { get; set; } = new(); // Last 7 days
}
```

- Calculate daily sales for last 7 days:
```csharp
var sevenDaysAgo = today.AddDays(-7);
var dailySalesTrend = await (from s in _context.Sales
    where s.TenantId == tenantId 
        && !s.IsDeleted 
        && s.InvoiceDate >= sevenDaysAgo 
        && s.InvoiceDate < today.AddDays(1)
    group s by s.InvoiceDate.Date into g
    select new DailySalesDto
    {
        Date = g.Key.ToString("yyyy-MM-dd"),
        Sales = g.Sum(s => s.GrandTotal),
        InvoiceCount = g.Count()
    })
    .OrderBy(d => d.Date)
    .ToListAsync();

result.DailySalesTrend = dailySalesTrend;
```

**Frontend (`DashboardTally.jsx`):**
- Install `recharts` (if not already installed)
- Add mini bar chart:
```jsx
import { BarChart, Bar, XAxis, YAxis, ResponsiveContainer, Tooltip } from 'recharts'

{data.dailySalesTrend && data.dailySalesTrend.length > 0 && (
    <div className="bg-white rounded-lg border border-neutral-200 p-4">
        <h3 className="text-sm font-medium text-neutral-700 mb-3">Sales Trend (Last 7 Days)</h3>
        <ResponsiveContainer width="100%" height={150}>
            <BarChart data={data.dailySalesTrend}>
                <XAxis dataKey="date" tickFormatter={d => new Date(d).toLocaleDateString('en', { weekday: 'short' })} />
                <YAxis tickFormatter={v => `${(v/1000).toFixed(0)}k`} />
                <Tooltip formatter={(v) => formatCurrency(v)} />
                <Bar dataKey="sales" fill="#3B82F6" />
            </BarChart>
        </ResponsiveContainer>
    </div>
)}
```

**TODO:**
- [ ] Add `DailySalesTrend` to `SummaryReportDto`
- [ ] Calculate daily sales for last 7 days in backend
- [ ] Add mini bar chart to dashboard UI
- [ ] Test: Shows 7 bars (one per day)
- [ ] Test: Empty days show 0 (no gap)

---

### ISSUE 8: No Top Customer or Top Product Today

**Problem:**
- Quick wins for owner insight missing
- Owner wants to see "Top customer today: ABC Company (AED 5,000)"
- Owner wants to see "Top product today: Product XYZ (50 units sold)"

**Fix Required:**

**Backend (`ReportService.cs`):**
- Add `TopCustomersToday` and `TopProductsToday` to `SummaryReportDto`:
```csharp
public class TopCustomerDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; }
    public decimal TotalSales { get; set; }
    public int InvoiceCount { get; set; }
}

public class TopProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalQty { get; set; }
    public string UnitType { get; set; }
}

public class SummaryReportDto
{
    // ... existing fields
    public List<TopCustomerDto> TopCustomersToday { get; set; } = new(); // Top 5
    public List<TopProductDto> TopProductsToday { get; set; } = new(); // Top 5
}
```

- Calculate top customers:
```csharp
var topCustomers = await (from s in _context.Sales
    join c in _context.Customers on s.CustomerId equals c.Id
    where s.TenantId == tenantId 
        && !s.IsDeleted 
        && s.InvoiceDate >= from 
        && s.InvoiceDate < to
        && s.CustomerId != null
    group s by new { c.Id, c.Name } into g
    select new TopCustomerDto
    {
        CustomerId = g.Key.Id,
        CustomerName = g.Key.Name,
        TotalSales = g.Sum(s => s.GrandTotal),
        InvoiceCount = g.Count()
    })
    .OrderByDescending(c => c.TotalSales)
    .Take(5)
    .ToListAsync();

result.TopCustomersToday = topCustomers;
```

- Calculate top products:
```csharp
var topProducts = await (from si in _context.SaleItems
    join s in _context.Sales on si.SaleId equals s.Id
    join p in _context.Products on si.ProductId equals p.Id
    where s.TenantId == tenantId 
        && !s.IsDeleted 
        && s.InvoiceDate >= from 
        && s.InvoiceDate < to
    group si by new { p.Id, p.NameEn, si.UnitType } into g
    select new TopProductDto
    {
        ProductId = g.Key.Id,
        ProductName = g.Key.NameEn,
        TotalSales = g.Sum(si => si.LineTotal),
        TotalQty = g.Sum(si => si.Qty),
        UnitType = g.Key.UnitType
    })
    .OrderByDescending(p => p.TotalSales)
    .Take(5)
    .ToListAsync();

result.TopProductsToday = topProducts;
```

**Frontend (`DashboardTally.jsx`):**
```jsx
{data.topCustomersToday && data.topCustomersToday.length > 0 && (
    <div className="bg-white rounded-lg border border-neutral-200 p-4">
        <h3 className="text-sm font-medium text-neutral-700 mb-3">Top Customers {dateRange === 'today' ? 'Today' : ''}</h3>
        <div className="space-y-2">
            {data.topCustomersToday.map((customer, idx) => (
                <div key={customer.customerId} className="flex items-center justify-between text-sm">
                    <span className="text-neutral-600">#{idx + 1} {customer.customerName}</span>
                    <span className="font-medium">{formatCurrency(customer.totalSales)}</span>
                    <span className="text-neutral-500">({customer.invoiceCount} invoices)</span>
                </div>
            ))}
        </div>
    </div>
)}

{data.topProductsToday && data.topProductsToday.length > 0 && (
    <div className="bg-white rounded-lg border border-neutral-200 p-4">
        <h3 className="text-sm font-medium text-neutral-700 mb-3">Top Products {dateRange === 'today' ? 'Today' : ''}</h3>
        <div className="space-y-2">
            {data.topProductsToday.map((product, idx) => (
                <div key={product.productId} className="flex items-center justify-between text-sm">
                    <span className="text-neutral-600">#{idx + 1} {product.productName}</span>
                    <span className="font-medium">{formatCurrency(product.totalSales)}</span>
                    <span className="text-neutral-500">({product.totalQty} {product.unitType})</span>
                </div>
            ))}
        </div>
    </div>
)}
```

**TODO:**
- [ ] Add `TopCustomersToday` and `TopProductsToday` to `SummaryReportDto`
- [ ] Calculate top 5 customers by sales amount
- [ ] Calculate top 5 products by sales amount
- [ ] Add "Top Customers" card to dashboard UI
- [ ] Add "Top Products" card to dashboard UI
- [ ] Make customer/product names clickable → link to detail pages
- [ ] Test: Shows top 5 customers/products
- [ ] Test: Empty period → shows empty list (not error)

---

## ✅ GOOD PRACTICES (Keep These)

1. **Request Throttling:** 10 second minimum between fetches ✅
2. **Auto-refresh stops when tab not visible** ✅
3. **Role-based visibility** (Staff can't see profit) ✅
4. **Event-driven updates** (already has `dataUpdated`, `paymentCreated` events) ✅

---

## 📋 COMPLETE TODO CHECKLIST

### Backend Changes (`ReportService.cs`, `DTOs.cs`):

- [ ] **ISSUE 1:** Fix profit calculation (use COGS from SaleItems, not purchasesToday)
- [ ] **ISSUE 1:** Add `CogsToday` field to `SummaryReportDto`
- [ ] **ISSUE 2:** Replace `allSales.ToListAsync()` with database-level filtering for pending bills
- [ ] **ISSUE 6:** Add `BranchBreakdown` field to `SummaryReportDto`
- [ ] **ISSUE 6:** Calculate branch-wise sales, expenses, profit
- [ ] **ISSUE 7:** Add `DailySalesTrend` field to `SummaryReportDto`
- [ ] **ISSUE 7:** Calculate daily sales for last 7 days
- [ ] **ISSUE 8:** Add `TopCustomersToday` and `TopProductsToday` to `SummaryReportDto`
- [ ] **ISSUE 8:** Calculate top 5 customers by sales
- [ ] **ISSUE 8:** Calculate top 5 products by sales

### Frontend Changes (`DashboardTally.jsx`):

- [ ] **ISSUE 3:** Reduce auto-refresh to 30 seconds OR add `invoiceCreated` event listener
- [ ] **ISSUE 4:** Add date range selector (Today / Week / Month / Custom)
- [ ] **ISSUE 4:** Update `fetchStats` to use selected date range
- [ ] **ISSUE 4:** Update card labels based on selected period
- [ ] **ISSUE 5:** Check if Staff user has assigned branches
- [ ] **ISSUE 5:** Add branch selector for Staff with multiple branches
- [ ] **ISSUE 5:** Pass `branchId` to API when branch selected
- [ ] **ISSUE 6:** Add branch breakdown card to dashboard UI
- [ ] **ISSUE 6:** Make branch names clickable → link to branch detail
- [ ] **ISSUE 7:** Add mini bar chart for sales trend (last 7 days)
- [ ] **ISSUE 7:** Install `recharts` if not already installed
- [ ] **ISSUE 8:** Add "Top Customers" card to dashboard UI
- [ ] **ISSUE 8:** Add "Top Products" card to dashboard UI
- [ ] **ISSUE 8:** Make customer/product names clickable → link to detail pages

### Testing:

- [ ] **ISSUE 1:** Test profit calculation: Buy stock yesterday, sell today → profit positive
- [ ] **ISSUE 1:** Test profit calculation: No sales today → profit 0 (not negative)
- [ ] **ISSUE 2:** Test pending bills: 10,000+ invoices → loads in < 1 second
- [ ] **ISSUE 3:** Test auto-refresh: Create invoice → dashboard updates within 5 seconds
- [ ] **ISSUE 4:** Test date selector: Switch to "This Week" → shows weekly totals
- [ ] **ISSUE 4:** Test date selector: Switch to "This Month" → shows monthly totals
- [ ] **ISSUE 5:** Test branch expenses: Staff with single branch → sees only their branch
- [ ] **ISSUE 6:** Test branch breakdown: Company with 3 branches → shows 3 rows
- [ ] **ISSUE 7:** Test sales trend: Shows 7 bars (one per day)
- [ ] **ISSUE 8:** Test top customers: Shows top 5 customers by sales
- [ ] **ISSUE 8:** Test top products: Shows top 5 products by sales

---

## 🎯 IMPLEMENTATION ORDER

**Phase 1 (Critical - Fix First):**
1. ISSUE 1: Fix profit calculation (COGS)
2. ISSUE 2: Fix pending bills performance

**Phase 2 (High Priority):**
3. ISSUE 3: Reduce auto-refresh to 30 seconds
4. ISSUE 4: Add date selector
5. ISSUE 5: Add branch-specific expenses for Staff
6. ISSUE 6: Add branch breakdown

**Phase 3 (Medium Priority):**
7. ISSUE 7: Add sales trend chart
8. ISSUE 8: Add top customers/products

---

**Estimated Time:**
- Phase 1: 2-3 hours
- Phase 2: 4-5 hours
- Phase 3: 3-4 hours
- **Total: 9-12 hours**

---

*Last updated: Feb 2026. Update as issues are fixed.*
