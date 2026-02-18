# POS Page Fixes — TODO List & Implementation Plan

**Page:** PosPage.jsx  
**Backend:** SaleService.cs, SettingsController.cs  
**Priority:** HIGH (POS is core business functionality)

---

## 🔴 CRITICAL ISSUES (Fix First)

### ISSUE 1: VAT Hardcoded at 0.05 (3 places)

**Problem:**
- Lines 495, 532, 587: `vatAmount = Math.round((rowTotal * 0.05) * 100) / 100`
- VAT settings in database are ignored completely
- Client in non-VAT country gets wrong invoices
- Changing VAT% in Settings → nothing changes in POS

**Fix Required:**
- Fetch VAT percentage from Settings API (`/api/settings/company` or `VAT_PERCENT` setting)
- Store in state: `const [vatPercent, setVatPercent] = useState(5)`
- Replace all `0.05` with `vatPercent / 100`
- Update UI labels: `VAT 5%` → `VAT ${vatPercent}%`

**Files:**
- `frontend/hexabill-ui/src/pages/company/PosPage.jsx` (lines 495, 532, 587, 1757, 2170, 2212, 2379)

---

### ISSUE 2: No Payment Amount Validation

**Problem:**
- User can enter Payment Amount of 999,999 for invoice of 500
- Backend accepts it, marking sale as "Paid" with overpayment
- Overpayment alert only if `paymentAmount > grandTotal * 1.1` (10% tolerance)

**Fix Required:**
- Add validation: `if (paymentAmount > grandTotal) { show error, prevent submit }`
- Show clear error message: "Payment amount cannot exceed invoice total"
- Optionally allow overpayment with explicit confirmation

**Files:**
- `frontend/hexabill-ui/src/pages/company/PosPage.jsx` (handleSubmit function)

---

## 🟡 HIGH PRIORITY ISSUES

### ISSUE 3: No Per-Item Discount Field

**Problem:**
- Cart items have no per-item discount field
- Backend `SaleItem.Discount` field exists but always set to 0
- Only global invoice-level discount exists
- Many businesses need per-item discounts (e.g., 10% off milk, 5% off bread)

**Fix Required:**
- Add discount input field per cart item
- Update cart item structure: `{ ...item, discount: 0 }`
- Calculate: `rowTotal = (qty * unitPrice) - discount`, then VAT on rowTotal
- Update backend to save `SaleItem.Discount`

**Files:**
- `frontend/hexabill-ui/src/pages/company/PosPage.jsx` (cart structure, calculation)
- `backend/HexaBill.Api/Modules/Billing/SaleService.cs` (ensure discount is saved)

---

### ISSUE 4: Product Search Requires Minimum 1 Character

**Problem:**
- No browse/scroll all products
- Staff on mobile must always type to find products
- No scrollable product grid or category filter
- Slow connection with 500 products = typing + waiting for each search

**Fix Required:**
- Allow empty search term to show all products (paginated)
- Add "Browse All" button that shows product grid
- Add category filter dropdown
- Implement virtual scrolling for large product lists

**Files:**
- `frontend/hexabill-ui/src/pages/company/PosPage.jsx` (product search logic)
- `backend/HexaBill.Api/Modules/Products/ProductsController.cs` (ensure empty search returns all)

---

### ISSUE 5: Unit Type Dropdown Hardcoded

**Problem:**
- Unit Type dropdown values hardcoded as CRTN/PCS/KG/BOX/LTR
- Product in database has its own `UnitType`
- POS shows selector regardless, allowing accidental changes
- Makes stock calculations wrong (1 KG vs 1 CRTN)

**Fix Required:**
- Remove hardcoded dropdown, use product's `UnitType` directly
- Display unit type as read-only text (not editable)
- Only allow editing if product has multiple unit types configured

**Files:**
- `frontend/hexabill-ui/src/pages/company/PosPage.jsx` (unit type display)

---

### ISSUE 6: Edit Reason Not Shown on Invoice PDF

**Problem:**
- Edit mode for locked invoice requires Edit Reason modal
- Reason is not shown on invoice PDF
- Owner cannot see WHY invoice was edited when reviewing history

**Fix Required:**
- Store `EditReason` in Sale model (already exists)
- Update PDF template to show edit reason if present
- Format: "This invoice was edited on [date] by [user]. Reason: [reason]"

**Files:**
- `backend/HexaBill.Api/Modules/Billing/PdfService.cs` (add edit reason to PDF)
- `backend/HexaBill.Api/Models/Sale.cs` (verify EditReason field exists)

---

### ISSUE 7: Hold Invoice Uses localStorage

**Problem:**
- Hold Invoice uses localStorage
- If staff logs out or uses different browser/device, held invoice is lost
- Should be server-side for multi-device access

**Fix Required:**
- Create backend endpoint: `POST /api/sales/hold` and `GET /api/sales/hold`
- Store held invoices in database (new `HeldInvoice` table or `Sale.IsHeld` flag)
- Update frontend to use API instead of localStorage

**Files:**
- `backend/HexaBill.Api/Modules/Billing/SalesController.cs` (add hold/resume endpoints)
- `backend/HexaBill.Api/Models/HeldInvoice.cs` (new model)
- `frontend/hexabill-ui/src/pages/company/PosPage.jsx` (use API instead of localStorage)

---

## 🟢 MEDIUM PRIORITY ISSUES

### ISSUE 8: No Quick Amount Buttons

**Problem:**
- No "quick amount" buttons for common payment amounts
- UAE distribution businesses often receive round amounts (100, 500, 1000 AED)
- Must type every time

