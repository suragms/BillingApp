# PROMPT 1 — FULL DATABASE STRUCTURE ANALYSIS

**Analysis Date:** February 17, 2026  
**System:** HexaBill Multi-Tenant SaaS Financial System  
**Database:** PostgreSQL (production), SQLite (development)

---

## 1. COMPLETE DATABASE TABLES LIST

The system contains **33 database tables** organized as follows:

### Core Multi-Tenant Tables
1. **Tenants** - Tenant/organization master
2. **Users** - User accounts (Owner, Admin, Staff, SystemAdmin)
3. **Settings** - Tenant-specific configuration (composite key: Key + OwnerId)

### Financial Core Tables
4. **Sales** - Invoices/Invoices (main financial document)
5. **SaleItems** - Invoice line items
6. **Payments** - Payment records
7. **Customers** - Customer master with balance tracking
8. **Products** - Product/inventory master
9. **Purchases** - Purchase orders from suppliers
10. **PurchaseItems** - Purchase line items
11. **Expenses** - Company-level expenses
12. **ExpenseCategories** - Expense category master

### Branch & Route Architecture Tables
13. **Branches** - Branch locations
14. **Routes** - Sales routes under branches
15. **RouteCustomers** - Many-to-many: Routes ↔ Customers
16. **RouteStaff** - Many-to-many: Routes ↔ Staff Users
17. **BranchStaff** - Many-to-many: Branches ↔ Staff Users
18. **RouteExpenses** - Route-level expenses (Fuel, Staff, Delivery, Misc)

### Inventory & Transaction Tables
19. **InventoryTransactions** - Stock movement audit trail
20. **PriceChangeLogs** - Product price change history

### Return & Adjustment Tables
21. **SaleReturns** - Sales return documents
22. **SaleReturnItems** - Sales return line items
23. **PurchaseReturns** - Purchase return documents
24. **PurchaseReturnItems** - Purchase return line items

### Audit & Versioning Tables
25. **AuditLogs** - User action audit trail
26. **InvoiceVersions** - Invoice edit history (versioning)
27. **ErrorLogs** - Server error logs
28. **Alerts** - System alerts/notifications

### Subscription & Billing Tables
29. **SubscriptionPlans** - SaaS subscription plan definitions
30. **Subscriptions** - Tenant subscription records

### System Tables
31. **InvoiceTemplates** - Invoice template HTML/CSS
32. **PaymentIdempotencies** - Payment idempotency keys
33. **DemoRequests** - Demo request tracking (Super Admin)

---

## 2. DETAILED TABLE SCHEMA

### 2.1 TENANTS TABLE

**Purpose:** Multi-tenant root entity. Each tenant is a separate company/organization.

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| Name | VARCHAR(200) | NO | | | Tenant company name |
| Subdomain | VARCHAR(100) | YES | NULL | UNIQUE INDEX (filtered: IS NOT NULL) | Future: tenant1.app.com |
| Domain | VARCHAR(200) | YES | NULL | UNIQUE INDEX (filtered: IS NOT NULL) | Future: tenant1.com |
| Country | VARCHAR(10) | NO | 'AE' | | Default UAE |
| Currency | VARCHAR(10) | NO | 'AED' | | Default AED |
| VatNumber | VARCHAR(50) | YES | NULL | | |
| CompanyNameEn | VARCHAR(200) | YES | NULL | | |
| CompanyNameAr | VARCHAR(200) | YES | NULL | | |
| Address | VARCHAR(500) | YES | NULL | | |
| Phone | VARCHAR(20) | YES | NULL | | |
| Email | VARCHAR(100) | YES | NULL | | |
| LogoPath | VARCHAR(500) | YES | NULL | | |
| Status | VARCHAR | NO | 'Active' | | Enum: Active, Suspended, Cancelled |
| CreatedAt | TIMESTAMP | NO | | | |
| TrialEndDate | TIMESTAMP | YES | NULL | | |
| SuspendedAt | TIMESTAMP | YES | NULL | | |
| SuspensionReason | VARCHAR(500) | YES | NULL | | |

**Indexes:**
- PRIMARY KEY: Id
- UNIQUE: Subdomain (filtered: IS NOT NULL)
- UNIQUE: Domain (filtered: IS NOT NULL)

**Foreign Keys:** None (root table)

**Relationships:**
- One-to-Many: Tenants → Users
- One-to-Many: Tenants → Settings
- One-to-Many: Tenants → Branches
- One-to-Many: Tenants → Subscriptions

---

### 2.2 USERS TABLE

**Purpose:** User accounts for authentication and authorization.

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| Name | VARCHAR(100) | NO | | | |
| Email | VARCHAR(100) | NO | | UNIQUE INDEX | |
| PasswordHash | TEXT | NO | | | BCrypt hash |
| Role | VARCHAR | NO | | | Enum: Owner, Admin, Staff |
| OwnerId | INT | YES | NULL | | **LEGACY** - Multi-tenant (will be removed) |
| TenantId | INT | YES | NULL | | **NEW** - Multi-tenant (replaces OwnerId) |
| Phone | VARCHAR(20) | YES | NULL | | |
| DashboardPermissions | TEXT | YES | NULL | | Comma-separated list |
| CreatedAt | TIMESTAMP | NO | | | |
| SessionVersion | INT | NO | 0 | | Incremented on Force Logout |

**Indexes:**
- PRIMARY KEY: Id
- UNIQUE: Email

**Foreign Keys:** None (TenantId/OwnerId are not FK constraints - application-level filtering)

**Relationships:**
- Many-to-One: Users → Tenant (via TenantId, not FK)
- One-to-Many: Users → Sales (CreatedBy, LastModifiedBy, DeletedBy)
- One-to-Many: Users → Payments (CreatedBy)
- One-to-Many: Users → Expenses (CreatedBy)
- One-to-Many: Users → AuditLogs (UserId)
- Many-to-Many: Users ↔ Routes (via RouteStaff)
- Many-to-Many: Users ↔ Branches (via BranchStaff)

**⚠️ CRITICAL TENANT ISOLATION NOTE:**
- Users table has **NO FK constraint** on TenantId
- Tenant isolation is enforced at **application level** (WHERE TenantId = ...)
- This is a **RISK** - orphan users possible if tenant deleted

---

### 2.3 SALES TABLE (Invoices)

**Purpose:** Main financial document - invoices/sales orders.

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| OwnerId | INT | NO | | | **LEGACY** - Multi-tenant |
| TenantId | INT | YES | NULL | | **NEW** - Multi-tenant |
| InvoiceNo | VARCHAR(100) | NO | | **UNIQUE INDEX** (OwnerId, InvoiceNo) filtered: IsDeleted=false | Auto-generated sequence |
| ExternalReference | VARCHAR(200) | YES | NULL | UNIQUE INDEX (filtered: IS NOT NULL) | Idempotency key |
| InvoiceDate | TIMESTAMP | NO | | | Can be past/future |
| CustomerId | INT | YES | NULL | FK → Customers | Nullable (cash sales) |
| BranchId | INT | YES | NULL | FK → Branches (SET NULL) | |
| RouteId | INT | YES | NULL | FK → Routes (SET NULL) | |
| Subtotal | DECIMAL(18,2) | NO | 0 | | Sum of line items before VAT |
| VatTotal | DECIMAL(18,2) | NO | 0 | | VAT amount (5% in UAE) |
| Discount | DECIMAL(18,2) | NO | 0 | | Invoice-level discount |
| GrandTotal | DECIMAL(18,2) | NO | 0 | | Subtotal + VAT - Discount |
| TotalAmount | DECIMAL(18,2) | NO | 0 | | Alias for GrandTotal |
| PaidAmount | DECIMAL(18,2) | NO | 0 | | Sum of cleared payments |
| PaymentStatus | VARCHAR | NO | 'Pending' | | Enum: Pending, Partial, Paid |
| LastPaymentDate | TIMESTAMP | YES | NULL | | |
| DueDate | TIMESTAMP | YES | NULL | | Credit customer due date |
| Notes | TEXT | YES | NULL | | |
| CreatedBy | INT | NO | | FK → Users (CASCADE) | |
| CreatedAt | TIMESTAMP | NO | | INDEX | |
| LastModifiedBy | INT | YES | NULL | FK → Users (NO ACTION) | |
| LastModifiedAt | TIMESTAMP | YES | NULL | | |
| IsDeleted | BOOLEAN | NO | false | | Soft delete flag |
| DeletedBy | INT | YES | NULL | FK → Users (NO ACTION) | |
| DeletedAt | TIMESTAMP | YES | NULL | | |
| IsFinalized | BOOLEAN | NO | true | | Stock decremented when true |
| IsLocked | BOOLEAN | NO | false | INDEX | Locked after 48 hours |
| LockedAt | TIMESTAMP | YES | NULL | | |
| EditReason | VARCHAR(500) | YES | NULL | | Required for Staff edits |
| Version | INT | NO | 1 | | Version number for edits |
| RowVersion | BYTEA/BLOB | NO | [0] | | Optimistic concurrency token |

