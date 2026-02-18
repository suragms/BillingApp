# Connection pooling and PgBouncer (PostgreSQL at scale)

For production with many tenants (e.g. 2000+), database connection limits can be hit. This doc covers max connections and how to use PgBouncer.

## Max connections

- **PostgreSQL default:** Typically `max_connections = 100` (varies by provider).
- **Render Postgres:** Plan-dependent. Free/small plans have a low limit (e.g. 25–97). Check the plan details in the Render dashboard or [Render Postgres docs](https://render.com/docs/databases).
- **Single API process:** EF Core + Npgsql pool connections per process (default pool size is 100). One web instance might use tens of connections under load. Multiple instances (horizontal scale) multiply that.

If you see errors like **"too many connections"** or **"remaining connection slots are reserved"**, you need to reduce connection usage or add pooling.

## Recommendations

### 1. Use a provider with connection pooling (recommended for 2000 tenants)

- **Render:** Use the **PgBouncer** add-on for your Postgres database if available, or choose a plan that includes pooling. Connect the API to the **PgBouncer** endpoint (not the direct Postgres internal URL). PgBouncer keeps a small pool of real connections to Postgres and multiplexes many client connections.
- **Other providers:** Use PgBouncer (self-hosted or managed), or a provider that offers connection pooling (e.g. Supabase, Neon, AWS RDS Proxy).

### 2. Connect to PgBouncer

When using PgBouncer (e.g. Render PgBouncer add-on):

- Set `ConnectionStrings__DefaultConnection` (or `DATABASE_URL`) to the **pooler URL** (host/port for PgBouncer), not the direct Postgres URL.
- **Transaction mode** is the usual choice for EF Core: each request gets a transaction-scoped connection. Session mode is not required for typical web workloads.
- No code changes are needed in the app; only the connection string host/port (and optionally port) change to point to the pooler.

### 3. Limit connections per API instance (optional)

If you cannot use PgBouncer yet, you can cap Npgsql pool size so each instance uses fewer connections:

- In the connection string, set **Max Pool Size**, e.g.  
  `Host=...;Database=...;Username=...;Password=...;Maximum Pool Size=20`  
- Tune the value so: `(instances × Max Pool Size) + background jobs + admin` stays below the Postgres `max_connections` limit.

### 4. Summary table

| Scenario | Action |
|---------|--------|
| Small / single tenant | Default connection string to Postgres is usually fine. |
| Many tenants (e.g. 2000), single region | Use PgBouncer (Render add-on or external). Point connection string to pooler. |
| Multiple API instances | Use PgBouncer and/or set `Maximum Pool Size` per instance so total connections &lt; `max_connections`. |
| "Too many connections" errors | Add PgBouncer or lower per-instance pool size; verify `max_connections` for your plan. |

## Where the connection string is set

- **Environment:** `ConnectionStrings__DefaultConnection` (e.g. on Render: Environment tab for the web service).
- **Code:** `Program.cs` reads this and passes it to `UseNpgsql(connectionString)`. See `docs/BACKGROUND_JOBS.md` for background services that also use the same DbContext/connection pool.

## References

- [Render Databases](https://render.com/docs/databases) (plan limits and optional PgBouncer).
- [Npgsql connection string parameters](https://www.npgsql.org/doc/connection-string-parameters.html) (e.g. `Maximum Pool Size`).
- [PgBouncer](https://www.pgbouncer.org/) (transaction vs session mode).
