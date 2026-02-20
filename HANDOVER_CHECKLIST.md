# HexaBill – Handover Checklist

**Date:** February 2026  
**Status:** Ready for handover

---

## Quick start (day of handover)

1. **Backend:** Stop any running API, then:
   - `cd backend/HexaBill.Api`
   - Ensure `appsettings.json` has `ConnectionStrings:DefaultConnection` and `JwtSettings:SecretKey`
   - `dotnet run` → listen on http://localhost:5000
2. **Frontend:** `cd frontend/hexabill-ui` → `npm install` → `npm run dev` → http://localhost:5173
3. **Test:** Login → Dashboard → Users page → Customer Ledger → POS (create sale with Branch + Route selected).

---

## 1. Fixes applied (summary)

| Area | What was fixed |
|------|----------------|
| **Auth** | Login null/empty validation; ILogger; production-safe 500 (no stack trace); JWT secret from env `JWT_SECRET_KEY` or config; `PageAccess` in login response. |
| **API client** | Axios `config.method` undefined → `toUpperCase` error: request interceptor now returns a plain config with `method` set; no Proxy; `api.get` passes plain object. |
| **Frontend auth** | `useAuth` stores `pageAccess` from login; health ping uses `getApiBaseUrlNoSuffix()` so `/health` is called correctly. |
| **POS create sale** | Branch/route: `branchId` is derived from selected route so backend never gets branch/route mismatch; 400 errors: toast + console show backend `message`/`errors` (and JSON logged). |
| **Build** | Frontend: `terser` added to devDependencies so `npm run build` works with Vite minify. |

---

## 2. Pre-handover verification

### Backend

- [ ] **Stop** any running HexaBill.Api process (so build can overwrite files).
- [ ] From repo root:  
  `cd backend/HexaBill.Api`  
  `dotnet build`  
  → Expect: **Build succeeded** (warnings only, 0 errors).
- [ ] Set connection string and JWT (see below), then:  
  `dotnet run`  
  → Expect: server listening on `http://localhost:5000` (or PORT env).
- [ ] Open `http://localhost:5000/health` → `{"status":"ok",...}`.

### Frontend

- [ ] From repo root:  
  `cd frontend/hexabill-ui`  
  `npm install`  
  `npm run build`  
  → Expect: build completes (no “terser not found”).
- [ ] `npm run dev` → app at `http://localhost:5173` (or next free port).

### End-to-end

- [ ] **Login** with valid user → 200, token stored, redirect to app.
- [ ] **Protected route** (e.g. Dashboard) → loads with token.
- [ ] **Users page** → loads without “toUpperCase” error.
- [ ] **Customer Ledger** → loads customer/reports without “toUpperCase” error.
- [ ] **POS** → Select Branch + Route, add product, Create Sale.  
  - If 400: check toast and console “400 Bad Request - Full Response:” for backend message (e.g. product/stock, route).

---

## 3. Environment / config

### Backend (`backend/HexaBill.Api`)

- **Database:**  
  - Local: `appsettings.json` → `ConnectionStrings:DefaultConnection` (e.g. `Data Source=hexabill.db` for SQLite).  
  - Production: `DATABASE_URL` or `ConnectionStrings__DefaultConnection` (PostgreSQL).
- **JWT:**  
  - Local: `appsettings.json` → `JwtSettings:SecretKey` (min 32 chars).  
  - Production: `JWT_SECRET_KEY` env (recommended).
- **CORS:**  
  - `ALLOWED_ORIGINS` env (comma-separated) or `appsettings.json` → `AllowedOrigins`.

### Frontend (`frontend/hexabill-ui`)

- **API URL:**  
  - Local (localhost): uses `http://localhost:5000/api` automatically.  
  - Production: set `VITE_API_BASE_URL` in build env (e.g. `https://your-api.com/api`).  
- Optional: copy `.env.example` to `.env` and set `VITE_API_BASE_URL` if needed.

---

## 4. Key files touched (for handover)

- `backend/HexaBill.Api/Program.cs` – (unchanged in this pass; already has CORS, DB, Serilog).
- `backend/HexaBill.Api/Modules/Auth/AuthController.cs` – Login validation, ILogger, safe 500.
- `backend/HexaBill.Api/Modules/Auth/AuthService.cs` – `GetJwtSecretKey()`, `PageAccess` in login.
- `backend/HexaBill.Api/Shared/Extensions/SecurityConfiguration.cs` – JWT secret from env.
- `backend/HexaBill.Api/Models/DTOs.cs` – `PageAccess` on `LoginResponse`.
- `frontend/hexabill-ui/src/services/api.js` – Request interceptor plain config, no Proxy; `api.get` plain object.
- `frontend/hexabill-ui/src/hooks/useAuth.jsx` – `pageAccess` from login.
- `frontend/hexabill-ui/src/App.jsx` – Health ping URL (`getApiBaseUrlNoSuffix()` + `/health`).
- `frontend/hexabill-ui/src/pages/company/PosPage.jsx` – Branch from route; 400 message/errors + JSON log.
- `frontend/hexabill-ui/package.json` – `terser` in devDependencies.

---

## 5. Known / operational notes

1. **Backend build while API is running:** Build may warn “Unable to delete file … Access denied” (process locking). Stop the API, then run `dotnet build` again.
2. **POS 400:** If create sale still returns 400, the **toast** and **console** (“400 Bad Request - Full Response:” and “Backend Error Response:”) now show the exact backend message (e.g. product not found, insufficient stock, route/branch). Use that to fix data or selection.
3. **Cache:** Frontend uses short-lived response cache for some GETs; hard refresh (Ctrl+Shift+R) if you need to avoid cached responses after backend changes.

---

## 6. Handover confirmation

- [ ] Backend builds and runs.
- [ ] Frontend builds and runs.
- [ ] Login works.
- [ ] No “toUpperCase” / axios method errors on Users or Customer Ledger.
- [ ] POS create sale: either succeeds or shows clear backend error message.
- [ ] Env/config documented above and in `PROJECT_AUDIT_FIXES.md` (and `.env.example` / `appsettings.example.json` where present).

**Signed off for handover:** _________________  
**Date:** _________________
