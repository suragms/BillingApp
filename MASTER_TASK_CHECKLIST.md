# HexaBill — Master Task Checklist (60+ Tasks)

**Source:** Master Enterprise Analysis Document v2 | **Last Updated:** February 2026

---

## PART 1: STILL NOT FIXED (8 items)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1 | Toast flood: add deduplication, shared showToast or id on financial toasts | ⬜ Partial | LimitedToaster exists; many toasts still lack id |
| 2 | Toaster visibleToasts limit = 3 | ✅ Done | main.jsx LimitedToaster |
| 3 | SuperAdmin Settings page full rebuild (6 sections) | ✅ Done | Platform Defaults, Feature Flags, Communication, Announcement, Security, Help |
| 4 | SuperAdmin Subscriptions full page implementation | ✅ Done | Table, filters, MRR, pagination |
| 5 | Branch Detail: Add Apply button for date range (no instant refetch) | ✅ Done | dateDraft + applyDateRange |
| 6 | Add Customer modal: Branch + Route + Payment Terms fields | ✅ Done | CustomerLedgerPage |
| 7 | Branch Comparison report tab | ✅ Done | ReportsPage id: 'branch' |
| 8 | Customer Aging report tab | ✅ Done | ReportsPage id: 'aging' |

---

## PART 2: CRITICAL RISKS — DO IMMEDIATELY

### RISK-1: Toast Flood / Double Payment (4 sub-tasks)

| # | Task | Status |
|---|------|--------|
| 9 | Add `{ id: 'action-key' }` to ALL financial action toasts (payment, invoice, delete customer) | ⬜ Partial |
| 10 | Disable submit buttons during loading on every modal | ⬜ Partial |
| 11 | Use toast.promise() for critical actions (invoice save, payment) | ⬜ Not done |
| 12 | Max 3 visible toasts (LimitedToaster) | ✅ Done |

### RISK-2: Ledger Balance Race Condition (4 sub-tasks)

| # | Task | Status |
|---|------|--------|
| 13 | Sequential queue: never allow concurrent ledger + payment for same customer | ⬜ Not done |
| 14 | After payment success: call recalculate-balance API, update ledger only after it completes | ⬜ Not done |
| 15 | Add version/timestamp to balance fetch; only apply if newer | ⬜ Not done |
| 16 | Show "Refreshing balance…" skeleton 500ms after payment | ⬜ Not done |

### RISK-3: POS Invoice Number (4 sub-tasks)

| # | Task | Status |
|---|------|--------|
| 17 | Do NOT pre-display invoice number; show "(Auto-generated)" | ✅ Done |
| 18 | Remove getNextInvoiceNumber() from POS | ✅ Done |
| 19 | Invoice number assigned at DB insert only | ✅ Backend |
| 20 | Success toast shows actual invoice number after create | ✅ Done |

### RISK-4: Staff Route Lock — Backend Validation (4 sub-tasks)

| # | Task | Status |
|---|------|--------|
| 21 | Backend: validate user assigned to route on invoice create; reject 403 if not | ⬜ Not done |
| 22 | Add GET /api/users/me/assigned-routes | ⬜ Not done |
| 23 | POS uses server-assigned routes, not localStorage | ⬜ Not done |
| 24 | Route lock is UX only; real lock is server-side | ⬜ Backend |

### RISK-5: SuperAdmin Real-Time Control (5 sub-tasks)

| # | Task | Status |
|---|------|--------|
| 25 | Feature A: Tenant Activity Monitor (top 10 by API calls, red if >500/hr) | ⬜ Not done |
| 26 | Feature B: Per-Tenant Rate Limiting UI on Tenant Detail | ⬜ Not done |
| 27 | Feature C: Force Single User Logout | ⬜ Not done |
| 28 | Feature D: Maintenance Mode toggle in SA Settings | ⬜ Not done |
| 29 | Feature E: Real Platform MRR, Churn, New Signups, Trials Expiring | ⬜ Partial |

### RISK-6: SuperAdmin Settings (already in #3)

### RISK-7: Audit SuperAdmin Actions (3 sub-tasks)

| # | Task | Status |
|---|------|--------|
| 30 | Backend: write AuditLog for suspend, activate, clearData, impersonate, etc. | ⬜ Not done |
| 31 | SA Audit Logs: filter "SuperAdmin Actions Only" | ⬜ Not done |
| 32 | SuperAdmin actions immutable (cannot delete) | ⬜ Not done |

---

## PART 3: UI/UX ISSUES

### ISSUE-1: Toast Message Problem (6 sub-tasks)

| # | Task | Status |
|---|------|--------|
| 33 | Silent cart actions (add/remove/quantity) — no toast | ✅ Done |
| 34 | Form saves: one toast with id | ⬜ Partial |
| 35 | toast.promise pattern for saves | ⬜ Not done |
| 36 | Max 3 visible toasts | ✅ Done |
| 37 | Errors at top-center, success at bottom-right | ⬜ Not done |
| 38 | Double error toast fix: _handledByInterceptor in ALL catch blocks | ⬜ Partial |

### ISSUE-2: Branch Report (4 sub-tasks)

| # | Task | Status |
|---|------|--------|
| 39 | Top Performer card at top of Branch tab | ⬜ Verify |
| 40 | Branch leaderboard table (Rank, Sales, Expenses, Profit, Growth%) | ⬜ Verify |
| 41 | Branch comparison bar chart (Recharts) | ⬜ Verify |
| 42 | Route sub-rows (collapsible) | ⬜ Verify |

### ISSUE-3: Sales Ledger Route Filter (3 sub-tasks)

