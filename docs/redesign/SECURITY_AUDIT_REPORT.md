# HexaBill Security Audit Report

**Date:** 2026-02-16  
**Scope:** Data isolation, SQL injection, authentication, input validation

---

## Executive Summary

| Category | Status | Notes |
|----------|--------|-------|
| Data Isolation | ✅ Fixed | ValidationController leak fixed; core modules use TenantId |
| SQL Injection | ✅ Low Risk | Parameterized EF queries; raw SQL uses lock IDs only |
| Authentication | ✅ OK | JWT with tenant_id claim; TenantScopedController base |
| Rate Limiting | ✅ Present | RateLimitingMiddleware in SecurityConfiguration |
| Database Indexes | ✅ Added | AddPerformanceIndexes.sql created |

---

## 1. Data Isolation

### ✅ Core Modules (OK)
- **SaleService**: All queries filter by `TenantId`; Super Admin (tenantId=0) intentionally sees all
- **CustomerService**: Tenant-scoped via `GetCustomersAsync(tenantId, ...)`
- **ProductService**: Tenant-scoped via `GetProductsAsync(tenantId, ...)`
- **RouteService, BranchService**: Tenant filters applied
- **PaymentService**: Uses tenant-scoped sales/customers
- **ReportService**: `if (tenantId > 0)` filter applied

### 🔧 Fixed
- **ValidationController / BalanceService**  
  - **Issue**: `DetectAllBalanceMismatchesAsync()` returned ALL customers across tenants when called by company Admin  
  - **Fix**: Added `tenantId` parameter; non-Super Admin now gets tenant-filtered results  
  - **Fix**: Customer-specific endpoints (validate, fix, recalculate) now verify `CanAccessCustomerAsync` before acting

### ℹ️ Intentional No-Filter (Super Admin only)
- **SuperAdminTenantService**: Platform-wide stats; Super Admin only
- **BackupService / ComprehensiveBackupService**: Full export; Super Admin only
- **ResetService**: Full reset; Super Admin only
- **AlertService (background)**: System-wide checks; no user context
- **CustomersController, ProductsController**: When `IsSystemAdmin`, show all (explicit branch)

---

## 2. SQL Injection

### ✅ Findings
- Raw SQL: `ExecuteSqlRawAsync` used for schema/admin, lock IDs (numeric), migrations
- Search/filter: EF `Contains`, `Where` with parameters
- **InvoiceNumberService**: Uses numeric `lockId` in raw SQL
- No user input concatenated into SQL

---

## 3. Authentication

### ✅ Implemented
- JWT with `tenant_id` (and legacy `owner_id`) claims
- `TenantIdExtensions.GetTenantIdFromToken()` – single source of tenant ID
- `TenantScopedController` with `CurrentTenantId`, `IsSystemAdmin`
- `[Authorize]` on protected endpoints

---

## 4. Recommendations

### Before Production
1. **PostgreSQL RLS** (if using Postgres): Add row-level security policies per doc
2. **AspNetCoreRateLimit**: Consider adding if stricter limits needed
3. **Data isolation test**: Create two tenants, log in as Company A, call `GET /api/invoices/{company_b_id}` → expect 403/404

### Indexes
`Migrations/AddPerformanceIndexes.sql` created with:
- `idx_sales_tenant_created`, `idx_sales_tenant_invoicedate`
- `idx_customers_tenant_name`
- `idx_products_tenant_sku`, `idx_products_tenant_name`
- `idx_payments_tenant_created`, `idx_payments_customer`

---

## 5. Files Changed

| File | Change |
|------|--------|
| `BalanceService.cs` | `DetectAllBalanceMismatchesAsync(tenantId?)` with tenant filter |
| `ValidationController.cs` | Inherit TenantScopedController; tenant filter; `CanAccessCustomerAsync` |
| `TenantBrandingContext.jsx` | Fix API base URL 5001→5000 |
| `Migrations/AddPerformanceIndexes.sql` | New performance indexes |
