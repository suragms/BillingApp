# Background jobs (in-process)

The API runs three hosted background services in the same process as the web app. For production at scale, consider moving long-running work (especially backup) to a separate worker.

## Services

| Service | Purpose | Frequency | Notes |
|--------|---------|-----------|--------|
| **DailyBackupScheduler** | Full DB backup + cleanup | Once per day (default 21:00) or weekly; configurable via Settings | Long-running; only one run at a time (concurrency guard). Default is off-peak. |
| **AlertCheckBackgroundService** | Check and create alerts (e.g. low stock) | Every 6 hours | Lightweight. |
| **TrialExpiryCheckJob** | Trial expiry notifications; subscription status; overdue payment alerts | Every 1 hour (after 2 min startup delay) | Lightweight. |

## Concurrency and scheduling

- **Backup**: A single `SemaphoreSlim` ensures only one backup runs at a time. If the next scheduled time arrives while a backup is still running, that run is skipped and the next check is in 1 hour. Schedule is read from Settings (`BACKUP_SCHEDULE_TIME`, default 21:00; `BACKUP_SCHEDULE_FREQUENCY`: daily/weekly).
- **Alerts and trial**: No overlap guard; each runs on its own interval and completes quickly.

## Scaling: separate worker for backup

Running backup in the same process as the API can use CPU, memory, and DB connections during the run. For many tenants or large DBs:

1. **Render**  
   - Add a **worker** or **cron job** that runs a small console app or script which calls your backup endpoint (e.g. Super Admin backup trigger) or runs `pg_dump` and uploads to S3.  
   - Disable the in-app schedule (Settings: `BACKUP_SCHEDULE_ENABLED` = false) so `DailyBackupScheduler` does not run backup.

2. **Off-peak**  
   - Keep the in-app scheduler but set `BACKUP_SCHEDULE_TIME` to a low-traffic hour (e.g. 02:00 or 21:00).

3. **Short chunks**  
   - For very large backups, the backup service could be extended to support chunked/tenant-by-tenant runs; currently it runs one full backup per execution.

## Registration

All three are registered in `Program.cs`:

```csharp
builder.Services.AddHostedService<DailyBackupScheduler>();
builder.Services.AddHostedService<AlertCheckBackgroundService>();
builder.Services.AddHostedService<TrialExpiryCheckJob>();
```

To disable automatic backup and rely on an external worker/cron, set `BACKUP_SCHEDULE_ENABLED` to `false` in Settings (OwnerId = 0).
