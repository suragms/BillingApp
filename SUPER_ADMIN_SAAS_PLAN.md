# Super Admin – SaaS Plan & Connection Reference

Single place for **backend URL**, **DB reference**, and **feature roadmap** so Super Admin can manage the platform faster and fix issues from one place.

---

## 1. Platform URLs (use in Super Admin only – never commit secrets)

| What | Value | Use |
|------|--------|-----|
| **Backend API** | `https://hexabill.onrender.com` | Base URL for API; frontend uses `https://hexabill.onrender.com/api` |
| **Backend health** | `https://hexabill.onrender.com/api/health` | Anonymous health check |
| **Render service ID** | `srv-d68jpdvpm1nc7393q4d0` | Render dashboard / MCP / logs |
| **Database** | PostgreSQL (Render Singapore) | Internal URL for backend only; set in Render env as `DATABASE_URL` |

- **Frontend** gets API base from `apiConfig.js` (production default: `https://hexabill.onrender.com/api`).
- **Super Admin UI** shows “Backend” and “Database” status from `/api/superadmin/platform-health` (no credentials in UI).

---

## 2. Super Admin – Feature roadmap (easier, maintainable SaaS)

### Done
- Platform overview (companies, MRR, users, storage)
- Companies list + detail (tenant) with tabs: Overview, Users, Invoices, Payments, Subscription, Usage, Limits, Reports
- Per-tenant: suspend, activate, clear data, edit, enter workspace, duplicate data, limits, lockout
- Live activity (top tenants by API calls, last 60 min)
- Revenue report, onboarding report, bulk extend trial / announcement
- Error logs, audit logs, SQL console (read-only), platform health
- Alert bell (unresolved errors), platform settings

### Next (priority order)
1. **Platform & connection card** on Dashboard – show Backend URL + DB status (from platform-health).
2. **Per-tenant request/usage controls** – show request count per tenant (from tenant-activity) and optional caps in tenant detail.
3. **Load / rate visibility** – surface “high volume” tenants and optional per-tenant rate limits in Limits tab.
4. **Super Admin UI/UX** – simple, modern layout: consistent cards, tabs, and mobile-friendly tables.
5. **Single “Client controls” section** – group suspend, activate, clear data, lockout, limits in one place per tenant.
6. **Backend request/usage** – persist request counts per tenant (e.g. daily) for usage reporting (optional).

---

## 3. Backend & API (for Super Admin)

- **Auth:** SystemAdmin only for `/api/superadmin/*` (except health).
- **Key endpoints:**
  - `GET /api/superadmin/platform-health` – DB, migrations, company count, **backendUrl**
  - `GET /api/superadmin/tenant-activity` – top tenants by API calls (last 60 min)
  - `GET /api/superadmin/tenant` – list tenants (paged)
  - `GET /api/superadmin/tenant/:id` – tenant detail
  - `GET /api/superadmin/tenant/:id/activity` – this tenant's request count (last 60 min), high-volume flag
  - `POST /api/superadmin/tenant/:id/clear-data` – clear tenant data (FK-safe order)
  - `GET /api/superadmin/onboarding-report` – onboarding completion
  - `GET /api/superadmin/alert-summary` – for alert bell

- **Load / requests:** Use existing tenant-activity for “last 60 min” request view; add per-tenant limits in tenant Limits tab when needed.

---

## 4. To-do checklist (implement in order)

- [x] Backend: add `backendUrl` to `GET /api/superadmin/platform-health`
- [x] Frontend: Platform & connection card on Super Admin Dashboard (Backend URL + DB status)
- [x] Per-tenant request/usage: GET tenant/:id/activity + display in Overview & Limits tab
- [x] Client controls block + Usage & storage card + upgrade reminder
- [ ] Super Admin UI pass: cards, spacing, mobile tabs, “Client controls” grouping
- [ ] Per-day usage: persist daily request counts (optional); UI pass; storage vs limit warning

---

## 5. Env reference (local .env only – never push)

- `DATABASE_URL` – internal PostgreSQL URL (backend only).
- `VITE_API_BASE_URL` – optional; production frontend defaults to `https://hexabill.onrender.com/api`.
- Backend URL for Super Admin is derived from the request host in platform-health (no env needed for display).
