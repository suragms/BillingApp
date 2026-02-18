# Backup, Restore & Import Strategy

## Production (e.g. Render): Ephemeral storage and Postgres

- **Ephemeral disk:** Backups saved under the app’s `backups` folder (e.g. `Directory.GetCurrentDirectory()/backups`) are **lost on restart or redeploy**. On Render (and similar hosts), the filesystem is ephemeral; do not rely on “Server” backups for retention.
- **What to do:** Use **Download** (create backup with “Download to browser” or download an existing backup) to keep a copy locally, or use **external storage** (e.g. S3/R2) so backups are not stored only on the app server. See also: “Move backup storage to S3/R2” in the production TODO.
- **PostgreSQL backup (pg_dump):** For a full, consistent Postgres dump the app uses `pg_dump` when available. On Render’s web service, the app container usually does **not** have `pg_dump` installed; the app then falls back to an EF Core–based export (slower but works). To use `pg_dump`:
  - **Option A:** Set `Backup:PostgresPgDumpPath` in appsettings (or environment) to the full path to `pg_dump` if you install the Postgres client tools in the same environment.
  - **Option B:** Use **Render Dashboard → Postgres → Backups** for native, scheduled Postgres backups (recommended for production).
  - **Option C:** Run `pg_dump` from a one-off shell or worker that has the Postgres client (e.g. `pg_dump $DATABASE_URL -Fc -f backup.dump`).

### S3/R2 backup storage (production)

When **BackupSettings:S3** is configured, backups are uploaded to S3 or an S3-compatible store (e.g. Cloudflare R2). List, download, delete, and restore all work against both local and S3 backups.

**Config (appsettings or environment):**

| Key | Description |
|-----|-------------|
| `BackupSettings:S3:Enabled` | `true` to upload and list/delete from S3 |
| `BackupSettings:S3:Bucket` | Bucket name |
| `BackupSettings:S3:Prefix` | Optional key prefix (e.g. `hexabill/backups`) |
| `BackupSettings:S3:Region` | AWS region (default `us-east-1`); ignored when ServiceUrl is set |
| `BackupSettings:S3:AwsAccessKeyId` | Access key (required for R2) |
| `BackupSettings:S3:AwsSecretAccessKey` | Secret key (required for R2) |
| `BackupSettings:S3:ServiceUrl` | **R2:** set to `https://<account_id>.r2.cloudflarestorage.com` for Cloudflare R2 |
| `BackupSettings:S3:DeleteLocalAfterUpload` | If `true` (default), delete local zip after successful S3 upload to avoid ephemeral disk use |

**R2 example:** Set `ServiceUrl` to your R2 endpoint and use R2 API token as AccessKeyId/SecretAccessKey. List, download, restore, and delete will use S3 when the file is not found locally.

---

## Why backup/restore and CSV import fail (not an AI issue)

- **Schema mismatch:** Backup from SQLite or old schema vs restore into PostgreSQL / new schema.
- **Missing columns:** e.g. `AuditLogs` missing `EntityId` (fixed by DatabaseFixer running first for SQLite).
- **TenantId / OwnerId:** Multi-tenant systems must map old OwnerId to TenantId and validate relationships.
- **Import:** Column name mismatch, encoding, required fields, duplicate keys, date/numeric format, missing FKs.

## What was fixed (SQLite)

- **AuditLogs.EntityId:** DatabaseFixer now adds **AuditLogs** columns **first**, so Purchase and other services no longer hit “table AuditLogs has no column named EntityId”. Only “duplicate column” is ignored; other errors are logged.
- **After pull:** Restart the backend so the fixer runs again.

## Professional approach (for later)

### Backup / Restore

1. **Versioning:** In backup payload store `schemaVersion`, `databaseType`, `createdAt`.
2. **On restore:** Compare with current schema; if mismatch, run a **transformer** (map fields, apply TenantId, validate relationships) instead of raw dump.
3. Never do direct DB dump restore in a multi-tenant SaaS; always parse → validate → insert.

### CSV / Excel import (3 layers)

1. **Schema detection:** Map user columns to internal fields (e.g. “Customer Name” / “Cust Name” → customer name). Optional: use LLM to suggest mapping.
2. **Validation engine:** Required fields, types, duplicate SKU/invoice, valid dates, no negative values. Return a structured error report (row + message).
3. **Dry run:** Preview + list of errors → user confirms before applying.

### AI usage (optional, for value-add only)

- Use **Groq / Gemini / HuggingFace** for: smart CSV column mapping suggestions, business insight text, alerts.
- Do **not** use AI for: backup restore, financial calculations, or core validation (keep deterministic).

## Super Admin and production import

- Super Admin can have a subpage for: usage, cost, and control (e.g. feature flags, API key placeholders).
- Production import from CSV (and other formats) should: detect/adapt to column keywords, validate, verify, then process with a loader. Two “agents” (product import vs other data import) can share the same validation/verification pipeline; Super Admin can later control which keys/models are used and update the app when variables change.
