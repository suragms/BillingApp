# HexaBill — Prioritized To-Do Plan (Partial & Pending → Complete)

**Updated:** February 2026  
**Purpose:** Finish remaining items per Master Enterprise Analysis.  
**See also:** `HEXABILL_PENDING_AND_TODO.md` for current status.

---

## LEGEND

- ✅ **Done** — Verified in codebase
- ⚠️ **Partial** — Partially done, needs completion
- ❌ **Pending** — Not started
- 🔍 **Verify** — Needs manual verification

---

## ✅ PHASE 1 — COMPLETED (Verified)

### 1. Branch 6-Tab Redesign — ✅ DONE

All 6 tabs: Overview, Routes, Staff, Customers, Expenses, Report. Apply button for date range.

### 2. Route 6-Tab Redesign — ✅ DONE

All 6 tabs: Overview, Customers, Sales, Expenses, Staff, Performance. Apply button.

### 3. Payment — ✅ DONE (Except optional duplicate warning)

- Cheque/Bank Transfer reference required ✅
- Backend idempotency ✅
- Double-click disabled ✅
- Overpayment warning ✅

**Optional remaining:** Duplicate payment warning modal (same amount + same day).

---

## 🟠 PHASE 2 — VERIFY & FIX LOGIC

### 4. Company Registration — Trial vs Paid/Active Display — 🔍 VERIFY

**Issue:** When SuperAdmin creates company with "Paid" or "Active", UI should show Active, not Trial.

**Checklist says Done (53).** Verify these flows:

| Flow | File / API | What to verify |
|------|------------|----------------|
| SuperAdmin Create Tenant | SuperAdminTenantService, CreateTenantAsync | When `request.Status == TenantStatus.Active` → pass `SubscriptionStatus.Active` to CreateSubscriptionAsync |
| CreateSubscriptionAsync | SubscriptionService | `initialStatus` param creates Active subscription (no trial dates) |
| Tenant list/detail display | SuperAdminTenantsPage, TenantDetailPage | Reads `subscription.status` not `tenant.status` |
| Signup flow (self-register) | SignupService | New companies get Trial by default; paid signup path sets Active |

**If still wrong:** Ensure subscription creation for Active skips trial, sets `Status=Active`, `TrialEndsAt=null` or past.

---

### 5. Staff-Scoped Data Access — 🔍 VERIFY

**Per analysis:** Staff should only see data for assigned routes.

- [ ] `/api/customers` — add `WHERE RouteId IN (user's routeIds)` for Staff
- [ ] `/api/sales` — add route filter for Staff
- [ ] `/api/reports/*` — apply user scope
- [ ] Frontend: Staff users see only assigned branches/routes in dropdowns

---

## 🟡 PHASE 3 — REMAINING ANALYSIS ITEMS (This Month)

### 6. Validation & Error Handling

| Form | Missing validations |
|------|---------------------|
| Add Customer | Duplicate phone/email warn; Payment Terms required if credit > 0 |
| Branch expense | Amount max 999,999.99; date not future; category required |

### 7. UI/UX Cleanup

- [ ] Consistent date input style across Branch/Route/Reports
- [ ] Rows-per-page selector on paginated tables
- [ ] Topbar icon tooltips (bell, document, chart, etc.)
- [ ] POS: Hide balance when "Cash Customer" selected

### 8. Scalability (1000 Clients)

- [ ] Customer Ledger: server-side search + pagination for customer dropdown
- [x] Dashboard: poll only when `document.visibilityState === 'visible'` ✅
- [ ] Reports: lazy load per tab; abort previous on filter change

---

## 🟢 PHASE 4 — GROWTH FEATURES (Next Quarter)

| # | Feature | Notes |
|---|---------|-------|
| 1 | WhatsApp Statement | From Customer Ledger; wa.me link or API |
| 2 | Customer Portal | Self-service invoices/balance |
| 3 | Recurring Invoices | Weekly/monthly auto-generate |
| 4 | SA Feature Flags per tenant | Enable/disable Branches, AI, WhatsApp |
| 5 | Customer Credit Scoring | 0–100 based on payment history |
| 6 | Route Expense Budget | Monthly budget vs actual; alert at 80% |
| 7 | Route Transfer | Move customer to new route; keep history |
| 8 | Batch Invoice for Route | Template → apply to multiple customers |

---

## IMPLEMENTATION ORDER

```
Week 1:
  1. Branch 6-tab: complete Routes, Staff, Customers, Report tabs
  2. Route 6-tab: complete Customers, Sales, Staff, Performance tabs
  3. Payment duplicate warning modal + reference validation

Week 2:
  4. Verify company registration Trial/Active everywhere
  5. Staff-scoped data access (backend filters)
  6. Form validations (Customer, Payment, Branch expense)

Week 3–4:
  7. UI/UX cleanup (date inputs, tooltips, rows-per-page)
  8. Scalability: customer search pagination, visibility polling
```

---

## QUICK REFERENCE — WHAT'S DONE vs PARTIAL

| Area | Done | Partial |
|------|------|---------|
| Severity 1 (Money) | 7/7 | 0 |
| Super Admin | 10/10 | 0 |
| UI/UX | 13/13 | 0 |
| Branch/Route | 8/8 | 0 |
| Deep Analysis | 11/11 | 0 |

**Remaining:** Verify Trial/Active + Staff scope; validation + UI polish; optional duplicate payment modal.

---

*Use this plan to assign tasks. Each section maps to analysis document line numbers and checklist items.*
