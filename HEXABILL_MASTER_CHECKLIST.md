# HexaBill — Master Implementation Checklist

**Status as of:** February 2026 (Post-Codebase Audit)  
**Based on:** HexaBill Master Enterprise Analysis v2 + Full Code Verification

---

## 🔴 SEVERITY 1 — DATA CORRUPTION / MONEY ERRORS

| # | Item | Status | Notes |
|---|------|--------|-------|
| RISK-1a | Add `{ id: 'action-key' }` to financial action toasts | ⚠️ Partial | Some pages use it (BranchDetailPage, CustomerLedgerPage). Many still don't. |
| RISK-1b | Disable submit buttons during loading (all modals) | ✅ Done | PaymentModal, forms use `disabled={loading}`. |
| RISK-1c | Toaster limit to 3 visible toasts | ✅ Done | `main.jsx` has `LimitedToaster` with `TOAST_LIMIT = 3`. |
| RISK-2 | Sequential ledger operations (no concurrent payment+refresh) | ✅ Done | CustomerLedgerPage blocks concurrent ops, balance skeleton, post-payment recalc. |
| RISK-3 | POS: Remove pre-fetched invoice number, show "Auto-generated" | ✅ Done | PosPage shows `(Auto-generated)`; no `getNextInvoiceNumber` call. |
| RISK-4 | Backend: Validate Staff user is assigned to route on invoice create | ✅ Done | SalesController validates route for Staff before CreateSale. RouteScopeService includes AssignedStaffId. |

---

## 🔴 SEVERITY 2 — SUPER ADMIN CONTROL GAPS

| # | Item | Status | Notes |
|---|------|--------|-------|
| RISK-5 | Tenant Activity Monitor (API calls, rate limiting UI) | ❌ Pending | Not implemented. |
| RISK-5 | Force Single User Logout | ❌ Pending | Not implemented. |
| RISK-5 | Maintenance Mode Toggle | ❌ Pending | Not implemented. |
| RISK-5 | Real Subscription MRR Dashboard | ⚠️ Partial | Subscriptions page has MRR; document wants "Platform MRR" vs "Tenant Sales" split. |
| RISK-6 | SuperAdmin Settings page full rebuild | ✅ Done | 6 tabs: Defaults, Features, Communication, Announcement, Security, Help. |
| RISK-7 | Audit Logs capture SuperAdmin's own actions | ✅ Done | SuperAdminTenantController writes audit for Suspend, Activate, CreateTenant, ClearData, UpdateSubscription. |

---

## 🟠 SEVERITY 3 — UI/UX CRITICAL MISTAKES

| # | Item | Status | Notes |
|---|------|--------|-------|
| ISSUE-1 | Silent cart actions in POS (no toast for add/remove/qty) | ✅ OK | PosPage has no success toasts for add/remove/qty — only invoice/PDF/email toasts. |
| ISSUE-1 | Double Error Toast Fix (`_handledByInterceptor`) | ✅ Done | api.js sets it; 20+ pages use `if (!e?._handledByInterceptor)`. |
| ISSUE-2 | Branch Report tab in Reports page | ✅ Done | Tab exists, Top Performer, chart, route sub-rows, getBranchComparison. |
| ISSUE-3 | Sales Ledger: Route filter cascades from Branch (server-side) | ✅ Done | `routesAPI.getRoutes(branchId)` when branch selected. |
| ISSUE-4 | Branch Detail: Date filters with Apply button | ✅ Done | dateDraft + applyDateRange, no instant refetch. |

---

## 📋 DOCUMENT "STILL NOT FIXED" (From Part 1)

| # | Item | Status | Notes |
|---|------|--------|-------|
| Toast flood (31 pages raw toast, no deduplication) | ⚠️ Partial | Toaster limit done. Per-page deduplication (ids) not everywhere. |
| SuperAdmin Settings empty | ✅ **FIXED** | Full 6-tab rebuild exists. |
| SuperAdmin Subscriptions placeholder | ✅ **FIXED** | Full page with tenants, MRR, filters. |
| Branch Detail date instant refetch | ✅ **FIXED** | Apply button added. |
| Add Customer: Branch + Route fields | ✅ **FIXED** | CustomerLedgerPage has branchId, routeId, paymentTerms, cascade. |
| No Branch Comparison report tab | ✅ **FIXED** | Branch Report tab exists. |
| No Customer Aging report tab | ✅ **FIXED** | Customer Aging tab exists in ReportsPage. |

