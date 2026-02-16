# 🚀 HEXABILL MASTER CURSOR PROMPT GENERATOR

## HOW TO USE THIS SYSTEM

### For Each Page You Want to Redesign:

1. **Copy the Base Template below**
2. **Replace [PAGE_NAME] with actual page (e.g., "Dashboard")**
3. **Add Current Issues from your code analysis**
4. **Paste into Cursor and let it work**

---

## 📋 MASTER CURSOR PRO PROMPT TEMPLATE

```
You are an elite SaaS product designer and full-stack UX architect with expertise in billing systems, inventory management, and Indian business workflows.

Context:
Redesigning Hexabill, a professional billing & inventory SaaS for Indian SMBs. Must feel as polished as Stripe, Linear, and Notion combined. The app is currently deployed on Vercel (frontend) and Render (backend) with PostgreSQL database.

Current Page: [PAGE_NAME]

Current Issues Identified:
[LIST SPECIFIC PROBLEMS - e.g.:
- Too many KPIs (10+) cluttering the interface
- Charts using curved lines (bad for data accuracy)
- No empty states for new users
- Mobile navigation broken
- Calculations wrong (revenue including pending invoices)
- No loading states (shows blank screen during fetch)
]

Target Users:
- Small business owners in India
- Age: 25-55
- Tech comfort: Medium
- Primary language: English (some Hindi)
- Usage: Daily for billing, weekly for reports
- Devices: 60% mobile, 40% desktop
- Connection: Often spotty 3G in tier-2/3 cities

Design Philosophy:
- Users are busy business owners under stress
- Every interaction must reduce anxiety, not add to it
- Speed > Beauty (but we need both)
- Data must be trustworthy (calculations perfect)
- Mobile-first thinking, desktop-optimized
- Support Indian business practices (GST, credit terms, etc.)

---

## MANDATORY DESIGN SYSTEMS TO APPLY

### LAYOUT SYSTEM

Desktop (1024px+):
- Fixed left sidebar: 240px width
- Top bar: 64px height, contextual actions
- Main content: 12-column grid, max-width 1280px
- Content padding: 24px on all sides
- Section spacing: 32-48px between major sections
- F-pattern priority: Top-left = most important

Tablet (768px - 1023px):
- Collapsible sidebar (icon-only or hidden)
- 2-column grid layout
- Touch targets: Minimum 44px
- Increased spacing: 1.2x desktop values

Mobile (< 768px):
- No sidebar
- Bottom navigation: 5 items maximum
- Floating Action Button (FAB): Primary action
- Thumb zone rule: All primary actions bottom 1/3 of screen
- Tap targets: Minimum 48px
- Single column, vertical stack
- Full-width cards with 16px margins

### TYPOGRAPHY SYSTEM

Font Scale (STRICT - No other sizes):
```css
--h1: 24-28px / Bold / 95% lightness / line-height 1.2
--h2: 20px / Semibold / 90% lightness / line-height 1.2
--h3: 16px / Medium / 85% lightness / line-height 1.3
--body: 14-16px / Regular / 75% lightness / line-height 1.5
--meta: 12-13px / Regular / 65% lightness / line-height 1.4
```

Hierarchy Rules:
- Use COLOR lightness for hierarchy, not just size
- Darker text = less important
- Never use more than 3 weights per page
- Line-height increases readability more than size

Microcopy Standards:
- Actionable verbs: "Send Invoice" not "Submit"
- Specific: "Record Payment" not "Save"
- Clear: "Download PDF" not "Export"
- User-focused: "Your customers" not "Customers"

### SPACING SYSTEM (8pt Grid - SACRED)

Base Unit: 8px

Scale:
```
4px  - Micro spacing (icon padding, tight layouts)
8px  - Related elements (label to input, icon to text)
16px - Component internal spacing
24px - Card internal padding
32px - Section spacing (between logical groups)
48px - Major section breaks
64px - Page-level spacing
```

Relationship Strength Rule:
```
Stronger relationship = Closer spacing
Weaker relationship = More spacing

Example: Invoice Card
├─ Invoice # + Date: 8px (same invoice)
├─ Date + Amount: 16px (related but different types)
├─ Amount + Status: 24px (different contexts)
└─ Card to Card: 32px (separate invoices)
```

Never Eyeball - Use Formula:
- Same purpose: 8-16px
- Same group: 24px
- Different sections: 32-48px

### COLOR SYSTEM (OKLCH Format)

Dark Mode (Default):
```css
/* Backgrounds */
--bg-base: oklch(0% 0 0)         /* Pure black base */
--bg-raised: oklch(5% 0 0)       /* Cards, surfaces */
--bg-elevated: oklch(10% 0 0)    /* Modals, dropdowns, hover states */

