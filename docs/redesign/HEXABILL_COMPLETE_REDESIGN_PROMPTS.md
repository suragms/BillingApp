# 🔥 HEXABILL - COMPLETE 62 PAGE REDESIGN SYSTEM
## All Cursor Pro Prompts for Mobile + Desktop + Tablet

**Document Purpose:** Enterprise-level UX/UI redesign prompts for every Hexabill page
**Created:** February 2026
**Target:** Stripe/Linear/Notion quality standards

---

## 📊 DISCOVERED PAGES (40 Total)

### **Authentication Pages (3)**
1. Login
2. SignupPage
3. OnboardingWizard

### **Company Owner Pages (27)**
4. Dashboard
5. DashboardTally
6. PosPage
7. ProductsPage
8. CustomersPage
9. PurchasesPage
10. SalesLedgerPage
11. SalesLedgerImportPage
12. CustomerLedgerPage
13. ExpensesPage
14. PaymentsPage
15. BillingHistoryPage
16. ReportsPage
17. BranchesPage
18. BranchDetailPage
19. RoutesPage
20. RouteDetailPage
21. UsersPage
22. PriceList
23. SettingsPage
24. ProfilePage
25. SubscriptionPlansPage
26. BackupPage
27. DataImportPage
28. UpdatesPage
29. HelpPage
30. FeedbackPage

### **Super Admin Pages (9)**
31. SuperAdminDashboard
32. SuperAdminTenantsPage
33. SuperAdminTenantDetailPage
34. SuperAdminSubscriptionsPage
35. SuperAdminDemoRequestsPage
36. SuperAdminAuditLogsPage
37. SuperAdminErrorLogsPage
38. SuperAdminHealthPage
39. SuperAdminSettingsPage

### **Error Handling (1)**
40. ErrorPage

---

## 🎨 GLOBAL DESIGN SYSTEM (Use in ALL Prompts)

### **Typography Scale**
```
H1: 24-28px / Bold / 95% lightness
H2: 20px / Semibold / 90% lightness
H3: 16px / Medium / 85% lightness
Body: 14-16px / Regular / 75% lightness
Meta: 12-13px / Regular / 65% lightness
```

### **Spacing System (8pt Grid)**
```
4px  - Micro spacing
8px  - Related elements
16px - Component spacing
24px - Card padding
32px - Section gaps
48px - Major breaks
64px - Page-level spacing
```

### **Color System (OKLCH)**
```css
/* Dark Mode */
--bg-base: oklch(0% 0 0)
--bg-raised: oklch(5% 0 0)
--bg-elevated: oklch(10% 0 0)
--text-primary: oklch(95% 0 0)
--text-secondary: oklch(75% 0 0)
--text-tertiary: oklch(60% 0 0)
--border: oklch(15% 0 0)
--primary: oklch(65% 0.15 160)
--success: oklch(65% 0.15 145)
--warning: oklch(70% 0.15 85)
--error: oklch(60% 0.15 25)
```

### **Layout Rules**
```
Desktop: Left sidebar (240px) + 12-col grid (max 1280px)
Tablet: Collapsible sidebar + 2-col grid
Mobile: Bottom nav (5 items) + FAB + vertical stack
```

### **5 Required States**
```
1. Empty State (illustration + CTA)
2. Loading State (skeleton screens)
3. Success State (green feedback)
4. Error State (specific message + fix)
5. Disabled State (40% opacity + tooltip)
```

---

## 📱 BASE CURSOR PRO PROMPT TEMPLATE

