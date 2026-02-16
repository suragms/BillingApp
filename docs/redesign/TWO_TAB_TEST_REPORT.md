# HexaBill Two-Tab Test Report

**Date:** 2026-02-16  
**Test Setup:** Tab 1 = Super Admin, Tab 2 = Company Owner  
**Credentials:**
- Super Admin: `admin@hexabill.com` / `Admin123!` (login at `/Admin26`)
- Company Owner: `owner1@hexabill.com` / `Owner1@123` (login at `/login`)

---

## 1. Login Flows

| Role | URL | Result |
|------|-----|--------|
| Super Admin | `/Admin26` | OK – redirects to `/superadmin/dashboard` |
| Company Owner | `/login` | OK – redirects to `/dashboard` |

**Note:** Super Admin must use `/Admin26`. Using `/login` shows error: *"Use the Admin Portal to sign in as Super Admin"*.

---

## 2. Super Admin Pages

| Page | URL | Result |
|------|-----|--------|
| Dashboard | `/superadmin/dashboard` | OK – loads tenant activity, platform metrics |
| **Companies (Tenants)** | `/superadmin/tenants` | **FAIL – Server Error** – "An unexpected error occurred" |
| Tenant Detail | `/superadmin/tenants/:id` | Not tested (tenants list broken) |
| Demo Requests | `/superadmin/demo-requests` | Not tested |
| Health | `/superadmin/health` | Not tested |
| Error Logs | `/superadmin/error-logs` | Not tested |
| Subscriptions | `/superadmin/subscriptions` | Not tested |
| Settings | `/superadmin/settings` | Not tested |

### Issue: Super Admin Tenants Page (500)

- **Symptom:** Visiting `/superadmin/tenants` shows ErrorPage with "Server Error" / "An unexpected error occurred".
- **Possible causes:**
  1. `GET /api/superadmin/tenant` returns 500 (backend exception in `GetTenantsAsync`)
  2. Frontend route or layout mismatch causing fallback to `*` → ErrorPage
  3. API response shape not matching frontend (e.g. `response.data.items`)
- **Next steps:**
  - Inspect backend logs when loading `/superadmin/tenants`
  - Call `GET /api/superadmin/tenant` directly (e.g. curl/Postman) with Super Admin token
  - Verify `SuperAdminTenantController` route and `GetTenantsAsync` implementation

---

## 3. Company Owner Pages

| Page | URL | Result |
|------|-----|--------|
| Dashboard | `/dashboard` | OK – Sales/Expenses/Profit today, Quick Actions |
| Products | `/products` | OK – `GET /api/products` 200 |
| POS | `/pos` | Not exercised in this run |
| Customer Ledger | `/ledger` | Not exercised |
| Sales Ledger | `/sales-ledger` | Not exercised |
| Reports | `/reports` | Not exercised |
| Branches/Routes | `/branches`, `/routes` | Not exercised |
| Settings | `/settings` | OK – `GET /api/settings` 200 |

### Dashboard Calculations (Company Owner)

- Sales Today: **0.00 AED**
- Expenses Today: **0.00 AED**
- Profit Today: **0.00 AED**
- Unpaid Bills: **0**
- Low Stock: **1**

**Assessment:** Values are consistent with empty/new tenant data.

---

## 4. API / Backend Notes

| Endpoint | Status (from logs) |
|----------|--------------------|
| `POST /api/auth/login` | 200 |
| `GET /api/auth/validate` | 200 |
| `GET /api/superadmin/tenant/dashboard` | 200 |
| `GET /api/superadmin/tenant-activity` | 200 (Super Admin), 403 (Company Owner – expected) |
| `GET /api/settings` | 200 |
| `GET /api/products` | 200 |
| `GET /api/reports/summary` | 200 |
| `GET /api/alerts/unread-count` | 200 |

---

## 5. Checklist for Full Manual Test

- [ ] Super Admin – fix Tenants page 500
- [ ] Super Admin – Tenant create / suspend / activate / delete
- [ ] Super Admin – Demo requests, Health, Error Logs, Subscriptions, Settings
- [ ] Company Owner – POS: create invoice, select customer/product, payment
- [ ] Company Owner – Products: add, edit, delete, stock
- [ ] Company Owner – Customer Ledger: invoices, payments
- [ ] Company Owner – Reports: filters, date range, exports
- [ ] Company Owner – Branches & Routes: CRUD, assignments
- [ ] Company Owner – Settings: logo, company info
- [ ] Cross-tenant: verify Company A cannot access Company B data
- [ ] Filters and action buttons on each page
- [ ] Error handling and loading states

---

## 6. Summary

| Category | Status |
|----------|--------|
| Login (both roles) | OK |
| Super Admin Dashboard | OK |
| Super Admin Tenants | **FAIL (500)** |
| Company Owner Dashboard | OK |
| Company Owner Products | OK |
| Data isolation | Not yet tested |
| Calculations | Looks correct for empty data |

**Priority:** Fix Super Admin Tenants page so full platform testing can continue.
