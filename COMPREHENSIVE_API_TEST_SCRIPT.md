# Comprehensive API Test Script - HexaBill Production

## Critical Endpoints to Test

### Authentication & User
- [ ] POST /api/auth/login
- [ ] GET /api/users/me/ping
- [ ] GET /api/users/me

### Settings & Configuration  
- [ ] GET /api/admin/settings
- [ ] PUT /api/admin/settings
- [ ] GET /api/settings

### Branches & Routes
- [ ] GET /api/branches
- [ ] GET /api/branches/{id}
- [ ] GET /api/routes
- [ ] GET /api/routes/{id}

### Customers & Ledger
- [ ] GET /api/customers
- [ ] GET /api/customers/{id}
- [ ] GET /api/customers/{id}/ledger
- [ ] GET /api/customers/{id}/outstanding-invoices

### Products & Inventory
- [ ] GET /api/products
- [ ] GET /api/products/{id}
- [ ] GET /api/categories

### Sales & POS
- [ ] GET /api/sales
- [ ] POST /api/sales
- [ ] GET /api/sales/{id}

### Reports
- [ ] GET /api/reports/summary
- [ ] GET /api/reports/sales
- [ ] GET /api/reports/profit

### Alerts & Notifications
- [ ] GET /api/alerts/unread-count
- [ ] GET /api/alerts

### Dashboard
- [ ] GET /api/dashboard/summary
- [ ] GET /api/dashboard/stats

## Test Results Log

### Test Date: 2026-02-19
### Tester: AI Assistant
### Environment: Production (https://www.hexabill.company)

#### Frontend Pages Test
- [ ] Dashboard (/dashboard) - Status: ___
- [ ] Ledger (/ledger) - Status: ___
- [ ] Products (/products) - Status: ___
- [ ] POS (/pos) - Status: ___
- [ ] Purchases (/purchases) - Status: ___
- [ ] Settings (/settings) - Status: ___

#### Console Errors
- [ ] TDZ Errors: ___
- [ ] Network Errors: ___
- [ ] Other JS Errors: ___

#### Backend Errors (Render Logs)
- [ ] Settings.Value errors: ___
- [ ] FeaturesJson errors: ___
- [ ] Other 500 errors: ___

## Automated Test Script

```bash
# Test critical endpoints
curl -X GET https://hexabill.onrender.com/api/admin/settings \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -w "\nHTTP Status: %{http_code}\n"

curl -X GET https://hexabill.onrender.com/api/branches \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -w "\nHTTP Status: %{http_code}\n"

curl -X GET https://hexabill.onrender.com/api/routes \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -w "\nHTTP Status: %{http_code}\n"

curl -X GET https://hexabill.onrender.com/api/alerts/unread-count \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -w "\nHTTP Status: %{http_code}\n"

curl -X GET https://hexabill.onrender.com/api/reports/summary \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -w "\nHTTP Status: %{http_code}\n"
```