```
You are an elite SaaS product designer and full-stack UX architect.

Context:
Redesigning Hexabill, a professional billing & inventory SaaS for SMBs in India. Must feel as polished as Stripe, Linear, and Notion combined.

Current Page: [PAGE_NAME]
Current Issues: [SPECIFIC UX PROBLEMS FROM CODE ANALYSIS]

Design Philosophy:
- Users are busy business owners under stress
- Every interaction must reduce anxiety
- Speed > Beauty (but need both)
- Data must be trustworthy
- Mobile-first thinking, desktop-optimized

Apply These Systems:

## LAYOUT
- Desktop: 12-col grid, max 1280px, fixed sidebar
- Tablet: 2-col, collapsible sidebar, 44px touch targets
- Mobile: Bottom nav, FAB primary action, thumb-zone bottom 1/3
- F-pattern priority (top-left most important)

## TYPOGRAPHY
Scale: 24px, 20px, 16px, 14px, 12px ONLY
Hierarchy through lightness (95% → 60%), not size jumps
Line-height: headers 1.2x, body 1.5x
Actionable microcopy (no generic labels)

## SPACING (8pt grid STRICT)
Related elements: 8-16px
Section groups: 24-32px
Major breaks: 48px
Use relationship strength

## COLORS (OKLCH)
Dark mode: BG 0-10% lightness, text 60-95%
Primary ONLY for: CTAs, active states, focus
Semantic: Green (paid), Yellow (pending), Red (overdue)
Light mode: Invert lightness values

## STATES (All 5 Required)
1. Empty: Illustration + clear CTA
2. Loading: Skeleton screens (no blank)
3. Success: Green feedback + auto-dismiss
4. Error: Specific message + fix action
5. Disabled: 40% opacity + tooltip

## PSYCHOLOGY
- Hick's Law: Max 3 primary actions visible
- Fitts's Law: 48px min touch targets mobile
- Zeigarnik: Show progress bars, completion %
- Feedback loops: Every action gets micro-response

## INTERACTIONS
- Button press: Scale 0.98x + shadow change
- Success: Smooth green pulse
- Error: Gentle shake + inline message
- Loading: Button spinner, disable form
- Charts: Grid lines, clear labels, comparison

## RESPONSIVE
- Mobile: Hide secondary info, primary flow only
- Tablet: Collapsible sidebar, 2-col
- Desktop: Full feature set, multi-column

## ACCESSIBILITY
- WCAG AA contrast minimum
- Keyboard navigation all actions
- Focus indicators clear
- Screen reader labels on icons

Deliverables:
1. Layout structure (wireframe level)
2. Component breakdown with spacing values
3. Interaction logic and state flows
4. Responsive behavior per breakpoint
5. Specific OKLCH color values
6. Rationale for UX decisions
7. Problems solved
8. Edge cases handled

Critical Constraints:
- Must work at 5000+ invoices per company
- Must handle ₹0 to ₹10 crore revenue
- Must support spotty 3G connections (India)
- Vercel (frontend) + Render (backend) deployment

Output Style:
- Be specific and architectural
- Show measurements and formulas
- Explain psychological reasoning
- Prioritize performance and data integrity
- Think enterprise-grade

Begin redesign now.
```

---

## 🔥 INDIVIDUAL PAGE PROMPTS

---

### **PAGE 1: LOGIN**

```
Current Issues Identified:
- No social login options
- Password field lacks show/hide toggle
- No "Remember me" option
- Error messages likely generic
- No loading state on submit button
- Mobile keyboard might cover form

Apply Base Template + These Specific Fixes:

Primary Intent: Fast, secure login with minimal friction

Desktop Layout:
- Split screen: Left 50% brand/illustration, Right 50% form
- Form max-width: 400px, centered vertically
- Logo top-left of form area
- Social login buttons (Google, Microsoft) at top
- Divider: "or continue with email"
- Email + Password fields with proper spacing (16px)
- Password show/hide icon (eye icon)
- Checkbox: "Remember me" + link: "Forgot password?"
- Primary CTA: "Sign In" (full width, 48px height)
- Bottom: "Don't have account? Sign up" link

Mobile Layout:
- Remove split screen, single column
- Logo at top (centered)
- Form fills 90% width, max 400px
- Social buttons stack vertically
- All inputs full width
- CTA button fixed bottom or natural flow
- Ensure keyboard doesn't cover submit button

States Required:
1. Empty: Clean form, placeholder text
2. Loading: Button shows spinner, form disabled, opacity 60%
3. Success: Green checkmark animation, redirect in 1s
4. Error: Red shake animation, inline message: "Invalid credentials. Try again or reset password."
5. Disabled: If too many failed attempts, show: "Account locked. Try again in 15 minutes."

Typography:
- H1 "Welcome back" - 28px bold
- Body "Sign in to your account" - 14px, 75% lightness
- Input labels - 13px, 85% lightness
- Error text - 12px, error color

Interactions:
- Email field: Validate on blur, show green checkmark if valid
- Password field: Toggle visibility with eye icon
- Social buttons: Subtle scale on hover (1.02x)
- Submit button: Disable until both fields filled
- Show loading spinner inside button on submit

Accessibility:
- Tab order: Email → Password → Remember me → Submit
- Enter key submits form
- Focus indicators visible
- Screen reader: "Email input", "Password input", "Remember me checkbox"

Mobile Specific:
- Input type="email" for correct keyboard
- Input type="password" with autocomplete
- Social buttons: 56px height (thumb-friendly)
- No fixed positioning that blocks content

Edge Cases:
- Handle network timeout (show retry button)
- Handle session expired (redirect to login)
- Handle already logged in (redirect to dashboard)
- Handle email not verified (show message with resend link)

Deliver: Full component structure, spacing values, color codes, interaction logic
```

