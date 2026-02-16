# HexaBill Redesign Documentation

**Purpose:** All redesign prompts, design systems, and build checklists for the HexaBill project.

---

## Folder Contents

| File | Purpose |
|------|---------|
| **HEXABILL_200_TASK_BUILD_CHECKLIST.md** | **Start here** — 200-task end-to-end build checklist |
| HEXABILL_MASTER_PROMPT_GENERATOR.md | Master Cursor Pro prompt for any page redesign |
| HEXABILL_COMPLETE_REDESIGN_PROMPTS.md | Pages 1–5 detailed prompts (Login, Signup, Onboarding, Dashboard, POS) |
| HEXABILL_REDESIGN_PART2.md | Pages 6–40 detailed prompts |
| HEXABILL_PRODUCTION_SECURITY_CHECKLIST.md | Security, performance, testing before production |
| HEXABILL_CURSOR_CODE_REVIEW_PROMPTS.md | Automated security/performance audit prompts |

---

## Workflow

1. **Redesign a page:** Use `HEXABILL_MASTER_PROMPT_GENERATOR.md` — replace `[PAGE_NAME]` and add issues.
2. **Build:** Follow `HEXABILL_200_TASK_BUILD_CHECKLIST.md` tasks in order.
3. **Verify:** Run `.\scripts\build-check.ps1` before commits (checks both backend and frontend build).
4. **Security:** Use `HEXABILL_CURSOR_CODE_REVIEW_PROMPTS.md` for code audits.

---

## Design System Quick Reference

- **Typography:** 24px, 20px, 16px, 14px, 12px only
- **Spacing:** 8pt grid (4, 8, 16, 24, 32, 48, 64)
- **Colors:** OKLCH format, primary only for CTAs
- **States:** Empty, Loading, Success, Error, Disabled (all 5 required)
- **Charts:** Straight lines, grid, tooltips, comparison mode
