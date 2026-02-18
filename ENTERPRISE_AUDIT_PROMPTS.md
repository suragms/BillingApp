# Enterprise SaaS Audit Prompt Pack — HexaBill

**⚠️ CRITICAL: This is a multi-tenant financial system. Run audits section by section. Do NOT run all prompts at once.**

---

## 🎯 GOAL

Make Cursor analyze:
- ✅ Complete database schema and relationships
- ✅ Financial calculation risks
- ✅ Multi-tenant isolation vulnerabilities
- ✅ Migration readiness for 241 invoices
- ✅ Transaction safety and data integrity
- ✅ Delete/restore logic correctness
- ✅ Super admin security
- ✅ Scalability (1000 tenants)
- ✅ Code structure and maintainability
- ✅ Production deployment risks

---

## 📋 EXECUTION TODO LIST

| Step | Prompt | Status | Notes |
|------|--------|-------|-------|
| **STEP 1** | PROMPT 1 — Database Structure | ☐ | Run first. Map all tables, columns, relationships. |
| **STEP 2** | PROMPT 2 — Multi-Tenant Security | ☐ | Critical for data isolation. Check every query. |
| **STEP 3** | PROMPT 3 — Financial Logic | ☐ | Validate invoice, VAT, payment, balance calculations. |
| **STEP 4** | PROMPT 4 — Migration Readiness | ☐ | Before importing 241 invoices. Check constraints, dates, defaults. |
| **STEP 5** | PROMPT 5 — Invoice Delete Safety | ☐ | Stock restore, payment reversal, transaction wrapping. |
| **STEP 6** | PROMPT 6 — Transaction Safety | ☐ | Find all create/update/delete flows. Check BEGIN/COMMIT/ROLLBACK. |
| **STEP 7** | PROMPT 7 — Code Structure | ☐ | Duplicate logic, unused files, dead routes, naming consistency. |
| **STEP 8** | PROMPT 8 — Super Admin Safety | ☐ | Destructive actions, audit logs, tenant data protection. |
| **STEP 9** | PROMPT 9 — Scalability Analysis | ☐ | 1000 tenants. Indexes, query optimization, N+1 risks. |
| **STEP 10** | PROMPT 10 — Production Deployment | ☐ | Environment configs, secrets, error handling, monitoring. |

**Execution order:** Run STEP 1 → wait for analysis → STEP 2 → wait → ... → STEP 10. Do NOT paste all at once.

---

## 🧠 PROMPT 1 — FULL DATABASE STRUCTURE ANALYSIS

```
You are acting as a senior SaaS architect and database auditor.

Analyze the entire backend project (backend/HexaBill.Api) and:

1. List ALL database tables (from Models/, Migrations/, AppDbContext.cs).
2. For each table, document:
   - All columns with exact data types (decimal precision, string max length, nullable)
   - Primary keys
   - Foreign keys (with cascade rules)
   - Indexes (unique, non-unique, composite)
   - Unique constraints
   - Default values
   - Check constraints (if any)
3. Identify which tables contain tenant_id (or TenantId).
4. Detect any tables that do NOT include tenant_id but SHOULD (data isolation risk).
5. Identify missing foreign key constraints (orphan record risk).
6. Identify possible orphan records (FKs without CASCADE DELETE).
7. Identify ALL financial tables:
   - Sales / Invoices
   - SaleItems / InvoiceItems
   - Payments
   - Customers (balance fields)
   - Products (cost, price)
   - Expenses
   - Ledger entries (if separate table)
8. Explain relationships between:
   - Sales → SaleItems
   - Sales → Customers
   - Sales → Payments
   - Payments → Sales
   - Customers → Sales (history)
   - Products → SaleItems
   - Expenses → Branches/Routes
   - Any ledger/balance tracking tables
9. Create a relationship diagram in text format (ASCII or Mermaid).
10. Document any computed columns or triggers.

Be extremely detailed. Do NOT summarize. Output table-by-table breakdown.
```

**Expected output:** Complete schema map, relationship diagram, tenant isolation gaps, financial table list.

