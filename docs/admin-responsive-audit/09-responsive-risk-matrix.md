# Admin Responsive Risk Matrix

This document lists the user experience risks, functional problems, and styling issues identified in the Admin area.

| Severity | Area | Issue | Evidence/File | Impact | Recommended Fix |
|----------|------|-------|---------------|--------|-----------------|
| **Critical** | Navigation | Sidebar stacks at the top on mobile, pushing content offscreen. | `_AdminLayout.cshtml` (Lines 26-27) | Heavy scrolling required (500px+) on every page reload; unusable. | Hide aside using `hidden lg:flex`, make it a mobile slide-out drawer triggered by a fixed top mobile header and toggle button. |
| **Critical** | Upload manager | MenuGroup Images card list has 15+ heavy forms stacked vertically. | `MenuGroup/Images.cshtml` (Line 132) | Page height is 7500px+ on mobile; slow page loading and browser lag. | Convert preview layout to a compact 2-column grid, scale down cards, group actions into clean bottom layouts. |
| **High** | Listing Tables | Wide tables (MenuGroup, Post, Branch) overflow the viewport. | `MenuGroup/Index.cshtml`, `Post/Index.cshtml`, `Branch/Index.cshtml` | Columns cut off offscreen; action buttons are hidden unless scrolled horizontally. | Transform table rows into responsive cards (`.admin-card`) below `md`/`lg` breakpoints. |
| **High** | Bulk actions | Bulk delete button scrolls out of view on mobile. | `MenuGroup/Images.cshtml` (Line 103) | Users must scroll back up to delete selected items, disrupting workflow. | Convert bulk actions panel to a sticky bar fixed to the bottom of the mobile viewport. |
| **High** | Form layout | Dense sub-grids stack but lack vertical separation. | `Branch/Create.cshtml`, `Branch/Edit.cshtml` | Form elements appear cluttered; label overlaps. | Enforce gap spacing and responsive grid padding tokens. |
| **Medium** | Actions and buttons | Form submit buttons are right-aligned. | `Branch/Create.cshtml` (Line 149), `Setting/Index.cshtml` (Line 98) | Buttons are off-center and hard to reach on mobile viewports. | Convert bottom submit buttons to full-width (`w-full`) and align centered on mobile. |
| **Medium** | Pagination | Pagination numbers wrap into multiple lines. | `Post/Index.cshtml` (Line 213) | Circles overlap and buttons distort on narrow screens. | Limit active pagination size to 3 items on mobile; replace text with arrows. |
| **Medium** | Notifications | Toast notifications overlap top header actions. | `_AdminLayout.cshtml` (Line 125) | Users cannot tap header controls for 4 seconds until toast fades. | Relocate mobile toaster container to the bottom center of the viewport. |
| **Low** | CSS Leakage | Base CSS changes risk leaking to public pages. | `input.css` | Public dark premium theme pages can have layout regressions. | Enforce scoping of admin class overrides; prioritize inline Tailwind rules. |
| **Low** | Script isolation | Global `site.js` executes on admin pages. | `_AdminLayout.cshtml` (Line 154) | Negligible execution overhead, but represents poor asset isolation. | Isolate public menu listeners; run admin-specific toggles in `admin.js`. |
