# HexaBill Full Test Report: Super Admin, Company Owner, Staff

**Date:** 2026-02-16  

---

## CRITICAL: How to Test Two Roles

**Both tabs share the same session (localStorage).** Logging in as Company Owner in one tab overwrites the Super Admin token. You cannot be both at once in the same browser.

**To test Super Admin + Company Owner:**
- **Option A:** Use two browsers (e.g. Chrome + Edge)
- **Option B:** Normal window (Super Admin) + Incognito/Private window (Company Owner)
- **Option C:** Logout in Tab 1, login as Owner, test Owner pages; then logout, login as Super Admin, test Super Admin pages

---

## Credentials

| Role | Email | Password | Login URL |
|------|-------|----------|-----------|
| **Super Admin** | admin@hexabill.com | Admin123! | `/Admin26` |
| **Company Owner** | owner1@hexabill.com | Owner1@123 | `/login` |
| **Staff** | (create via Users) | (set by Owner) | `/login` |

**Staff:** There is no pre-seeded Staff user. Owner must go to **Users** → Add User → Role: Staff, then use that account to test.

---

## 1. Super Admin – Pages to Test

| # | Page | URL | Status |
|---|------|-----|--------|
| 1 | Dashboard | /superadmin/dashboard | ⏳ |
| 2 | Companies (Tenants) | /superadmin/tenants | ⏳ |
| 3 | Tenant Detail | /superadmin/tenants/:id | ⏳ |
| 4 | Demo Requests | /superadmin/demo-requests | ⏳ |
| 5 | Health (Infrastructure) | /superadmin/health | ⏳ |
| 6 | Error Logs | /superadmin/error-logs | ⏳ |
| 7 | Audit Logs | /superadmin/audit-logs | ⏳ |
| 8 | Subscriptions | /superadmin/subscriptions | ⏳ |
| 9 | Settings | /superadmin/settings | ⏳ |
| 10 | Help | /help | ⏳ |
| 11 | Feedback | /feedback | ⏳ |

**Actions to test:** Create tenant, suspend/activate, view tenant detail, platform metrics, demo requests list.

---

## 2. Company Owner – Pages to Test

| # | Page | URL | Status |
|---|------|-----|--------|
| 1 | Dashboard | /dashboard | ⏳ |
| 2 | Products | /products | ⏳ |
| 3 | Price List | /pricelist | ⏳ |
| 4 | Purchases | /purchases | ⏳ |
| 5 | POS Billing | /pos | ⏳ |
| 6 | Customer Ledger | /ledger | ⏳ |
| 7 | Expenses | /expenses | ⏳ |
| 8 | Sales Ledger | /sales-ledger | ⏳ |
| 9 | Reports | /reports | ⏳ |
| 10 | Outstanding Bills | /reports/outstanding | ⏳ |
| 11 | Branches | /branches | ⏳ |
| 12 | Branch Detail | /branches/:id | ⏳ |
| 13 | Routes | /routes | ⏳ |
| 14 | Route Detail | /routes/:id | ⏳ |
| 15 | Users | /users | ⏳ |
| 16 | Settings | /settings | ⏳ |
| 17 | Backup | /backup | ⏳ |
| 18 | Import | /import | ⏳ |
| 19 | Profile | /profile | ⏳ |
| 20 | Subscription | /subscription | ⏳ |

**Actions to test:** Add product, create invoice (POS), record payment, add customer, add expense, create branch/route, add Staff user, filters, date ranges.

---

## 3. Staff – Pages to Test

Staff has restricted access. Typically:

| # | Page | Expected |
|---|------|----------|
| 1 | Dashboard | ✅ See only assigned routes/branches |
| 2 | POS | ✅ Only for assigned routes |
| 3 | Customer Ledger | ✅ Filtered by assigned routes |
| 4 | Sales Ledger | ✅ Filtered |
| 5 | Products | ✅ View (read) |
| 6 | Users | ❌ No access (or limited) |
| 7 | Settings | ❌ No access |
| 8 | Branches/Routes | ❌ Limited or view-only |

**Actions to test:** Create invoice for assigned route only, view customers in route, cannot access other routes.

---

## 4. Known Issues Found

| Issue | Page/Role | Notes |
|-------|-----------|-------|
| **Server Error on Super Admin** | /superadmin/dashboard, /superadmin/tenants | If logged in as Owner and navigate to /superadmin/*, routes don't exist (userIsSystemAdmin=false) → catch-all shows ErrorPage. **Fix:** Logout, login as Super Admin first. |
| **Tenants API 500** | GET /api/superadmin/tenant | Possible null ref in subscription Plan query. Fix applied: `s.Plan != null` in Where. Restart backend to verify. |
| **Two-tab session conflict** | Both roles | Same localStorage = only one role active. Use two browsers or incognito for parallel testing. |

---

## 5. Test Procedure

### Super Admin
1. Open browser, go to `http://localhost:5173/Admin26`
2. Login: admin@hexabill.com / Admin123!
3. Visit each page in Section 1. Note any errors, blank screens, failed API calls.
4. Test: Create tenant, view tenant detail, check platform metrics.

### Company Owner
1. **Logout** (or use incognito/second browser)
2. Go to `http://localhost:5173/login`
3. Login: owner1@hexabill.com / Owner1@123
4. Visit each page in Section 2. Test filters, buttons, forms.
5. Create a Staff user (Users → Add User → Role: Staff).

### Staff
1. Logout, login as the Staff user you created.
2. Visit pages in Section 3. Verify restricted access and route scoping.

---

## 6. Error Checklist

When testing each page, check:

- [ ] Page loads without "Server Error" or blank screen
- [ ] Data loads (or empty state shows correctly)
- [ ] Filters work (date, branch, route, search)
- [ ] Action buttons work (Add, Edit, Delete, Save)
- [ ] No console errors (F12)
- [ ] No wrong calculations (e.g. totals, dashboard numbers)
- [ ] Forms validate (required fields, invalid input)
