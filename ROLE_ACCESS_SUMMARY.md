# Role Access & Conflict Prevention Summary

**Date:** February 21, 2026

---

## Access Matrix

| Page | Super Admin | Owner | Admin | Staff |
|------|-------------|-------|-------|-------|
| **Super Admin** (Dashboard, Companies, Error Logs, etc.) | ✅ Full | ❌ | ❌ | ❌ |
| **Tenant** (Dashboard, POS, Products, etc.) | ✅ When impersonating | ✅ Full | ✅ Full | ✅ Per pageAccess |
| **Users, Settings, Backup, Branches, Routes, Purchases** | ✅ | ✅ | ✅ | ❌ Never |

---

## Changes Made

### 1. Staff Block – Admin-Only Pages
- **STAFF_NEVER_ACCESS:** Staff can never access `users`, `settings`, `backup`, `branches`, `routes`, `purchases`
- App.jsx: Added these paths to `getPageIdForPath` so Staff hitting them are redirected to `/dashboard`
- `canAccessPage`: Staff always gets `false` for these pageIds, regardless of `pageAccess`

### 2. Role Logic
- **Super Admin:** `tenantId = 0`; uses `/Admin26` login; sees Super Admin pages only
- **Owner / Admin:** `isAdminOrOwner` → full tenant access, all pages
- **Staff:** `pageAccess` for pos, invoices, reports, products, customers, expenses; never users, settings, backup, branches, routes, purchases

---

## No Conflicts

| Scenario | Behavior |
|----------|----------|
| Super Admin at /login | Logout + "Use Admin Portal" |
| Owner at /Admin26 | Logout + "Use main app" |
| Staff → /users | Redirect to /dashboard |
| Staff → /settings | Redirect to /dashboard |
| Staff → /backup | Redirect to /dashboard |
| Staff → /branches or /routes | Redirect to /dashboard |
| Staff → /purchases | Redirect to /dashboard |
| Admin → any tenant page | Full access |
| Owner → any tenant page | Full access |

---

## How to Test

1. **Super Admin:** Log in at `/Admin26` → Super Admin Dashboard, Companies, Error Logs, etc.
2. **Owner/Admin:** Log in at `/login` → Tenant Dashboard, Users, Settings, Backup, etc.
3. **Staff:** Log in → only allowed pages; direct visit to `/users` or `/settings` → redirect to Dashboard.
