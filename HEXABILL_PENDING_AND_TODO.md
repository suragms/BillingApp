# HexaBill — Pending Tasks & To-Do Plan

**Updated:** February 2026  
**Purpose:** Single source of truth for remaining work per Master Enterprise Analysis v2 + Full Deep Analysis.

---

## LEGEND

- ✅ **Done** — Verified in codebase
- ⚠️ **Partial** — Partially done, needs completion
- ❌ **Pending** — Not started
- 🔍 **Verify** — Needs manual verification

---

## ✅ VERIFIED DONE (Codebase Audit)

| Category | Items |
|----------|-------|
| **Severity 1** | Toast IDs on financial actions, disabled submit during loading, Toaster limit 3, sequential ledger ops, POS Auto-generated invoice, backend staff route validation, assigned-routes API |
| **Branch Detail** | 6 tabs (Overview, Routes, Staff, Customers, Expenses, Report), Apply button for date range, Edit/Add Route |
| **Route Detail** | 6 tabs (Overview, Customers, Sales, Expenses, Staff, Performance), Apply button, Edit expense, Vehicle Maintenance/Toll/Parking categories |
| **Add Customer** | Branch + Route dropdowns with cascade, Payment Terms, validation |
| **Sales Ledger** | Branch/Route/Staff filters, Route cascades from Branch (routesAPI.getRoutes(branchId)), Apply Filter |
| **Reports** | Branch Report tab (Top Performer, chart, routes), Customer Aging tab, Route filters by Branch |
| **SuperAdmin** | Settings 6 tabs, Credentials Modal, Trial expiry, Health Score, Force Logout, Maintenance Mode, Activity Monitor |
| **UI/UX** | Silent POS cart actions, double error toast fix, Profit arrow direction, div-by-zero guard, NaN guard, sticky headers |
| **window.confirm** | Replaced with ConfirmDangerModal (no raw window.confirm in app code) |
| **Payment** | Cheque/Bank Transfer reference required |

---

## ⚠️ PARTIAL / REMAINING (Higher Priority)

### 1. Payment Duplicate Warning — Optional Enhancement

**Status:** Backend idempotency ✅; double-click disabled ✅; reference required for Cheque/Bank ✅  
**Remaining:**
- [ ] **Frontend modal:** Before submit, if same customer + same amount + same day → *"A payment of AED X was already recorded for this customer today. Record another?"* [Yes] [Cancel]
- [ ] **API:** `GET /api/payments/duplicate-check?customerId=&amount=&date=` or include in allocate flow

---

### 2. Company Registration — Trial vs Active — 🔍 VERIFY

**Issue:** When SuperAdmin creates company with "Paid" or "Active", UI must show Active, not Trial.

**Check:**
- SuperAdmin create tenant with Status=Active → CreateSubscriptionAsync(Active)
- Tenant list/detail reads `subscription.status` not `tenant.status`
- If wrong: ensure subscription creation for Active skips trial, sets Status=Active

---

### 3. Staff-Scoped Data Access — 🔍 VERIFY

**Rule:** Staff should only see data for their assigned routes.

| API | Status | Action |
|-----|--------|--------|
| `/api/customers` | 🔍 | Add route filter for Staff |
| `/api/sales` | 🔍 | Add route filter for Staff |
| `/api/reports/*` | 🔍 | Apply user scope |
| Frontend dropdowns | ✅ | Staff see only assigned branches/routes |

---

## ❌ PENDING (This Month)

### 4. Validation & Error Handling

| Form | Missing |
|------|---------|
| Add Customer | Duplicate phone/email warning; Payment Terms required if credit > 0 |
| Branch expense | Amount max 999,999.99; date not future; category required |

---

### 5. UI/UX Cleanup

| Item | Status |
|------|--------|
| Consistent date input style (Branch/Route/Reports) | ❌ |
| Rows-per-page selector on paginated tables | ❌ |
| Topbar icon tooltips (bell, document, chart) | ❌ |
| POS: Hide balance when "Cash Customer" selected | ❌ |

---

### 6. Scalability (1000 Clients)

| Item | Status |
|------|--------|
| Customer Ledger: server-side search + pagination for customer dropdown | ❌ |
| Dashboard: poll only when `document.visibilityState === 'visible'` | ✅ (28e in checklist) |
| Reports: lazy load per tab; abort previous on filter change | ❌ |

---

## 🟢 GROWTH (Next Quarter)

| # | Feature |
|---|---------|
| 1 | WhatsApp Statement from Customer Ledger |
| 2 | Customer Portal (self-service) |
| 3 | Recurring Invoices |
| 4 | SA Feature Flags per tenant |
| 5 | Customer Credit Scoring |
| 6 | Route Expense Budget |
| 7 | Route Transfer (move customer) |
| 8 | Batch Invoice for Route |

---

## RISKS & ISSUES (From Analysis)

### Addressed
- Toast flood → Toast IDs, 3-toast limit, silent cart
- Ledger race → Sequential ops
- POS invoice number → Auto-generated
- Staff route lock → Backend validates route
- Branch/Route date filters → Apply button

### All Addressed
- **POS quick customer search** — ✅ Done (inline search)
- **Summary bar on Sales Ledger** — ✅ Recomputes from filtered data (filteredSummary)
- **Export Excel** on Sales Ledger — ✅ Done
- **Overpayment warning** — ✅ Done (PaymentModal shows excess-as-credit)

---

## IMPLEMENTATION ORDER

```
Week 1 (Verify):
  1. Company registration Trial/Active — manual test
  2. Staff-scoped data access — backend audit

Week 2:
  3. Payment duplicate warning modal (optional)
  4. Add Customer: duplicate phone/email warn, Payment Terms if credit > 0
  5. Branch expense validation

Week 3–4:
  6. UI/UX: date inputs, tooltips, rows-per-page
  7. POS: hide balance for Cash Customer
  8. Scalability: customer search pagination
```

---

## QUICK REFERENCE

| Area | Done | Partial | Pending |
|------|------|---------|---------|
| Severity 1 (Money) | 7 | 0 | 0 |
| Super Admin | 10 | 0 | 0 |
| UI/UX | 13 | 0 | 4 |
| Branch/Route | 8 | 0 | 0 |
| Validation | — | 1 | 2 |
| Scalability | 1 | 0 | 2 |

**Main remaining work:** Verify Trial/Active + Staff scope, then validation + UI polish.

---

*Generated from codebase audit. Cross-check with HEXABILL_FULL_CHECKLIST.md for full item list.*