**Fix Required:**
- Add quick amount buttons: 100, 500, 1000, 2000, 5000 AED
- Clicking button sets payment amount to that value
- Show buttons near payment amount input

**Files:**
- `frontend/hexabill-ui/src/pages/company/PosPage.jsx` (UI addition)

---

### ISSUE 9: No Barcode Scan Input

**Problem:**
- No barcode scan input
- Browser-based barcode scanning via device camera is possible
- Critical for fast POS in warehouse or van sales

**Fix Required:**
- Add barcode scanner button/input
- Use library: `html5-qrcode` or `quagga` for barcode scanning
- On scan, search product by barcode/SKU
- Add to product search area

**Files:**
- `frontend/hexabill-ui/src/pages/company/PosPage.jsx` (add barcode scanner component)
- `package.json` (add barcode library dependency)

---

### ISSUE 10: No Repeat Last Invoice Button

**Problem:**
- No repeat last invoice button
- Route staff who sell same products daily want to load yesterday's invoice as template

**Fix Required:**
- Add "Repeat Last Invoice" button
- Fetch last invoice for current user/route
- Pre-fill cart with same products and quantities
- Allow editing before submitting

**Files:**
- `backend/HexaBill.Api/Modules/Billing/SalesController.cs` (add endpoint: `GET /api/sales/last`)
- `frontend/hexabill-ui/src/pages/company/PosPage.jsx` (add button and handler)

---

### ISSUE 11: No Product Image in Cart

**Problem:**
- No product image in cart
- Makes it harder to verify correct product selected

**Fix Required:**
- Display product image thumbnail in cart items
- Use `Product.ImageUrl` or placeholder if no image
- Show image next to product name

**Files:**
- `frontend/hexabill-ui/src/pages/company/PosPage.jsx` (cart item display)
- `backend/HexaBill.Api/Models/Product.cs` (verify ImageUrl field exists)

---

## ✅ GOOD PRACTICES (Keep These)

1. **Credit limit warning** shown before checkout ✅
2. **Stock validation** on submit ✅
3. **Invoice number** auto-generated ✅
4. **Staff route lock** (server-side) ✅
5. **Duplicate cash payment prevention** ✅

---

## 📋 COMPLETE TODO CHECKLIST

### Backend Changes:

- [ ] **ISSUE 1:** Verify VAT settings API endpoint works
- [ ] **ISSUE 6:** Add EditReason to PDF template
- [ ] **ISSUE 7:** Create HeldInvoice model and endpoints
- [ ] **ISSUE 10:** Add "Get Last Invoice" endpoint
- [ ] **ISSUE 11:** Verify Product.ImageUrl field exists

### Frontend Changes:

- [ ] **ISSUE 1:** Fetch VAT percentage from settings API
- [ ] **ISSUE 1:** Replace all `0.05` hardcoded values with `vatPercent / 100`
- [ ] **ISSUE 1:** Update UI labels to show dynamic VAT percentage
- [ ] **ISSUE 2:** Add payment amount validation (≤ grand total)
- [ ] **ISSUE 3:** Add per-item discount input field
- [ ] **ISSUE 3:** Update cart calculation to include per-item discounts
- [ ] **ISSUE 4:** Allow empty search term (show all products)
- [ ] **ISSUE 4:** Add "Browse All" button with product grid
- [ ] **ISSUE 5:** Remove hardcoded unit type dropdown, use product UnitType
- [ ] **ISSUE 7:** Replace localStorage with API calls for hold/resume
- [ ] **ISSUE 8:** Add quick amount buttons (100, 500, 1000, 2000, 5000)
- [ ] **ISSUE 9:** Add barcode scanner component
- [ ] **ISSUE 10:** Add "Repeat Last Invoice" button
- [ ] **ISSUE 11:** Display product images in cart

### Testing:

- [ ] **ISSUE 1:** Change VAT in Settings → verify POS uses new VAT
- [ ] **ISSUE 2:** Try payment > grand total → verify error shown
- [ ] **ISSUE 3:** Add per-item discount → verify calculation correct
- [ ] **ISSUE 4:** Empty search → verify all products shown
- [ ] **ISSUE 5:** Add product → verify unit type matches product
- [ ] **ISSUE 6:** Edit locked invoice → verify reason on PDF
- [ ] **ISSUE 7:** Hold invoice → logout → login → verify invoice still held
- [ ] **ISSUE 8:** Click quick amount → verify payment amount set
- [ ] **ISSUE 9:** Scan barcode → verify product added to cart
- [ ] **ISSUE 10:** Click repeat last → verify cart pre-filled
- [ ] **ISSUE 11:** Add product → verify image shown in cart

---

## 🎯 IMPLEMENTATION ORDER

**Phase 1 (Critical - Fix First):**
1. ISSUE 1: Fix VAT hardcoded (affects all invoices)
2. ISSUE 2: Add payment validation (prevents data corruption)

**Phase 2 (High Priority):**
3. ISSUE 3: Add per-item discount
4. ISSUE 4: Allow empty product search
5. ISSUE 5: Fix unit type dropdown
6. ISSUE 6: Show edit reason on PDF
7. ISSUE 7: Move hold invoice to server-side

**Phase 3 (Medium Priority):**
8. ISSUE 8: Add quick amount buttons
9. ISSUE 9: Add barcode scanner
10. ISSUE 10: Add repeat last invoice
11. ISSUE 11: Add product images in cart

---

**Estimated Time:**
- Phase 1: 1-2 hours
- Phase 2: 4-5 hours
- Phase 3: 3-4 hours
- **Total: 8-11 hours**

---

*Last updated: Feb 2026. Update as issues are fixed.*