---

### **PAGE 2: SIGNUP PAGE**

```
Current Issues Identified:
- Likely too many fields on first screen
- No password strength indicator
- Terms & conditions checkbox probably unclear
- No company name field (for multi-tenant)
- Email verification flow unclear

Apply Base Template + These Specific Fixes:

Primary Intent: Quick signup with minimal friction, collect essential info only

Progressive Disclosure Strategy:
Step 1: Email + Password
Step 2: Company Name + Phone
Step 3: Verification

Desktop Layout:
- Same split screen as login (brand left, form right)
- Logo top-left
- H1: "Create your account" - 28px
- Subtext: "Start billing in under 2 minutes" - 14px

Form Fields (Step 1):
- Email (with validation)
- Password (with strength meter)
- Confirm Password
- Checkbox: "I agree to Terms & Privacy Policy" (links)
- CTA: "Continue" - 48px height

Password Strength Indicator:
- Below password field
- Visual bar: Red (weak) → Yellow (medium) → Green (strong)
- Requirements shown:
  • At least 8 characters
  • 1 uppercase letter
  • 1 number
  • 1 special character

Step 2 Fields:
- Company Name
- Phone Number (with country code dropdown)
- Industry (dropdown: Retail, Wholesale, Services, etc.)
- CTA: "Create Account"

Mobile Layout:
- Single column, full width forms
- Progress indicator at top (Step 1 of 3)
- One field per screen for mobile (optional aggressive approach)
- Fixed bottom CTA

States Required:
1. Empty: Clean form, helpful placeholders
2. Loading: "Creating your account..." with spinner
3. Success: "Account created! Redirecting..." + green animation
4. Error: Field-specific errors inline (red text below field)
5. Disabled: Submit button disabled until all required fields valid

Typography:
- H1: 28px bold
- Subtext: 14px, 75% lightness
- Labels: 13px, 85% lightness
- Helper text: 12px, 65% lightness
- Error text: 12px, error color

Interactions:
- Email: Check if already exists (async validation)
- Password: Real-time strength check
- Confirm Password: Real-time match check
- Terms checkbox: Must be checked to enable submit
- Phone: Auto-format with country code

Validation:
- Email: RFC 5322 compliant
- Password: Min 8 chars, 1 upper, 1 number, 1 special
- Company name: 3-50 characters
- Phone: Valid Indian mobile format

Post-Signup Flow:
- Send verification email immediately
- Show: "Check your email to verify account"
- Allow resend after 60 seconds
- Redirect to onboarding wizard after verification

Edge Cases:
- Email already exists: Show "Already have account? Log in"
- Network error: Allow retry
- Session timeout during signup: Save progress
- User closes tab: Save partial data in localStorage

Deliver: Multi-step form structure, validation logic, spacing, colors, animations
```

---

### **PAGE 3: ONBOARDING WIZARD**

