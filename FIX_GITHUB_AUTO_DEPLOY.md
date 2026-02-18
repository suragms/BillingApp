# Fix GitHub Auto-Deploy in Vercel

**Problem:** Production shows "Source: vercel deploy" (CLI) instead of GitHub. Auto-deploy from GitHub pushes isn't working.

---

## Why This Happens

1. **GitHub not connected** to Vercel project
2. **Wrong repository** linked (e.g., `HexaBill` instead of `HexaBillAd`)
3. **Wrong branch** configured (should be `main`)
4. **Webhook not working** (GitHub → Vercel webhook failed or missing)

---

## Fix Steps

### Step 1: Check GitHub Connection in Vercel

1. **Vercel Dashboard** → Your project (`hexabill-ui` or `hexa-bill-sw`)
2. **Settings** → **Git** (left sidebar)
3. **Check:**
   - **Repository:** Should show `ANANDU-2000/HexaBillAd` (not `HexaBill`)
   - **Production Branch:** Should be `main`
   - **Auto-deploy:** Should be **Enabled**

### Step 2: Connect GitHub (If Not Connected)

If it says "Not connected" or shows wrong repo:

1. Click **"Connect Git Repository"** (or **"Change"** if wrong repo)
2. Select **GitHub** as provider
3. **Search/Select:** `ANANDU-2000/HexaBillAd`
4. **Branch:** `main`
5. Click **Connect**

### Step 3: Verify GitHub App Permissions

**GitHub** must allow Vercel to access your repo:

1. **GitHub** → Your profile → **Settings** → **Applications** → **Installed GitHub Apps**
2. Find **Vercel**
3. Click **Configure**
4. **Repository access:**
   - Should include `ANANDU-2000/HexaBillAd` (or "All repositories")
   - If missing, add `HexaBillAd` → **Save**

### Step 4: Test Auto-Deploy

After connecting:

1. **Make a small change** (or use the push we just did: commit `f071bf5`)
2. **Push to GitHub:**
   ```powershell
   git commit --allow-empty -m "test: trigger Vercel auto-deploy"
   git push origin main
   ```
3. **Vercel Dashboard** → **Deployments**
4. **Wait 10-30 seconds** — you should see a new deployment appear
5. **Check the Source:** Should say `GitHub` (not `vercel deploy`)

---

## If Auto-Deploy Still Doesn't Work

### Option A: Manual Deploy from GitHub

1. **Vercel Dashboard** → **Deployments**
2. Click **"Create Deployment"** (top right)
3. **Source:** Select **GitHub**
4. **Branch:** `main`
5. **Commit:** Leave empty (uses latest) or enter commit hash (e.g., `f071bf5`)
6. Click **Deploy**

This creates a deployment from GitHub (source will show `GitHub`), then **Promote to Production**.

### Option B: Use Direct Deploy Script (Current Workaround)

If GitHub auto-deploy keeps failing, use the direct deploy script:

```powershell
.\scripts\deploy-vercel-direct.ps1
```

This deploys from local build (source shows `vercel deploy`), but it works reliably.

---

## Current Status

✅ **Fix pushed:** Commit `f071bf5` includes:
- PostgreSQL `LastActiveAt` / `LastLoginAt` fix
- Updated deploy docs

**Next:** 
- **Render** will auto-deploy (backend fix)
- **Vercel** should auto-deploy if GitHub is connected (frontend)

**Check Vercel Deployments** in 1-2 minutes to see if a new deployment appears from GitHub.

---

## Quick Checklist

- [ ] Vercel → Settings → Git → Repository = `ANANDU-2000/HexaBillAd`
- [ ] Vercel → Settings → Git → Production Branch = `main`
- [ ] GitHub → Settings → Applications → Vercel → Has access to `HexaBillAd`
- [ ] Push to `main` triggers Vercel deployment (check Deployments tab)

If all checked but still not working, use **"Create Deployment"** from GitHub branch manually.
