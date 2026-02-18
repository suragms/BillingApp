# Products Migration Guide

## Overview
This migration adds the following features to the Products system:
1. **Soft Delete** - `IsActive` field for deactivating products
2. **Barcode Support** - `Barcode` field for POS scanning
3. **Product Categories** - New `ProductCategories` table and `CategoryId` foreign key
4. **Image Upload** - `ImageUrl` field for product images

## Migration Files

### 1. `COMPLETE_PRODUCTS_MIGRATION.sql` ⭐ **MAIN MIGRATION**
   - **Run this file** to apply all changes
   - Idempotent (safe to run multiple times)
   - Production-ready PostgreSQL script
   - Includes all 4 feature additions

### 2. `VERIFY_PRODUCTS_MIGRATION.sql`
   - Verification script to check migration status
   - Run after migration to confirm all changes applied
   - Shows summary of columns, indexes, and constraints

### 3. `ROLLBACK_PRODUCTS_MIGRATION.sql`
   - **WARNING**: Removes all migration changes
   - Use only if you need to rollback
   - Does NOT drop ProductCategories table by default (to prevent data loss)

## Individual Migration Scripts (Optional)
If you prefer to run migrations separately:
- `ADD_PRODUCT_ISACTIVE_FIELD.sql` - Soft delete only
- `ADD_PRODUCT_BARCODE_FIELD.sql` - Barcode field only
- `ADD_PRODUCT_CATEGORIES.sql` - Categories table and FK only
- `ADD_PRODUCT_IMAGE_URL.sql` - Image URL field only

## How to Run

### Production PostgreSQL
```bash
# Connect to your PostgreSQL database
psql -h your-host -U your-user -d your-database

# Run the complete migration
\i backend/HexaBill.Api/Scripts/COMPLETE_PRODUCTS_MIGRATION.sql

# Verify migration
\i backend/HexaBill.Api/Scripts/VERIFY_PRODUCTS_MIGRATION.sql
```

### Using pgAdmin or DBeaver
1. Open SQL Editor
2. Copy contents of `COMPLETE_PRODUCTS_MIGRATION.sql`
3. Execute
4. Run `VERIFY_PRODUCTS_MIGRATION.sql` to confirm

### Using Entity Framework Migrations (Alternative)
If you prefer EF Core migrations:
```bash
dotnet ef migrations add AddProductsFeatures --project backend/HexaBill.Api
dotnet ef database update --project backend/HexaBill.Api
```

## What Gets Added

### Products Table Changes
- `IsActive` (boolean, NOT NULL, DEFAULT true) - Soft delete flag
- `Barcode` (varchar(100), NULL) - Barcode for scanning
- `CategoryId` (integer, NULL) - Foreign key to ProductCategories
- `ImageUrl` (varchar(500), NULL) - Product image path

### New Table: ProductCategories
- `Id` (integer, primary key)
- `TenantId` (integer, NULL) - Multi-tenant support
- `Name` (varchar(100), NOT NULL) - Category name
- `Description` (varchar(200), NULL) - Optional description
- `ColorCode` (varchar(7), DEFAULT '#3B82F6') - UI color
- `IsActive` (boolean, DEFAULT true) - Soft delete
- `CreatedAt` (timestamp) - Creation timestamp
- `UpdatedAt` (timestamp) - Update timestamp

### Indexes Created
- `IX_Products_IsActive` - Filter active products
- `IX_Products_Barcode` - Search by barcode
- `IX_Products_TenantId_Barcode` - Unique barcode per tenant
- `IX_Products_CategoryId` - Filter by category
- `IX_Products_ImageUrl` - Partial index (non-null only)
- `IX_ProductCategories_TenantId_Name` - Unique category name per tenant

### Constraints
- Foreign key: `FK_Products_ProductCategories_CategoryId`
  - ON DELETE SET NULL (products keep category reference even if category deleted)

## Verification Checklist

After running migration, verify:
- [ ] Products table has IsActive column
- [ ] Products table has Barcode column
- [ ] Products table has CategoryId column
- [ ] Products table has ImageUrl column
- [ ] ProductCategories table exists
- [ ] All indexes are created
- [ ] Foreign key constraint exists
- [ ] Existing products have IsActive = true
- [ ] No errors in migration log

## Troubleshooting

### Error: "column already exists"
- Migration is idempotent, this is normal
- Script will skip existing columns

### Error: "relation ProductCategories already exists"
- Table already exists, migration will continue
- Check if you need to update existing table structure

### Error: "constraint already exists"
- Foreign key already exists, safe to ignore
- Migration will skip creating duplicate constraints

### Missing columns after migration
- Run `VERIFY_PRODUCTS_MIGRATION.sql` to check status
- Check PostgreSQL logs for errors
- Ensure you have proper permissions

## Rollback

**⚠️ WARNING**: Rolling back will:
- Remove all product categories
- Remove barcode data
- Remove image URLs
- Reset IsActive flags

To rollback:
```sql
-- Review rollback script first
-- Then run if needed
\i backend/HexaBill.Api/Scripts/ROLLBACK_PRODUCTS_MIGRATION.sql
```

## Post-Migration Steps

1. **Update Application Code**
   - Ensure backend code uses new fields
   - Update frontend to display new features
   - Test product creation/editing

2. **Data Migration (if needed)**
   - Migrate existing product data
   - Create default categories if needed
   - Update product images if migrating from old system

3. **Testing**
   - Test product deactivation/reactivation
   - Test barcode scanning
   - Test category assignment
   - Test image uploads

## Support

If you encounter issues:
1. Check PostgreSQL logs
2. Run verification script
3. Review error messages
4. Check database permissions
5. Ensure PostgreSQL version >= 12
