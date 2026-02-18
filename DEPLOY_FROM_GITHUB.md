# Deploy from GitHub (Source = GitHub, not "vercel deploy")

Your repo is connected, but auto-deploy might not be working. **Manually deploy from GitHub** so Source shows **GitHub**:

---

## Step-by-step: Deploy latest GitHub code

1. **Vercel Dashboard** → project **hexabill-ui** → **Deployments** tab
2. Click **"Create Deployment"** (top right, blue button)
3. In the dialog:
   - **Source:** Select **Git** (GitHub) — NOT "CLI"
   - **Branch:** `main`
   - **Commit:** Leave empty (uses latest) OR enter: `0a318b4`
   - Click **Deploy**
4. Wait for build to finish (1-2 minutes)
5. When status is **Ready**:
   - Open the deployment
   - Click **⋯** (three dots) → **Promote to Production**
6. Done. Source will now show **GitHub** (not "vercel deploy")

---

## Check Production Branch

If auto-deploy still doesn't work after manual deploy:

1. **Vercel** → **Settings** → **Git**
2. Check **Production Branch** = `main` (not `master` or something else)
3. If wrong, change to `main` → **Save**

---

## After this

- **Source** will show **GitHub** (commit hash, e.g. `0a318b4`)
- Future pushes to `main` should auto-deploy (if webhook is working)
- If auto-deploy still fails, use "Create Deployment" from GitHub branch manually
