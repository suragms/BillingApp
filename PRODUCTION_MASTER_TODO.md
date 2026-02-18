# HexaBill Production Master TODO — Ordered Plan (60+ Items)

**Rule:** Work in order. Mark each item PENDING → IN_PROGRESS → COMPLETE. Do not skip. Only consider the list "ended" when every item is COMPLETE.

**Code anchors:** Each item includes file(s) and, where known, line or area so implementation is unambiguous and conflicts are avoided when building.

---

## SECTION 1 — WEEK 1: MUST FIX (Data Integrity + Core Bugs)

| # | Status | Fix | File(s) | Code ref / notes | Est |
|---|--------|-----|---------|------------------|-----|
| 1 | COMPLETE | Fix BranchService dead return / wrong branch list bug | `backend/HexaBill.Api/Modules/Branches/BranchService.cs` | Verified: no dead return found; GetBranchesAsync/GetBranchByIdAsync tenant filter correct. Left as-is. | 1h |
| 2 | COMPLETE | Login: single indexed query by email (no full table scan) | `backend/HexaBill.Api/Modules/Auth/AuthService.cs` | Replaced ToListAsync + FirstOrDefault with single FirstOrDefaultAsync(u => u.Email == normalizedEmail). Index on Users(Email) already in AppDbContext. | 30m |
| 3 | COMPLETE | Register: same — find user by email with single query | `backend/HexaBill.Api/Modules/Auth/AuthService.cs` | Replaced full Users load with single FirstOrDefaultAsync(u => u.Email == normalizedEmail). | 15m |
| 4 | COMPLETE | VAT from company settings everywhere in POS (no hardcoded 5%) | `frontend/hexabill-ui/src/pages/company/PosPage.jsx` | FALLBACK_VAT_PERCENT constant for initial/error only; all calcs use vatPercent from settings; backend SaleService already uses GetVatPercentAsync() from settings | 15m |
| 5 | COMPLETE | VAT from settings in Purchases page | `frontend/hexabill-ui/src/pages/company/PurchasesPage.jsx` | Added useEffect to fetch vatPercent from settingsAPI.getCompanySettings(); replaced hardcoded "VAT (5%)" labels with VAT ({vatPercent}%) in table header and mobile cards | 2h |
| 6 | COMPLETE | Unify profit calculation (one formula everywhere) | `backend/HexaBill.Api/Modules/Reports/ProfitService.cs`, `ReportService.cs` | Single formula: Net Profit = GrandTotal(Sales) - COGS - Expenses. COGS from SaleItems (Qty×conversion×CostPrice). ProfitService now uses COGS (not Purchases); daily chart uses GrandTotal for Sales; both files document formula | 3h |
| 7 | COMPLETE | Cheque balance double-count: align Customer.PendingBalance with cleared-only | `BalanceService.cs`, `CustomerService.cs`, `PaymentService.cs` | TotalPayments and PendingBalance now use CLEARED payments only. Sale.PaidAmount = sum of CLEARED payments only. Invoice and customer ledger both show "amount still owed" consistently; pending cheques do not reduce balance until cleared | 2h |
| 8 | COMPLETE | Sales Ledger summary from filtered data only | `frontend/hexabill-ui/src/pages/company/SalesLedgerPage.jsx` | filteredSummary is from filteredLedger; cards/footer use it. Comment added: do not use server salesLedgerSummary for UI totals | 1h |
| 9 | COMPLETE | Aging report use remaining balance (not total balance) | `backend/.../ReportService.cs` GetAgingReportAsync | Uses sale.PaidAmount (CLEARED only) so balance = GrandTotal - PaidAmount; skip when balance <= 0.01. Bucket totals sum BalanceAmount (remaining) | 2h |
| 10 | COMPLETE | Add Customer form: Branch + Route fields | `frontend/hexabill-ui/src/pages/company/CustomersPage.jsx` | Add modal: Branch + Route dropdowns (optional); routes filtered by branch; reset form on open Add; payload sends branchId/routeId as int or null; Edit already had Branch/Route | 2h |
| 11 | COMPLETE | Backup: document ephemeral risk + add pg_dump path for Postgres | `BackupController.cs`, `ComprehensiveBackupService.cs`, `docs/BACKUP_AND_IMPORT_STRATEGY.md` | Ephemeral risk documented in code comments and BACKUP_AND_IMPORT_STRATEGY.md; pg_dump path configurable via Backup:PostgresPgDumpPath; doc lists Render Dashboard backup and one-off pg_dump options | 4h |
| 12 | COMPLETE | Move backup storage to S3/R2 (or Supabase Storage) | `ComprehensiveBackupService.cs`, `BackupController.cs`, `docs/BACKUP_AND_IMPORT_STRATEGY.md` | S3/R2: upload on create; list (Location=S3); download via GetBackupForDownloadAsync; delete; restore/preview/import resolve from S3; ServiceUrl for R2; DeleteLocalAfterUpload; create+download streams from S3 | 4h |

