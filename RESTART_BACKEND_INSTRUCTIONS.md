# 🔄 Backend Restart Instructions

## Current Status
- ✅ **Fixes Applied:** PageAccess column fix + CORS headers on errors
- ⚠️ **Backend Still Running:** Process 6004 needs to be stopped
- ❌ **Fixes Not Active:** Backend must restart to apply fixes

---

## How to Restart Backend

### Option 1: Stop via Terminal (Recommended)
1. Find the terminal window running the backend
2. Press `Ctrl + C` to stop it
3. Run: `dotnet run`

### Option 2: Stop via Task Manager
1. Open Task Manager (Ctrl + Shift + Esc)
2. Find process `HexaBill.Api` or `dotnet` (PID 6004)
3. Right-click → End Task
4. Restart: `cd backend\HexaBill.Api` then `dotnet run`

### Option 3: PowerShell Command
```powershell
# Stop the process
Stop-Process -Id 6004 -Force

# Wait a moment
Start-Sleep -Seconds 2

# Restart backend
cd C:\Users\anand\Downloads\HexaBil-App2\HexaBil-App2\backend\HexaBill.Api
dotnet run
```

---

## What to Look For After Restart

### ✅ Success Indicators:
1. **PageAccess Column Fix:**
   ```
   ✅ Successfully added PageAccess column
   ```
   OR
   ```
   ✅ PageAccess column exists
   ```

2. **CORS Configuration:**
   ```
   ✅ CORS: Configuration complete. Environment: Development
   CORS: Allowed origin: http://localhost:5173
   ```

3. **Server Started:**
   ```
   Server configured to listen on http://localhost:5000
   ```

---

## After Restart - Test These

1. **Health Check:**
   - Open: `http://localhost:5000/api/health`
   - Should return: `{"status":"ok","timestamp":"..."}`
   - No CORS errors in browser console

2. **Login:**
   - Go to: `http://localhost:5173/login`
   - Try to login
   - Should work without 500 error
   - No CORS errors

---

## If Still Getting Errors

### Check Backend Logs:
Look for these error messages:
- `❌ Failed to add PageAccess column` → Database permission issue
- `Database connection string is required` → Check appsettings.json
- `SQLite Error` → Database file might be locked

### Quick Database Fix (if PageAccess still missing):
```powershell
cd backend\HexaBill.Api
# If sqlite3 is available:
sqlite3 hexabill.db "ALTER TABLE Users ADD COLUMN PageAccess TEXT NULL;"
```

---

## Summary

**Current Issue:** Backend needs restart  
**Fixes Ready:** ✅ PageAccess + CORS  
**Action:** Restart backend now  
**Expected Result:** Login works, no CORS errors
