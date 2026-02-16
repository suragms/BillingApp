# HexaBill Owner Growth & Build Plan

**Purpose:** Roadmap for improving owner experience, saving time, profit analytics, and feature gaps. Focus on helping business owners grow and make better decisions.

**Last Updated:** 2026-02-16

---

## 1. Issues Fixed (This Session)

| Area | Issue | Fix |
|------|-------|-----|
| **Sales Ledger** | Branch/route filter showed no related data | Backend now filters payments by branch/route; only shows payments for filtered sales. Staff filter added (`staffId` API param). |
| **Sales Ledger** | Staff filter did nothing | Added `staffId` to `/api/reports/sales-ledger`; filters sales by `CreatedBy`. |
| **Reports** | Every filter change triggered refresh | Added "Apply Filters" button; filters are draft until Apply. No more refresh on every dropdown change. |
| **Branch/Route Profit** | Profit = Sales − Expenses (ignored COGS) | Added Cost of Goods Sold (COGS) from `Product.CostPrice × qty` per sale. Profit = Sales − COGS − Expenses. |

---

## 2. Feature Gaps & Recommendations

### 2.1 Branch/Route Staff Assignment

| Need | Current | Recommended |
|------|---------|-------------|
| Assign staff when creating branch | Staff assigned via Users page only | Add staff multi-select in Branch create/edit. |
| Assign staff when creating route | Same | Add staff multi-select in Route create/edit. |
| Owner can edit assignments anytime | Yes (Users page) | Keep; add quick-edit from Branch/Route detail. |
| Staff can add expenses? | Expense creation has role checks | Add permission: "Can Add Expenses" per branch/route or role. |

**Implementation:** Extend `BranchDetailPage` and `RouteDetailPage` with inline staff assignment. Use `adminAPI.updateUser` with `assignedBranchIds`/`assignedRouteIds`. Add "Assign Staff" button/modal on branch/route forms.

### 2.2 Permissions Plan

| Permission | Owner | Admin | Staff |
|------------|-------|-------|-------|
| View all branches/routes | ✓ | ✓ | Filtered by assignment |
| Create/edit branch/route | ✓ | ✓ | ✗ |
| Assign staff to branch/route | ✓ | ✓ | ✗ |
| Add expenses (branch/route) | ✓ | ✓ | Configurable |
| View reports (all) | ✓ | ✓ | Filtered |
| Clear data / Reset | ✓ | ✗ | ✗ |

**Recommendation:** Add a "Permissions" matrix in Settings or Users, editable by Owner. Store in `User` or a `UserPermission` table.

### 2.3 Purchases vs Branches/Routes

**Current:** Purchases are tenant-level only. No `BranchId` or `RouteId` on Purchase.

**Impact:** Branch/route profit uses COGS from Product.CostPrice (sale items), not actual purchase cost. This is acceptable for most use cases.

**Future:** If branch-level inventory/purchasing is needed, add `BranchId` to Purchase and update reporting.

---

## 3. Build Tasks (Prioritized)

### Phase A — Quick Wins (1–2 days)

1. **Sales Ledger — ensure sales have BranchId/RouteId**  
   When creating a sale from POS, set `BranchId` and `RouteId` from selected branch/route or user's default. Verify existing sales have these set.

2. **Reports — Apply Filters UX**  
   ✓ Done. Date range still triggers fetch on change; filters require Apply.

3. **Branch/Route — COGS in profit**  
   ✓ Done. Profit = Sales − COGS − Expenses.

### Phase B — Staff & Permissions (2–3 days)

4. **Branch create/edit — staff assignment**  
   Add multi-select for staff when creating/editing branch. Call `adminAPI.updateUser` for each selected user's `assignedBranchIds`.

5. **Route create/edit — staff assignment**  
   Same for routes. Update `assignedRouteIds`.

6. **Permissions — "Can Add Expenses"**  
   Add role or per-user flag. Check in expense creation flow.

### Phase C — Analytics & Time Savings (3–5 days)

7. **Dashboard — key metrics by branch**  
   Add branch filter to Dashboard KPIs. Show top branch, route performance.

8. **Reports — export with filters**  
   Ensure PDF/Excel export respects applied filters (branch, route, date).

9. **Alerts — low stock by branch/route**  
   If inventory is route-scoped, alert when a route's products are low.

### Phase D — Advanced (Future)

10. **Purchase by branch**  
    Add `BranchId` to Purchase; branch-level purchase reports.

11. **Commission tracking**  
    Staff performance → commission calculation based on sales by route.

12. **Mobile/offline POS**  
    Sync when online; support field sales.

---

## 4. Folder & File Reference

| Purpose | Path |
|---------|------|
| Pre-production checklist | `docs/PRE_PRODUCTION_TEST_CHECKLIST.md` |
| Branch service (COGS, profit) | `backend/HexaBill.Api/Modules/Branches/BranchService.cs` |
| Route service | `backend/HexaBill.Api/Modules/Branches/RouteService.cs` |
| Sales Ledger API | `backend/HexaBill.Api/Modules/Reports/ReportService.cs` |
| Reports controller | `backend/HexaBill.Api/Modules/Reports/ReportsController.cs` |
| Sales Ledger page | `frontend/hexabill-ui/src/pages/company/SalesLedgerPage.jsx` |
| Reports page | `frontend/hexabill-ui/src/pages/company/ReportsPage.jsx` |
| Branch detail | `frontend/hexabill-ui/src/pages/company/BranchDetailPage.jsx` |
| Users (staff assignment) | `frontend/hexabill-ui/src/pages/company/UsersPage.jsx` |

---

## 5. What's Missing for Owner Growth

- **Time savings:** Apply Filters reduces unnecessary refreshes. Consider saved filter presets (e.g. "This Month – Branch X").
- **Profit visibility:** COGS now in branch/route. Consider gross margin % and trend charts.
- **Staff accountability:** Staff Performance report exists. Add commission rules or targets.
- **Predictive:** AI Insights tab exists. Expand with restock suggestions, overdue customer reminders.
- **Multi-branch comparison:** Branch report has chart. Add period-over-period comparison.

---

*Build plan tasks can be tracked in project management. Complete Phase A first, then prioritize B–D based on user feedback.*