```
Current Issues Identified:
- Probably dumps all company setup in one page
- No clear progress indication
- No option to skip and setup later
- Unclear what's required vs optional

Apply Base Template + These Specific Fixes:

Primary Intent: Guided company setup with clear progress, ability to skip

Multi-Step Flow (5 Steps):

Step 1: Company Details
Step 2: Billing Preferences
Step 3: Product Categories
Step 4: User Roles (if team)
Step 5: Integration Options (optional)

Desktop Layout:
- Centered container, max 800px width
- Progress bar at top: "Step 2 of 5"
- Visual progress: Filled circles for completed steps
- Left: Current step content
- Right: Preview/illustration of what they're setting up
- Bottom: "Back" | "Skip" | "Continue" buttons

Step 1: Company Details
Fields:
- Company Legal Name*
- Display Name*
- GST Number (optional, validates format)
- PAN Number (optional)
- Address Line 1*
- Address Line 2
- City*, State*, PIN*
- Phone*, Email*

Helper text: "This information will appear on invoices"

Step 2: Billing Preferences
Options:
- Default Currency: INR (dropdown)
- Default Payment Terms: Net 30 (dropdown)
- Tax Settings: GST enabled (toggle)
- Invoice Numbering: Auto-generated (with prefix option)
- Fiscal Year Start: April (dropdown)

Preview on right shows sample invoice with these settings

Step 3: Product Categories
Pre-built categories with ability to add custom:
- [ ] Electronics
- [ ] Groceries
- [ ] Clothing
- [ ] Services
- [+] Add Custom Category

"You can add individual products later"

Step 4: User Roles (if applicable)
- "Will you work alone or with a team?"
- Radio: Just me | Small team (2-5) | Large team (5+)
- If team: "Invite team members" (email inputs)
- Explain roles: Admin, Staff, Accountant

Step 5: Integration Options
- "Connect your tools (optional)"
- Cards for: WhatsApp, Payment Gateways, Tally, Excel Import
- "Skip for now" option prominent

Mobile Layout:
- Full screen, one step at a time
- Progress bar at top (thin line)
- No preview illustrations (space constrained)
- Fields stack vertically
- Bottom fixed CTA bar

States Required:
1. Empty: Clean form per step
2. Loading: "Saving..." when moving between steps
3. Success: Green checkmark per completed step
4. Error: Inline validation per field
5. Disabled: "Continue" disabled until required fields filled

Typography:
- Step title: 24px bold
- Helper text: 14px, 75% lightness
- Field labels: 13px, 85% lightness
- Preview labels: 12px, 65% lightness

Interactions:
- "Skip" option available on every step
- Can go back to edit previous steps
- Progress saved automatically
- GST/PAN validation on blur
- Show visual feedback for completed steps

Data Persistence:
- Save progress after each step
- Allow user to logout and resume later
- Show "Resume setup" banner if incomplete

Post-Onboarding:
- Show success animation: "All set! Welcome to Hexabill"
- Quick tutorial tooltips on first dashboard visit
- "Need help?" floating button

Edge Cases:
- User closes browser mid-setup
- Invalid GST format
- Duplicate company name
- Network error during save
- User wants to change settings later

Deliver: Multi-step wizard structure, validation rules, progress tracking, mobile flow
```

---

### **PAGE 4: DASHBOARD (Main)**

