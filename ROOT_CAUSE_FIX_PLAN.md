# Root Cause Fix Plan

## Problem Summary

Two main issues identified:

1. **ERR_CONNECTION_REFUSED** - Backend server at `localhost:5000` is not running
2. **toUpperCase() Error** - Already fixed (all instances are safe with null checks)

## Issue 1: Backend Not Running

### Current Status
- `netstat` shows no process listening on port 5000
- No dotnet processes found running
- Frontend is configured to use `http://localhost:5000/api` when running locally

### Root Cause
Backend server needs to be started manually with `dotnet run` in the backend directory.

### Fix Steps

#### Step 1: Verify Backend Directory Structure
- Check that `backend/HexaBill.Api/HexaBill.Api.csproj` exists
- Verify `Program.cs` is present

#### Step 2: Create Backend Startup Script
Create `start-backend.ps1` in project root:
```powershell
# Start Backend Server
Write-Host "Starting HexaBill Backend API..." -ForegroundColor Cyan
cd backend/HexaBill.Api
dotnet run
```

#### Step 3: Verify Backend Starts Successfully
- Backend should show: "Now listening on: http://localhost:5000"
- Health check endpoint should respond: `GET http://localhost:5000/api/health`

#### Step 4: Update Frontend Logo URL Handling
The logo URLs from backend are relative paths (`/uploads/logo_xxx.png`). When backend is down, these fail. Need to ensure logo URLs use `getApiBaseUrlNoSuffix()` consistently.

**File**: `frontend/hexabill-ui/src/contexts/TenantBrandingContext.jsx`
- Line 68: Logo URL comes from backend
- Line 73: Logo URL is used directly - needs to be prefixed with API base URL if relative

**Fix**: Ensure logo URLs are constructed with full backend URL when relative:
```javascript
const logoUrl = data.COMPANY_LOGO || data.logoUrl || data.companyLogo || null
// If logoUrl is relative (starts with /uploads), prefix with backend base URL
const apiBase = getApiBaseUrlNoSuffix()
const fullLogoUrl = logoUrl && !logoUrl.startsWith('http') 
  ? `${apiBase}${logoUrl.startsWith('/') ? '' : '/'}${logoUrl}` 
  : logoUrl
```

## Issue 2: toUpperCase() Safety (Already Fixed)

All instances checked are safe:
- Line 538: `String(response.config.method).toUpperCase()` - safe (null check)
- Line 605: `String(error.config.method).toUpperCase()` - safe (null check)
- Line 627: `String(retryConfig.method).toUpperCase()` - safe (null check)
- Line 783: `(error.config?.method || '').toUpperCase()` - safe (default empty string)

## Issue 3: Environment Configuration

### Current Configuration
- `apiConfig.js` correctly detects localhost vs production
- Uses `VITE_API_BASE_URL` env variable if set
- Falls back to `https://hexabill.onrender.com/api` for production

### Verification Needed
1. Check if `.env.local` exists and has correct `VITE_API_BASE_URL`
2. Verify `vite.config.js` proxy configuration (lines 52-56) is correct
3. Ensure frontend uses proxy in development, direct URL in production

## Testing Plan

### Phase 1: Backend Startup
1. Start backend: `cd backend/HexaBill.Api && dotnet run`
2. Verify health check: `curl http://localhost:5000/api/health` or open in browser
3. Check backend logs for any startup errors

### Phase 2: Frontend Connection
1. Start frontend: `cd frontend/hexabill-ui && npm run dev`
2. Open browser: `http://localhost:5173`
3. Check browser console - should see successful API calls
4. Verify no `ERR_CONNECTION_REFUSED` errors

### Phase 3: Logo Loading
1. Login to application
2. Navigate to Settings page
3. Upload a logo (if not already uploaded)
4. Verify logo displays correctly
5. Check Network tab - logo should load from correct URL

### Phase 4: Page-by-Page Testing
Test each page after backend is running:
1. **Dashboard** - `/dashboard`
2. **Reports** - `/reports`
3. **Customer Ledger** - `/customers/ledger`
4. **Sales Ledger** - `/sales/ledger`
5. **Expenses** - `/expenses`
6. **Settings** - `/settings`
7. **Users** - `/users`
8. **Branches** - `/branches`
9. **Routes** - `/routes`
10. **Super Admin** (if applicable) - `/superadmin`

### Phase 5: Role-Based Testing
Test with different roles:
1. **Owner** - Full access
2. **Admin** - Limited access
3. **Staff** - Read-only access
4. **SuperAdmin** - System-wide access

## Files to Modify

1. **frontend/hexabill-ui/src/contexts/TenantBrandingContext.jsx**
   - Fix logo URL construction to use `getApiBaseUrlNoSuffix()`

2. **start-backend.ps1** (NEW)
   - Create startup script for backend

3. **BACKEND_STARTUP_GUIDE.md** (NEW)
   - Create comprehensive guide for starting backend

## Expected Outcomes

After fixes:
- ✅ Backend starts successfully on port 5000
- ✅ Frontend connects to backend without ERR_CONNECTION_REFUSED
- ✅ Logo images load correctly
- ✅ All API endpoints respond successfully
- ✅ No toUpperCase() errors
- ✅ All pages load data correctly

## Next Steps

1. Create backend startup script
2. Fix logo URL construction in TenantBrandingContext
3. Test backend startup
4. Test frontend connection
5. Test all pages systematically
6. Document any remaining issues