---

## 🛡️ PROMPT 2 — MULTI-TENANT SECURITY AUDIT

```
Act as a multi-tenant SaaS security auditor.

Audit the project (backend/HexaBill.Api) for tenant isolation risks.

1. Check EVERY query in:
   - Controllers (all GET/POST/PUT/DELETE endpoints)
   - Services (all business logic)
   - Repositories (if any)
2. For each data fetch operation, verify:
   - Does it filter by tenant_id (or TenantId)?
   - Is tenant_id taken from JWT/claims (not user input)?
   - Are there any direct ID-based fetches without tenant validation?
3. Identify routes/controllers that fetch data WITHOUT tenant filtering (CRITICAL RISK).
4. Check super admin bypass logic:
   - Can super admin queries leak tenant data?
   - Is impersonation properly scoped?
   - Can super admin accidentally query wrong tenant?
5. Check financial operations:
   - Are payment queries tenant-scoped?
   - Are invoice queries tenant-scoped?
   - Are customer balance queries tenant-scoped?
   - Are expense queries tenant-scoped?
6. Detect possible data leakage vulnerabilities:
   - Direct ID access (e.g., GET /api/invoices/123 without tenant check)
   - Cross-tenant joins
   - Missing WHERE tenant_id = ...
7. Check if tenant_id is validated on CREATE/UPDATE operations.
8. Check if DELETE operations verify tenant ownership.

List ALL findings with:
- File path
- Line number (if possible)
- Risk level (CRITICAL / HIGH / MEDIUM / LOW)
- Suggested fix

Explain all critical risks clearly.
```

**Expected output:** List of tenant isolation gaps, file locations, risk levels, fixes.

---

## 💵 PROMPT 3 — ACCOUNTING LOGIC VALIDATION

```
Act as a financial system auditor.

Analyze the logic for financial calculations (backend/HexaBill.Api/Modules/Sales, Payments, Customers).

1. Invoice total calculation:
   - How is GrandTotal computed?
   - Is it sum of line items + VAT - discount?
   - Where is this logic (service/controller)?
2. VAT calculation:
   - Is VAT computed per line item or on total?
   - Is VAT rate configurable or hardcoded?
   - Is VAT rounded correctly (decimal precision)?
3. Payment recording:
   - How are payments linked to invoices?
   - Can one payment pay multiple invoices?
   - Can one invoice have multiple payments?
   - Is partial payment supported?
4. Partial payments:
   - How is remaining balance calculated?
   - Is balance updated atomically?
   - What happens if payment > invoice total?
5. Invoice status determination:
   - How is status set (Pending/Paid/Partial)?
   - Is status computed or stored?
   - Is status updated on payment?
6. Balance updates:
   - Where is customer.balance updated?
   - Is it updated on invoice creation?
   - Is it updated on payment?
   - Is it updated on invoice deletion?
   - Is it recalculated or incremented?
7. Ledger entry creation:
   - When are ledger entries created?
   - Are they created for every invoice?
   - Are they created for every payment?
   - Are they deleted when invoice is deleted?
8. Stock restore logic when invoice deleted:
   - Is stock restored correctly?
   - Is it restored per line item?
   - Is quantity conversion handled (unit types)?
   - Is this wrapped in a transaction?

Check for:
- Floating point errors (using float instead of decimal)
- NaN risks (division by zero, null arithmetic)
- Decimal precision issues (too few decimal places)
- Double counting risks (balance updated twice)
- Missing transaction wrapping (partial updates)
- Inconsistent balance logic (recalc vs increment)

Explain if accounting logic is enterprise-safe. List all risks.
```

**Expected output:** Financial calculation risks, precision issues, transaction gaps, balance logic inconsistencies.

---

## 📦 PROMPT 4 — DATA MIGRATION READINESS