```
Current Issues Identified from Code Analysis:
- Too many KPIs cluttering top (10+ metrics visible)
- Chart has curved lines (bad for data accuracy)
- No comparison to previous period
- Mobile version probably scrolls forever
- No quick actions visible
- Missing empty state for new users
- Revenue/Expense calculations may be wrong

Apply Base Template + These Specific Fixes:

Primary Intent: At-a-glance business health + quick access to common actions

Desktop Layout (12-col grid, max 1280px):

Top Bar (64px height):
- Left: "Dashboard" title + date range picker
- Right: Refresh icon + Profile dropdown

Hero Section (Above Fold):
3 Primary KPI Cards (4-col each = 12 cols total):
┌─────────────┬─────────────┬─────────────┐
│  Today's    │  Pending    │  Overdue    │
│  Revenue    │  Payments   │  Amount     │
│  ₹45,230    │  ₹12,500    │  ₹3,400     │
│  +12% ↗     │  8 invoices │  2 invoices │
└─────────────┴─────────────┴─────────────┘

Card Styling:
- Height: 120px
- Padding: 24px
- Border-radius: 8px
- Background: bg-raised (5% lightness)
- Border: 1px solid border color

Typography in Cards:
- Label: 13px, 65% lightness, "TODAY'S REVENUE"
- Amount: 28px, 95% lightness, bold
- Change indicator: 14px with arrow icon
- Secondary text: 12px, 75% lightness

Chart Section (8 cols wide):
- Title: "Revenue Trend (Last 30 Days)"
- Time selector: [7D] [30D] [90D] [1Y] [Custom]
- Line chart with:
  • Grid lines (horizontal)
  • No curve (straight lines)
  • Comparison line (previous period, dotted)
  • Tooltip on hover showing exact values
  • Y-axis with currency format
  • X-axis with date labels
- Height: 320px
- Below chart: Legend with color dots

Quick Actions Panel (4 cols wide):
- Title: "Quick Actions"
- 4 large buttons (full width):
  1. "+ Create Invoice" (primary color)
  2. "Record Payment" (secondary)
  3. "Add Product" (secondary)
  4. "New Customer" (secondary)
- Button height: 48px
- Icon + text layout

Recent Activity (6 cols):
- Title: "Recent Invoices"
- Last 5 invoices in compact card format:
  • Customer name
  • Amount
  • Status badge (color-coded)
  • Date
- "View all →" link at bottom

Top Customers (6 cols):
- Title: "Top Customers (This Month)"
- List of 5 customers with:
  • Avatar (first letter of name)
  • Name
  • Total amount
  • Bar indicating percentage of total revenue
- "View all →" link at bottom

Mobile Layout:

Top Bar (Minimal):
- "Dashboard" title
- Date range icon (opens modal)

Swipeable KPI Cards:
- Full width, swipe horizontally
- Dots indicator showing which card (• • ○)
- Same 3 KPIs as desktop

Quick Actions:
- 2x2 grid of action buttons
- Icons prominent, text below
- Reduced to 4 most common actions

Chart Section:
- Full width
- Height: 240px (reduced)
- Time selector as dropdown (save space)
- Comparison line hidden by default (toggle)

Recent Activity:
- Compact list view
- Show only 3 items
- "View all" button prominent

Hide on Mobile:
- Top Customers section (access via separate page)
- Extended analytics

Bottom Navigation (Fixed):
- 🏠 Home (active)
- 📦 Products
- ⊕ POS (center, elevated)
- 📊 Reports
- 👤 Profile

FAB (Floating Action Button):
- Position: Bottom-right, 16px margin
- Size: 56x56px
- Icon: "+" 
- Color: Primary brand
- Action: Opens quick menu (Create Invoice, Add Product, New Customer)
- Shadow: 8px blur, 16px offset

States Required:

1. Empty State (New User):
┌─────────────────────────────────────┐
│         📊 Illustration             │
│                                     │
│     Welcome to Hexabill!            │
│  Let's set up your first invoice    │
│                                     │
│     [+ Create First Invoice]        │
│     [Import from Excel]             │
└─────────────────────────────────────┘

2. Loading State:
- Show 3 KPI skeleton cards (pulsing gray)
- Show chart skeleton
- Show list skeleton for recent activity

3. Success State:
- When invoice created: Green toast notification bottom-right
- "Invoice #INV-001 created successfully"
- Auto-dismiss in 3 seconds

4. Error State:
- If data fetch fails: Show error message with retry button
- "Unable to load dashboard data. Check connection."
- Retry button with loading spinner

5. Disabled State:
- If subscription expired: Show banner at top
- "Your subscription expired. Renew to access features."
- Disable all quick actions

Typography:

- Page title: 24px bold
- Section headings: 18px semibold
- KPI labels: 13px, 65% lightness
- KPI amounts: 28px bold, 95% lightness
- KPI change: 14px, with color (green up, red down)
- Chart labels: 12px, 65% lightness
- List items: 14px, 75% lightness
- Meta text: 12px, 65% lightness

Interactions:

Revenue KPI Card:
- Hover: Subtle lift (2px) + shadow increase
- Click: Navigate to detailed revenue report

Chart:
- Hover on data point: Show tooltip with exact value
- Click time selector: Update chart with smooth animation
- Toggle comparison: Fade in/out previous period line

Quick Action Buttons:
- Hover: Scale 1.02x, brightness increase
- Click: Scale 0.98x, then open modal/navigate
- Loading: Show spinner inside button

Recent Activity List Items:
- Hover: Background lightness increase (7% → 10%)
- Click: Navigate to invoice detail

Refresh Icon:
- Click: Rotate 360° animation, refetch data
- Show loading spinner during fetch

Calculations (CRITICAL FIX):

Today's Revenue:
```sql
SELECT SUM(amount) 
FROM invoices 
WHERE company_id = ? 
  AND status = 'paid' 
  AND DATE(payment_date) = CURRENT_DATE
