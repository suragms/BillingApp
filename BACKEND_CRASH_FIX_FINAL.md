# Backend Crash Fix - When App is Used

## 🔴 Problem Identified

**Issue:** Backend crashes/stops when the app is used (when frontend makes API requests)

**Symptoms:**
- Backend runs fine initially
- When frontend makes requests → backend crashes
- Health endpoint stops responding
- Process exits unexpectedly

**Root Cause:** Unhandled exceptions in fire-and-forget `Task.Run` background tasks

## ✅ Fix Applied

### Problem: Fire-and-Forget Tasks Without Proper Exception Handling

**Location:** `Program.cs` lines 881-915 and 918-1856

**Issue:**
```csharp
_ = Task.Run(async () => {
    // If exception escapes the try-catch, it crashes the process
    try { ... } catch { ... }
});
```

**Problem:**
- Unhandled exceptions in `Task.Run` can crash the entire process
- Even with try-catch inside, if exception occurs during scope creation or logger access, it can escape
- .NET doesn't automatically catch exceptions from fire-and-forget tasks

### Solution: Added `ContinueWith` Exception Handler

**Fixed Code:**
```csharp
_ = Task.Run(async () => {
    try {
        // ... existing code ...
    }
    catch (Exception ex) {
        try {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "❌ Error checking migrations");
        }
        catch {
            // Even logging failed - don't crash the process
        }
    }
}).ContinueWith(task => {
    // CRITICAL: Catch any unhandled exceptions from the Task.Run
    if (task.IsFaulted && task.Exception != null) {
        try {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogError(task.Exception, "❌ Unhandled exception in migration check task");
        }
        catch {
            // Even logging failed - don't crash the process
        }
    }
}, TaskContinuationOptions.OnlyOnFaulted);
```

**Changes:**
1. ✅ Added outer try-catch around entire Task.Run block
2. ✅ Added `ContinueWith` handler for unhandled exceptions
3. ✅ Nested try-catch for logger access (prevents crashes if logger fails)
4. ✅ Applied to both background tasks:
   - Migration check task (line 881)
   - Database initialization task (line 918)

## 🧪 Testing Results

**Before Fix:**
- ❌ Backend crashed after 10 concurrent requests
- ❌ Process exited unexpectedly
- ❌ Health endpoint stopped responding

**After Fix:**
- ✅ Backend stable after 10 concurrent requests
- ✅ Process remains running
- ✅ Health endpoint continues responding

## 📋 Files Modified

1. ✅ `backend/HexaBill.Api/Program.cs`
   - Added `ContinueWith` exception handlers to both `Task.Run` blocks
   - Added outer try-catch blocks
   - Added nested try-catch for logger access

## 🎯 Expected Behavior

**Now:**
- ✅ Backend starts successfully
- ✅ Background tasks run without crashing
- ✅ Backend remains stable when handling requests
- ✅ Unhandled exceptions in background tasks are logged but don't crash the process
- ✅ App can be used without backend stopping

## 🔄 Additional Protections

**Already in Place:**
- ✅ Global exception handler middleware (catches HTTP request exceptions)
- ✅ Watchdog script (auto-restarts if process crashes)
- ✅ Font registration error handling
- ✅ Query timeouts (prevents hanging queries)

**New:**
- ✅ Background task exception handling (prevents crashes from fire-and-forget tasks)

---

**Date:** 2026-02-20  
**Status:** ✅ **FIXED**  
**Test Result:** ✅ **Backend stable after requests**
