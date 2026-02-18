# PostgreSQL Migration Compatibility Audit

**Date:** 2026-02-18  
**Task:** PROD-15 - Audit all migrations for PostgreSQL compatibility

---

## Summary

Audited all migration files for SQLite-specific syntax that could cause failures on PostgreSQL. EF Core generally handles type translation automatically, but some migrations need provider-specific logic.

---

## Migration Files Audited

### ✅ **GOOD - Provider-Specific Migrations**

1. **InitialPostgreSQL.cs** ✅
   - Only runs for PostgreSQL (line 15: `if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL") return;`)
   - Uses PostgreSQL-specific types (`integer`, `character varying`, `timestamp with time zone`)
   - **Status:** Safe for PostgreSQL

2. **FixBooleanColumnsType.cs** ✅
   - Only runs for PostgreSQL (line 13: checks for Npgsql)
   - Uses PostgreSQL-specific syntax (`DO $$ BEGIN ... END $$`)
   - **Status:** Safe for PostgreSQL

3. **EnterpriseBranchRoutePlan.cs** ✅
   - Has provider-specific logic (lines 14-51 for PostgreSQL, 52-135 for SQLite)
   - PostgreSQL branch uses proper PostgreSQL syntax
   - **Status:** Safe for PostgreSQL

4. **AddUserSessionVersion.cs** ✅
   - Has provider-specific logic (lines 14-21 for PostgreSQL, 22-50 for SQLite)
   - PostgreSQL branch uses proper PostgreSQL syntax
   - **Status:** Safe for PostgreSQL

5. **AddProductsFeatures.cs** ⚠️ **NEEDS FIX**
   - Has provider-specific logic for AddColumn operations (lines 14-51)
   - **BUT:** `CreateTable` for `ProductCategories` (lines 53-70) uses SQLite types without provider check
   - Uses `Sqlite:Autoincrement` annotation (line 58)
   - **Issue:** Table creation will use SQLite types even on PostgreSQL
   - **Status:** Needs provider check for table creation

---

### ⚠️ **ISSUES - Migrations Without Provider Checks**

1. **AddHeldInvoiceTable.cs** ⚠️
   - Creates table with SQLite types (`INTEGER`, `TEXT`)
   - Uses `Sqlite:Autoincrement` annotation (line 19)
   - No provider check
   - **Risk:** EF Core should translate types automatically, but explicit PostgreSQL types would be safer
   - **Status:** Low risk (EF Core handles translation), but should add provider check

2. **AddMissingTables.cs** ⚠️
   - Creates `BranchStaff` table with SQLite types
   - Uses `Sqlite:Autoincrement` annotation (line 25)
   - Adds column to `Expenses` with SQLite type (`INTEGER`)
   - No provider check
   - **Risk:** EF Core should translate types automatically
   - **Status:** Low risk (EF Core handles translation), but should add provider check

3. **AddExpenseRouteId.cs** ⚠️
   - Creates multiple tables (`CustomerVisits`, `RecurringExpenses`, `UserSessions`) with SQLite types
   - Uses `Sqlite:Autoincrement` annotations
   - Adds columns with SQLite types (`TEXT`, `INTEGER`)
   - No provider check
   - **Risk:** EF Core should translate types automatically
   - **Status:** Low risk (EF Core handles translation), but should add provider check

4. **AddBranchAndRoute.cs** ⚠️
   - Creates many tables with SQLite types
   - Uses `Sqlite:Autoincrement` annotations throughout
   - No provider check
   - **Note:** This migration appears to be SQLite-only (comment on line 14 mentions "Sequence already created by InitialPostgreSQL")
   - **Risk:** EF Core should translate types automatically, but this migration might conflict with `InitialPostgreSQL`
   - **Status:** Medium risk - should verify this migration doesn't run on PostgreSQL (or add provider check)

---

## EF Core Type Translation Behavior

**Important:** EF Core automatically translates SQLite types to PostgreSQL types:
- `INTEGER` → `integer` (PostgreSQL)
- `TEXT` → `text` or `character varying` (PostgreSQL)
- `REAL` → `double precision` (PostgreSQL)
- `BLOB` → `bytea` (PostgreSQL)

**However:**
- `Sqlite:Autoincrement` annotations are ignored by PostgreSQL provider (PostgreSQL uses sequences)
- Explicit PostgreSQL types are still preferred for clarity and to avoid potential edge cases

---

## Recommendations

### High Priority
1. **AddProductsFeatures.cs** - Add provider check for `ProductCategories` table creation
2. **AddBranchAndRoute.cs** - Verify this migration doesn't conflict with `InitialPostgreSQL` on PostgreSQL

### Medium Priority
3. **AddHeldInvoiceTable.cs** - Add provider check for table creation
4. **AddMissingTables.cs** - Add provider check for table creation
5. **AddExpenseRouteId.cs** - Add provider check for table creation

### Low Priority
- Other migrations use EF Core's automatic type translation, which should work but explicit PostgreSQL types would be safer

---

## Testing Recommendations

1. **Test migrations on PostgreSQL:**
   - Create fresh PostgreSQL database
   - Run all migrations in order
   - Verify no SQLite-specific errors occur

2. **Verify table schemas:**
   - Check that all tables are created with correct PostgreSQL types
   - Verify sequences are used instead of AUTOINCREMENT
   - Check that boolean columns are `boolean` not `integer`

3. **Test migration rollback:**
   - Test `Down()` methods work correctly on PostgreSQL

---

## Notes

- EF Core migrations are generally database-agnostic when using fluent API
- Raw SQL in migrations must be provider-specific (already handled in most migrations)
- `AppDbContextModelSnapshot.cs` uses SQLite types, but this is EF Core's internal model representation and doesn't affect actual database schema
