# Error Matrix — HTTP Status Codes and Frontend Handling

**Purpose:** Document how the frontend maps API HTTP status codes to user messages and actions. Used for production readiness and support.

**Implementation:** `frontend/hexabill-ui/src/services/api.js` (response interceptor).

---

## Status Code → Message and Action

| Code | Meaning | User message (or source) | Retry? | Notes |
|------|---------|---------------------------|--------|--------|
| **400** | Bad Request | Validation: `errors` array joined, or `message`, or "Invalid request. Please check your input." | No | Show field-level errors when API returns `errors[]`. |
| **401** | Unauthorized | Handled by interceptor: redirect to login / refresh token. | No | Token invalid or expired. |
| **403** | Forbidden | `message` or "Access denied". Tenant/suspended message gets "Please contact your administrator." | No | Tenant suspended, insufficient permissions, or Staff no route. |
| **404** | Not Found | `message` or "Resource not found." Branches/Routes 404: "Branches & Routes feature is currently unavailable…" (once per session). | No | Special case for /branches, /routes. |
| **429** | Too Many Requests | Throttled toast; backend rate limit. | Yes (after backoff) | Rate limiter (e.g. 100/min per IP). |
| **500** | Internal Server Error | "Something went wrong." + correlation ID when present. | Yes (Retry button) | Logged to ErrorLogs (SQLite + PostgreSQL). |
| **502** | Bad Gateway | "Server temporarily unavailable. Please try again." | Yes (Retry button) | Proxy/upstream issue. |
| **503** | Service Unavailable | Maintenance overlay: "System under maintenance. Back shortly." | No | Branded maintenance screen. |
| **520** / other 5xx | Generic server error | Throttled error toast; optional correlation ID. | Yes when treated as server error | Same as 500 handling where applicable. |

---

## Backend 500 Response Shape

All unhandled exceptions return consistent JSON:

```json
{
  "success": false,
  "message": "An unexpected error occurred.",
  "correlationId": "<guid>"
}
```

- **correlationId** is logged server-side (and in ErrorLogs for Super Admin). Frontend may show it in toast or error UI for support.
- **Production:** No stack traces in response. Full exception and correlation ID are logged to ErrorLogs.

---

## Validation Errors (400)

When the API returns `400` with a body like:

```json
{
  "success": false,
  "message": "Validation failed",
  "errors": ["Field X is required", "Field Y must be a number"]
}
```

The frontend shows a single toast with `errors` joined, or `message` if no `errors` array.

---

## Retry and Throttling

- **Retry:** 500, 502, and (where applicable) 5xx show a "Retry" button that reloads the page.
- **Throttling:** Error toasts are throttled (e.g. one per 3–12 seconds for server errors) to avoid flooding the UI.
- **Network errors:** Treated as server/unavailable; optional retry and reconnection handling via ConnectionStatus.

---

## Checklist Reference

Before go-live, verify:

1. No 500 on main flows (login, dashboard, reports, POS, key CRUD).
2. 400 shows validation messages (not generic only).
3. 401 redirects to login.
4. 403 shows clear message (e.g. "No route assigned" for Staff).
5. 502/503 show user-friendly message and (where applicable) retry.

See also: `PRODUCTION_CHECKLIST.md`, `docs/PRE_PRODUCTION_TEST_CHECKLIST.md`, and the Production Readiness section in `PRODUCTION_MASTER_TODO.md`.
