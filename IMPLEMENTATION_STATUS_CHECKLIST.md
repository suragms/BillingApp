# HexaBill — Definitive Implementation Status Checklist

**Generated:** February 2026 | **Source:** Code audit of current codebase vs Master Enterprise Analysis Document v2

---

## PART A: Document's "Still Not Fixed" — Actual Status

| # | Document Says "Still Not Fixed" | Actual Status | Evidence |
|---|--------------------------------|---------------|----------|
| 1 | Toast flood: 492 toast calls, 31 pages import raw toast, zero deduplication | **PARTIALLY FIXED** | `main.jsx` has `LimitedToaster` with `TOAST_LIMIT=3`. POS cart actions are silent. Invoice save uses `{ id: 'invoice-save' }`. Many pages still use raw toast without `id` for financial actions. |
| 2 | Toaster has NO visibleToasts limit | **FIXED** | `main.jsx` lines 11-22: `LimitedToaster` uses `useToasterStore` + effect to dismiss excess toasts when >3 visible |
| 3 | SuperAdmin Settings page is just 2 links — completely empty | **FIXED** | `SuperAdminSettingsPage.jsx` rebuilt with 6 tabbed sections: Platform Defaults, Feature Flags, Communication, Announcement, Security, Help & Support |
| 4 | SuperAdmin Subscriptions page is full placeholder with zero functionality | **FIXED** | `SuperAdminSubscriptionsPage.jsx` has full table, filters, MRR summary, pagination |
| 5 | Branch Detail: date filters trigger instant refetch on every keystroke | **FIXED** | `BranchDetailPage.jsx` lines 28-31, 66-69, 221-243: `dateDraft` state, `applyDateRange()`, Apply button — no refetch until Apply clicked |
| 6 | Add Customer modal: missing Branch and Route fields | **FIXED** | `CustomerLedgerPage.jsx` has `branchId`, `routeId`, `paymentTerms` (43 references) with branch→route cascade |
| 7 | No Branch Comparison report tab | **FIXED** | `ReportsPage.jsx` line 102: `{ id: 'branch', name: 'Branch Report', icon: Building2 }` |
| 8 | No Customer Aging report tab | **FIXED** | `ReportsPage.jsx` line 103: `{ id: 'aging', name: 'Customer Aging', icon: Clock }` |

---

## PART B: Critical Production Risks — Status

### RISK-1: Toast Flood Creates False Payment Confirmations

| Sub-item | Status | Evidence |
|----------|--------|----------|
| Toaster max 3 visible | FIXED | `main.jsx` LimitedToaster |
| Toast deduplication via `id` on financial actions | PARTIAL | Invoice save uses `{ id: 'invoice-save' }`; Payment modal and other financial toasts need audit |
| Submit buttons disabled while loading | NEEDS AUDIT | PaymentModal and other modals — verify `disabled={loading}` on all submit buttons |
| toast.promise for critical actions | NOT DONE | Payment/invoice creation could use toast.promise pattern |

### RISK-2: Ledger Balance Race Condition

| Sub-item | Status | Evidence |
|----------|--------|----------|
| Sequential queue for ledger operations | NOT VERIFIED | `CustomerLedgerPage.jsx` has refs (`recalculateInProgress`, `paymentLoadingRef`, `customerLoadingRef`) — logic may still allow race |
| Recalculate after payment, then update | NOT VERIFIED | Need to confirm payment success → recalculate API → then update ledger |
| Version/timestamp on balance response | NOT DONE | No version check on balance updates |
| "Refreshing balance…" skeleton | NOT DONE | |

### RISK-3: POS Invoice Number Pre-fetched But Not Locked

| Sub-item | Status | Evidence |
|----------|--------|----------|
| Remove pre-display of invoice number | **FIXED** | `PosPage.jsx` lines 1314, 1355: shows `'(Auto-generated)'` in edit mode |
| No getNextInvoiceNumber call from POS | **FIXED** | Grep: no `getNextInvoiceNumber` in PosPage.jsx |
| Invoice number assigned at insert only | BACKEND | Server assigns; frontend no longer pre-fetches |

### RISK-4: Staff Route Lock Depends on user.routeId — Easily Spoofed

| Sub-item | Status |
|----------|--------|
| Backend validates user assigned to route on invoice create | NOT DONE |
| GET /api/users/me/assigned-routes | NOT VERIFIED |
| Server-side 403 if routeId doesn't match | NOT DONE |

### RISK-5, 6, 7: SuperAdmin Control Gaps

| Item | Status |
|------|--------|
| RISK-5: Tenant Activity Monitor, Rate Limiting UI, Force Logout, Maintenance Mode, Real MRR Dashboard | MOSTLY NOT DONE — dashboard has trials expiring; MRR uses platformRevenue |
| RISK-6: SA Settings full rebuild | **FIXED** |
| RISK-7: Audit logs for SuperAdmin's own actions | NOT DONE |