---

## SECTION 2 — STAFF EXPERIENCE (What They See / Can Do)

| # | Status | Fix | File(s) | Code ref / notes | Est |
|---|--------|-----|---------|------------------|-----|
| 13 | COMPLETE | Staff: redirect away from /branches and /routes (harden) | `frontend/hexabill-ui/src/App.jsx` | Path normalized (trailing slash); /branches and /branches/:id and /routes and /routes/:id covered; route-level redirect for Staff (defense in depth) | 30m |
| 14 | COMPLETE | Staff POS: friendly message when no route assigned (not generic 403) | `SalesController.cs`, `PosPage.jsx` | Backend: Staff with no routes get 403 + "You have no route assigned. Ask your admin to assign you to a route before creating invoices." (NO_ROUTE_ASSIGNED); UnauthorizedAccessException → 403; Frontend uses API message or friendly fallback for 403 | 1h |
| 15 | COMPLETE | Expense form: add Branch + Route fields | `Expense.cs`, `DTOs.cs`, `AppDbContext.cs`, `ExpenseService.cs`, `ExpensesController.cs`, `ExpensesPage.jsx` | Expense.RouteId + FK; CreateExpenseRequest/ExpenseDto RouteId/RouteName; staff route validation via IRouteScopeService; Add/Edit modals have Route dropdown (filtered by branch); list shows Route column | 1h |
| 16 | COMPLETE | Staff dashboard: hide Expenses card or show only "Add expense" (consistent) | `Dashboard.jsx`, `DashboardTally.jsx` | Staff see Expenses card as "Add expense" / "Log expense"; DEFAULT_STAFF_VIEW_ITEMS includes 'expenses'; gateway menu shows "Add expense" for Staff; Admin/Owner keep "Expenses" / "Manage" | 30m |
| 17 | COMPLETE | Gateway menu: ensure Branches/Routes hidden for Staff in sidebar | `frontend/hexabill-ui/src/components/Layout.jsx` | "Branches & Routes" already gated by isAdminOrOwner(user); comment added for clarity | 30m |

---

## SECTION 3 — SUPER ADMIN (Dashboard, Tenant Detail, Logs)