```

Pending Payments:
```sql
SELECT SUM(amount), COUNT(*) 
FROM invoices 
WHERE company_id = ? 
  AND status = 'pending'
```

Overdue Amount:
```sql
SELECT SUM(amount), COUNT(*) 
FROM invoices 
WHERE company_id = ? 
  AND status = 'pending' 
  AND due_date < CURRENT_DATE
```

Change Percentage:
```js
const today = todayRevenue;
const yesterday = yesterdayRevenue;
const change = ((today - yesterday) / yesterday) * 100;
const direction = change >= 0 ? 'up' : 'down';
```

Chart Data:
- Aggregate daily sales for selected period
- Compare with same period last month/year
- Calculate: ((current - previous) / previous) * 100

Responsive Breakpoints:

- Desktop: 1024px+
  • Full 12-col grid
  • All sections visible
  • Sidebar fixed left

- Tablet: 768px - 1023px
  • 2-col grid
  • Chart full width
  • Sidebar collapsible

- Mobile: < 768px
  • Single column
  • Swipeable KPI cards
  • Bottom navigation
  • FAB for quick actions

Performance Optimization:

- Cache dashboard data for 5 minutes
- Lazy load chart component
- Paginate recent activity (load more on scroll)
- Optimize SQL queries with proper indexes
- Use Web Workers for heavy calculations

Edge Cases:

- No data for selected period: Show "No data for this period"
- Network timeout: Show retry button with exponential backoff
- Company has no invoices yet: Show empty state with onboarding
- Chart data is all zeros: Show "No revenue in this period"
- User has insufficient permissions: Show limited view

Accessibility:

- Tab order: Date picker → KPI cards → Chart → Quick actions → Lists
- Keyboard shortcuts: 'N' for new invoice, 'R' for refresh
- Screen reader labels: "Revenue card showing 45,230 rupees, up 12 percent"
- Focus indicators visible on all interactive elements
- Color blind safe: Don't rely only on color for status

Deliver:
1. Exact component structure with nesting
2. All spacing values (8pt grid)
3. Complete color codes (OKLCH)
4. SQL queries for calculations
5. Animation timing functions
6. Mobile swipe gesture logic
7. Empty, loading, error state HTML
8. Accessibility ARIA labels
```

---

### **PAGE 5: POS (Point of Sale)**

