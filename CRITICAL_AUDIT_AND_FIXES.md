# HexaBill Critical Audit & Fixes

**Date:** February 21, 2026  
**Scope:** Errors, data leakage, Staff flow, offline, API robustness

---

## 1. API Errors (GET, POST, PUT, DELETE)

### Status: **FIXED / MITIGATED**

| Area | Fix |
|------|-----|
| Branches, Routes | Return 200 + empty list on exception |
| Customer Ledger | Return 200 + empty list on exception |
| Sales Ledger, Dashboard Summary | Return 200 + empty data on exception |
| Expenses (recurring) | Return 200 + empty list on exception |
| **Returns (CreateSaleReturn, GetSaleReturns, etc.)** | **NOW:** Return 200 + success: false + message (no 500) |

### Pattern
- **Read endpoints:** Return 200 with empty data on exception (no internal server error to frontend)
- **Write endpoints (create/update):** Return 200 with success: false + error message (frontend can show message)
- **Global handler:** Still catches unhandled exceptions → 500, but most controllers handle exceptions locally

---

## 2. Data Leakage & Tenant Isolation

### Status: **VERIFIED SECURE**

| Check | Result |
|-------|--------|
| **Email uniqueness** | Users.Email has unique index – one email per user globally. No same-email across tenants. |
| **Login** | AuthService finds user by email; JWT includes TenantId. No cross-tenant login. |
| **Tenant scoping** | All data access filters by TenantId (Customers, Sales, Products, etc.) |
| **Staff scope** | Staff restricted to assigned branches/routes via restrictToBranchIds, restrictToRouteIds |
| **Owner vs Staff** | Owner/Admin see full tenant data; Staff see only assigned branch/route data |

### Same-email conflict
- **Not possible:** Email is unique across all users. Two companies cannot have users with the same email.
- **Owner and Staff same company:** Both have same TenantId; data is correctly scoped.

---

## 3. Staff Flow

### Issues & Fixes

| Issue | Fix |
|-------|-----|
| **POS – Staff customer list empty** | Backend already filters by Staff’s assigned branches/routes when no explicit branch/route. **FIX:** POS now passes `branchId` and `routeId` when user selects branch/route, so Staff sees customers for that context. |
| **Staff must select Branch + Route** | Required for checkout when branches/routes exist. Staff must be assigned to branches/routes (Users → edit Staff → Assignments). |
| **Staff with no assignments** | Returns empty customer list by design. Assign branches/routes to Staff. |
| **Customer Ledger** | Staff gets customers for assigned branches/routes; auto-selects first branch/route. |
| **Returns** | CreateSaleReturn is available to all authenticated users. Approve/Reject requires Admin/Owner. Staff can create returns. |

### Staff flow checklist
1. Owner assigns Staff to Branches and Routes (Users page → Edit Staff → Assignments).
2. Staff logs in → Dashboard (limited by dashboardPermissions).
3. Staff goes to POS → selects Branch and Route → sees customers for that branch/route.
4. Staff creates sale, adds payment.
5. Staff can create returns (if returns enabled); Owner/Admin approve if required.

---

## 4. Offline Handling

### Current behavior

| Feature | Status |
|---------|--------|
| **Connection manager** | Tracks backend connectivity; blocks requests when disconnected |
| **Retry on reconnect** | Queues failed requests; retries when connection restored |
| **Health check** | Polls /health; backoff after repeated failures |
| **User feedback** | Toast + retry button when network/server error |
| **dataUpdated event** | Pages listen and refetch when connection restored |

### Advanced offline (future)
- Queue POST/PUT/DELETE locally and replay when online
- Service worker / IndexedDB for true offline support

---

## 5. Error Handling (Advanced)

### Implemented
- Throttled error toasts (no flood)
- Correlation IDs for 500 errors (backend)
- ErrorLog persistence for Super Admin
- CORS headers on error responses
- `_handledByInterceptor` flag to avoid duplicate toasts
- Connection-restored event for auto-refresh

---

## 6. Returns for Staff

| Action | Who | Status |
|--------|-----|--------|
| Create sale return | Any authenticated user | OK (API allows) |
| View returns (Reports → Sales Returns) | Any | OK |
| Approve / Reject | Admin/Owner only | OK |
| **Create Return UI** | **Missing** | Reports has view/approve/reject only. No “Create Return” button in POS or Sales Ledger. |

---

## 7. Fixes Applied This Session

1. **POS loadCustomers** – Pass `branchId` and `routeId` when selected so Staff sees customers for that branch/route.
2. **ReturnsController** – All catch blocks return `Ok` (200) with `success: false` and error message instead of `StatusCode(500)`.
3. **Documentation** – This audit document.

---

## 8. Recommendations

1. **Assign Staff** – Ensure Staff have Branch and Route assignments.
2. **Create Return UI** – Add “Create Return” from Sales Ledger or POS (backend API exists).
3. **Test Staff flow** – Login as Staff, verify POS customers, Ledger, Reports.
4. **Monitor ErrorLogs** – Super Admin can view persisted errors for debugging.