| # | Status | Fix | File(s) | Code ref / notes | Est |
|---|--------|-----|---------|------------------|-----|
| 18 | COMPLETE | Super Admin MRR: show "No subscription data" when no plans | `SuperAdminTenantService.cs`, `SuperAdminDashboard.jsx` | Backend: HasSubscriptionData on PlatformDashboardDto; frontend: Platform MRR card shows "No subscription data" when !hasSubscriptionData | 30m |
| 19 | COMPLETE | Super Admin storage: use real PG size or label as estimate | Backend dashboard API + `SuperAdminDashboard.jsx` | Backend: pg_database_size(current_database()) when Postgres; else row-based estimate. DTO: IsRealDatabaseSize, StorageFormulaDescription. Frontend: title "Estimated DB Storage" when estimate, desc = formula or "PostgreSQL database size" | 1h |
| 20 | COMPLETE | Live Activity: document in-memory reset on deploy | `SuperAdminDashboard.jsx` + backend ITenantActivityService | UI note: "Activity resets on server restart."; TenantActivityService comment documents in-memory reset and optional DB persistence | 30m |
| 21 | COMPLETE | Tenant Detail Reports tab: fix links so they open tenant context | `frontend/hexabill-ui/src/pages/superadmin/SuperAdminTenantDetailPage.jsx` | Reports/links now: impersonateEnter(tenant.id), impersonateTenant(id), then navigate(path) so tenant context is set and reports open in that tenant | 2h |
| 22 | COMPLETE | Force logout: ensure SessionVersion checked on every JWT request | `JwtMiddleware.cs`, `SecurityConfiguration.cs` | SessionVersion enforced in JWT Bearer OnTokenValidated (all API paths). Mismatch → 401, X-Auth-Failure: Session-Expired. Verify: force logout then call any protected API with old token → 401 | 1h |
| 23 | COMPLETE | Reset Database: confirm scope + add safety (e.g. exclude subscription/settings or warn) | `SuperAdminTenantDetailPage.jsx` + backend clear-tenant API | Backend: XML doc states preserved (Tenant, Users, Products, Customers, Subscriptions, Settings) vs wiped. Frontend: modal and Danger Zone text state preserved vs wiped and recommend backup first | 2h |
| 24 | COMPLETE | Duplicate Data: add preview of what will be copied | `SuperAdminTenantDetailPage.jsx` + backend | Backend: GetDuplicateDataPreviewAsync + GET duplicate-data/preview; frontend: preview box with source/target counts, warning when target has existing products/settings | 2h |
| 25 | COMPLETE | Tenant health score: add tooltip or breakdown | `SuperAdminTenantDetailPage.jsx` | Backend: TenantHealthDto.ScoreDescription with formula text. Frontend: Info icon tooltip + visible breakdown paragraph | 1h |
| 26 | COMPLETE | Error Logs: add TenantId / UserId columns or in message | Backend ErrorLog model + `SuperAdminErrorLogsPage.jsx` | Model already had TenantId/UserId. Middleware now uses NameIdentifier/id for userId. API returns tenantName via join; UI shows Tenant (name + id) and User ID columns | 2h |
| 27 | COMPLETE | Error Logs: add "Resolve" or "Suppress" action | Backend ErrorLogs API + `SuperAdminErrorLogsPage.jsx` | ErrorLog.ResolvedAt; GET error-logs?includeResolved; PATCH error-logs/:id/resolve; UI: Resolve button per row, "Include resolved" checkbox | 2h |
| 28 | COMPLETE | Audit Logs: pagination (e.g. 50 per page, load more) | Backend audit API + `SuperAdminAuditLogsPage.jsx` | Backend already had page/pageSize (1–100). Frontend: 50 per page, Prev/Next, "Load more" appends next page; footer shows "Showing X of Y" | 2h |
| 29 | COMPLETE | Audit Logs: filter by action type | `SuperAdminAuditLogsPage.jsx` + backend | Action type dropdown added: User Created/Updated/Deleted, Sale, Payment, Expense, Purchase, SuperAdmin, Backup, System Reset, Tenant Data Clear; uses existing action param | 1h |
| 30 | COMPLETE | Audit Logs: store and show old/new values for edits | Backend audit write + model + `SuperAdminAuditLogsPage.jsx` | SaleService: Sale Updated audit sets OldValues/NewValues (GrandTotal, Subtotal, Discount, VatTotal). API returns oldValues/newValues; UI has "Old → New" column with parsed display | 3h |

---

## SECTION 4 — PRODUCTION RISKS AT SCALE

| # | Status | Fix | File(s) | Code ref / notes | Est |
|---|--------|-----|---------|------------------|-----|
| 31 | COMPLETE | Replace in-memory rate limiter with ASP.NET or Redis | `SecurityConfiguration.cs` | Custom RateLimitingMiddleware removed. Built-in AddRateLimiter + UseRateLimiter: GlobalLimiter by IP, FixedWindow 100/min, OnRejected 429 + JSON; no in-memory lock | 2h |
| 32 | COMPLETE | Verify AddPerformanceIndexes.sql is deployed and applied | `Program.cs`, `Migrations/AddPerformanceIndexes.sql`, `docs/PERFORMANCE_INDEXES.md` | File already in publish (Migrations\*.sql in csproj). Log: "Index SQL file found at: {Path}"; when not found, warning references docs/PERFORMANCE_INDEXES.md. Doc lists manual PostgreSQL CREATE INDEX CONCURRENTLY for Sales, Payments, Products, Customers. | 1h |
| 33 | COMPLETE | Dashboard pending bills: use SQL (not load all in memory) | `ReportService.cs` GetPendingBillsAsync | Single query: join Sales+Customers, filter (GrandTotal-PaidAmount)>0.01 in DB, project to PendingBillDto; DaysOverdue set in lightweight in-memory pass; no ToListAsync of full entities | 1h |
| 34 | COMPLETE | Background jobs: consider separate worker or limit concurrency | `DailyBackupScheduler.cs`, `docs/BACKGROUND_JOBS.md` | Backup: SemaphoreSlim ensures only one run at a time; skip if previous still in progress; default off-peak 21:00. Doc describes all three jobs, in-process vs Render worker/cron, and how to disable in-app backup for external cron | 4h |
| 35 | COMPLETE | Connection pooling: document or add PgBouncer | `docs/CONNECTION_POOLING_AND_PGBOUNCER.md`, `Program.cs` | Doc: max connections, PgBouncer (Render add-on or external), connect to pooler URL; optional Maximum Pool Size. Program.cs comment points to doc for scale | 1h |
| 36 | COMPLETE | JWT "Remember Me" 30-day token: document force-logout as mitigation | `AuthService.cs`, `docs/PRODUCTION_DEPLOYMENT_VERCEL_RENDER.md` | AuthService comment: long-lived tokens invalidated by SessionVersion on each request when admin force-logout. Deploy doc §6: "Remember Me and force logout" — 30-day token revoked on next request after force logout | 30m |

