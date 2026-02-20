# Comprehensive Fix Plan: Root Cause Resolution

## Executive Summary

**Primary Issue**: Backend server at `localhost:5000` is not running, causing all `ERR_CONNECTION_REFUSED` errors.

**Secondary Issue**: Logo URLs may need proper base URL prefixing (already handled in most places).

**Status**: `toUpperCase()` errors are already fixed - all instances have proper null checks.

---

## Issue 1: Backend Server Not Running

### Diagnosis
- `netstat` shows no process on port 5000
- Browser test to `http://localhost:5000/api/health` fails
- All API calls return `ERR_CONNECTION_REFUSED`

### Root Cause
Backend needs to be started manually with `dotnet run` command.

### Solution

#### Step 1.1: Create Backend Startup Script
**File**: `start-backend.ps1` (create in project root)

```powershell
# HexaBill Backend Startup Script
Write-Host "`n🚀 Starting HexaBill Backend API..." -ForegroundColor Cyan
Write-Host "📁 Directory: backend/HexaBill.Api`n" -ForegroundColor Gray

# Change to backend directory
Set-Location -Path "backend/HexaBill.Api"

# Check if dotnet is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "❌ ERROR: dotnet CLI not found!" -ForegroundColor Red
    Write-Host "   Please install .NET SDK: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