```
Act as an enterprise SaaS migration specialist.

Analyze if this system is ready for importing 241 invoices from an external system.

Evaluate:

1. Invoice schema:
   - Does invoice_number have unique constraint? (per tenant or global?)
   - Can invoice_number be manually set or only auto-generated?
   - What fields are mandatory (NOT NULL)?
   - What fields have defaults?
   - Can invoice_date be in the past?
   - Can invoice_date be in the future?
2. Payment schema:
   - Can payments be inserted with custom dates?
   - Can payment_date be before invoice_date?
   - Is payment_amount validated (cannot exceed invoice total)?
   - Can partial payments be imported?
   - Are payment methods restricted or free text?
3. Customer balance:
   - Does system auto-calculate balance from invoices/payments?
   - Or must balance be imported separately?
   - If imported, will it be overwritten?
4. Ledger entries:
   - Does ledger auto-generate entries on invoice create?
   - If importing invoices, will ledger entries be created?
   - Or must ledger entries be imported separately?
5. Import support:
   - Is there an import API endpoint?
   - Does it validate tenant_id?
   - Does it validate foreign keys (customer_id, product_id)?
   - Does it use transactions?
6. Direct CSV/DB insert risks:
   - What will break if CSV data is inserted directly?
   - Will balances be wrong?
   - Will ledger be missing?
   - Will stock be wrong?
   - Will invoice numbers conflict?
7. Database constraints:
   - What unique constraints exist?
   - What foreign key constraints exist?
   - What check constraints exist?
   - Will migration fail on constraint violation?
8. Default values:
   - Are default values safe for imported data?
   - Will missing fields get wrong defaults?
9. Transactions:
   - Are import operations wrapped in transactions?
   - Can partial import cause corruption?

Give step-by-step risk assessment and migration plan.
```

**Expected output:** Migration readiness checklist, constraint analysis, import flow risks, step-by-step plan.

---

## 🧾 PROMPT 5 — INVOICE DELETE SAFETY

```
Act as a backend transaction safety expert.

Audit the invoice deletion logic (backend/HexaBill.Api/Modules/Sales).

Check:

1. Stock restore:
   - Is stock restored correctly?
   - Is it restored per SaleItem?
   - Is quantity conversion handled (unit types, ConversionToBase)?
   - Is stock restored BEFORE or AFTER invoice deletion?
   - What if product was deleted?
   - What if product.StockQty is negative?
2. Payment reversal:
   - Are payments deleted or marked as deleted?
   - Is customer balance updated when payments are reversed?
   - Is balance updated BEFORE or AFTER payment deletion?
   - What if payment was used to pay multiple invoices?
   - What if payment was partial?
3. Ledger entries:
   - Are ledger entries deleted?
   - Or are they marked as deleted?
   - Is customer balance updated when ledger entries are deleted?
4. Transaction wrapping:
   - Is the entire delete operation wrapped in a database transaction?
   - What happens if stock restore fails?
   - What happens if payment deletion fails?
   - What happens if ledger deletion fails?
   - Is there a rollback mechanism?
5. Partial deletion risk:
   - Can invoice be partially deleted (items deleted but invoice remains)?
   - Can this cause data corruption?
   - Can this cause negative stock?
   - Can this cause wrong balance?
6. Audit log:
   - Is deletion logged?
   - Is user_id logged?
   - Is timestamp logged?
   - Is reason logged (if required)?
7. Cascade deletes:
   - Are SaleItems cascade deleted?
   - Are Payments cascade deleted?
   - Are LedgerEntries cascade deleted?
   - Or are they manually deleted in code?

Explain if this is production safe. List all risks.
```

**Expected output:** Delete flow analysis, transaction safety, cascade risks, audit logging gaps.

---

## 🔐 PROMPT 6 — DATABASE TRANSACTION SAFETY

