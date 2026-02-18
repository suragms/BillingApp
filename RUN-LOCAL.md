# Run HexaBill locally

## 1. Start the backend (required)

**Option A – Double-click**
- Run **`START-BACKEND.bat`** in the project root.
- Keep the window **open** (backend runs there).

**Option B – Terminal**
```bash
cd backend\HexaBill.Api
dotnet run
```

When ready you should see: `Now listening on: http://localhost:5000`

## 2. Start the frontend

In a **new** terminal:

```bash
cd frontend\hexabill-ui
npm install
npm run dev
```

Open the URL shown (e.g. http://localhost:5173).

## 3. Login

- **Email:** `admin@hexabill.com`
- **Password:** `Admin123!`

---

**If you see "ERR_CONNECTION_REFUSED" or "Server connection unavailable"**  
→ The backend is not running. Start it with step 1 and **keep that window open**.

**If the app worked and then stopped after a few minutes**  
→ The backend process exited (window closed, crash, or sleep). Run **START-BACKEND.bat** again and keep its window open. The frontend will reconnect automatically within about 10 seconds once the backend is back.