/* Text */
--text-primary: oklch(95% 0 0)   /* Headings, important text */
--text-secondary: oklch(75% 0 0) /* Body text, labels */
--text-tertiary: oklch(60% 0 0)  /* Meta text, de-emphasized */

/* Borders */
--border: oklch(15% 0 0)         /* Subtle borders */
--border-highlight: oklch(25% 0 0) /* Focus, active states */

/* Brand/Primary (Use Sparingly) */
--primary: oklch(65% 0.15 160)   /* Green/Teal - CTA buttons only */
--primary-hover: oklch(70% 0.15 160)

/* Semantic */
--success: oklch(65% 0.15 145)   /* Paid status, confirmations */
--warning: oklch(70% 0.15 85)    /* Pending, alerts */
--error: oklch(60% 0.15 25)      /* Overdue, errors, destructive */
```

Light Mode (Auto-generate by inverting lightness):
```css
--bg-base: oklch(100% 0 0)
--bg-raised: oklch(97% 0 0)
--bg-elevated: oklch(94% 0 0)
--text-primary: oklch(15% 0 0)
--text-secondary: oklch(35% 0 0)
--text-tertiary: oklch(50% 0 0)
```

Usage Rules:
Primary color ONLY for:
- Primary CTA buttons
- Active navigation state
- Form focus states
- Progress indicators

NEVER use primary for:
- Large backgrounds
- Body text
- Decorative elements
- More than one button per section

### STATE SYSTEM (All 5 Required for Every Page)

1. Empty State:
```
When: No data exists yet
Show:
├─ Illustration (simple, on-brand, not clipart)
├─ Clear message: "No [items] yet"
├─ Helpful subtext: Why this matters
├─ Primary action: "+ Create First [Item]"
└─ Optional: Secondary action (Import, Learn More)

Typography:
├─ Heading: H2 (20px)
├─ Subtext: Body (14px)
└─ Centered layout
```

2. Loading State (NEVER SHOW BLANK SCREEN):
```
When: Data is fetching, action is processing
Show:
├─ Skeleton screens (ghost UI matching final layout)
├─ Subtle pulse animation (1s duration)
├─ Maintain exact layout structure
└─ For buttons: Spinner inside button, disable form

Never:
├─ Generic spinner in center of blank screen
├─ "Loading..." text without context
└─ Different layout than final state
```

3. Success State:
```
When: Action completes successfully
Show:
├─ Green checkmark icon (subtle, not aggressive)
├─ Success message: "[Item] created successfully"
├─ Auto-dismiss after 3 seconds
├─ Undo option (if applicable)
└─ Smooth transition to next state

Visual Feedback:
├─ Affected element: Green pulse (once, 400ms)
├─ Toast notification: Bottom-right (desktop), Top (mobile)
└─ No jarring animations
```

4. Error State:
```
When: Something fails, validation fails, network error
Show:
├─ Red icon (not alarming, just clear)
├─ SPECIFIC message: "Payment failed: Card declined"
├─ Actionable fix: "Update payment method"
└─ Support contact (if critical error)

Never Show:
├─ "Error 500"
├─ "Something went wrong"
├─ Technical jargon
├─ Stack traces
└─ Generic error without guidance

Inline Errors (Forms):
├─ Show below field (not replacing label)
├─ Red text with icon
├─ Appear immediately on blur
└─ Clear on edit
```

5. Disabled State:
```
When: Action temporarily unavailable
Show:
├─ 40% opacity on element
├─ Cursor: not-allowed
├─ Tooltip on hover: "Complete profile to enable"
└─ NEVER hide - always show with explanation

Never:
├─ Remove element entirely
└─ Show without explanation
```

### PSYCHOLOGY PRINCIPLES TO APPLY

Hick's Law (Reduce Decision Fatigue):
```
Rule: Fewer choices = Faster action

Application:
├─ Show max 3 primary actions at once
├─ Hide advanced options in "More" menu
├─ Dashboard: 1 prominent CTA, 2-3 secondary
└─ Navigation: 5-7 items max

Example:
❌ Dashboard showing: Create Invoice, Add Product, New Customer, 
   Record Payment, Add Expense, View Reports, Settings, etc.

✅ Dashboard showing:
   Primary: [+ Create Invoice] (prominent)
   Secondary: Quick actions in dropdown
   Rest: In navigation sidebar
```

Fitts's Law (Make Targets Easy to Hit):
```
Rule: Larger targets + Closer to user = Faster interaction

