# Deploy to Production — Quick Steps

**Before deploy:** Backend and frontend build OK. Backend runs locally; `/health` returns 200.

---

## 1. GitHub push (trigger Vercel + Render)

```powershell
cd "c:\Users\anand\Downloads\HexaBil-App\HexaBil-App"
git status
git add -A
git commit -m "Production: ping/connection fixes, deploy prep"
git push origin main
```

- **Vercel:** Auto-deploys when you push to `main` (if repo is connected).
- **Render:** Auto-deploys when you push to `main` (if repo is connected and Root Directory = `backend/HexaBill.Api`).

---

## 2. Vercel (frontend) — check

| Item | Action |
|------|--------|
| **Root Directory** | `frontend/hexabill-ui` |
| **Build** | `npm run build` |
| **Output** | `dist` |
| **Env (Production)** | `VITE_API_BASE_URL` = `https://hexabill-api.onrender.com/api` (or your Render backend URL + `/api`) |

After push, open the Vercel project → **Deployments** → latest should build and go live.

---

## 3. Render (backend) — check

| Item | Action |
|------|--------|
| **Root Directory** | `backend/HexaBill.Api` |
| **Runtime** | Native or Docker (per `render.yaml` / dashboard) |
| **Env** | `ASPNETCORE_ENVIRONMENT=Production`, `DATABASE_URL`, `ALLOWED_ORIGINS` (include your Vercel URL), `JwtSettings__SecretKey` |
| **Health** | `/health` — used by Render for health checks |

After push, Render will rebuild and deploy. First request after free-tier spin-down may take 30–60 s.

---

## 4. Production checks after deploy

1. **Backend:** Open `https://hexabill-api.onrender.com/health` (or your URL) → expect `{"status":"Healthy",...}`.
2. **Frontend:** Open your Vercel URL → login and use Dashboard/Reports.
3. **CORS:** If login/API fails with CORS, add the **exact** Vercel URL to Render env `ALLOWED_ORIGINS` (comma-separated, no spaces) and redeploy backend.

---

## 5. Errors / risks (already addressed)

- **Ping 404:** Handled; ping is best-effort, no disconnect/toast.
- **Connection spam:** Health check backoff (15s → 60s) when backend is down.
- **API base:** Production uses `VITE_API_BASE_URL`; localhost only when host is localhost/127.0.0.1.

Full details: `docs/PRODUCTION_DEPLOYMENT_VERCEL_RENDER.md`.