```
Audit the backend for transaction safety.

Find ALL operations that modify financial data:

1. Create invoice flows:
   - SalesController.CreateSale
   - SaleService.CreateSaleAsync
   - Any other invoice creation endpoints
2. Payment insert flows:
   - PaymentsController (all POST endpoints)
   - PaymentService (all create methods)
3. Delete flows:
   - Invoice deletion
   - Payment deletion
   - Customer deletion
   - Product deletion
4. Update flows:
   - Invoice updates
   - Payment updates
   - Balance updates
   - Stock updates

For each flow, check:

1. Is it wrapped in a transaction?
   - Using BEGIN TRANSACTION / COMMIT / ROLLBACK?
   - Using Entity Framework transaction (context.Database.BeginTransaction)?
   - Using a transaction wrapper service?
   - Or NO transaction at all?
2. If transaction is used:
   - Is it properly committed on success?
   - Is it properly rolled back on error?
   - Are exceptions caught and rolled back?
3. If NO transaction:
   - What happens if operation fails halfway?
   - Can this cause data corruption?
   - Can this cause inconsistent balances?
   - Can this cause wrong stock?

List ALL flows with:
- File path
- Method name
- Transaction status (YES / NO / PARTIAL)
- Risk level (CRITICAL / HIGH / MEDIUM / LOW)
- Suggested fix

Explain where data corruption can happen.
```

**Expected output:** Transaction coverage map, corruption risks, file locations, fixes.

---

## 🧩 PROMPT 7 — CODE STRUCTURE AUDIT

```
Act as senior SaaS refactoring architect.

Analyze project structure (both backend and frontend).

1. Detect duplicate logic:
   - Duplicate SQL queries
   - Duplicate business logic
   - Duplicate validation logic
   - Duplicate calculation logic
2. Detect duplicate code:
   - Copy-paste methods
   - Similar controllers
   - Similar services
3. Detect unused files:
   - Unused models
   - Unused controllers
   - Unused services
   - Unused frontend components
   - Unused API endpoints
4. Detect unused code:
   - Unused JavaScript functions
   - Unused C# methods
   - Unused imports
   - Dead code paths
5. Detect dead routes:
   - Frontend routes that don't exist
   - API endpoints that aren't called
   - Redirects to non-existent pages
6. Detect inconsistent naming:
   - Controller naming (PascalCase vs camelCase)
   - Service naming
   - Variable naming
   - File naming
7. Detect poor separation of concerns:
   - Business logic in controllers
   - Database logic in controllers
   - UI logic in services
   - Mixed responsibilities
8. Suggest clean folder structure:
   - Backend: Controllers, Services, Models, DTOs, Repositories
   - Frontend: Pages, Components, Services, Utils, Hooks
   - Shared: Types, Constants, Validators

Make it enterprise maintainable. List all issues with file paths.
```

**Expected output:** Duplicate code list, unused files, dead routes, naming inconsistencies, structure recommendations.

---

## 👑 PROMPT 8 — SUPER ADMIN SAFETY AUDIT

```
Act as enterprise SaaS platform architect.

Audit super admin panel (backend/HexaBill.Api/Modules/SuperAdmin, frontend superadmin pages).

Check:

1. Destructive actions:
   - Can super admin delete tenant data?
   - Can super admin modify financial data directly?
   - Can super admin reset tenant database?
   - Are these actions logged?
2. Financial data access:
   - Can super admin view all tenant invoices?
   - Can super admin modify invoices?
   - Can super admin create payments?
   - Can super admin delete payments?
   - Is this logged?
3. Audit logging:
   - Is there activity audit log?
   - Are super admin actions logged?
   - Are tenant modifications logged?
   - Is user_id logged?
   - Is timestamp logged?
   - Is action type logged?
4. Tenant reset:
   - Is tenant reset safe?
   - Does it delete all data?
   - Does it preserve audit logs?
   - Can it be undone?
5. Database-level access:
   - Is raw SQL exposed?
   - Can super admin run arbitrary queries?
   - Is this a security risk?
6. Impersonation:
   - Is impersonation logged?
   - Can super admin impersonate any tenant?
   - Is impersonation scoped correctly?
   - Can impersonation leak data?

Give professional recommendations for:
- Audit logging requirements
- Access control improvements
- Security hardening
```

**Expected output:** Super admin risks, audit logging gaps, access control issues, security recommendations.

---

## 🚀 PROMPT 9 — SCALABILITY ANALYSIS