Application:
Desktop:
├─ Buttons: Min 40px height, 120px width
├─ Links: Min 24px height with padding
└─ Icons: Min 24x24px with 8px padding

Tablet:
├─ Buttons: Min 44px height, 140px width
└─ Touch targets: 44x44px minimum

Mobile:
├─ Buttons: Min 48px height, full-width or 160px
├─ FAB: 56x56px
├─ Bottom nav items: 56px height
└─ Thumb zone: Bottom 1/3 of screen for primary actions

Position Rules:
├─ Most important action: Bottom-right on mobile
├─ Destructive action: Separated with space
└─ Related actions: Adjacent (within 8px)
```

Zeigarnik Effect (Show Progress):
```
Rule: People remember unfinished tasks + want to complete

Application:
├─ Progress bars with percentages
├─ "3 of 10 invoices paid this month"
├─ "Almost there! 2 more fields to complete profile"
├─ Monthly target: "₹68,450 / ₹100,000 (68%)"
└─ Streaks: "5 day billing streak 🔥"

Visual:
├─ Use progress bars (not just numbers)
├─ Show how close to completion
├─ Celebrate milestones (80%, 100%)
└─ Make next step obvious
```

Emotional Feedback Loops:
```
Rule: Every action needs acknowledgment

Application:
Button Click:
├─ Scale to 0.98x (40ms)
├─ Subtle shadow change
├─ Return to normal (60ms)
└─ Feel responsive, not laggy

Invoice Created:
├─ Card slides in from right (300ms ease-out)
├─ Green glow pulse (once, 400ms)
├─ Optional confetti (first invoice only)
└─ Success toast

Payment Received:
├─ Status badge transitions color
├─ Amount pulses green (once)
├─ Optional sound (subtle)
└─ Update dashboard KPIs smoothly

Form Submission:
├─ Button shows spinner inside
├─ Disable all form fields (40% opacity)
├─ Success: Smooth transition to next screen
└─ Error: Gentle shake (300ms) + red highlight
```

### INTERACTION REQUIREMENTS

Button Styles:
```css
/* Primary CTA */
background: var(--primary);
color: white;
border-radius: 6px;
padding: 12px 24px;
font-weight: 600;
transition: all 150ms ease;

/* Hover */
transform: translateY(-1px);
box-shadow: 0 4px 12px rgba(primary, 0.3);

/* Active */
transform: scale(0.98);

/* Loading */
opacity: 0.6;
cursor: wait;
/* Show spinner inside */

/* Disabled */
opacity: 0.4;
cursor: not-allowed;
```

Input Fields:
```css
/* Default */
background: var(--bg-raised);
border: 1px solid var(--border);
border-radius: 6px;
padding: 12px 16px;
font-size: 14px;

/* Focus */
border-color: var(--primary);
box-shadow: 0 0 0 3px oklch(65% 0.15 160 / 0.1);
outline: none;

/* Error */
border-color: var(--error);
box-shadow: 0 0 0 3px oklch(60% 0.15 25 / 0.1);

/* Disabled */
opacity: 0.6;
cursor: not-allowed;
background: var(--bg-base);
```

Charts (CRITICAL):
```
Requirements:
├─ NO curved lines (use straight lines for data accuracy)
├─ Grid lines (horizontal at minimum)
├─ Clear axis labels (currency format for money)
├─ Tooltip on hover (exact values)
├─ Legend with color indicators
├─ Comparison mode (this period vs. previous)
└─ Time scale selector: [7D] [30D] [90D] [1Y] [Custom]

Never:
├─ Faded/transparent lines (hard to read)
├─ Decorative curves that distort data
├─ Unlabeled axes
└─ Charts without context
```

Modals:
```
Structure:
├─ Overlay: 50% black opacity
├─ Modal: Centered, max 600px width (desktop)
├─ Close button: Top-right (always visible)
├─ Footer: Sticky with actions
└─ Mobile: Full-screen bottom sheet

Behavior:
├─ Open: Fade in + scale from 0.9 to 1.0 (200ms)
├─ Close: Reverse animation
├─ Esc key: Close modal
├─ Click overlay: Close modal (with confirmation if form dirty)
└─ Focus trap: Tab cycles within modal

