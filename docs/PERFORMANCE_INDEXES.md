# Performance indexes (AddPerformanceIndexes.sql)

## Deployment

- **File:** `backend/HexaBill.Api/Migrations/AddPerformanceIndexes.sql`
- **Publish:** The project copies `Migrations\*.sql` to the output and publish directory (see `HexaBill.Api.csproj`). So `AddPerformanceIndexes.sql` is deployed with the app.
- **Startup:** On startup the API looks for this file under several paths (output directory, current directory, parent folders) and, if found, applies the index statements. When found, the log shows: **"Index SQL file found at: &lt;path&gt;"**.

## If the file is not found

If you see in logs: *"Index SQL file not found. Searched paths: ..."* (e.g. custom deploy or different working directory), create the indexes manually.

### PostgreSQL (recommended for production: CONCURRENTLY)

Run these **one at a time** (each in its own transaction). `CONCURRENTLY` avoids long table locks:

```sql
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_sales_tenant_created ON "Sales"("TenantId", "CreatedAt" DESC);
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_sales_tenant_invoicedate ON "Sales"("TenantId", "InvoiceDate" DESC);
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_sales_tenant_deleted ON "Sales"("TenantId", "IsDeleted");
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_customers_tenant_name ON "Customers"("TenantId", "Name");
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_products_tenant_sku ON "Products"("TenantId", "Sku");
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_products_tenant_name ON "Products"("TenantId", "NameEn");
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_payments_tenant_created ON "Payments"("TenantId", "CreatedAt" DESC);
CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_payments_customer ON "Payments"("CustomerId", "CreatedAt" DESC);
```

Key columns covered: **Sales.TenantId**, **Payments.CustomerId**, **Products.TenantId**, **Customers.TenantId**, plus tenant+date/name/SKU for list and filter queries.

### Same indexes without CONCURRENTLY

If you cannot use `CONCURRENTLY` (e.g. non-Postgres or one-off script), use the contents of `Migrations/AddPerformanceIndexes.sql` as-is (standard `CREATE INDEX IF NOT EXISTS`).