**Indexes:**
- PRIMARY KEY: Id
- **UNIQUE:** (OwnerId, InvoiceNo) WHERE IsDeleted = false
- **UNIQUE:** ExternalReference WHERE ExternalReference IS NOT NULL
- INDEX: CreatedAt
- INDEX: IsLocked
- INDEX: BranchId
- INDEX: RouteId

**Foreign Keys:**
- CustomerId → Customers (nullable, no cascade)
- BranchId → Branches (SET NULL on delete)
- RouteId → Routes (SET NULL on delete)
- CreatedBy → Users (CASCADE on delete)
- LastModifiedBy → Users (NO ACTION)
- DeletedBy → Users (NO ACTION)

**Relationships:**
- One-to-Many: Sales → SaleItems (CASCADE delete)
- One-to-Many: Sales → Payments (no cascade - manual delete)
- One-to-Many: Sales → InvoiceVersions (versioning)
- Many-to-One: Sales → Customer
- Many-to-One: Sales → Branch
- Many-to-One: Sales → Route

**⚠️ CRITICAL FINDINGS:**

1. **Invoice Number Uniqueness:**
   - Unique constraint: (OwnerId, InvoiceNo) WHERE IsDeleted = false
   - **RISK:** After soft delete, invoice number can be reused
   - **RISK:** If migrating 241 invoices, must ensure no conflicts with existing InvoiceNo

2. **Tenant Isolation:**
   - Has both OwnerId (legacy) and TenantId (new)
   - **RISK:** Queries must filter by TenantId, not just OwnerId
   - **RISK:** No FK constraint on TenantId → orphan invoices possible

3. **Payment Status:**
   - Computed field (not always updated atomically)
   - **RISK:** PaymentStatus may be stale if payments added/deleted outside transaction

4. **Stock Decrement:**
   - IsFinalized flag controls stock decrement
   - **RISK:** If IsFinalized = false, stock not decremented → negative stock possible

---

### 2.4 SALEITEMS TABLE (Invoice Line Items)

**Purpose:** Line items for each invoice.

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| SaleId | INT | NO | | FK → Sales (CASCADE) | |
| ProductId | INT | NO | | FK → Products | |
| UnitType | VARCHAR(20) | NO | 'CRTN' | | CRTN, KG, PIECE, etc. |
| Qty | DECIMAL(18,2) | NO | 0 | | Quantity in UnitType |
| UnitPrice | DECIMAL(18,2) | NO | 0 | | Price per unit |
| Discount | DECIMAL(18,2) | NO | 0 | | Line-level discount |
| VatAmount | DECIMAL(18,2) | NO | 0 | | VAT for this line (5%) |
| LineTotal | DECIMAL(18,2) | NO | 0 | | (Qty × UnitPrice - Discount) + VAT |

**Indexes:**
- PRIMARY KEY: Id
- Foreign Key Index: SaleId (implicit)