Actions:
├─ Primary: Right side, prominent
├─ Secondary: Left side, subtle
└─ Destructive: Red, separate with space
```

### RESPONSIVE BEHAVIOR (Critical for Each Breakpoint)

Desktop (1024px+):
- Full feature set visible
- Multi-column layouts
- Hover states active
- Keyboard shortcuts enabled
- Tooltips on hover

Tablet (768-1023px):
- Collapsible sidebar
- 2-column grid
- Larger touch targets (44px)
- Simplified navigation
- Touch-optimized interactions

Mobile (< 768px):
- Hide all secondary information
- Show only primary user flow
- Bottom navigation (5 items max)
- FAB for primary action
- Full-width forms
- Stack everything vertically
- Swipeable cards where appropriate
- Pull-to-refresh

### ACCESSIBILITY REQUIREMENTS (WCAG AA Minimum)

Contrast:
- Text on background: Min 4.5:1 (body text)
- Text on background: Min 3:1 (large text 18px+)
- Interactive elements: Min 3:1

Keyboard Navigation:
- All interactive elements accessible via Tab
- Visible focus indicators (outline + background change)
- Logical tab order (top-left to bottom-right)
- Skip to content link
- Esc to close modals
- Enter to submit forms
- Arrow keys for dropdowns

Screen Readers:
- Semantic HTML (header, nav, main, footer)
- ARIA labels on all icon buttons
- Alt text on images
- Form labels properly associated
- Error announcements
- Loading state announcements

Focus Management:
- Auto-focus on modal open (first input)
- Return focus on modal close
- Focus visible on all interactive elements
- Skip navigation links

---

## DELIVERABLES REQUIRED

Provide the following in your response:

1. **Layout Structure** (Wireframe Level):
   - Desktop grid breakdown (show columns)
   - Tablet adaptation strategy
   - Mobile single-column flow
   - Component nesting hierarchy

2. **Component Breakdown**:
   - List all UI components needed
   - Spacing values for each (using 8pt grid)
   - Relationships between components
   - Reusable vs. page-specific components

3. **Interaction Logic**:
   - User flow diagram (Step 1 → Step 2 → Step 3)
   - State transitions (empty → loading → success)
   - Form validation rules (when, what, how)
   - Error handling strategy

4. **Responsive Behavior Per Breakpoint**:
   - What hides on tablet
   - What hides on mobile
   - What reorganizes
   - Touch vs. click optimizations

5. **Specific OKLCH Color Values**:
   - List all colors used with actual codes
   - Explain when each color is used
   - Hover/active state colors

6. **Rationale for UX Decisions**:
   - Why this layout over alternatives
   - Which psychology principles applied where
   - What user pain points solved

7. **Problems This Design Solves**:
   - List specific issues from "Current Issues" section
   - Explain how each issue is resolved
   - Before/after comparison

8. **Edge Cases Handled**:
   - What happens if data is empty
   - What happens if network fails
   - What happens if user has no permissions
   - What happens with malformed data
   - What happens on slow 3G connection

---

## CRITICAL CONSTRAINTS

Technical:
- Frontend: React + Vite on Vercel
- Backend: Node.js on Render (Starter plan - limited resources)
- Database: PostgreSQL (connection pooling required)
- Must work with 5000+ invoices per company
- Must handle ₹0 to ₹10 crore revenue ranges
- Must support spotty 3G connections (India tier-2/3 cities)

Business:
- Multi-tenant SaaS (data isolation critical)
- GST compliance required (Indian tax)
- Support credit terms (Net 15, Net 30, etc.)
- Multiple price lists per customer segment
- Branch and route management

Performance:
- Page load: < 2 seconds on 3G
- Interaction response: < 100ms
- Search results: < 300ms
- PDF generation: < 3 seconds
- Pagination: 50 items per page default

---

## OUTPUT STYLE REQUIREMENTS

Your response must be:
1. **Specific and Architectural** (not generic advice)
2. **Show Actual Measurements** (spacing in px, colors in OKLCH)
3. **Include Formulas** (for calculations, validations)
4. **Explain Psychological Reasoning** (why users will prefer this)
5. **Prioritize Performance** (load speed, data integrity)
6. **Think Enterprise-Grade** (not MVP, not prototype)

Structure your response:
```
# [PAGE NAME] - Complete Redesign

## Current Problems Identified
[List each problem from my input]

## Design Solution Overview
[2-3 paragraph summary of the approach]

## Desktop Layout
[ASCII diagram + detailed description]

## Mobile Layout
[ASCII diagram + adaptations from desktop]

## State Designs
[Empty, Loading, Success, Error, Disabled - with examples]

## Component Structure
[Nested list of all components with spacing]

## Color Usage
[All OKLCH codes used and where]

## Typography Scale
[All font sizes used and where]