# Check if project file exists
if (-not (Test-Path "HexaBill.Api.csproj")) {
    Write-Host "❌ ERROR: HexaBill.Api.csproj not found!" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Starting backend server..." -ForegroundColor Green
Write-Host "   Backend will be available at: http://localhost:5000" -ForegroundColor Gray
Write-Host "   Health check: http://localhost:5000/api/health`n" -ForegroundColor Gray

# Start backend
dotnet run
```

#### Step 1.2: Verify Backend Startup
After running `start-backend.ps1`, verify:
1. Backend logs show: "Now listening on: http://localhost:5000"
2. Health check responds: Open `http://localhost:5000/api/health` in browser
3. No startup errors in console

#### Step 1.3: Test Frontend Connection
1. Start frontend: `cd frontend/hexabill-ui && npm run dev`
2. Open browser: `http://localhost:5173`
3. Check browser console - should see successful API calls
4. Verify no `ERR_CONNECTION_REFUSED` errors

---

## Issue 2: Logo URL Construction

### Current State
- `Logo.jsx` correctly uses `getApiBaseUrlNoSuffix()`
- `SettingsPage.jsx` correctly uses `getApiBaseUrlNoSuffix()`
- `TenantBrandingContext.jsx` uses logo URL directly from backend

### Potential Issue
If backend returns relative path `/uploads/logo_xxx.png`, it needs to be prefixed with backend base URL.

### Solution

#### Step 2.1: Fix TenantBrandingContext Logo URL
**File**: `frontend/hexabill-ui/src/contexts/TenantBrandingContext.jsx`

**Current Code** (lines 68-73):
```javascript
const logoUrl = data.COMPANY_LOGO || data.logoUrl || data.companyLogo || null
// Add cache-busting parameter to logo URL to force refresh
const logoUrlWithCache = logoUrl ? `${logoUrl}${logoUrl.includes('?') ? '&' : '?'}t=${Date.now()}` : null
```

**Fixed Code**:
```javascript
const logoUrl = data.COMPANY_LOGO || data.logoUrl || data.companyLogo || null
// Ensure logo URL is full URL (prefix with backend base if relative)
const apiBase = getApiBaseUrlNoSuffix()
let fullLogoUrl = logoUrl
if (logoUrl && !logoUrl.startsWith('http')) {
  // If relative path (starts with /uploads), prefix with backend base URL
  fullLogoUrl = logoUrl.startsWith('/') 
    ? `${apiBase}${logoUrl}` 
    : `${apiBase}/uploads/${logoUrl}`
}
// Add cache-busting parameter
const logoUrlWithCache = fullLogoUrl ? `${fullLogoUrl}${fullLogoUrl.includes('?') ? '&' : '?'}t=${Date.now()}` : null
```

**Also update favicon URL** (line 87):
```javascript
if (fullLogoUrl) {
  const faviconUrl = `${fullLogoUrl}${fullLogoUrl.includes('?') ? '&' : '?'}t=${Date.now()}`
  updateFavicon(faviconUrl)
}
```

---

## Issue 3: toUpperCase() Safety (Already Fixed)

### Verification
All instances checked are safe:
- ✅ Line 538: `String(response.config.method).toUpperCase()` - has null check
- ✅ Line 605: `String(error.config.method).toUpperCase()` - has null check  
- ✅ Line 627: `String(retryConfig.method).toUpperCase()` - has null check
- ✅ Line 783: `(error.config?.method || '').toUpperCase()` - has default empty string

**No changes needed** - all instances are already safe.

---

## Issue 4: Environment Configuration

### Current Configuration
- ✅ `apiConfig.js` correctly detects localhost vs production
- ✅ Uses `VITE_API_BASE_URL` env variable if set
- ✅ Falls back to `https://hexabill.onrender.com/api` for production
- ✅ `vite.config.js` has proxy configuration for development

### Verification Steps
1. Check `.env.local` exists (optional, for local overrides)
2. Verify `VITE_API_BASE_URL` is not set (or set to `http://localhost:5000/api` for local dev)
3. Confirm `vite.config.js` proxy is active in development mode

---

## Testing Plan

### Phase 1: Backend Startup Verification
1. Run `start-backend.ps1`
2. Wait for "Now listening on: http://localhost:5000"
3. Test health endpoint: `http://localhost:5000/api/health`
4. Verify response: `{"status":"ok"}` or similar

### Phase 2: Frontend Connection Test
1. Start frontend: `cd frontend/hexabill-ui && npm run dev`
2. Open `http://localhost:5173`
3. Check browser console:
   - ✅ Should see successful API calls
   - ✅ No `ERR_CONNECTION_REFUSED` errors
   - ✅ Health check succeeds

### Phase 3: Logo Loading Test
1. Login to application
2. Navigate to Settings page
3. Check if logo displays (if previously uploaded)
4. Upload new logo (if needed)
5. Verify logo loads without errors
6. Check Network tab - logo URL should be correct

### Phase 4: Page-by-Page Testing

Test each page systematically:

#### 4.1 Dashboard (`/dashboard`)
- ✅ Loads without errors
- ✅ Displays summary statistics
- ✅ Charts render correctly

#### 4.2 Reports (`/reports`)
- ✅ Filter dropdowns load (products, customers)
- ✅ Report data loads
- ✅ Date range filters work
- ✅ Export functions work

#### 4.3 Customer Ledger (`/customers/ledger`)
- ✅ Customer list loads
- ✅ Ledger entries display
- ✅ Date filters work
- ✅ No `toUpperCase()` errors

#### 4.4 Sales Ledger (`/sales/ledger`)
- ✅ Sales list loads
- ✅ Filters work
- ✅ Export works

#### 4.5 Expenses (`/expenses`)
- ✅ Expense list loads
- ✅ Add/edit/delete work
- ✅ Categories load

#### 4.6 Settings (`/settings`)
- ✅ Settings load
- ✅ Logo upload works
- ✅ Company info saves
- ✅ Logo displays correctly

#### 4.7 Users (`/users`)
- ✅ User list loads
- ✅ Add/edit/delete work
- ✅ Role assignments work

#### 4.8 Branches (`/branches`)
- ✅ Branch list loads
- ✅ Branch details load
- ✅ Summary data loads

#### 4.9 Routes (`/routes`)
- ✅ Route list loads
- ✅ Route details load
- ✅ Expenses load

#### 4.10 Super Admin (`/superadmin`) - if applicable
- ✅ Tenant list loads
- ✅ Tenant details load
- ✅ Impersonation works

### Phase 5: Role-Based Testing

Test with different user roles:

#### 5.1 Owner Role
- ✅ Full access to all pages
- ✅ Can modify settings
- ✅ Can manage users
- ✅ Can access all reports

#### 5.2 Admin Role
- ✅ Limited access (per role permissions)
- ✅ Cannot access Super Admin
- ✅ Can manage assigned branches/routes

#### 5.3 Staff Role
- ✅ Read-only access
- ✅ Cannot modify settings
- ✅ Cannot manage users

#### 5.4 SuperAdmin Role
- ✅ System-wide access
- ✅ Can impersonate tenants
- ✅ Can manage all tenants

---

## Files to Modify

### 1. `start-backend.ps1` (NEW)
- Create PowerShell script to start backend
- Include error checking and helpful messages

### 2. `frontend/hexabill-ui/src/contexts/TenantBrandingContext.jsx`
- Fix logo URL construction to use `getApiBaseUrlNoSuffix()`
- Ensure relative paths are prefixed with backend base URL

### 3. `BACKEND_STARTUP_GUIDE.md` (NEW)
- Create comprehensive guide for starting backend
- Include troubleshooting steps

---

## Expected Outcomes

After implementing fixes:

✅ **Backend Server**
- Starts successfully on port 5000
- Health endpoint responds correctly
- No startup errors

✅ **Frontend Connection**
- Connects to backend without errors
- No `ERR_CONNECTION_REFUSED` errors
- API calls succeed

✅ **Logo Loading**
- Logo URLs constructed correctly
- Images load from correct backend URL
- No 404 errors for logos

✅ **API Calls**
- All endpoints respond successfully
- No `toUpperCase()` errors
- Retry logic works correctly

✅ **Page Functionality**
- All pages load data correctly
- Filters work
- Forms submit successfully
- No console errors

---

## Troubleshooting Guide

### Backend Won't Start

**Error**: `dotnet: command not found`
- **Solution**: Install .NET SDK from https://dotnet.microsoft.com/download

**Error**: `The project file was not found`
- **Solution**: Verify you're in `backend/HexaBill.Api` directory

**Error**: `Port 5000 is already in use`
- **Solution**: 
  ```powershell
  # Find process using port 5000
  netstat -ano | findstr :5000
  # Kill process (replace PID with actual process ID)
  taskkill /PID <PID> /F
  ```

**Error**: Database migration errors
- **Solution**: Run `dotnet ef database update` in backend directory

### Frontend Still Shows ERR_CONNECTION_REFUSED

**Check 1**: Is backend running?
- Open `http://localhost:5000/api/health` in browser
- Should return JSON response

**Check 2**: Is frontend using correct API URL?
- Check browser console for API calls
- Verify base URL is `http://localhost:5000/api`

**Check 3**: Is Vite proxy configured?
- Check `vite.config.js` has proxy for `/api`
- Restart frontend dev server after changes

### Logo Still Not Loading

**Check 1**: Is logo URL correct?
- Check Network tab for logo request
- Verify URL includes backend base URL

**Check 2**: Is backend serving static files?
- Check `Program.cs` has static file middleware
- Verify `/uploads` directory exists

**Check 3**: Is logo file actually uploaded?
- Check Settings page
- Verify logo path in database

---

## Next Steps

1. ✅ Create `start-backend.ps1` script
2. ✅ Fix logo URL in `TenantBrandingContext.jsx`
3. ✅ Create `BACKEND_STARTUP_GUIDE.md`
4. ⏳ Test backend startup
5. ⏳ Test frontend connection
6. ⏳ Test all pages systematically
7. ⏳ Document any remaining issues

---

## Summary

**Main Issue**: Backend server not running - fix by starting with `dotnet run`

**Secondary Issue**: Logo URLs may need base URL prefixing - fix in `TenantBrandingContext.jsx`

**No Issue**: `toUpperCase()` errors - already fixed with proper null checks

**Action Required**: Start backend server, then test all pages.
