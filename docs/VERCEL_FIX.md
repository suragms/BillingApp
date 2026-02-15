# Clear solutions: Vercel not updated, .gitignore, and UI

## Why the live site is not updated (main reason)

**Production was rolled back.** In Vercel → Deployments you see:  
**"Rolled back to 2yuVphCt9 11m ago"**

So the **current** live site is an **old** deployment. Newer deployments exist but were rolled back.

### Fix 1: Undo the rollback (do this first)

1. Open **Vercel** → your project **hexa-bill** → **Deployments**.
2. At the top you should see a yellow banner: **"Rolled back to 2yuVphCt9 ..."**.
3. Click **"Undo Rollback"** on the right of that banner.
4. The latest deployment will become **Current** and your updated UI/icons will be live.

---

## .gitignore is not hiding your UI or icons

- **Repo root** `.gitignore`: ignores `node_modules/`, `dist/`, `bin/`, `.env`, etc. It does **not** ignore `src/`, icons, or React components.
- **Frontend** `frontend/hexabill-ui/.gitignore`: only has `.vercel` (so Vercel CLI config is not committed). It does **not** ignore any source files.

So your updated UI pages and icon order **are** in the repo and **are** pushed to GitHub. The reason they don’t show on the site is the **rollback**, not .gitignore.

---

## Vercel build settings (if Undo Rollback is not enough)

Your repo is already correct:

- **Root Directory:** `frontend/hexabill-ui` (set in Vercel project settings).
- **Build / Output:** `frontend/hexabill-ui/vercel.json` has:
  - `"buildCommand": "npm run build"`
  - `"outputDirectory": "dist"`
  - `"framework": "vite"`

If the live build is still wrong after undoing rollback:

1. Vercel → **Settings** → **Build & Development**.
2. Set **Framework Preset** to **Vite** (or leave **Other**).
3. Set **Output Directory** to **`dist`** (override if it shows `public` or `.`).
4. **Save** and use **Redeploy** on the latest deployment.

---

## Billing warning

The banner **"The billing address on your payment method is missing or incomplete"** can affect some features. To avoid issues:

- Vercel → **Settings** (account) → **Billing** → **Update Address**.

---

## Summary

| Problem              | Cause              | Solution                    |
|----------------------|--------------------|-----------------------------|
| Site not updated     | Rollback to old deploy | **Undo Rollback** in Vercel |
| .gitignore concern   | None (icons/UI not ignored) | No change needed            |
| Wrong build output   | Possible wrong Output in dashboard | Set Output to `dist`       |
| Billing              | Incomplete address  | Update in Billing           |

Do **Fix 1 (Undo Rollback)** first; that should bring your latest UI and icon order live.
