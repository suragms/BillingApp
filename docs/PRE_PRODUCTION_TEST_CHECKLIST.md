# HexaBill Pre-Production Test Checklist

**Purpose:** Full test plan before going live. Covers data leakage, API failures, load, calculations, every page, every form/field/button, and known gaps.

**Target:** 1000 companies, 1000 staff, 1000 sales – load and stress validation.

---

## 1. Critical Known Gaps (Must Understand)

### 1.1 Purchases vs Branches/Routes/Sales

| Area | Status | Notes |
|------|--------|-------|
| **Purchase model** | No BranchId, No RouteId | Purchases are **tenant-level only**. Not linked to Branches or Routes. |
| **Sale model** | Has BranchId, RouteId | Sales ARE linked to Branch/Route. |
| **Profit calculation** | Simplified | Gross Profit = Total Sales − Total Purchases (both company-wide). COGS uses Product.CostPrice, not actual purchase history. |
| **Purchases page** | No branch/route filter | Cannot filter purchases by branch or route. |
| **Reports** | Sales filterable by branch/route | Purchases always company-wide in profit. |

**Why:** Design choice for simplicity. If you need branch/route-level purchase tracking, Purchase model needs BranchId/RouteId and reporting must be updated.

**Test:** Verify profit numbers make sense for your use case. Sales by route vs purchases (company-wide) is expected.

---

### 1.2 Profit Logic Summary

- **COGS:** Uses `Product.CostPrice × qty` (not FIFO or actual purchase cost)
- **Gross Profit:** `Total Sales − Total Purchases` (both with VAT)
- **Net Profit:** `Gross Profit − Operating Expenses`
- **Daily chart:** Uses Product.CostPrice for COGS per day

---

## 2. Data Leakage Tests (CRITICAL)

### 2.1 Multi-Tenant Isolation

| Test | Steps | Expected |
|------|-------|----------|
| Company A cannot see Company B customers | 1. Create 2 tenants. 2. Add customers to each. 3. Login as Company A. 4. List customers. | Only Company A customers. |
| Company A cannot access Company B invoice by ID | 1. Note Company B invoice ID. 2. Login as Company A. 3. GET /api/sales/{company_b_id} | 404 or 403. Never Company B data. |
| Company A cannot modify Company B product | 1. Note Company B product ID. 2. Login as Company A. 3. PUT /api/products/{id} | 404 or 403. |
| Reports only show current company | Login as Company A, open Reports. | All figures for Company A only. |
| Dashboard KPIs only for current company | Login as Company A, open Dashboard. | Sales, customers, etc. for Company A only. |
| Validation / balance check scoped | Company user runs validation. | Only that tenant’s data. |

### 2.2 Super Admin Isolation

| Test | Steps | Expected |
|------|-------|----------|
| Super Admin sees all tenants | Login as Super Admin, open Tenants. | List of all companies. |
| Super Admin impersonation | Impersonate Company A. | See only Company A data. |
| Super Admin stops impersonation | Click “Exit” / stop. | Back to Super Admin view. |

---

## 3. Load & Stress Tests

### 3.1 Current Seed Limits

- **Seed controller:** 100 customers, 20 products, 50 sales per tenant.
- **Endpoint:** `POST /api/seed/demo` (Owner/Admin only).

### 3.2 Target: 1000 Companies, 1000 Staff, 1000 Sales

**Option A – Manual multi-tenant:**

1. Create 10+ tenants via signup or Super Admin.
2. In each tenant, run seed 2–3 times (or extend seed to create more).
3. Create staff users via Users → Add User in each tenant.

**Option B – Load Test Seed (available):**

Endpoint: `POST /api/seed/load-test?customers=1000&products=100&sales=1000`

- Requires: Owner or Admin (logged-in company user).
- Creates customers, products, and sales for the **current tenant**.
- Limits: customers≤5000, products≤500, sales≤5000.