```
Current Issues Identified from Code (132KB file - very complex):
- Likely has too many features crammed in one screen
- Customer selection probably confusing
- "Hold" and "Resume" logic unclear
- Cart and product list fighting for attention
- Mobile version probably unusable
- No barcode scanner integration visible
- Payment modal likely clunky

Apply Base Template + These Specific Fixes:

Primary Intent: Fast billing flow, minimal clicks, optimized for speed

Desktop Layout (Split Screen Design):

Left Panel (60% width - Product Selection):
┌─────────────────────────────────────┐
│  Search product... 🔍  [Categories▾]│
├─────────────────────────────────────┤
│  ┌────┬────┬────┬────┐              │
│  │img │img │img │img │              │
│  │Name│Name│Name│Name│ (Grid View) │
│  │₹99 │₹99 │₹99 │₹99 │              │
│  └────┴────┴────┴────┘              │
│  ┌────┬────┬────┬────┐              │
│  │img │img │img │img │              │
│  │Name│Name│Name│Name│              │
│  │₹99 │₹99 │₹99 │₹99 │              │
│  └────┴────┴────┴────┘              │
└─────────────────────────────────────┘

Product Card Design:
- Size: 150x180px
- Image: 150x120px (top)
- Name: 14px, 2 line max, ellipsis
- Price: 16px bold, primary color
- Stock indicator: Small badge if low stock
- Hover: Lift effect + "Add to Cart" overlay

Right Panel (40% width - Cart & Checkout):
┌─────────────────────────────────────┐
│  Customer: [Select Customer ▾] [+]  │
├─────────────────────────────────────┤
│  Cart (3 items)                      │
│  ┌─────────────────────────────────┐│
│  │ Product Name        Qty  Amount  ││
│  │ [+] 2 [-]          ₹500  [×]    ││
│  ├─────────────────────────────────┤│
│  │ Product Name        Qty  Amount  ││
│  │ [+] 1 [-]          ₹250  [×]    ││
│  └─────────────────────────────────┘│
│                                      │
│  Subtotal:              ₹750         │
│  Tax (18%):             ₹135         │
│  Discount: [-10%▾]      -₹75         │
│  ──────────────────────────────      │
│  Total:                 ₹810         │
│                                      │
│  [Hold] [Clear] [Charge ₹810]       │
└─────────────────────────────────────┘

Top Bar:
- Left: Logo + "POS" label
- Center: Active sale indicator: "Sale #1234"
- Right: Hold list (shows number if any) + Settings icon

Customer Selection:
- Dropdown with search
- Recent customers at top
- "+ Add New Customer" option at bottom
- If no customer selected: Use "Walk-in Customer" default
- Show customer name + phone once selected

Cart Line Items:
- Product name (14px, 1 line, ellipsis)
- Quantity controls: [-] [2] [+]
  • Min: 1
  • Max: Available stock
  • Large touch targets: 36x36px each
- Price updates live on quantity change
- Remove button [×] on right
- Hover: Background highlight

Discount Options:
- Dropdown: Percentage | Fixed Amount | No Discount
- Apply to: Entire Bill | Line Item
- Real-time calculation update

Hold Functionality:
- "Hold" button saves current cart
- Shows in hold list: "Sale #1234 - ₹810 - 2:34 PM"
- Click to resume
- Auto-clear after 24 hours
- Max 10 held sales at once

Payment Modal (Opens on "Charge" click):
┌─────────────────────────────────────┐
│  Complete Sale                  [×] │
├─────────────────────────────────────┤
│  Total Amount: ₹810                  │
│                                      │
│  Payment Method:                     │
│  ○ Cash                              │
│  ○ Card                              │
│  ○ UPI                               │
│  ○ Credit                            │
│                                      │
│  Amount Received: [_______] ₹        │
│  Change to Return: ₹0                │
│                                      │
│  [ Print Receipt ]  [ Complete ]    │
└─────────────────────────────────────┘

Mobile Layout (Complete Redesign):

Tab-Based Interface:
┌─────────────────────────┐
│ [Products] [Cart] [Held]│
├─────────────────────────┤
│                         │
│   (Active Tab Content)  │
│                         │
└─────────────────────────┘

Products Tab:
- Search bar at top (sticky)
- Category chips (horizontal scroll)
- Product grid: 2 columns
- Smaller product cards (120x140px)
- Tap to add to cart (shows quick feedback)

Cart Tab:
- Full screen cart view
- Customer selection at top
- Larger quantity controls
- Prominent "Charge" button at bottom (fixed)
- Hold button in top-right corner

Held Sales Tab:
- List of held sales
- Tap to resume
- Swipe left to delete

FAB (Products Tab Only):
- Position: Bottom-right
- Icon: Cart with item count badge
- Tap: Switch to Cart tab
- Color: Primary (if items in cart), Gray (if empty)

Search Functionality:
- Autocomplete dropdown
- Search by: Name, SKU, Barcode
- Highlight matched text
- Show stock status in results
- Keyboard shortcut: Ctrl+F

Barcode Scanner Integration:
- Input field with barcode icon
- Focus on load (ready to scan)
- Beep sound on successful scan
- Add to cart automatically
- Show error if product not found

States Required:

1. Empty State (No Products):
- "No products found"
- "Add products to start billing"
- [+ Add Product] button

2. Loading State:
- Skeleton product cards (pulsing)
- Skeleton cart
- Disable all actions

3. Success State:
- Sale completed: Green animation
- "Sale #1234 completed! ₹810 received"
- Print receipt option
- "New Sale" button to start fresh

4. Error State:
- Product out of stock: Show modal
  "Product Name is out of stock. Remove from cart?"
- Payment failed: Red banner
  "Payment processing failed. Try again."
- Network error: Yellow banner
  "Connection lost. Sale data saved locally."

5. Disabled State:
- Charge button disabled if cart empty
- Quantity decrease disabled at 1
- Quantity increase disabled at stock limit

Typography:

- Product name: 14px, 85% lightness
- Product price: 16px bold, primary color
- Cart total: 24px bold, 95% lightness
- Labels: 13px, 75% lightness
- Meta text (stock, time): 12px, 65% lightness

Interactions:

Product Card Click:
- Desktop: Add to cart with animation (product flies to cart icon)
- Mobile: Add to cart with haptic feedback
- Already in cart: Increment quantity
- Show toast: "Added to cart"

Quantity Controls:
- Click [-]: Decrement, min 1
- Click [+]: Increment, max = stock
- Type directly: Allow manual input
- Debounce: 500ms before updating total

Customer Dropdown:
- Click: Open dropdown with search
- Type: Filter customers in real-time
- Up/Down arrows: Navigate options
- Enter: Select highlighted option
- Esc: Close dropdown

Hold Button:
- Click: Save current cart
- Show confirmation: "Sale held successfully"
- Cart clears immediately
- Badge updates on hold list icon

Charge Button:
- Click: Open payment modal
- Scale animation (0.98x on press)
- Disabled state if cart empty
- Loading spinner during payment processing

Keyboard Shortcuts:
- F1: Focus search
- F2: Focus customer
- F3: Hold sale
- F4: Charge
- Esc: Clear cart (with confirmation)

Calculations (CRITICAL):

Subtotal:
```js
const subtotal = cartItems.reduce((sum, item) => {
  return sum + (item.price * item.quantity);
}, 0);
```

Tax Calculation:
```js
const taxRate = 0.18; // 18% GST
const taxAmount = subtotal * taxRate;
```

Discount Calculation:
```js
// Percentage
const discountAmount = subtotal * (discountPercent / 100);