**Foreign Keys:**
- SaleId → Sales (CASCADE DELETE - items deleted when invoice deleted)
- ProductId → Products (no cascade - product deletion doesn't delete items)

**Relationships:**
- Many-to-One: SaleItems → Sale
- Many-to-One: SaleItems → Product

**⚠️ CRITICAL FINDINGS:**

1. **CASCADE DELETE:**
   - SaleItems CASCADE delete when Sale deleted
   - **GOOD:** Prevents orphan line items
   - **RISK:** Stock restore must happen BEFORE SaleItems deleted

2. **Product Reference:**
   - ProductId has FK but NO CASCADE
   - **RISK:** If product deleted, SaleItems.ProductId becomes invalid
   - **RISK:** Historical invoices may reference deleted products

3. **Unit Conversion:**
   - UnitType stored as string
   - ConversionToBase stored on Product, not SaleItem
   - **RISK:** If product.ConversionToBase changes, historical invoices show wrong base qty

---

### 2.5 PAYMENTS TABLE

**Purpose:** Payment records linked to invoices.

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| OwnerId | INT | NO | | | **LEGACY** - Multi-tenant |
| TenantId | INT | YES | NULL | | **NEW** - Multi-tenant |
| SaleId | INT | YES | NULL | FK → Sales | Nullable (advance payments) |
| CustomerId | INT | YES | NULL | FK → Customers | |
| Amount | DECIMAL(18,2) | NO | 0 | | Payment amount |
| Mode | VARCHAR | NO | | | Enum: CASH, CHEQUE, ONLINE, CREDIT |
| Reference | VARCHAR(200) | YES | NULL | | Cheque no / Transaction ID |
| Status | VARCHAR | NO | | | Enum: PENDING, CLEARED, RETURNED, VOID |
| PaymentDate | TIMESTAMP | NO | | | Can be past/future |
| CreatedBy | INT | NO | | FK → Users (CASCADE) | |
| CreatedAt | TIMESTAMP | NO | | | |
| UpdatedAt | TIMESTAMP | YES | NULL | | |
| RowVersion | BYTEA/BLOB | YES | NULL | | Optimistic concurrency |

**Indexes:**
- PRIMARY KEY: Id
- Foreign Key Indexes: SaleId, CustomerId, CreatedBy (implicit)

**Foreign Keys:**
- SaleId → Sales (no cascade - manual delete)
- CustomerId → Customers (no cascade)
- CreatedBy → Users (CASCADE on delete)

**Relationships:**
- Many-to-One: Payments → Sale (nullable)
- Many-to-One: Payments → Customer (nullable)
- Many-to-One: Payments → User (CreatedBy)

**⚠️ CRITICAL FINDINGS:**

1. **Payment Status:**
   - Status: PENDING, CLEARED, RETURNED, VOID
   - **RISK:** Only CLEARED payments should update customer balance
   - **RISK:** If Status changes, balance must be recalculated

2. **Payment Date:**
   - Can be in past or future
   - **RISK:** PaymentDate can be before InvoiceDate (advance payment OK, but may confuse reports)

3. **No Cascade Delete:**
   - Payments NOT cascade deleted when Sale deleted
   - **RISK:** Manual payment deletion required on invoice delete
   - **RISK:** Orphan payments if invoice soft-deleted but payments remain

4. **Customer Balance Update:**
   - Payment insertion should update Customer.TotalPayments
   - **RISK:** If payment inserted without updating customer, balance wrong
   - **RISK:** Must be in same transaction

---

### 2.6 CUSTOMERS TABLE

**Purpose:** Customer master with real-time balance tracking.

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| OwnerId | INT | NO | | | **LEGACY** - Multi-tenant |
| TenantId | INT | YES | NULL | | **NEW** - Multi-tenant |
| BranchId | INT | YES | NULL | FK → Branches | |
| RouteId | INT | YES | NULL | | No FK (application-level) |
| Name | VARCHAR(200) | NO | | | |
| CustomerType | INT | NO | 0 | | Enum: Credit=0, Cash=1 |
| Phone | VARCHAR(20) | YES | NULL | | |
| Email | VARCHAR(100) | YES | NULL | | |
| Trn | VARCHAR(50) | YES | NULL | | Tax Registration Number |
| Address | VARCHAR(500) | YES | NULL | | |
| CreditLimit | DECIMAL(18,2) | NO | 0 | DEFAULT 0 | |
| PaymentTerms | VARCHAR(100) | YES | NULL | | Net 7, Net 30, etc. |
| **TotalSales** | DECIMAL(18,2) | NO | 0 | DEFAULT 0 | Sum of invoice GrandTotal |
| **TotalPayments** | DECIMAL(18,2) | NO | 0 | DEFAULT 0 | Sum of CLEARED payments |
| **PendingBalance** | DECIMAL(18,2) | NO | 0 | DEFAULT 0 | TotalSales - TotalPayments |
| **Balance** | DECIMAL(18,2) | NO | 0 | DEFAULT 0 | **LEGACY** - use PendingBalance |
| LastActivity | TIMESTAMP | YES | NULL | | Last transaction date |
| LastPaymentDate | TIMESTAMP | YES | NULL | | Last payment received |
| CreatedAt | TIMESTAMP | NO | | | |
| UpdatedAt | TIMESTAMP | NO | | | |
| RowVersion | BYTEA/BLOB | NO | [0] | | Optimistic concurrency |

**Indexes:**
- PRIMARY KEY: Id
- Foreign Key Index: BranchId (implicit)

**Foreign Keys:**
- BranchId → Branches (no cascade)

**Relationships:**
- One-to-Many: Customers → Sales
- One-to-Many: Customers → Payments
- Many-to-One: Customers → Branch
- Many-to-Many: Customers ↔ Routes (via RouteCustomers)

**⚠️ CRITICAL FINDINGS:**

1. **Balance Tracking Fields:**
   - **TotalSales:** Sum of invoice GrandTotal (excluding deleted)
   - **TotalPayments:** Sum of CLEARED payments only
   - **PendingBalance:** TotalSales - TotalPayments (computed)
   - **Balance:** Legacy field (kept for backward compatibility)
   - **RISK:** These fields MUST be updated atomically with invoice/payment operations
   - **RISK:** If invoice created without updating TotalSales → balance wrong
   - **RISK:** If payment created without updating TotalPayments → balance wrong
   - **RISK:** If payment Status changes from CLEARED to VOID → balance must be recalculated

2. **RouteId:**
   - RouteId has NO FK constraint
   - **RISK:** Orphan customers if route deleted
   - **RISK:** Route assignment via RouteCustomers junction table (many-to-many)

3. **Tenant Isolation:**
   - Has both OwnerId and TenantId
   - **RISK:** Queries must filter by TenantId

---

### 2.7 PRODUCTS TABLE

**Purpose:** Product/inventory master.

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| OwnerId | INT | NO | | | **LEGACY** - Multi-tenant |
| TenantId | INT | YES | NULL | | **NEW** - Multi-tenant |
| Sku | VARCHAR(50) | NO | | **UNIQUE INDEX** (TenantId, Sku) | Per-tenant unique |
| NameEn | VARCHAR(200) | NO | | | |
| NameAr | VARCHAR(200) | YES | NULL | | |
| UnitType | VARCHAR(20) | NO | 'PIECE' | | CRTN, KG, PIECE, etc. |
| ConversionToBase | DECIMAL(18,2) | NO | 1 | | Multiplier to base unit |
| CostPrice | DECIMAL(18,2) | NO | 0 | | Cost per base unit (VAT-excluded) |
| SellPrice | DECIMAL(18,2) | NO | 0 | | Selling price per base unit |
| StockQty | DECIMAL(18,2) | NO | 0 | | Current stock in base units |
| ReorderLevel | INT | NO | 0 | | Low stock threshold |
| ExpiryDate | TIMESTAMP | YES | NULL | | |
| DescriptionEn | TEXT | YES | NULL | | |
| DescriptionAr | TEXT | YES | NULL | | |
| RowVersion | BYTEA/BLOB | NO | [0] | | Optimistic concurrency |
| CreatedAt | TIMESTAMP | NO | | | |
| UpdatedAt | TIMESTAMP | NO | | | |

**Indexes:**
- PRIMARY KEY: Id
- **UNIQUE:** (TenantId, Sku) - SKU unique per tenant

**Foreign Keys:** None (root table per tenant)

**Relationships:**
- One-to-Many: Products → SaleItems
- One-to-Many: Products → PurchaseItems
- One-to-Many: Products → InventoryTransactions
- One-to-Many: Products → PriceChangeLogs

**⚠️ CRITICAL FINDINGS:**

1. **SKU Uniqueness:**
   - Unique per tenant: (TenantId, Sku)
   - **GOOD:** Prevents duplicate SKUs within tenant
   - **RISK:** If migrating products, must ensure SKU uniqueness

2. **CostPrice:**
   - Updated from PurchaseItems when purchase created
   - **RISK:** If CostPrice never set (no purchases), COGS = 0 in reports
   - **RISK:** CostPrice is VAT-excluded (good for profit calc)

3. **StockQty:**
   - Updated on Sale (decrement) and Purchase (increment)
   - **RISK:** If stock update fails, StockQty becomes wrong
   - **RISK:** Negative stock possible if checks fail

4. **ConversionToBase:**
   - Used to convert sale qty to base units for stock
   - **RISK:** If ConversionToBase = 0 → division by zero
   - **RISK:** Historical invoices use product's current ConversionToBase (may be wrong)

---

### 2.8 PURCHASES TABLE

**Purpose:** Purchase orders from suppliers.

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| OwnerId | INT | NO | | | **LEGACY** - Multi-tenant |
| TenantId | INT | YES | NULL | | **NEW** - Multi-tenant |
| SupplierName | VARCHAR(200) | NO | | | |
| InvoiceNo | VARCHAR(100) | NO | | **UNIQUE INDEX** (OwnerId, InvoiceNo) | Supplier invoice number |
| ExternalReference | VARCHAR(200) | YES | NULL | | |
| ExpenseCategory | VARCHAR(100) | YES | NULL | | Inventory, Supplies, Equipment |
| PurchaseDate | TIMESTAMP | NO | | | |
| Subtotal | DECIMAL(18,2) | YES | NULL | | Amount before VAT (nullable for backward compat) |
| VatTotal | DECIMAL(18,2) | YES | NULL | | VAT amount (nullable) |
| TotalAmount | DECIMAL(18,2) | NO | 0 | | Grand total (Subtotal + VAT) |
| InvoiceFilePath | VARCHAR(500) | YES | NULL | | |
| InvoiceFileName | VARCHAR(255) | YES | NULL | | |
| CreatedBy | INT | NO | | FK → Users (CASCADE) | |
| CreatedAt | TIMESTAMP | NO | | | |

**Indexes:**
- PRIMARY KEY: Id
- **UNIQUE:** (OwnerId, InvoiceNo) - Supplier invoice unique per tenant

**Foreign Keys:**
- CreatedBy → Users (CASCADE on delete)

**Relationships:**
- One-to-Many: Purchases → PurchaseItems (CASCADE delete)
- Many-to-One: Purchases → User (CreatedBy)

**⚠️ CRITICAL FINDINGS:**

1. **Invoice Number:**
   - Unique per tenant: (OwnerId, InvoiceNo)
   - **RISK:** If migrating purchases, must ensure InvoiceNo uniqueness

2. **VAT Fields:**
   - Subtotal and VatTotal are nullable (backward compatibility)
   - **RISK:** Old purchases may have NULL → profit calc may be wrong
   - **RISK:** New purchases should always set Subtotal and VatTotal

3. **Product Cost Update:**
   - PurchaseItems update Product.CostPrice
   - **RISK:** If purchase created without updating CostPrice → COGS = 0

---

### 2.9 EXPENSES TABLE

**Purpose:** Company-level expenses (rent, utilities, etc.).

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| OwnerId | INT | NO | | | **LEGACY** - Multi-tenant |
| TenantId | INT | YES | NULL | | **NEW** - Multi-tenant |
| BranchId | INT | YES | NULL | FK → Branches | Null = company-level |
| CategoryId | INT | NO | | FK → ExpenseCategories | |
| Amount | DECIMAL(18,2) | NO | 0 | | |
| Date | TIMESTAMP | NO | | | Expense date |
| Note | VARCHAR(500) | YES | NULL | | |
| CreatedBy | INT | NO | | FK → Users (CASCADE) | |
| CreatedAt | TIMESTAMP | NO | | | |

**Indexes:**
- PRIMARY KEY: Id
- Foreign Key Indexes: BranchId, CategoryId, CreatedBy (implicit)

**Foreign Keys:**
- BranchId → Branches (no cascade)
- CategoryId → ExpenseCategories (no cascade)
- CreatedBy → Users (CASCADE on delete)

**Relationships:**
- Many-to-One: Expenses → Branch (nullable)
- Many-to-One: Expenses → ExpenseCategory
- Many-to-One: Expenses → User (CreatedBy)

**⚠️ CRITICAL FINDINGS:**

1. **Branch Assignment:**
   - BranchId nullable = company-level expense
   - **RISK:** Reports must aggregate company + branch expenses

2. **Tenant Isolation:**
   - Has TenantId
   - **RISK:** Queries must filter by TenantId

---

### 2.10 BRANCHES TABLE

**Purpose:** Branch locations under tenants.

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| TenantId | INT | NO | | FK → Tenants | |
| Name | VARCHAR(200) | NO | | | |
| Address | VARCHAR(500) | YES | NULL | | |
| Location | VARCHAR(200) | YES | NULL | | |
| ManagerUserId | INT | YES | NULL | | No FK (application-level) |
| IsActive | BOOLEAN | NO | true | | |
| CreatedAt | TIMESTAMP | NO | | | |
| UpdatedAt | TIMESTAMP | YES | NULL | | |

**Indexes:**
- PRIMARY KEY: Id
- INDEX: TenantId
- INDEX: ManagerUserId (implicit)

**Foreign Keys:**
- TenantId → Tenants (no cascade specified - default CASCADE)

**Relationships:**
- Many-to-One: Branches → Tenant
- One-to-Many: Branches → Routes
- One-to-Many: Branches → Sales (BranchId)
- One-to-Many: Branches → Customers (BranchId)
- One-to-Many: Branches → Expenses (BranchId)
- Many-to-Many: Branches ↔ Users (via BranchStaff)

**⚠️ CRITICAL FINDINGS:**

1. **TenantId FK:**
   - Has FK constraint on TenantId
   - **GOOD:** Prevents orphan branches

2. **ManagerUserId:**
   - No FK constraint
   - **RISK:** Orphan branches if manager user deleted

---

### 2.11 ROUTES TABLE

**Purpose:** Sales routes under branches.

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| BranchId | INT | NO | | FK → Branches | |
| TenantId | INT | NO | | FK → Tenants | |
| Name | VARCHAR(200) | NO | | | |
| AssignedStaffId | INT | YES | NULL | FK → Users (SET NULL) | Primary staff |
| IsActive | BOOLEAN | NO | true | | |
| CreatedAt | TIMESTAMP | NO | | | |
| UpdatedAt | TIMESTAMP | YES | NULL | | |

**Indexes:**
- PRIMARY KEY: Id
- INDEX: BranchId
- INDEX: TenantId

**Foreign Keys:**
- BranchId → Branches (no cascade specified - default CASCADE)
- TenantId → Tenants (no cascade specified - default CASCADE)
- AssignedStaffId → Users (SET NULL on delete)

**Relationships:**
- Many-to-One: Routes → Branch
- Many-to-One: Routes → Tenant
- Many-to-One: Routes → User (AssignedStaffId - primary staff)
- One-to-Many: Routes → Sales (RouteId)
- Many-to-Many: Routes ↔ Customers (via RouteCustomers)
- Many-to-Many: Routes ↔ Users (via RouteStaff)
- One-to-Many: Routes → RouteExpenses

**⚠️ CRITICAL FINDINGS:**

1. **Dual Tenant Reference:**
   - Has both BranchId (→ Tenant via Branch) and TenantId (direct)
   - **RISK:** TenantId may become inconsistent if Branch.TenantId changes
   - **RISK:** Should derive TenantId from Branch, or enforce consistency

---

### 2.12 ROUTEEXPENSES TABLE

**Purpose:** Route-level expenses (Fuel, Staff, Delivery, Misc).

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| RouteId | INT | NO | | FK → Routes | |
| TenantId | INT | NO | | FK → Tenants | |
| Category | VARCHAR | NO | | | Enum: Fuel, Staff, Delivery, Misc |
| Amount | DECIMAL(18,2) | NO | 0 | | |
| ExpenseDate | TIMESTAMP | NO | | INDEX | |
| Description | VARCHAR(500) | YES | NULL | | |
| CreatedBy | INT | NO | | FK → Users (CASCADE) | |
| CreatedAt | TIMESTAMP | NO | | | |

**Indexes:**
- PRIMARY KEY: Id
- INDEX: RouteId
- INDEX: TenantId
- INDEX: ExpenseDate

**Foreign Keys:**
- RouteId → Routes (no cascade specified)
- TenantId → Tenants (no cascade specified)
- CreatedBy → Users (CASCADE on delete)

**Relationships:**
- Many-to-One: RouteExpenses → Route
- Many-to-One: RouteExpenses → Tenant
- Many-to-One: RouteExpenses → User (CreatedBy)

---

### 2.13 AUDITLOGS TABLE

**Purpose:** User action audit trail.

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| OwnerId | INT | NO | | | **LEGACY** - Multi-tenant |
| TenantId | INT | YES | NULL | | **NEW** - Multi-tenant |
| UserId | INT | NO | | FK → Users | |
| Action | VARCHAR(200) | NO | | | e.g., "Invoice Created", "Payment Deleted" |
| EntityType | VARCHAR(100) | YES | NULL | | e.g., "Sale", "Payment" |
| EntityId | INT | YES | NULL | | |
| OldValues | TEXT | YES | NULL | | JSON serialized |
| NewValues | TEXT | YES | NULL | | JSON serialized |
| IpAddress | VARCHAR(45) | YES | NULL | | IPv6 max length |
| Details | TEXT | YES | NULL | | |
| CreatedAt | TIMESTAMP | NO | | INDEX | |

**Indexes:**
- PRIMARY KEY: Id
- INDEX: CreatedAt
- INDEX: UserId
- INDEX: TenantId
- INDEX: (EntityType, EntityId)

**Foreign Keys:**
- UserId → Users (no cascade specified)

**Relationships:**
- Many-to-One: AuditLogs → User

**⚠️ CRITICAL FINDINGS:**

1. **Audit Logging:**
   - Logs user actions with before/after values
   - **GOOD:** Provides audit trail
   - **RISK:** Large table if not archived

2. **Tenant Isolation:**
   - Has TenantId
   - **RISK:** Queries must filter by TenantId

---

### 2.14 INVOICEVERSIONS TABLE

**Purpose:** Invoice edit history (versioning).

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| OwnerId | INT | NO | | | **LEGACY** - Multi-tenant |
| TenantId | INT | YES | NULL | | **NEW** - Multi-tenant |
| SaleId | INT | NO | | FK → Sales | |
| VersionNumber | INT | NO | | | |
| CreatedById | INT | NO | | FK → Users | |
| CreatedAt | TIMESTAMP | NO | | | |
| DataJson | JSONB/TEXT | NO | | | Full snapshot of Sale + SaleItems |
| EditReason | VARCHAR(500) | YES | NULL | | |
| DiffSummary | VARCHAR(1000) | YES | NULL | | |

**Indexes:**
- PRIMARY KEY: Id
- INDEX: SaleId
- INDEX: (SaleId, VersionNumber)

**Foreign Keys:**
- SaleId → Sales (no cascade specified)
- CreatedById → Users (no cascade specified)

**Relationships:**
- Many-to-One: InvoiceVersions → Sale
- Many-to-One: InvoiceVersions → User (CreatedById)

**⚠️ CRITICAL FINDINGS:**

1. **Versioning:**
   - Stores full JSON snapshot of invoice
   - **GOOD:** Provides edit history
   - **RISK:** Large storage if many edits

---

### 2.15 INVENTORYTRANSACTIONS TABLE

**Purpose:** Stock movement audit trail.

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| OwnerId | INT | NO | | | **LEGACY** - Multi-tenant |
| TenantId | INT | YES | NULL | | **NEW** - Multi-tenant |
| ProductId | INT | NO | | FK → Products | |
| ChangeQty | DECIMAL(18,2) | NO | 0 | | Positive = increase, Negative = decrease |
| TransactionType | VARCHAR | NO | | | Enum: Purchase, Sale, Adjustment, Return, PurchaseReturn |
| RefId | INT | YES | NULL | | Reference to Sale/Purchase ID |
| Reason | TEXT | YES | NULL | | |
| CreatedAt | TIMESTAMP | NO | | | |

**Indexes:**
- PRIMARY KEY: Id
- Foreign Key Index: ProductId (implicit)

**Foreign Keys:**
- ProductId → Products (no cascade specified)

**Relationships:**
- Many-to-One: InventoryTransactions → Product

**⚠️ CRITICAL FINDINGS:**

1. **Audit Trail:**
   - Tracks all stock movements
   - **GOOD:** Can reconstruct stock history
   - **RISK:** Large table if not archived

---

### 2.16 SUBSCRIPTIONS TABLE

**Purpose:** Tenant subscription records.

| Column | Data Type | Nullable | Default | Constraints | Notes |
|--------|-----------|----------|---------|-------------|-------|
| Id | INT | NO | AUTO | PRIMARY KEY | |
| TenantId | INT | NO | | FK → Tenants | |
| PlanId | INT | NO | | FK → SubscriptionPlans | |
| Status | VARCHAR | NO | 'Trial' | | Enum: Trial, Active, Expired, Cancelled, Suspended, PastDue |
| BillingCycle | VARCHAR | NO | 'Monthly' | | Enum: Monthly, Yearly |
| StartDate | TIMESTAMP | NO | | | |
| EndDate | TIMESTAMP | YES | NULL | | |
| TrialEndDate | TIMESTAMP | YES | NULL | | |
| CancelledAt | TIMESTAMP | YES | NULL | | |
| CancellationReason | VARCHAR(500) | YES | NULL | | |
| ExpiresAt | TIMESTAMP | YES | NULL | | |
| NextBillingDate | TIMESTAMP | YES | NULL | | |
| Amount | DECIMAL(18,2) | NO | 0 | | Current subscription amount |
| Currency | VARCHAR(10) | NO | 'AED' | | |
| PaymentGatewaySubscriptionId | VARCHAR(200) | YES | NULL | | Stripe/Razorpay ID |
| PaymentGatewayCustomerId | VARCHAR(200) | YES | NULL | | |
| PaymentMethod | VARCHAR(50) | YES | NULL | | stripe, razorpay, manual |
| CreatedAt | TIMESTAMP | NO | | | |
| UpdatedAt | TIMESTAMP | YES | NULL | | |

**Indexes:**
- PRIMARY KEY: Id
- Foreign Key Indexes: TenantId, PlanId (implicit)

**Foreign Keys:**
- TenantId → Tenants (no cascade specified)
- PlanId → SubscriptionPlans (no cascade specified)

**Relationships:**
- Many-to-One: Subscriptions → Tenant
- Many-to-One: Subscriptions → SubscriptionPlan

---

### 2.17 REMAINING TABLES (Summary)

**ExpenseCategories:**
- Id (PK), Name (UNIQUE), ColorCode, IsActive, CreatedAt
- No tenant_id (global categories)

**Settings:**
- Composite PK: (Key, OwnerId)
- TenantId (nullable)
- Value (TEXT)
- **RISK:** Composite key uses OwnerId (legacy)

**PriceChangeLogs:**
- Id (PK), OwnerId, TenantId, ProductId, OldPrice, NewPrice, PriceDifference, ChangedBy, Reason, ChangedAt
- FK: ProductId → Products

**SaleReturns / SaleReturnItems:**
- Return documents with line items
- FK: SaleId → Sales, CustomerId → Customers
- CASCADE: SaleReturnItems → SaleReturn

**PurchaseReturns / PurchaseReturnItems:**
- Purchase return documents
- FK: PurchaseId → Purchases
- CASCADE: PurchaseReturnItems → PurchaseReturn

**PaymentIdempotencies:**
- IdempotencyKey (PK), PaymentId, UserId, ResponseSnapshot (JSONB)
- Prevents duplicate payments

**InvoiceTemplates:**
- Id (PK), Name, Version, HtmlCode, CssCode, IsActive, CreatedBy, CreatedAt
- No tenant_id (global templates)

**Alerts:**
- Id (PK), OwnerId, TenantId, Type, Title, Message, Severity, IsRead, IsResolved, CreatedAt, ResolvedBy
- FK: ResolvedBy → Users (SET NULL)

**ErrorLogs:**
- Id (PK), TraceId, ErrorCode, Message, StackTrace, Path, Method, TenantId, UserId, CreatedAt
- INDEX: CreatedAt, TenantId

**DemoRequests:**
- Id (PK), CompanyName, ContactName, Email, Status, CreatedTenantId, CreatedAt
- INDEX: Status, Email, CreatedAt
- No tenant_id (Super Admin table)

**SubscriptionPlans:**
- Id (PK), Name, Description, MonthlyPrice, YearlyPrice, Currency, MaxUsers, MaxInvoicesPerMonth, etc.
- No tenant_id (global plans)

**BackupManifest:**
- Not a table (JSON model for backup files)

---

## 3. TENANT_ID COVERAGE ANALYSIS

### Tables WITH TenantId (✅ Tenant-Scoped):

1. ✅ **Tenants** - Root table (no TenantId needed)
2. ✅ **Users** - TenantId (nullable for SystemAdmin)
3. ✅ **Sales** - TenantId (nullable, also has OwnerId legacy)
4. ✅ **SaleItems** - Inherits from Sale (no direct TenantId)
5. ✅ **Payments** - TenantId (nullable, also has OwnerId legacy)
6. ✅ **Customers** - TenantId (nullable, also has OwnerId legacy)
7. ✅ **Products** - TenantId (nullable, also has OwnerId legacy)
8. ✅ **Purchases** - TenantId (nullable, also has OwnerId legacy)
9. ✅ **PurchaseItems** - Inherits from Purchase (no direct TenantId)
10. ✅ **Expenses** - TenantId (nullable, also has OwnerId legacy)
11. ✅ **Branches** - TenantId (NOT NULL, FK to Tenants)
12. ✅ **Routes** - TenantId (NOT NULL, FK to Tenants)
13. ✅ **RouteExpenses** - TenantId (NOT NULL, FK to Tenants)
14. ✅ **InventoryTransactions** - TenantId (nullable, also has OwnerId legacy)
15. ✅ **AuditLogs** - TenantId (nullable, also has OwnerId legacy)
16. ✅ **InvoiceVersions** - TenantId (nullable, also has OwnerId legacy)
17. ✅ **PriceChangeLogs** - TenantId (nullable, also has OwnerId legacy)
18. ✅ **SaleReturns** - TenantId (nullable, also has OwnerId legacy)
19. ✅ **PurchaseReturns** - TenantId (nullable, also has OwnerId legacy)
20. ✅ **Alerts** - TenantId (nullable, also has OwnerId legacy)
21. ✅ **ErrorLogs** - TenantId (nullable)
22. ✅ **Settings** - TenantId (nullable, composite key uses OwnerId)

### Tables WITHOUT TenantId (⚠️ Risk Assessment):

1. ⚠️ **ExpenseCategories** - No TenantId
   - **RISK:** Categories are global, not tenant-specific
   - **IMPACT:** All tenants share same categories
   - **RECOMMENDATION:** Add TenantId if categories should be tenant-specific

2. ⚠️ **InvoiceTemplates** - No TenantId
   - **RISK:** Templates are global
   - **IMPACT:** All tenants share templates
   - **RECOMMENDATION:** Add TenantId if templates should be tenant-specific

3. ⚠️ **SubscriptionPlans** - No TenantId
   - **GOOD:** Plans are global (Super Admin manages)
   - **NO RISK:** Intended design

4. ⚠️ **DemoRequests** - No TenantId
   - **GOOD:** Super Admin table
   - **NO RISK:** Intended design

5. ⚠️ **RouteCustomers** - No TenantId
   - **RISK:** Junction table, should derive from Route.TenantId
   - **IMPACT:** Low (Route has TenantId)

6. ⚠️ **RouteStaff** - No TenantId
   - **RISK:** Junction table, should derive from Route.TenantId
   - **IMPACT:** Low (Route has TenantId)

7. ⚠️ **BranchStaff** - No TenantId
   - **RISK:** Junction table, should derive from Branch.TenantId
   - **IMPACT:** Low (Branch has TenantId)

8. ⚠️ **SaleItems** - No TenantId
   - **RISK:** Inherits from Sale.TenantId
   - **IMPACT:** Low (Sale has TenantId)

9. ⚠️ **PurchaseItems** - No TenantId
   - **RISK:** Inherits from Purchase.TenantId
   - **IMPACT:** Low (Purchase has TenantId)

10. ⚠️ **SaleReturnItems** - No TenantId
    - **RISK:** Inherits from SaleReturn.TenantId
    - **IMPACT:** Low (SaleReturn has TenantId)

11. ⚠️ **PurchaseReturnItems** - No TenantId
    - **RISK:** Inherits from PurchaseReturn.TenantId
    - **IMPACT:** Low (PurchaseReturn has TenantId)

**⚠️ CRITICAL TENANT ISOLATION RISKS:**

1. **Dual Tenant Fields (OwnerId + TenantId):**
   - Many tables have BOTH OwnerId (legacy) and TenantId (new)
   - **RISK:** Queries must use TenantId, not OwnerId
   - **RISK:** OwnerId may become inconsistent
   - **RECOMMENDATION:** Migrate all queries to TenantId, remove OwnerId after migration

2. **Nullable TenantId:**
   - Many TenantId fields are nullable
   - **RISK:** NULL TenantId = SystemAdmin or orphan record
   - **RISK:** Queries must handle NULL (WHERE TenantId = ? OR TenantId IS NULL for SystemAdmin)
   - **RECOMMENDATION:** Ensure NULL only for SystemAdmin, not orphan records

3. **Missing FK Constraints:**
   - TenantId has NO FK constraints (except Branches, Routes, RouteExpenses)
   - **RISK:** Orphan records if tenant deleted
   - **RISK:** Invalid TenantId values possible
   - **RECOMMENDATION:** Add FK constraints where possible (may break if tenant deletion needed)

---

## 4. MISSING FOREIGN KEY CONSTRAINTS

### Missing FKs (Orphan Record Risk):

1. **Users.TenantId** → Tenants.Id
   - **RISK:** Orphan users if tenant deleted
   - **IMPACT:** HIGH (users can't login if tenant missing)

2. **Sales.TenantId** → Tenants.Id
   - **RISK:** Orphan invoices
   - **IMPACT:** HIGH (financial data integrity)

3. **Payments.TenantId** → Tenants.Id
   - **RISK:** Orphan payments
   - **IMPACT:** HIGH (financial data integrity)

4. **Customers.TenantId** → Tenants.Id
   - **RISK:** Orphan customers
   - **IMPACT:** HIGH (financial data integrity)

5. **Products.TenantId** → Tenants.Id
   - **RISK:** Orphan products
   - **IMPACT:** MEDIUM (inventory integrity)

6. **Customers.RouteId** → Routes.Id
   - **RISK:** Orphan customers if route deleted
   - **IMPACT:** MEDIUM (data integrity)

7. **Branches.ManagerUserId** → Users.Id
   - **RISK:** Orphan branches if manager deleted
   - **IMPACT:** LOW (manager is optional)

**⚠️ RECOMMENDATION:**
- Add FK constraints where possible
- Use SET NULL or CASCADE based on business logic
- Test tenant deletion scenarios

---

## 5. FINANCIAL TABLES IDENTIFICATION

### Core Financial Tables:

1. **Sales** (Invoices)
   - GrandTotal, Subtotal, VatTotal, Discount
   - PaidAmount, PaymentStatus
   - **CRITICAL:** Invoice totals, payment tracking

2. **SaleItems** (Invoice Line Items)
   - Qty, UnitPrice, Discount, VatAmount, LineTotal
   - **CRITICAL:** Line item totals

3. **Payments**
   - Amount, Status (PENDING, CLEARED, RETURNED, VOID)
   - **CRITICAL:** Only CLEARED payments affect balance

4. **Customers**
   - TotalSales, TotalPayments, PendingBalance, Balance
   - **CRITICAL:** Balance tracking fields

5. **Products**
   - CostPrice, SellPrice
   - **CRITICAL:** COGS calculation

6. **Purchases**
   - Subtotal, VatTotal, TotalAmount
   - **CRITICAL:** Cost tracking

7. **PurchaseItems**
   - UnitCost, UnitCostExclVat, VatAmount, LineTotal
   - **CRITICAL:** Product cost update

8. **Expenses**
   - Amount, Date, BranchId
   - **CRITICAL:** Profit calculation (Sales - COGS - Expenses)

9. **RouteExpenses**
   - Amount, ExpenseDate, Category
   - **CRITICAL:** Route-level profit calculation

**⚠️ FINANCIAL CALCULATION FLOW:**

```
Invoice Creation:
  Sale.GrandTotal = SUM(SaleItems.LineTotal) + VAT - Discount
  Customer.TotalSales += Sale.GrandTotal
  Customer.PendingBalance = Customer.TotalSales - Customer.TotalPayments
  Product.StockQty -= (SaleItem.Qty × Product.ConversionToBase)

Payment Creation:
  Payment.Status = CLEARED (or PENDING)
  IF Payment.Status = CLEARED:
    Sale.PaidAmount += Payment.Amount
    Customer.TotalPayments += Payment.Amount
    Customer.PendingBalance = Customer.TotalSales - Customer.TotalPayments
    IF Sale.PaidAmount >= Sale.GrandTotal:
      Sale.PaymentStatus = 'Paid'
    ELSE IF Sale.PaidAmount > 0:
      Sale.PaymentStatus = 'Partial'
    ELSE:
      Sale.PaymentStatus = 'Pending'

Invoice Deletion:
  Customer.TotalSales -= Sale.GrandTotal
  Customer.PendingBalance = Customer.TotalSales - Customer.TotalPayments
  FOR EACH Payment WHERE Payment.SaleId = Sale.Id:
    IF Payment.Status = CLEARED:
      Customer.TotalPayments -= Payment.Amount
  Product.StockQty += (SaleItem.Qty × Product.ConversionToBase) [restore stock]
```

---

## 6. RELATIONSHIP DIAGRAM

```
TENANTS (Root)
  ├── USERS (TenantId)
  ├── SETTINGS (TenantId)
  ├── BRANCHES (TenantId) [FK]
  │   ├── ROUTES (BranchId, TenantId) [FK]
  │   │   ├── ROUTECUSTOMERS (RouteId) [FK]
  │   │   ├── ROUTESTAFF (RouteId) [FK]
  │   │   └── ROUTEEXPENSES (RouteId, TenantId) [FK]
  │   └── BRANCHSTAFF (BranchId) [FK]
  ├── SALES (TenantId, CustomerId, BranchId, RouteId) [FKs]
  │   ├── SALEITEMS (SaleId, ProductId) [FKs, CASCADE]
  │   ├── PAYMENTS (SaleId, CustomerId) [FKs]
  │   └── INVOICEVERSIONS (SaleId) [FK]
  ├── CUSTOMERS (TenantId, BranchId, RouteId)
  ├── PRODUCTS (TenantId)
  │   ├── PRICECHANGELOGS (ProductId) [FK]
  │   └── INVENTORYTRANSACTIONS (ProductId) [FK]
  ├── PURCHASES (TenantId) [FK: CreatedBy]
  │   └── PURCHASEITEMS (PurchaseId, ProductId) [FKs, CASCADE]
  ├── EXPENSES (TenantId, BranchId, CategoryId) [FKs]
  ├── EXPENSECATEGORIES (No TenantId - Global)
  ├── AUDITLOGS (TenantId, UserId) [FK]
  ├── ALERTS (TenantId) [FK: ResolvedBy]
  ├── ERRORLOGS (TenantId)
  └── SUBSCRIPTIONS (TenantId, PlanId) [FKs]

SYSTEM TABLES (No TenantId):
  ├── SUBSCRIPTIONPLANS
  ├── INVOICETEMPLATES
  ├── DEMOREQUESTS
  └── PAYMENTIDEMPOTENCIES
```

**Key Relationships:**

1. **Sales → SaleItems:** One-to-Many, CASCADE DELETE
2. **Sales → Payments:** One-to-Many, NO CASCADE (manual delete)
3. **Sales → Customers:** Many-to-One
4. **Sales → Branches:** Many-to-One (SET NULL on delete)
5. **Sales → Routes:** Many-to-One (SET NULL on delete)
6. **Payments → Sales:** Many-to-One (nullable - advance payments)
7. **Payments → Customers:** Many-to-One (nullable)
8. **Customers → Sales:** One-to-Many (via CustomerId)
9. **Products → SaleItems:** One-to-Many
10. **Purchases → PurchaseItems:** One-to-Many, CASCADE DELETE
11. **PurchaseItems → Products:** Many-to-One (updates Product.CostPrice)
12. **Expenses → Branches:** Many-to-One (nullable - company-level)
13. **RouteExpenses → Routes:** Many-to-One
14. **Branches → Routes:** One-to-Many
15. **Routes → RouteCustomers:** One-to-Many (junction table)
16. **Routes → RouteStaff:** One-to-Many (junction table)
17. **Branches → BranchStaff:** One-to-Many (junction table)

---

## 7. ORPHAN RECORD RISKS

### High Risk Orphans:

1. **Sales without Customer:**
   - CustomerId is nullable (cash sales OK)
   - **RISK:** If CustomerId set but customer deleted → orphan invoice
   - **MITIGATION:** FK constraint prevents (but CustomerId nullable)

2. **SaleItems without Product:**
   - ProductId has FK but NO CASCADE
   - **RISK:** If product deleted, SaleItems.ProductId invalid
   - **MITIGATION:** FK prevents product deletion if referenced

3. **Payments without Sale:**
   - SaleId is nullable (advance payments OK)
   - **RISK:** If SaleId set but sale deleted → orphan payment
   - **MITIGATION:** FK prevents (but SaleId nullable)

4. **Payments without Customer:**
   - CustomerId is nullable
   - **RISK:** If CustomerId set but customer deleted → orphan payment
   - **MITIGATION:** FK prevents (but CustomerId nullable)

5. **Customers without Tenant:**
   - TenantId nullable, no FK
   - **RISK:** Orphan customers if tenant deleted
   - **MITIGATION:** Application-level filtering

6. **Sales without Tenant:**
   - TenantId nullable, no FK
   - **RISK:** Orphan invoices if tenant deleted
   - **MITIGATION:** Application-level filtering

---

## 8. UNIQUE CONSTRAINTS

### Critical Unique Constraints:

1. **Sales:** (OwnerId, InvoiceNo) WHERE IsDeleted = false
   - **PURPOSE:** Prevent duplicate invoice numbers per tenant
   - **RISK:** After soft delete, InvoiceNo can be reused
   - **MIGRATION RISK:** Must ensure imported invoices don't conflict

2. **Sales:** ExternalReference WHERE ExternalReference IS NOT NULL
   - **PURPOSE:** Idempotency (prevent duplicate imports)
   - **GOOD:** Prevents duplicate imports

3. **Purchases:** (OwnerId, InvoiceNo)
   - **PURPOSE:** Supplier invoice unique per tenant
   - **MIGRATION RISK:** Must ensure imported purchases don't conflict

4. **Products:** (TenantId, Sku)
   - **PURPOSE:** SKU unique per tenant
   - **MIGRATION RISK:** Must ensure imported products don't conflict

5. **Users:** Email
   - **PURPOSE:** Email unique globally
   - **RISK:** Cross-tenant email conflicts

6. **Tenants:** Subdomain WHERE Subdomain IS NOT NULL
   - **PURPOSE:** Subdomain unique
   - **GOOD:** Prevents duplicate subdomains

7. **Tenants:** Domain WHERE Domain IS NOT NULL
   - **PURPOSE:** Domain unique
   - **GOOD:** Prevents duplicate domains

8. **RouteCustomers:** (RouteId, CustomerId)
   - **PURPOSE:** Prevent duplicate route-customer assignments
   - **GOOD:** Prevents duplicates

9. **RouteStaff:** (RouteId, UserId)
   - **PURPOSE:** Prevent duplicate route-staff assignments
   - **GOOD:** Prevents duplicates

10. **BranchStaff:** (BranchId, UserId)
    - **PURPOSE:** Prevent duplicate branch-staff assignments
    - **GOOD:** Prevents duplicates

11. **ExpenseCategories:** Name
    - **PURPOSE:** Category name unique globally
    - **RISK:** Cross-tenant category conflicts

12. **SaleReturns:** ReturnNo
    - **PURPOSE:** Return number unique globally
    - **RISK:** Cross-tenant return number conflicts

13. **PurchaseReturns:** ReturnNo
    - **PURPOSE:** Return number unique globally
    - **RISK:** Cross-tenant return number conflicts

---

## 9. INDEXES ANALYSIS

### Critical Indexes:

1. **Sales:**
   - INDEX: CreatedAt (for date range queries)
   - INDEX: IsLocked (for edit window queries)
   - INDEX: BranchId (for branch reports)
   - INDEX: RouteId (for route reports)
   - **MISSING:** INDEX on TenantId (critical for tenant filtering)
   - **MISSING:** INDEX on InvoiceDate (for date range queries)
   - **MISSING:** INDEX on PaymentStatus (for payment reports)

2. **Payments:**
   - **MISSING:** INDEX on TenantId (critical for tenant filtering)
   - **MISSING:** INDEX on PaymentDate (for date range queries)
   - **MISSING:** INDEX on Status (for payment status queries)

3. **Customers:**
   - **MISSING:** INDEX on TenantId (critical for tenant filtering)
   - **MISSING:** INDEX on BranchId (for branch reports)
   - **MISSING:** INDEX on RouteId (for route reports)

4. **Products:**
   - INDEX: (TenantId, Sku) UNIQUE
   - **MISSING:** INDEX on TenantId alone (for tenant filtering)

5. **Expenses:**
   - **MISSING:** INDEX on TenantId (critical for tenant filtering)
   - **MISSING:** INDEX on Date (for date range queries)
   - **MISSING:** INDEX on BranchId (for branch reports)

6. **RouteExpenses:**
   - INDEX: RouteId, TenantId, ExpenseDate
   - **GOOD:** Well indexed

7. **Branches:**
   - INDEX: TenantId
   - **GOOD:** Well indexed

8. **Routes:**
   - INDEX: BranchId, TenantId
   - **GOOD:** Well indexed

**⚠️ CRITICAL MISSING INDEXES:**

- **TenantId indexes missing on:** Sales, Payments, Customers, Products, Expenses, Purchases, InventoryTransactions, AuditLogs, InvoiceVersions, PriceChangeLogs, SaleReturns, PurchaseReturns
- **IMPACT:** Slow tenant filtering queries
- **RECOMMENDATION:** Add TenantId indexes on all tenant-scoped tables

---

## 10. COMPUTED COLUMNS & TRIGGERS

**No database-level computed columns or triggers found.**

All calculations are done at **application level**:
- Customer.PendingBalance = TotalSales - TotalPayments (computed in code)
- Sale.PaymentStatus = computed from PaidAmount vs GrandTotal (computed in code)
- Sale.GrandTotal = SUM(SaleItems.LineTotal) + VAT - Discount (computed in code)

**⚠️ RISK:**
- Computed fields may become stale if updates fail
- No database-level validation of calculations
- **RECOMMENDATION:** Consider database triggers or computed columns for critical calculations

---

## 11. SUMMARY OF CRITICAL RISKS

### 🔴 CRITICAL RISKS:

1. **Tenant Isolation:**
   - Many tables have nullable TenantId with NO FK constraints
   - Queries MUST filter by TenantId at application level
   - **RISK:** Data leakage if queries miss TenantId filter

2. **Balance Tracking:**
   - Customer.TotalSales, TotalPayments, PendingBalance updated in code
   - **RISK:** If update fails, balance becomes wrong
   - **RISK:** Must be in same transaction as invoice/payment operations

3. **Invoice Number Uniqueness:**
   - Unique constraint: (OwnerId, InvoiceNo) WHERE IsDeleted = false
   - **RISK:** After soft delete, InvoiceNo can be reused
   - **MIGRATION RISK:** Imported invoices may conflict

4. **Payment Status:**
   - Only CLEARED payments affect balance
   - **RISK:** If Status changes, balance must be recalculated
   - **RISK:** PaymentStatus on Sale may be stale

5. **Stock Management:**
   - Stock decremented on Sale creation (if IsFinalized = true)
   - Stock restored on Sale deletion
   - **RISK:** If stock update fails, StockQty becomes wrong
   - **RISK:** Must be in same transaction

6. **Missing Indexes:**
   - TenantId indexes missing on many tables
   - **RISK:** Slow tenant filtering queries
   - **IMPACT:** Performance degradation with many tenants

### 🟡 MEDIUM RISKS:

1. **Dual Tenant Fields (OwnerId + TenantId):**
   - Legacy OwnerId still present
   - **RISK:** Inconsistent queries (some use OwnerId, some TenantId)

2. **Nullable Foreign Keys:**
   - Many FKs are nullable (CustomerId, BranchId, RouteId)
   - **RISK:** Orphan records if parent deleted

3. **No Cascade Deletes:**
   - Payments NOT cascade deleted when Sale deleted
   - **RISK:** Manual payment deletion required

### 🟢 LOW RISKS:

1. **Junction Tables without TenantId:**
   - RouteCustomers, RouteStaff, BranchStaff derive TenantId from parent
   - **IMPACT:** Low (parent has TenantId)

2. **Global Tables:**
   - ExpenseCategories, InvoiceTemplates are global
   - **IMPACT:** Low (intended design)

---

## 12. MIGRATION READINESS ASSESSMENT

### ✅ READY FOR MIGRATION:

1. **Invoice Schema:**
   - InvoiceNo has unique constraint (per tenant)
   - InvoiceDate can be in past/future
   - All required fields have defaults

2. **Payment Schema:**
   - PaymentDate can be in past/future
   - Status field exists (PENDING, CLEARED, RETURNED, VOID)
   - Partial payments supported (Sale.PaidAmount tracks)

3. **Customer Balance:**
   - Balance fields exist (TotalSales, TotalPayments, PendingBalance)
   - **RISK:** Must recalculate after import (don't import balance directly)

### ⚠️ MIGRATION RISKS:

1. **Invoice Number Conflicts:**
   - Unique constraint: (OwnerId, InvoiceNo) WHERE IsDeleted = false
   - **RISK:** Imported invoices may conflict with existing InvoiceNo
   - **SOLUTION:** Check existing InvoiceNo before import, or use ExternalReference

2. **Balance Calculation:**
   - Customer balance is computed (TotalSales - TotalPayments)
   - **RISK:** If importing invoices/payments, balance must be recalculated
   - **SOLUTION:** Import invoices/payments, then recalculate Customer.TotalSales and TotalPayments

3. **Stock Updates:**
   - Stock decremented on Sale creation
   - **RISK:** If importing historical invoices, stock may become negative
   - **SOLUTION:** Set IsFinalized = false for historical invoices, or adjust stock manually

4. **Transaction Wrapping:**
   - Import operations should be wrapped in transactions
   - **RISK:** Partial import causes data corruption
   - **SOLUTION:** Use database transactions for import

---

## 13. RECOMMENDATIONS

### Immediate Actions:

1. **Add TenantId Indexes:**
   - Add INDEX on TenantId for: Sales, Payments, Customers, Products, Expenses, Purchases, InventoryTransactions, AuditLogs, InvoiceVersions

2. **Add FK Constraints:**
   - Add FK: Users.TenantId → Tenants.Id (SET NULL for SystemAdmin)
   - Add FK: Sales.TenantId → Tenants.Id
   - Add FK: Payments.TenantId → Tenants.Id
   - Add FK: Customers.TenantId → Tenants.Id

3. **Balance Calculation:**
   - Ensure all invoice/payment operations update Customer balance in same transaction
   - Add database trigger or computed column for PendingBalance

4. **Migration Preparation:**
   - Create import API that validates TenantId
   - Create import API that wraps operations in transactions
   - Create import API that recalculates Customer balance after import

---

**END OF PROMPT 1 ANALYSIS**

**Next Steps:**
- Review this analysis
- Address critical risks
- Proceed to PROMPT 2 (Multi-Tenant Security Audit)
