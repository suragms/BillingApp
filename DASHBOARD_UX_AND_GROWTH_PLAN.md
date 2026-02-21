# HexaBill Dashboard UX & Business Logic – Plan

**Date:** February 21, 2026  
**Purpose:** Clarify confusion, fix gaps, and plan for growth

---

## 1. Why Two Sales? (Confusion)

**Current cards:** Sales (Gross) Today, Return Value Today, Net Sales Today

| Card | Meaning |
|------|---------|
| **Sales (Gross)** | Total sales before returns |
| **Return Value** | Amount refunded/returned |
| **Net Sales** | Gross − Returns (actual revenue) |

**Recommendation:**
- Keep all three (standard accounting).
- Add tooltips: e.g. “Gross = before returns”, “Net = after returns”.
- Optionally rename for clarity: “Total Sales” + “Returns” + “Net Revenue”.

---

## 2. Purchase Report Card – Missing

**Current state:**
- DashboardTally shows: Sales, Returns, Net Sales, Expenses, Profit.
- **No “Purchases Today” card.**
- Purchases data exists in API: `summary.purchasesToday` (Dashboard.jsx uses it).

**Action:** Add a “Purchases Today” stat card on DashboardTally (same pattern as Expenses).

---

## 3. Return Button & Return Policy

**Current state:**
- “Return Value Today” card: shows amount returned.
- **No “Create Return” button** on Dashboard or Quick Actions.
- Reports → Sales Returns: view/approve/reject only.
- Backend: `returnsAPI.createSaleReturn` exists but is **never called** from any UI.

**Return policy / feature flags:**
- Backend: `returnsEnabled`, `returnsRequireApproval`.
- No visible “Return Policy” settings page; settings live in backend/tenant config.

**Actions:**
1. Add “Create Return” in Sales Ledger (on invoice row/detail) or POS.
2. Add “Returns” in Quick Actions or right panel → Reports → Sales Returns.
3. Add Return Policy section in Settings (enable/disable, require approval, damage categories).

---

## 4. Stock, Categories, Data Management

| Area | Where | How |
|------|-------|-----|
| **Product categories** | Products page | In ProductForm: category dropdown + inline “Add category” |
| **Expense categories** | Expenses page | `expensesAPI.getExpenseCategories()`, create in Expenses |
| **Stock** | Products page | Computed from transactions; “Reset Stock”, “Adjust Stock” for admins |
| **Low stock alert** | Dashboard | Card linking to Products (filter=lowstock) |

**Action:** Add a **Categories** section in Settings or Masters to manage product & expense categories in one place (optional).

---

## 5. Categories Plan – Where to Add

| Category Type | Location Today | Recommendation |
|---------------|----------------|----------------|
| **Product** | Products page (ProductForm) | Keep; consider Settings → Masters → Categories |
| **Expense** | Expenses page (via API) | Add visible “Manage categories” in Expenses header |
| **Damage (Returns)** | Returns report filters | Backend-driven; no add UI yet |

**Reports subpages:** Summary, Sales, Product, Customer, Expenses, Branch, Route, etc. – all under Reports with filters.

---

## 6. Desktop & Mobile Layout Issues

**Desktop – Report tabs outside window:**
- Reports page has 16+ tabs; uses `overflow-x-auto` + “Scroll for more”.
- On narrow desktop, tabs can overflow.

**Fixes:**
- Reduce tab label length on small screens.
- Use `flex-wrap` or a dropdown for “More reports” on small viewports.
- Ensure container has `max-w-full` and proper `overflow-x-auto`.

**Mobile – Branch/Route subpage tabs overlap:**
- BranchesPage: `branches | routes` tabs.
- On mobile, text can overlap if labels are long.

**Fixes:**
- Use `whitespace-nowrap`, `min-w-0`, `truncate`.
- Or switch to icon-only tabs on very small screens.
- Add `flex-shrink-0` and limit tab width.

---

## 7. Critical Business Logic – Priority Plan

| Priority | Item | Impact |
|----------|------|--------|
| P1 | Add Purchases Today card to DashboardTally | Completeness, user expectation |
| P2 | Add “Create Return” flow (Sales Ledger or POS) | Returns are core business flow |
| P3 | Sales Gross vs Net – add tooltips | Reduces confusion |
| P4 | Fix Reports tabs overflow (desktop) | Usability |
| P5 | Fix Branches/Routes tabs overlap (mobile) | Usability |
| P6 | Add Return Policy in Settings | Policy control |
| P7 | Add “Purchase Report” link in REPORTS panel | Navigation consistency |

---

## 8. Growth Plan

1. **Stability:** Finish fixes (500→200, toUpperCase, Cash Customer 400, etc.).
2. **Completeness:** Purchase card, Create Return, Return policy.
3. **Clarity:** Tooltips, labels, simple help text.
4. **Responsive:** Desktop overflow + mobile tab overlap.
5. **Extend:** Category management, AI insights, more reports.

---

## Quick Reference – Where Things Are

| Need | Go to |
|------|-------|
| Product categories | Products → Add/Edit Product → Category dropdown |
| Expense categories | Expenses (API: `/expenses/categories`) |
| Create return | Not yet in UI |
| Purchase report | Reports → Summary (Total Purchases) or Profit & Loss |
| Return policy | Backend tenant config (no UI yet) |
| Branches / Routes | Masters (Branches, Routes) or `/branches` |