## Interactions
[Every interactive element's behavior]

## Calculations
[All formulas in JavaScript/SQL]

## Edge Cases
[Comprehensive list with solutions]

## Performance Optimizations
[Specific technical strategies]

## Accessibility Implementation
[WCAG compliance details]
```

---

## BEGIN REDESIGN NOW

Apply all systems above to redesign: [PAGE_NAME]

Focus on solving the current issues listed while maintaining consistency with the design system.
```

---

## 📝 QUICK REFERENCE CHECKLIST

Before submitting prompt to Cursor, verify:

- [ ] Replaced [PAGE_NAME] with actual page name
- [ ] Listed specific current issues from code analysis
- [ ] Confirmed all design system rules are in template
- [ ] Ready to receive detailed wireframes + code structure
- [ ] Have screenshots ready if needed for context

---

## 🎯 PAGE-SPECIFIC ISSUE TEMPLATES

Use these as starting points for "Current Issues Identified" section:

### Dashboard Issues:
```
- Too many KPIs (10+) causing information overload
- Revenue calculation includes pending invoices (should only be paid)
- Chart uses curved lines (distorts data accuracy)
- No comparison to previous period
- Missing empty state for new users
- No loading skeletons (shows blank screen)
- Quick actions buried in menu
- Mobile version scrolls excessively
```

### POS Issues:
```
- Product grid and cart compete for attention
- Customer selection workflow confusing
- "Hold" and "Resume" functionality unclear
- No barcode scanner integration visible
- Payment modal requires too many clicks
- Cart calculations update slowly (no debounce)
- Mobile version unusable (tiny buttons)
- Stock updates not reflected in real-time
```

### Products Issues:
```
- Table view only (no grid/card option)
- No bulk actions (import/export)
- Stock alerts not prominently displayed
- Category management buried
- Mobile table view completely broken
- No product variants support
- Search doesn't include SKU
- Images not optimized (slow load)
```

### Invoices Issues:
```
- Status badges unclear (color only, no text)
- Duplicate invoice numbers possible
- No bulk send email option
- Print format not thermal printer compatible
- Mobile: Can't view invoice details easily
- No payment link generation
- Tax calculations sometimes wrong
- Customer balance not updated immediately
```

### Reports Issues:
```
- Profit/loss calculation wrong (missing COGS)
- Date range selector confusing
- No export to Excel
- Charts not responsive on mobile
- GST report format doesn't match official
- Loading takes >10 seconds (no pagination)
- No visual comparison between periods
- Inventory report shows incorrect stock values
```

---

## 💡 EXAMPLE USAGE

```
[Copy the Master Template]
↓
[Replace [PAGE_NAME] with "Dashboard"]
↓
[Add Current Issues:
- Too many KPIs (10+) causing information overload
- Revenue calculation includes pending invoices
- etc.]
↓
[Paste into Cursor]
↓
[Receive Complete Redesign with:
- Exact layouts
- Spacing values
- Color codes
- Interaction logic
- SQL queries
- Everything you need]
```

---

## 🔥 ALL 40 PAGES COVERED

Use this template for:

**Authentication (3):**
1. Login
2. Signup
3. Onboarding Wizard

**Company Owner (27):**
4. Dashboard
5. Dashboard Tally (Accounting view)
6. POS Page
7. Products Page
8. Customers Page
9. Purchases Page
10. Sales Ledger Page
11. Sales Ledger Import
12. Customer Ledger Page
13. Expenses Page
14. Payments Page
15. Billing History Page
16. Reports Page
17. Branches Page
18. Branch Detail Page
19. Routes Page
20. Route Detail Page
21. Users Page
22. Price List
23. Settings Page
24. Profile Page
25. Subscription Plans Page
26. Backup Page
27. Data Import Page
28. Updates Page
29. Help Page
30. Feedback Page

**Super Admin (9):**
31. Super Admin Dashboard
32. Super Admin Tenants Page
33. Super Admin Tenant Detail Page
34. Super Admin Subscriptions Page
35. Super Admin Demo Requests Page
36. Super Admin Audit Logs Page
37. Super Admin Error Logs Page
38. Super Admin Health Page
39. Super Admin Settings Page

**Error (1):**
40. Error Page

---

## 🎯 FINAL NOTES

- **Each page** gets this full treatment
- **Consistency** is maintained through the design system
- **Mobile** is not an afterthought - it's designed equally
- **Performance** is baked into every decision
- **Accessibility** is non-negotiable
- **Psychology** makes it addictive to use
- **Enterprise-grade** means zero compromises

**Your Hexabill will feel like a Fortune 500 product.**

---

Ready to redesign all 62 pages? Start with Dashboard, then POS, then Products. These three are your highest traffic pages and will set the standard for everything else.

Copy the Master Template above and GO! 🚀
