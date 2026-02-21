# HexaBill: Owner Business Flow & Issues Report

**Date:** February 21, 2026  
**Scope:** Real business flow, 500 errors, production failures, validations, refunds, backup, reports

---

## 1. Correct Owner Business Flow (Plan)

This is the **intended real-world order** for a business owner:

| Step | Action | Page | Status |
|------|--------|------|--------|
| 1 | Owner logs in | `/login` | ✅ Works |
| 2 | Add staff/admin (optional) | `/users` | ✅ Modal; validation exists |
| 3 | Create branches | `/branches` | ⚠️ API may return error → "An error occurred" |
| 4 | Create routes (under branches) | `/branches?tab=routes` | ⚠️ Same as above |
| 5 | Add customers | `/customers` or Customer Ledger | ✅ Works |
| 6 | Add products | `/products` | ✅ Works |
| 7 | Add purchase (stock-in) | `/purchases` | ✅ Works |
| 8 | Create sale / invoice | `/pos` or Sales Ledger | ⚠️ Branch/Route "Loading..." if branches API fails |
| 9 | Check sales & collections | `/sales-ledger`, `/reports` | ✅ Works |
| 10 | Reports (each subpage, filters) | `/reports` | ✅ All tabs load; Apply Filters works |
| 11 | Backup | `/backup` | ✅ Loads |

---

## 2. Why 500 Internal Server Errors?

**Root causes identified:**

1. **Database schema mismatch** – Migrations not applied or columns missing (e.g. `PageAccess`, `Settings.Value`, `DamageCategories`). Backend catches and returns 500 with message.
2. **Branches/Routes API** – `BranchesController.GetBranches()` returns 500 if `CheckDatabaseConnectionAsync` or `GetBranchesAsync` throws. Also returns 503 if DB unavailable.
3. **Report APIs** – Some report endpoints (e.g. `/api/reports/summary`) can 500 if DB query fails or table doesn't exist.
4. **ProductCategories** – 500 on POST if table missing (Program.cs has fix for this).
5. **Expenses** – 500 if RecurringExpenses table or columns missing.

**Where 500s are returned:**
- `BranchesController.cs` lines 50–56
- `GlobalExceptionHandlerMiddleware` – catches all unhandled exceptions → 500, persisted to ErrorLogs
- Various controllers on `catch (Exception ex)` → `StatusCode(500, ...)`

---

## 3. Why GET Requests Fail in Production (hexabill.onrender.com)

**Main cause: Render free / starter tier cold start**

- **Backend sleeps** after ~15 minutes of no traffic.
- **First request** after sleep hits a sleeping instance → long delay (30–60 seconds) or timeout.
- **Frontend timeout** is 30 seconds → GET fails with timeout / ECONNABORTED.
- **api.js** message: *"The request timed out. If using Render free tier, the backend may be starting (cold start). Wait 30-60 seconds and try again."*

**Mitigations in place:**
- `App.jsx` – keep-alive ping every 9 minutes when user is logged in.
- `api.js` – retry up to 3 times (500, 502, 503, 504, network errors) with exponential backoff (2s, 4s, 8s).
- `connectionManager.js` – blocks further requests after 3 failures to avoid flooding.

**Why it still feels broken:**
- If user leaves tab idle > 9 minutes, ping may stop and backend sleeps.
- After cold start, first requests can time out before backend is ready.
- Network/SSL issues between frontend and Render can cause intermittent failures.

---

## 4. Why Data Load Fails After a Few Minutes

**Likely causes:**

1. **Backend sleep** – Same cold start as above.
2. **JWT expiry** – Token expires; 401 on subsequent requests; no visible "session expired" on some pages.
3. **Response cache** – Some endpoints cached 30s–5min; after cache expires, a new request may fail if backend is sleeping.
4. **DB connection pool exhaustion** – Unlikely with low traffic, but possible under load.
5. **ConnectionManager marks disconnected** – After 3 failed requests, blocks all requests until health check succeeds.

---

## 5. Refund / Returns Status

| Item | Status |
|------|--------|
| **Backend** | ✅ `ReturnService`, `ReturnsController`, Sale Return APIs |
| **Reports → Sales Returns** | ✅ View, approve, reject returns |
| **Create Return UI** | ❌ **MISSING** – No "Create Return" or "Refund" in POS, Sales Ledger, or Customer Ledger |
| **returnsAPI.createSaleReturn** | Exists but **never called** from UI |