---

## SECTION 5 — CALCULATIONS & BALANCE (Audit)

| # | Status | Fix | File(s) | Code ref / notes | Est |
|---|--------|-----|---------|------------------|-----|
| 37 | COMPLETE | Sales calculation: ensure VAT% from settings in backend | `SaleService.cs` GetVatPercentAsync | GetVatPercentAsync(tenantId) reads Settings VAT_PERCENT scoped by OwnerId == tenantId; fallback 5. Create/Update/CreateWithOverride all pass tenantId; comments reference #37 | 1h |
| 38 | COMPLETE | Single profit formula doc in code | `ProfitService.cs`, `ReportService.cs` | File headers + method docs: "Single definition: Profit = GrandTotal(Sales) - COGS - Expenses". COGS = SaleItems (Qty×ConversionToBase×CostPrice). ProfitService CalculateProfitAsync/GetDailyProfitAsync; ReportService GetSummaryReportAsync profit block; #38 refs | 30m |
| 39 | COMPLETE | Cheque clearing workflow UI | `PaymentsPage.jsx` | Pending cheques: "Mark cleared" / "Return". Cleared cheques: "Mark pending". Normalized method/chequeStatus from API (mode/status). Page note: balance reflects cleared only. Backend UpdatePaymentStatus already supports PENDING↔CLEARED (#7) | 3h |

---

## SECTION 6 — WEEK 2: UX + SCALE

| # | Status | Fix | File(s) | Code ref / notes | Est |
|---|--------|-----|---------|------------------|-----|
| 40 | COMPLETE | Reports: shared date range across tabs | `frontend/hexabill-ui/src/pages/company/ReportsPage.jsx` | One date range selector that applies to all report tabs (or persist last range per tab) | 2h |
| 41 | COMPLETE | Route Performance tab: real content | `frontend/hexabill-ui/src/pages/company/RouteDetailPage.jsx` | Implement or wire Route Performance tab with real metrics (sales per route, visits, etc.) | 3h |
| 42 | COMPLETE | Customer list: pagination (e.g. 100 per page) | `frontend/hexabill-ui/src/pages/company/CustomersPage.jsx` | Add page size and next/prev or infinite scroll; avoid loading thousands at once | 2h |
| 43 | COMPLETE | Payment gateway (Stripe or Tap) for subscriptions | `SubscriptionPlansPage.jsx` + backend subscription/billing | Allow tenants to pay for plans; webhook for success/failure; update subscription status | 8h |

---

## SECTION 7 — SUPER ADMIN: MISSING FEATURES (Critical for Scale)

| # | Status | Fix | File(s) | Code ref / notes | Est |
|---|--------|-----|---------|------------------|-----|
| 44 | COMPLETE | Global customer search (e.g. "Find invoice #4532 across tenants") | Backend super-admin API + new Super Admin page or tenant detail | Search invoices/customers across all tenants (read-only); restrict to Super Admin | 1d |
| 45 | COMPLETE | Platform revenue report (MRR trend, churn, new signups) | Backend + `SuperAdminDashboard.jsx` or new page | Report: subscription revenue by month, churn rate, new signups trend | 2d |
| 46 | COMPLETE | Tenant onboarding tracker (who completed setup, who stuck) | Backend + Super Admin UI | Backend: GET /api/superadmin/onboarding-report?incompleteOnly; SuperAdminTenantService.GetOnboardingReportAsync derives 5 steps (Company, VAT, Product, Customer, Invoice) from tenant data; SuperAdminDashboard section with summary cards, "Incomplete only" filter, table with step checkmarks and link to tenant detail | 2d |
| 47 | COMPLETE | Read-only SQL console for Super Admin | Backend secure endpoint + Super Admin UI | SqlConsoleController: POST /api/superadmin/sql-console, SELECT-only validation + read-only transaction, 30s timeout, 1000 row limit; SuperAdminSqlConsolePage + nav "SQL Console" | 3d |
| 48 | COMPLETE | Bulk tenant actions (e.g. extend trial 7 days, send announcement) | Backend + Super Admin UI | POST /api/superadmin/tenant/bulk-actions: extend_trial (days), send_announcement (title, message, severity); SuperAdminTenantsPage: checkbox column, Select all on page, bulk bar with Extend trial + Send announcement modals | 2d |
| 49 | COMPLETE | Error alert bell (critical events) for Super Admin | Backend + Super Admin layout | GET /api/superadmin/alert-summary: unresolvedCount, last24hCount, last1hCount, recent (5 items); SuperAdminLayout: bell icon with badge, dropdown with summary + recent + link to Error Logs; mobile bell navigates to error-logs; poll every 60s | 1d |
| 50 | COMPLETE | View tenant invoices (read-only) without full impersonation | Backend + Super Admin tenant detail | GET /api/superadmin/tenant/{id}/invoices (page, pageSize); SuperAdminTenantDetailPage "Invoices" tab with read-only table + pagination | 1d |
| 51 | COMPLETE | Payment history tab per tenant | Super Admin tenant detail + backend | GET tenant/{id}/payment-history; Payments tab on tenant detail with table (Plan, Status, Cycle, Start, Expires/Next, Amount, Payment method, Created) | 1d |
| 52 | COMPLETE | Export tenant data (e.g. CSV) for offboarding/compliance | Backend export API + Super Admin UI | GET tenant/{id}/export returns ZIP (invoices.csv, customers.csv, products.csv); Overview "Export data" card with Download ZIP button | 1d |

---

## SECTION 8 — OWNER & STAFF NEEDS (Priority Features)

| # | Status | Fix | File(s) | Code ref / notes | Est |
|---|--------|-----|---------|------------------|-----|
| 53 | COMPLETE | Outstanding collections list with phone number | Reports or Ledger | Reports → Collections (with phone): customers with PendingBalance > 0; table Name, Phone, Balance, Address; Export CSV + Print | 2h |
| 54 | COMPLETE | Staff performance view (Owner) | Dashboard or Reports | Reports → Staff Performance (adminOnly): per-staff Invoices, Total Billed, Collected, Collection %, Avg days to pay; route filter; Dashboard shortcut "Staff Performance" → /reports?tab=staff | 4h |
| 55 | COMPLETE | Stock alerts before run-out | Alerts + Products | Per-product ReorderLevel; optional global fallback in Settings (LOW_STOCK_GLOBAL_THRESHOLD); Products/Reports/Alerts use it; Settings → Low stock global threshold | 2h |
| 56 | COMPLETE | One-click invoice to WhatsApp | PosPage, InvoicePreviewModal, Sales Ledger, utils/whatsapp.js | wa.me link with optional customer phone; Share on POS (post-sale), InvoicePreviewModal (customerPhone prop), Sales Ledger (Actions → WhatsApp per Sale row); PDF download then open WhatsApp | 1w |
| 57 | COMPLETE | Branch-wise profit breakdown | Reports, ProfitService, ProfitController, ReportsPage | Profit by branch: Sales, COGS, Expenses, Gross/Net profit & margin; GET /api/profit/branch-breakdown; Reports → Branch Profit tab (Owner/Admin) | 4h |
| 58 | COMPLETE | Monthly P&L export for accountant | ReportsPage | Export P&L (or summary) as PDF/Excel for a given month | 4h |
| 59 | PENDING | Today's route customer list with balances (Staff) | Route detail or POS | Staff sees today's route with customer outstanding balance | 2h |
| 60 | PENDING | Collection sheet (Staff) with print layout | Payments or Reports | Printable collection sheet for route with customer, balance, collected amount | 1d |

---

## SECTION 9 — MONTH 2 / GROWTH (Summary List)

| # | Status | Fix | File(s) | Notes | Est |
|---|--------|-----|---------|-------|-----|
| 61 | PENDING | Email invoice sending (e.g. SendGrid) | Backend + frontend | Send invoice PDF by email | 3d |
| 62 | PENDING | Supplier master with ledger | New module | Suppliers and supplier payments/ledger | 1w |
| 63 | PENDING | Purchase returns module | PurchasesPage + backend | Record and track purchase returns | 3d |
| 64 | PENDING | Recurring invoices / subscriptions (tenant-facing) | Billing + frontend | Recurring invoice or subscription billing per customer | 1w |
| 65 | PENDING | Customer bulk import (CSV) | CustomersPage + backend | Upload CSV to create/update customers | 2d |
| 66 | PENDING | Customer detail page (/customers/:id) deep link | CustomersPage + routing | Full customer detail with ledger, invoices, payments | 3d |
| 67 | PENDING | Barcode scanner in POS | PosPage | Scan barcode to add product to cart | 2d |
| 68 | PENDING | Per-item discount in POS | PosPage + backend | Discount per line item, not only invoice total | 1d |
| 69 | PENDING | Product categories (filter, display) | Products + backend | Category on product; filter by category in POS and products list | 2d |
| 70 | PENDING | API key management for tenants (if API plan) | Backend + tenant Settings | Generate/revoke API keys for API access plans | 3d |

---

## PRODUCTION READINESS (Plan)

| Item | Status | Notes |
|------|--------|-------|
| Fix remaining 500s and verify SQLite/PostgreSQL parity | COMPLETE | GlobalExceptionHandlerMiddleware persists 500s to ErrorLogs; frontend 4xx/5xx mapping with retry. |
| Reports full-width and Collections visible | COMPLETE | Layout uses max-w-full for /reports; tab strip with "Scroll for more"; Collections label short. |
| Reports load only active tab | COMPLETE | Per-tab cache; skeleton in tab content area; summary first, then tab on first visit. |
| Verify tenant/logo isolation | COMPLETE | selected_tenant_id set on login (normal users); X-Tenant-Id sent; branding refreshes after impersonation. |
| Centralized frontend error handling for 4xx/5xx | COMPLETE | api.js: 400 validation, 404, 502 with retry; 500/503 already handled. |
| Staff online indicator (if required) | Phase 6 | Optional: last active ping, green/red on Users or Team view. |
| Load test and error matrix | COMPLETE | See `docs/ERROR_MATRIX.md`; PRODUCTION_CHECKLIST and PRE_PRODUCTION_TEST_CHECKLIST updated. |

### Same-email auth (Phase 5)

- **Intended behavior:** One email can exist in multiple tenants (different companies). Login is by email + password; the backend resolves the tenant from the JWT (tenant_id claim) or from the selected tenant after Super Admin impersonation.
- **After login:** For a normal tenant user, the frontend sets `selected_tenant_id` from the login response so that API calls and branding (GET /api/settings) use that tenant. For System Admin, no tenant is set until they impersonate; then `X-Tenant-Id` is sent.
- **Risk:** Same password across tenants is a user choice; enforce strong password policy and rate limiting on login (already in place).

### Super Admin list performance

- **Tenants list:** Backend `GetTenants` uses server-side pagination (page, pageSize, search, statusFilter). See `SuperAdminTenantController.GetTenants` and `SuperAdminTenantService.GetTenantsAsync`.
- **Users list (platform):** Backend `GetUsers` returns `PagedResponse<UserDto>`. Filters and scopes applied (e.g. only active tenants when relevant).

---

## HOW TO USE THIS LIST

1. **Order:** Work from #1 to #70. Do not skip.
2. **Status:** Update each row: `PENDING` → `IN_PROGRESS` → `COMPLETE`. Only when all are COMPLETE is the list "ended".
3. **Conflicts:** Each item has file(s) and code refs; complete one item fully before starting the next to avoid merge/conflict issues.
4. **Est:** Estimates are for planning; adjust as you go.
5. **Production:** Sections 1, 4, and 5 are critical for production stability and data correctness. Sections 2–3 and 6 improve Staff and Super Admin experience. Sections 7–9 are growth and scale.

---

**END OF MASTER TODO — 70 items. Finish in order; then only end.**