---

## 📋 BRANCH & ROUTE ENTERPRISE PLAN

| # | Item | Status | Notes |
|---|------|--------|-------|
| Branch expenses (table + API) | ✅ Done | BranchDetailPage has Expenses section; BranchService includes branch expenses in summary. |
| Branch 6-tab redesign | ⚠️ Partial | Overview, Routes, Expenses exist. Staff, Customers, Report tabs not added. |
| Route 6-tab redesign | ⚠️ Partial | RouteDetailPage has basics; full 6 tabs not done. |
| Add Customer Branch/Route | ✅ Done | Modal has branch, route, payment terms. |
| Collection sheet | ❌ Pending | Not implemented. |

---

## 📋 IMPLEMENTATION PRIORITY — DO IMMEDIATELY

| # | Item | Status |
|---|------|--------|
| 1 | RISK-1: Toast ids, disabled buttons | ⚠️ Partial |
| 2 | RISK-2: Sequential ledger | ✅ Done |
| 3 | RISK-3: POS invoice number | ✅ Done |
| 4 | ISSUE-1: Silent cart toasts | ⚠️ Unknown |
| 5 | ISSUE-4: Branch Detail Apply button | ✅ Done |
| 6 | Double Error Toast Fix | ✅ Done |

---

## 📋 IMPLEMENTATION PRIORITY — DO THIS MONTH

| # | Item | Status |
|---|------|--------|
| 1 | Branch Detail 6-tab redesign | ⚠️ Partial |
| 2 | Route Detail 6-tab redesign | ⚠️ Partial |
| 3 | Branch Expense table + API | ✅ Done |
| 4 | Branch Report tab | ✅ Done |
| 5 | Add Customer Branch/Route | ✅ Done |
| 6 | SA Settings rebuild | ✅ Done |
| 7 | SA Subscriptions page | ✅ Done |
| 8 | SA Credentials Modal after tenant create | ✅ Done |
| 9 | Trial expiry warning in SA Dashboard | ✅ Done |

---

## ❌ REMAINING CRITICAL PENDING TASKS (Must Fix)

1. ~~**RISK-4: Backend route validation for Staff**~~ ✅ DONE

2. ~~**RISK-7: SuperAdmin action audit logging**~~ ✅ DONE

3. ~~**SA Credentials Modal**~~ ✅ DONE (already existed)

4. ~~**Trial expiry warning**~~ ✅ DONE (TrialsExpiringThisWeek now populated in dashboard)

5. ~~**Tenant Health Score UI**~~ ✅ DONE (on Tenant Detail Overview tab)

6. ~~**GET /api/users/me/assigned-routes**~~ ✅ DONE + POS uses it for Staff route filter

7. ~~**SuperAdmin Audit Logs:** Add filter "SuperAdmin Actions Only"~~ ✅ DONE — preset button added

8. **Toast deduplication (remaining pages)**
   - Add `{ id: 'action-key' }` to critical financial toasts across all pages
   - Audit PosPage: Remove toasts for add/remove cart, qty change

---

## ✅ COMPLETION SUMMARY

| Category | Done | Partial | Pending |
|----------|------|---------|---------|
| Severity 1 | 3 | 1 | 1 |
| Severity 2 | 1 | 1 | 4 |
| Severity 3 | 4 | 1 | 0 |
| Branch/Route | 3 | 2 | 1 |
| Document "Still Not Fixed" | 5 | 1 | 0 |

**Estimated completion: ~75%** of document items addressed.  
**Critical remaining: SA Audit Logs filter, toast deduplication on remaining pages, Force Logout, Maintenance Mode.**