**Impact:** Users cannot create sale returns or refunds from the app; only view/approve in Reports.

**Recommendation:** Add "Create Return" flow from Sales Ledger (invoice detail) or POS.

---

## 6. Flood / Loop / Block Issues

| Issue | Status | Notes |
|-------|--------|-------|
| **Infinite loops** | ✅ None observed | ReportsPage uses 2s throttle, tab-change guards |
| **Request flood** | ✅ Mitigated | `api.js`: deduplication, 50ms throttle, max 20 concurrent |
| **Blocked flows** | ⚠️ POS | Branch/Route required when feature enabled; dropdowns stuck on "Loading..." if branches/routes API fails |
| **429 Too Many** | ✅ Handled | Throttled error, Retry-After respected |
| **Connection block** | ✅ By design | After 3 failed requests, ConnectionManager blocks until health check passes |

---

## 7. Reports Page – Subpages & Filters

**Tabs tested:** Summary, Sales, Product, Customer, Expenses, Branch, Route, Aging, Profit & Loss, Outstanding Bills, Returns, Collections, Cheque, Staff, AI.

- **Apply Filters** – Works; date range, branch, route, product, customer applied.
- **Tab switching** – 300ms debounce to avoid rapid requests.
- **2 second throttle** – Min 2s between fetches to prevent floods.
- **Cache** – Tab data cached; no refetch on quick tab switch.

**Error banner:** "An error occurred. Please try again." appears when any report API returns non-200.

---

## 8. Backup Page

- **Page load** – ✅ Works.
- **Create Backup / Restore** – Backend APIs exist; not fully exercised in automated test.
- **Schedule** – UI present; behavior depends on backend scheduler.

---

## 9. Validation Gaps

| Page | Present | Missing / Weak |
|------|---------|----------------|
| **Users** | Name, email, password, role, strength | Email format, duplicate email |
| **Customers** | Name, phone | Duplicate phone/email, TRN format |
| **Products** | SKU, name, price | — |
| **Branches/Routes** | Name, branch | — |
| **Expenses** | Category, amount, date | — |
| **POS** | Branch+Route when enabled | — |
| **Settings** | Company name, currency, VAT | TRN format |

---

## 10. Critical / Serious Bugs & Risks

| Severity | Issue |
|----------|-------|
| **Critical** | No Create Return / Refund UI – users cannot process returns |
| **Critical** | Production GET failures after idle – Render cold start + timeout |
| **Serious** | Branches/Routes API errors cause generic "An error occurred" and POS Branch/Route stuck on "Loading..." |
| **Serious** | Backend 500 messages often generic; hard to debug in production |
| **Medium** | Missing email format and duplicate checks on Users/Customers |
| **Low** | Some modals (Add User, Add Branch) not fully exercised in automation |

---

## 11. Data Leakage

- **Tenant isolation** – Enforced via `TenantId` in backend; `TenantScopedController` base.
- **JWT** – Contains `tenantId`; used for scoping.
- **RLS** – PostgreSQL RLS supported where configured.
- **CORS** – Configured for specific origins.
- **No obvious cross-tenant data leakage** found in code.

---

## 12. Recommendations

1. **Branches/Routes** – Return `[]` when feature disabled or no data, instead of 404/500. Fix schema if tables/columns missing.
2. **Create Return UI** – Add flow to create sale returns from Sales Ledger or POS.
3. **Production stability** – Upgrade Render to paid tier (no cold start) or add external keep-alive (e.g. cron pinging `/health`).
4. **Error UX** – Surface backend error message when available instead of generic "An error occurred."
5. **JWT / Session** – Add clear "Session expired" handling and redirect to login on 401.
6. **Validations** – Add email format and duplicate checks for Users and Customers.
7. **Frontend timeout** – Consider 45–60s for production to better tolerate cold starts.

---

## 13. API Endpoints That Often Cause 500 / Failures

| Endpoint | Typical failure |
|----------|-----------------|
| `GET /api/branches` | 500 if Branches table/columns missing |
| `GET /api/routes` | 500 if Routes table/columns missing |
| `GET /api/reports/summary` | 500 if query or schema issue |
| `POST /api/productcategories` | 500 if ProductCategories missing |
| `GET /api/settings` | 500 if Settings.Value column missing |
| `GET /api/expenses` | 500 if RecurringExpenses or columns missing |
| `GET /api/health` | Used for keep-alive; 503 when backend starting |

---

*Report generated from code analysis and automated flow testing.*
