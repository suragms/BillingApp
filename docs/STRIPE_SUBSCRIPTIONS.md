# Stripe subscription payments (PRODUCTION_MASTER_TODO #43)

Tenants can pay for subscription plans via Stripe Checkout. If Stripe is not configured, the app falls back to creating a trial subscription without payment.

## Backend configuration

Set in `appsettings.json` or environment variables:

- **Stripe:SecretKey** — Stripe secret key (sk_test_... or sk_live_...). If missing, checkout is skipped and tenants get trial-only.
- **Stripe:WebhookSecret** — Stripe webhook signing secret (wh_sec_...). Required for the webhook endpoint to verify and activate subscriptions.

Example env vars:

- `STRIPE_SECRET_KEY`
- `STRIPE_WEBHOOK_SECRET`

## Webhook

1. In Stripe Dashboard → Developers → Webhooks, add endpoint:  
   `https://your-api.onrender.com/api/subscription/webhook`
2. Select event: **checkout.session.completed**
3. Copy the **Signing secret** into `Stripe:WebhookSecret`.

The webhook creates/activates the subscription for the tenant using session metadata (tenantId, planId, billingCycle).

## Flow

1. User chooses a plan and clicks Subscribe → frontend calls `POST /api/subscription/checkout-session` with planId, billingCycle, successUrl, cancelUrl.
2. Backend creates a Stripe Checkout Session (one-time payment) and returns the URL.
3. User is redirected to Stripe, pays, then to successUrl (e.g. `/subscription-plans?success=1`).
4. Stripe sends `checkout.session.completed` to the webhook; backend activates the subscription and sets tenant to Active.

## Frontend

- SubscriptionPlansPage tries checkout first; if the API returns 404 or no URL (gateway not configured), it falls back to `createSubscription` (trial).
