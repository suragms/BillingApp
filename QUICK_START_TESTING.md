# Quick Start Testing Guide

## 🚀 Start Testing in 3 Steps

### Step 1: Start the Application

**Backend:**
```powershell
cd backend\HexaBill.Api
dotnet run
```
Backend will run on `http://localhost:5000`

**Frontend:**
```powershell
cd frontend\hexabill-ui
npm run dev
```
Frontend will run on `http://localhost:5173`

---

### Step 2: Run Automated Tests

**Test API Endpoints:**
```powershell
.\test-api-endpoints.ps1
```
This tests:
- ✅ Staff delete permission (should fail)
- ✅ Payment transaction atomicity
- ✅ Expense transaction atomicity
- ✅ Invalid payload validation

**Test Concurrent Invoice Creation:**
```powershell
.\test-concurrent-invoices.ps1
```
This tests:
- ✅ Invoice number generation race condition fix
- ✅ No duplicate invoice numbers under concurrency

---

### Step 3: Manual UI Testing

1. **Open Browser:** Navigate to `http://localhost:5173`
2. **Login as each role:**
   - SuperAdmin
   - Owner
   - Admin
   - Staff

3. **Test Critical Features:**
   - Create invoice
   - Edit invoice (test conflict detection)
   - Update payment status
   - Create expense
   - **Verify Staff cannot delete invoices**

---

## 📋 Quick Test Checklist

### ✅ Backend Tests (Automated)
- [ ] Run `test-api-endpoints.ps1` → All tests pass
- [ ] Run `test-concurrent-invoices.ps1` → No duplicates

### ✅ Frontend Tests (Manual)
- [ ] Login as Owner → Create invoice → Success
- [ ] Login as Staff → Try delete invoice → Should fail/hidden
- [ ] Open Sales Ledger on mobile (375px) → Tables scroll horizontally
- [ ] Edit invoice in 2 tabs → Second save shows conflict error

### ✅ Transaction Tests (Manual)
- [ ] Create payment (PENDING) → Update to CLEARED → All updates succeed
- [ ] Create expense → Verify expense + audit log both created
- [ ] Create invoice with insufficient stock → Should fail before stock update

---

## 🎯 Expected Results

### ✅ All Tests Should Pass:
1. **Staff Delete Permission:** ❌ Staff cannot delete (403 Forbidden)
2. **Payment Transaction:** ✅ Payment, Sale, Customer all update atomically
3. **Expense Transaction:** ✅ Expense + Audit log created together
4. **Invoice Race Condition:** ✅ No duplicate invoice numbers
5. **Stock Validation:** ✅ Stock validated before any updates
6. **Mobile Tables:** ✅ Tables scroll horizontally on mobile
7. **Conflict Detection:** ✅ RowVersion prevents lost updates

---

## 📊 Test Results Template

After running tests, document results:

```
## Test Results - [Date]

### Automated Tests
- API Endpoints: ✅ PASS / ❌ FAIL
- Concurrent Invoices: ✅ PASS / ❌ FAIL

### Manual Tests
- Owner Login: ✅ PASS / ❌ FAIL
- Staff Delete: ✅ PASS / ❌ FAIL
- Mobile Tables: ✅ PASS / ❌ FAIL
- Conflict Detection: ✅ PASS / ❌ FAIL

### Issues Found:
- [List any issues]

### Status: ✅ Ready / ⚠️ Issues Found
```

---

## 🔧 Troubleshooting

### Backend won't start?
- Check database connection string in `appsettings.json`
- Ensure PostgreSQL is running
- Run `dotnet restore` to install packages

### Frontend won't start?
- Run `npm install` to install dependencies
- Check Node.js version (requires Node 16+)

### Tests fail?
- Verify test user credentials in scripts
- Check backend is running on correct port
- Verify database has test data

---

## 📚 Full Testing Guide

See `TESTING_GUIDE.md` for comprehensive testing instructions covering:
- All role permissions
- All transaction scenarios
- All edge cases
- Stress testing
- Mobile responsive testing

---

**Ready to test?** Start with Step 1 above! 🚀