| # | Task | Status |
|---|------|--------|
| 43 | routesAPI.getRoutes(branchId) when branch selected | ✅ Done |
| 44 | Routes dropdown disabled until branch selected | ✅ Done |
| 45 | Clear route when branch cleared | ⬜ Verify |

### ISSUE-4: Branch Detail Date Filters (already in #5)

---

## PART 4: BRANCH & ROUTE — FULL PLAN

### Branch Detail 6-Tab Redesign (6 tabs)

| # | Task | Status |
|---|------|--------|
| 46 | Tab 1: Overview (KPIs, date presets, Apply, sparkline) | ⬜ Partial |
| 47 | Tab 2: Routes (table, Add Route, Edit/Delete) | ⬜ Partial |
| 48 | Tab 3: Staff (assign, Branch Manager) | ⬜ Not done |
| 49 | Tab 4: Customers (table, Add Customer) | ⬜ Not done |
| 50 | Tab 5: Expenses (branch-level, new API) | ⬜ Not done |
| 51 | Tab 6: Branch Report (charts, export PDF) | ⬜ Not done |

### Route Detail 6-Tab Redesign (6 tabs)

| # | Task | Status |
|---|------|--------|
| 52 | Tab 1: Overview (KPIs, sparkline) | ⬜ Partial |
| 53 | Tab 2: Customers | ⬜ Partial |
| 54 | Tab 3: Sales | ⬜ Partial |
| 55 | Tab 4: Expenses (+ Vehicle Maintenance, Toll/Parking, Edit, Budget, ConfirmDangerModal) | ⬜ Partial |
| 56 | Tab 5: Staff Assignment | ⬜ Not done |
| 57 | Tab 6: Performance (charts, export) | ⬜ Not done |

### New Features

| # | Task | Status |
|---|------|--------|
| 58 | Branch Expense API: POST/GET /api/branches/:id/expenses | ⬜ Not done |
| 59 | Branch Expense UI (table, Add Expense modal) | ⬜ Not done |
| 60 | Collection Sheet (printable daily view per route) | ⬜ Not done |

---

## PART 5: STAFF & DATA ACCESS

| # | Task | Status |
|---|------|--------|
| 61 | Backend: GetUserScopeFilter for Staff role | ⬜ Not done |
| 62 | Apply scope to /api/customers, /api/sales, /api/reports | ⬜ Not done |
| 63 | Staff Performance Report tab in Reports | ⬜ Not done |
| 64 | Staff see only assigned branches/routes in dropdowns | ⬜ Partial |

---

## PART 6: SUPER ADMIN FEATURES

| # | Task | Status |
|---|------|--------|
| 65 | SA-1: Credentials modal after tenant creation (one-time) | ✅ Done |
| 66 | SA-2: Trial expiry warning in Dashboard | ✅ Done |
| 67 | SA-2: Extend Trial by 7 days button | ⬜ Not done |
| 68 | SA-3: Tenant Health Score on Tenant Detail | ⬜ Not done |
| 69 | SA-4: Subscriptions full page | ✅ Done |
| 70 | SA-5: Impersonation audit trail (Enter/Exit workspace) | ⬜ Not done |
| 71 | SA-6: Demo Requests — Remove or properly integrate | ⬜ Not done |

---

## PART 7: ERROR HANDLING & VALIDATION

| # | Task | Status |
|---|------|--------|
| 72 | api.js: error._handledByInterceptor = true | ✅ Done |
| 73 | All page catch blocks: if (!e?._handledByInterceptor) toast.error | ✅ Done |
| 74 | Actionable errors: "Check connection and [Retry]" | ⬜ Not done |
| 75 | connectionRestored event → auto-refresh pages | ⬜ Not done |
| 76 | Add Customer: Branch/Route required, Phone regex, TRN regex, Payment Terms | ⬜ Partial |
| 77 | Payment form: duplicate payment warning, overpayment warning | ⬜ Not done |
| 78 | POS: credit limit warning, stock warning | ⬜ Partial |

---

## PART 8: CLIENT RISKS

| # | Task | Status |
|---|------|--------|
| 79 | Subscription Grace Period (3-5 days before suspend) | ⬜ Not done |
| 80 | BackupPage: Last backup timestamp + color indicator | ⬜ Not done |
| 81 | PDF failure fallback: Print Plain Text | ⬜ Not done |
| 82 | POS: re-fetch product price at checkout | ⬜ Not done |

---

## PART 9: FUTURE (Quarter 2+)

| # | Task | Status |
|---|------|--------|
| 83 | WhatsApp Statement | ⬜ Not done |
| 84 | Daily Collection Summary digest | ⬜ Not done |
| 85 | Customer Credit Scoring | ⬜ Not done |
| 86 | Recurring Invoices | ⬜ Not done |
| 87 | Customer Portal (self-service) | ⬜ Not done |
| 88 | Multi-Currency Support | ⬜ Not done |

---

## Summary Count

| Category | Done | Partial | Not Done |
|----------|------|---------|----------|
| Part 1 (8) | 7 | 1 | 0 |
| Part 2 (24) | 5 | 2 | 17 |
| Part 3 (13) | 5 | 3 | 5 |
| Part 4 (15) | 0 | 3 | 12 |
| Part 5 (4) | 0 | 1 | 3 |
| Part 6 (7) | 3 | 0 | 4 |
| Part 7 (7) | 1 | 2 | 4 |
| Part 8 (4) | 0 | 0 | 4 |
| Part 9 (6) | 0 | 0 | 6 |
| **Total** | **21** | **12** | **55** |

**Tasks requiring work: 67** (55 not done + 12 partial)

---

*Use this checklist to track progress. Update status as tasks complete.*