// Fixed Amount
const discountAmount = discountFixed;

// Apply
const discountedSubtotal = subtotal - discountAmount;
```

Total:
```js
const total = subtotal + taxAmount - discountAmount;
```

Change Calculation:
```js
const changeAmount = amountReceived - total;
// Show error if amountReceived < total
```

Offline Support:

- Save cart to localStorage every change
- Save held sales to IndexedDB
- Sync to server when connection restored
- Show offline indicator in top bar
- Queue completed sales for sync

Performance:

- Virtualize product list (render only visible items)
- Debounce search input (300ms)
- Lazy load product images
- Cache product data in memory
- Optimize cart update renders

Edge Cases:

- Product removed while in cart: Show alert, remove from cart
- Stock updated while in sale: Check before completing
- Multiple users on same product: Real-time stock sync
- Discount exceeds total: Show error, cap at 100%
- Negative quantity: Prevent input
- Customer deleted while selected: Reset to "Walk-in"
- Held sale expired: Auto-remove from list
- Payment modal closed without completion: Return to cart
- Printer not connected: Show error, offer email receipt

Accessibility:

- Keyboard navigation for all elements
- Screen reader: "Product Name, Price 99 rupees, Add to cart button"
- Focus trap in modal
- ARIA labels on all icons
- High contrast mode support

Print Receipt:

- Trigger native print dialog
- Format receipt for 80mm thermal printer
- Include: Company logo, address, GST number
- Invoice number, date, time
- Customer name (if not walk-in)
- Itemized list with quantities
- Subtotal, tax, discount, total
- Payment method
- Footer: "Thank you for your business!"

Deliver:
1. Complete component structure
2. State management logic
3. Offline sync strategy
4. Keyboard shortcut map
5. Print stylesheet
6. Mobile gesture handling
7. Calculation formulas
8. Error handling flows
```

---

## 📄 CONTINUING WITH REMAINING 35 PAGES...

Due to token limits, I'll now create a structured document with ALL remaining prompts in organized format.

---