```
Act as SaaS scalability engineer.

Assume 1000 tenants, each with:
- 1000 invoices
- 5000 customers
- 10000 products
- 50000 payments

Analyze:

1. Database design:
   - Will single PostgreSQL DB handle 1M invoices?
   - Will single DB handle 5M customers?
   - Is tenant_id indexed on all tenant-scoped tables?
   - Are composite indexes needed (tenant_id + invoice_date)?
2. Index coverage:
   - Are invoice queries indexed?
   - Are customer queries indexed?
   - Are payment queries indexed?
   - Are report queries indexed?
3. Query optimization:
   - Are there N+1 query risks?
   - Are there missing JOIN optimizations?
   - Are there missing WHERE optimizations?
   - Are there missing LIMIT clauses?
4. Heavy reports:
   - How long will branch report take with 1M invoices?
   - How long will customer ledger take with 5M customers?
   - Are reports paginated?
   - Are reports cached?
5. Storage growth:
   - How much storage per tenant?
   - How much storage for 1000 tenants?
   - Is there data archival plan?
   - Is there data retention policy?
6. Table size explosion:
   - Will SaleItems table explode?
   - Will Payments table explode?
   - Will AuditLogs table explode?
   - Are there partition strategies?

Explain scaling plan:
- When to add read replicas
- When to partition tables
- When to archive old data
- When to move to multi-DB
```

**Expected output:** Scalability bottlenecks, index gaps, query optimization needs, scaling roadmap.

---

## 🏭 PROMPT 10 — PRODUCTION DEPLOYMENT RISKS

```
Act as DevOps and production safety auditor.

Analyze production deployment readiness.

1. Environment configuration:
   - Are secrets in environment variables?
   - Are database URLs in env vars?
   - Are API keys in env vars?
   - Is JWT secret in env vars?
   - Are there hardcoded secrets?
2. Error handling:
   - Are exceptions caught?
   - Are errors logged?
   - Are errors returned to user safely?
   - Are stack traces hidden in production?
3. Monitoring:
   - Is there error tracking (Sentry, etc.)?
   - Is there performance monitoring?
   - Is there database monitoring?
   - Is there API monitoring?
4. Database migrations:
   - Are migrations tested?
   - Are migrations reversible?
   - Are migrations run automatically?
   - Are migrations run in transactions?
5. Backup and restore:
   - Is database backed up?
   - Is backup tested?
   - Is restore procedure documented?
6. Health checks:
   - Is there /health endpoint?
   - Does it check database?
   - Does it check external services?
7. Rate limiting:
   - Is API rate limited?
   - Is login rate limited?
   - Is payment rate limited?
8. CORS and security headers:
   - Is CORS configured correctly?
   - Are security headers set?
   - Is HTTPS enforced?

List all production risks and recommendations.
```

**Expected output:** Deployment risks, security gaps, monitoring needs, backup strategy, production checklist.

---

## 🔥 CRITICAL CHECKLIST BEFORE MIGRATION

Before importing 241 invoices, you MUST verify:

- [ ] **STEP 1** — Database schema is fully mapped
- [ ] **STEP 2** — All queries are tenant-scoped
- [ ] **STEP 3** — Financial calculations are correct
- [ ] **STEP 4** — Migration readiness is confirmed
- [ ] **STEP 5** — Delete logic is safe
- [ ] **STEP 6** — All operations use transactions
- [ ] **STEP 7** — Code structure is clean
- [ ] **STEP 8** — Super admin is secure
- [ ] **STEP 9** — System can scale
- [ ] **STEP 10** — Production is ready

**Do NOT migrate until all 10 steps are complete and risks are addressed.**

---

## 📝 HOW TO USE THIS DOCUMENT

1. Open Cursor.
2. Copy **PROMPT 1** → paste in Cursor → wait for analysis.
3. Review output → document findings.
4. Copy **PROMPT 2** → paste in Cursor → wait for analysis.
5. Repeat for all 10 prompts.
6. Create action items from findings.
7. Fix critical issues before migration.

**DO NOT paste all prompts at once. Run one at a time.**

---

*Last updated: Feb 2026. This is a living document. Update as you fix issues.*