**Example (with auth token):**
```bash
curl -X POST "http://localhost:5000/api/seed/load-test?customers=1000&products=100&sales=1000" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**To test 1000 sales:** Login as Owner, call this endpoint. Then verify Dashboard, Reports, Products, Customers, Sales Ledger.

### 3.3 Load Test Checklist

| Test | Target | How |
|------|--------|-----|
| 1000 companies | Create via signup / Super Admin | Script or manual. |
| 1000 staff | Add users in multiple tenants | Users → Add User. |
| 1000 sales | Run seed multiple times or extend | POST /api/seed/demo. |
| Dashboard with 1000 sales | Load dashboard | &lt; 3s, no 500s. |
| Reports with large date range | Run reports | &lt; 5s. |
| Product list with 500+ products | List products | Pagination works, no timeout. |
| Customer list with 500+ customers | List customers | Pagination works. |

---

## 4. API Failure & Error Handling

**Error matrix:** See `ERROR_MATRIX.md` in this folder for the full mapping of status codes (400, 401, 403, 404, 500, 502, 503) to user messages and retry.

### 4.1 API Down / Network Error

| Scenario | Expected |
|----------|----------|
| Backend unreachable | Connection status / error message. No blank screen. |
| 401 on protected route | Redirect to login. |
| 500 from API | User-friendly error, no stack trace. |
| Slow API (10s+) | Loading indicator, no infinite spin. |

### 4.2 Failed Load Tests

| API | Test | Expected |
|-----|------|----------|
| GET /products | Invalid tenant | 401/403, no data. |
| GET /sales | Invalid date range | 400 or empty result. |
| POST /auth/login | Wrong password | Clear error message. |
| POST /auth/login | Rate limited | 429, retry message. |

---

## 5. Page-by-Page Test Matrix

### 5.1 Public Pages

| Page | Path | Fields/Buttons | Test |
|------|------|----------------|------|
| Login | /login | Email, Password, Remember me, Forgot?, Sign in | Fill, submit, error states. |
| Signup | /signup | Name, Email, Password, Confirm, Company, etc. | Step 1→2→3, validation. |
| Admin Login | /Admin26 | Same as login | Super Admin login flow. |

### 5.2 Super Admin Pages

| Page | Path | Forms/Fields/Buttons | Test |
|------|------|----------------------|------|
| Dashboard | /superadmin/dashboard | KPIs, charts | Load, no errors. |
| Tenants | /superadmin/tenants | Search, filters, table | List, pagination, search. |
| Tenant Detail | /superadmin/tenants/:id | Tabs, Impersonate, Suspend, etc. | All tabs, all actions. |
| Demo Requests | /superadmin/demo-requests | List, filters | Load, actions. |
| Health | /superadmin/health | Status indicators | Load. |
| Error Logs | /superadmin/error-logs | Filters, table | Load, filter. |
| Audit Logs | /superadmin/audit-logs | Filters, table | Load, filter. |
| Subscriptions | /superadmin/subscriptions | List, filters | Load. |
| Settings | /superadmin/settings | Form fields | Load, save. |

### 5.3 Company (Owner/Staff) Pages

| Page | Path | Forms/Fields/Buttons | Test |
|------|------|----------------------|------|
| Dashboard | /dashboard | KPIs, charts | Load, correct tenant data. |
| Products | /products | Add, Edit, Delete, search, filters | CRUD, pagination. |
| Price List | /pricelist | Table, edit | Load, edit. |
| Purchases | /purchases | Add, filters, date range | CRUD, filters. |
| POS | /pos | Products, qty, customer, pay | Create sale, finalize. |
| Ledger | /ledger | Customer, date, filters | Load, filter, no NaN. |
| Expenses | /expenses | Add, filters | CRUD, filters. |
| Sales Ledger | /sales-ledger | Filters, date | Load, filter. |
| Reports | /reports | Date range, type | Generate, export. |
| Branches | /branches | Add, list | CRUD. |
| Branch Detail | /branches/:id | Edit, routes | Load, edit. |
| Routes | /routes | Add, list | CRUD. |
| Route Detail | /routes/:id | Edit, customers | Load, edit. |
| Users | /users | Add user, roles, permissions | Add staff, set role. |
| Settings | /settings | Company name, logo, etc. | Save. |
| Backup | /backup | Export | Download. |
| Import | /import | File upload | Import products/customers. |
| Profile | /profile | Name, password | Update. |
| Subscription | /subscription | Plans | View, upgrade. |

---

## 6. Form & Field Checklist (Super Admin)

For each Super Admin form:

- [ ] All fields render
- [ ] Required fields validated
- [ ] Submit button works
- [ ] Cancel/Back works
- [ ] Error messages show
- [ ] Success feedback (toast/message)
- [ ] Checkboxes toggle correctly
- [ ] Dropdowns load options
- [ ] Date pickers work
- [ ] No console errors

---

## 7. Calculation Verification

| Calculation | Where | How to verify |
|-------------|-------|----------------|
| Invoice total | POS, Sales | Subtotal + VAT − Discount = GrandTotal |
| Customer balance | Ledger | Sum(invoices) − Sum(payments) |
| Pending balance | Ledger | No NaN, matches expectations |
| Profit | Reports | Gross = Sales − Purchases; Net = Gross − Expenses |
| Stock after sale | Products | Decremented by sold qty |
| Stock after purchase | Products | Incremented by purchased qty |

---

## 8. Pending / Blockers for Production

| Item | Status | Action |
|------|--------|--------|
| Data isolation | Audited | Re-test with 2+ tenants. |
| Purchase branch/route | Not implemented | Document as known limitation or add BranchId/RouteId. |
| Load test script | Not built | Add load-test seed or manual process. |
| Rate limiting | Present | Verify login limit (e.g. 5/5min). |
| RLS (PostgreSQL) | Optional | Consider for extra safety. |
| Indexes | Added | Verify performance with real data. |

---

## 9. Quick Smoke Test (5 Minutes)

1. Login (company)
2. Dashboard loads
3. Create one sale (POS)
4. View Reports
5. Logout
6. Login (Super Admin)
7. View Tenants
8. Impersonate a tenant
9. View company dashboard
10. Stop impersonation

---

## 10. Production Go/No-Go

**Go** if:

- Data leakage tests pass
- All critical pages load
- No 500s on main flows
- Calculations verified
- Forms and buttons work
- Known gaps documented and accepted

**No-Go** if:

- Any data leakage
- Repeated 500s
- Wrong calculations in core flows
- Critical forms broken

---

*Last updated: 2026-02-16*