---

## PART C: UI/UX Issues — Status

### ISSUE-1: Toast Flood — Message Problem

| Sub-item | Status | Evidence |
|----------|--------|----------|
| Silent cart actions (add/remove/quantity) | **FIXED** | `PosPage.jsx` addToCart, removeFromCart, updateCartItem — all have "Silent - cart update is visual feedback" |
| Form saves: one toast with id | PARTIAL | Invoice save has `{ id: 'invoice-save' }`; other forms need audit |
| toast.promise pattern | NOT DONE | |
| Max 3 visible toasts | **FIXED** | LimitedToaster |

### ISSUE-2: Branch Report — Missing Highest Sales View

| Sub-item | Status |
|----------|--------|
| Branch Report tab exists | **FIXED** |
| Top Performer card, leaderboard table, chart | NEEDS VERIFICATION — tab exists, content structure unknown |

### ISSUE-3: Sales Ledger Route Filter Cascading

| Sub-item | Status | Evidence |
|----------|--------|----------|
| Route filter loads from API when branch selected | **FIXED** | `SalesLedgerPage.jsx` line 83: `routesAPI.getRoutes(branchId)` |
| Routes dropdown disabled until branch selected | **FIXED** | Line 527: `filters.branchId ? routesForBranch : []` |

### ISSUE-4: Branch Detail Date Filters Instant Refetch

| Sub-item | Status |
|----------|--------|
| Apply button for date range | **FIXED** |
| No refetch on keystroke | **FIXED** |

---

## PART D: api.js & Error Handling

| Item | Status | Evidence |
|------|--------|----------|
| error._handledByInterceptor set in api.js | **FIXED** | 11 places in `api.js` (lines 223, 252, 271, 285, 322, 331, 346, 354, 362, 369, 374) |
| Pages use `if (!e?._handledByInterceptor)` before toast | PARTIAL | BranchDetailPage, CustomerLedgerPage, ReportsPage, SuperAdminSubscriptionsPage, SuperAdminSettingsPage use it. **SuperAdminDashboard does NOT** (line 43) — and many other pages likely don't |

---

## PART E: SuperAdmin Features

| Item | Status | Evidence |
|------|--------|----------|
| SA Credentials Modal after tenant creation | **FIXED** | `SuperAdminTenantsPage.jsx` — `showCredentialsModal`, `credentialsData`, Copy All, "I've saved" checkbox |
| Trials Expiring This Week in SA Dashboard | **FIXED** | `SuperAdminDashboard.jsx` lines 124-151 — `trialsExpiringThisWeek` section |
| SA Settings 6 sections | **FIXED** | SuperAdminSettingsPage |
| SA Subscriptions full page | **FIXED** | SuperAdminSubscriptionsPage |

---

## PART F: What's Still Missing (Priority Order)

### Do Immediately
1. **Audit payment/invoice toasts** — Add `{ id: 'payment-success' }` and similar to all financial action toasts
2. **Audit submit buttons** — Ensure `disabled={loading}` on PaymentModal and all modals with financial actions
3. **Add _handledByInterceptor check** to remaining catch blocks (e.g. SuperAdminDashboard, PosPage, others)
4. **Verify RISK-2** — Sequential ledger ops, recalculate-after-payment flow

### Do This Month
1. Branch Detail 6-tab redesign (Overview, Routes, Staff, Customers, Expenses, Report)
2. Route Detail 6-tab redesign
3. Branch Expense API + UI (`POST/GET /api/branches/:id/expenses`)
4. RISK-4: Backend route validation for staff

### Do This Quarter
- Staff Performance Report tab
- Collection Sheet printable view
- SA Feature Flags per tenant
- Force Logout single user
- Subscription Grace Period
- Customer Credit Scoring
- Others from document Parts 6–8

---

## Quick Reference: Files Modified vs Document

| Document Section | Primary Files |
|------------------|---------------|
| Toast / Toaster | `main.jsx`, `api.js`, `PosPage.jsx` |
| Branch Detail | `BranchDetailPage.jsx` |
| Add Customer | `CustomerLedgerPage.jsx` |
| Reports | `ReportsPage.jsx` |
| Sales Ledger | `SalesLedgerPage.jsx` |
| SuperAdmin | `SuperAdminSettingsPage.jsx`, `SuperAdminSubscriptionsPage.jsx`, `SuperAdminTenantsPage.jsx`, `SuperAdminDashboard.jsx` |

---

*This checklist reflects the actual codebase as of the latest audit. The document's "Still Not Fixed" list was written before these fixes and is outdated for items marked FIXED above.*
