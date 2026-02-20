# Backend Startup Guide

## Quick Start

### Windows PowerShell
```powershell
# From project root
.\start-backend.ps1
```

### Manual Start
```powershell
cd backend/HexaBill.Api
dotnet run
```

## Verification

After starting, verify backend is running:

1. **Check Console Output**
   - Should see: "Now listening on: http://localhost:5000"
   - Should see: "Swagger UI available at: http://localhost:5000/swagger" (Development only)

2. **Test Health Endpoint**
   - Open browser: `http://localhost:5000/api/health`
   - Should return: `{"status":"ok"}` or similar JSON

3. **Check Frontend Connection**
   - Start frontend: `cd frontend/hexabill-ui && npm run dev`
   - Open `http://localhost:5173`
   - Check browser console - should see successful API calls

## Troubleshooting

### Port 5000 Already in Use

**Find process using port:**
```powershell
netstat -ano | findstr :5000
```

**Kill process (replace PID):**
```powershell
taskkill /PID <PID> /F
```

### Database Migration Errors

**Run migrations:**
```powershell
cd backend/HexaBill.Api
dotnet ef database update
```

### .NET SDK Not Found

**Install .NET SDK:**
- Download from: https://dotnet.microsoft.com/download
- Install .NET 8 SDK or later
- Restart PowerShell after installation

### Project File Not Found

**Verify directory structure:**
```powershell
# Should exist:
backend/HexaBill.Api/HexaBill.Api.csproj
backend/HexaBill.Api/Program.cs
```

## Common Issues

### Backend Starts But Frontend Can't Connect

1. Check backend is listening on correct port (5000)
2. Verify frontend `apiConfig.js` uses `http://localhost:5000/api`
3. Check Vite proxy configuration in `vite.config.js`
4. Restart frontend dev server

### Backend Crashes on Startup

1. Check console for error messages
2. Verify database connection string in `appsettings.json`
3. Check if SQLite database file exists (if using SQLite)
4. Run `dotnet restore` to restore packages

### Logo/Image Files Not Loading

1. Verify backend static file middleware is configured
2. Check `/uploads` directory exists in `wwwroot`
3. Verify logo URLs use correct base URL
4. Check browser Network tab for 404 errors

## Production Deployment

For production (Render/other hosting):

1. Set `VITE_API_BASE_URL` environment variable to your backend URL
2. Backend should be deployed separately
3. Frontend will use production API URL automatically

## Next Steps

After backend is running:
1. ✅ Test health endpoint
2. ✅ Start frontend
3. ✅ Test login
4. ✅ Test all pages systematically
5. ✅ Verify logo loading
6. ✅ Check for any remaining errors
