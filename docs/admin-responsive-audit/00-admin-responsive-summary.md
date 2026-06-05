# Admin Responsive Summary

## Current Admin UI Status

The Admin UI of the KIGHolding project is built with Razor Views, TailwindCSS, and PostgreSQL. It features standard CRUD capabilities for the website's main features (reservations, posts, branches, menu groups, contacts, reviews, and site settings).

While desktop layout styling is clean and modern using the Light Theme identity (cream background, white cards, dark gray typography, red accents), the mobile and tablet experience suffers from severe usability issues. The administration panel is currently difficult to use on small screens due to a static sidebar layout, horizontal table overflows, small interactive elements, and stacked button groups without adequate vertical separation.

## Main Admin Areas

The admin panel is organized into 9 operational areas:
1. **Dashboard (`/Admin/Dashboard`)**: Core operational summary and shortcuts.
2. **Auth (`/Admin/Auth/Login`)**: Admin portal entry.
3. **Reservations (`/Admin/Reservation`)**: Reservation list and status update workflow.
4. **Contacts (`/Admin/Contact`)**: Contact inbox list and details.
5. **Menu Groups (`/Admin/MenuGroup`)**: Brand menu categories and multi-image page upload.
6. **Posts (`/Admin/Post`)**: News articles and promotional updates.
7. **Reviews (`/Admin/Review`)**: Customer testimonial moderation.
8. **Branches (`/Admin/Branch`)**: Restaurant locations and details.
9. **Settings (`/Admin/Setting`)**: Global site config (Brand, Hotline, Social links, Map).

## Current Responsive Maturity

**Partially responsive**

*   **Responsive Elements**: Centered container layouts, login card alignment, form grids (`lg:grid-cols-2`, `sm:grid-cols-2`) that automatically stack on mobile, and basic grid structures.
*   **Non-Responsive Elements**: Global layout sidebar stacked directly on top of content on mobile, lack of a hamburger navigation drawer, raw HTML tables overflowing the viewport, cramped/nested button grids, and heavy media managers with long vertical scroll requirements.

## Top 10 Admin UI Findings

1.  **Sidebar Stacked Top**: The `<aside>` sidebar is NOT hidden on mobile; it stacks above the `<main>` content container. Users on mobile must scroll down past the branding, 8 navigation links, and "Xem website" button before viewing the actual page content.
2.  **Lack of Mobile Shell Toggle**: There is no mobile header, hamburger menu, overlay, drawer, or backdrop to collapse and expand navigation on mobile/tablet.
3.  **Horizontal Table Overflow Fallback**: The admin tables rely purely on `overflow-x-auto` wrappers. This forces a horizontal scroll on mobile, hiding critical data and action buttons like "Xem chi tiết" or "Sửa/Ẩn".
4.  **Twin Form-Class Systems**: The styling system contains a duplicate set of CSS classes: standard classes (`.admin-field`, `.admin-label`, `.admin-validation`) and post-specific classes (`.admin-post-field`, `.admin-post-label`, `.admin-post-validation`). These are functionally identical but define different border radiuses (`rounded-xl` vs `rounded-2xl`).
5.  **Cramped Row Action Buttons**: Row action buttons in tables (e.g., `Quản lý ảnh`, `Sửa`, `Ẩn`, `Ẩn nhóm` in MenuGroup list) are placed in a dense horizontal flex list (`.admin-actions`). On mobile, these wrap into vertical columns that make rows extremely tall.
6.  **Destructive Actions Too Close**: Destructive buttons (like "Xóa mềm" or "Ẩn") are adjacent to safe navigation buttons ("Sửa", "Xem chi tiết"). On mobile, these have small margins, increasing the risk of accidental taps.
7.  **Right-Aligned Button Inaccessibility**: Form submission buttons are right-aligned inside `text-right` divs on large screens. On mobile, this leaves small, off-center buttons that are hard to tap with a single thumb.
8.  **Heavy Media Previews on Mobile**: In `MenuGroup/Images.cshtml`, each menu image is rendered as a full card containing an image preview, two text fields, a checkbox, and two buttons. If a menu group contains 15+ pages, this creates a page height of over 10,000px on mobile.
9.  **Site Header & Footer Shared Asset Risks**: Both public and admin sites share the same compiled `wwwroot/css/site.css` which is built from `wwwroot/css/input.css`. Edits to global styles like `body`, `button`, or `input` in `input.css` risk leaking layout anomalies to the public dark premium pages.
10. **Site.js is Not Isolated**: JavaScript in `wwwroot/js/site.js` runs globally on both public and admin pages. While it targets specific DOM attributes (e.g., `[data-mobile-menu]`), loading it on admin pages without checking element presence causes minor execution overhead, and its functions are not designed to support the admin panel.

## Top 10 Responsive Risks

1.  **High Navigation Friction**: Mobile admin users must scroll past the entire navigation sidebar on every single page load.
2.  **Accidental Data Deletions**: Row actions like "Ẩn" or "Xóa mềm" are placed in close proximity to navigation links, increasing the risk of accidental data modification due to tight mobile touch targets.
3.  **Action Buttons Hidden Offscreen**: Table actions (e.g., "Xem chi tiết") are placed in the rightmost column. If horizontal scroll is missed, the user may think the table is read-only.
4.  **Cramped Textareas**: Large form textareas (like `Description` or `Content`) use fixed rows or default heights, stretching beyond the mobile screen height and breaking user focus.
5.  **Multi-Upload Overflow**: Selecting 10+ large files on `MenuGroup/Images.cshtml` renders a long list of filenames (`#menu-page-upload-file-list`) that overlaps other form items on small viewports.
6.  **Broken Viewport Scaling**: Text inputs like `SearchQuery` in table filter forms have fixed widths or layout structures that stack into a single column, making filter forms take up more vertical space than the actual data table.
7.  **Toast Notification Overlap**: Notifications are positioned at `right-4 top-4 z-50` with width `w-[min(24rem,calc(100vw-2rem))]`. On mobile, this overlays the top header and action buttons, blocking users from tapping other controls until they auto-dismiss (4 seconds).
8.  **No Focus States on Mobile Safari**: Text inputs and buttons rely on Tailwind `focus-visible:ring-2` rules, which behave inconsistently on mobile Safari, leading to poor keyboard navigation and visual feedback.
9.  **Leakage of CSS Edits to Public Site**: Editing base CSS rules in `wwwroot/css/input.css` can break the Dark Premium layout of the home page (`Views/Home/Index.cshtml`) or menu details.
10. **Broken DearFlip Flipbook on Mobile**: While not part of the admin panel itself, the DearFlip canvas settings in `input.css` (line 3203) are tightly integrated with the styling compiled for admin, meaning any responsive style refactoring must avoid breaking the public menu flipbook.

## Recommended Implementation Phases

*   **Phase 1 — Admin Shell**: Refactor `_AdminLayout.cshtml` to introduce a responsive header, collapsible hamburger menu drawer, overlay, and layout z-index separation.
*   **Phase 2 — Tables and Lists**: Implement mobile card transformations for tables, hiding low-priority fields and grouping row actions into clean, finger-friendly layouts.
*   **Phase 3 — Forms**: Standardize mobile form layouts (full-width stacked inputs, enlarged tap targets, full-width action bars).
*   **Phase 4 — Upload and Media Manager**: Optimize `MenuGroup/Images.cshtml` layout, preview grids, and progress indicators for mobile screens.
*   **Phase 5 — Polish, Accessibility, and QA**: Audit toast notifications, validation tooltips, button contrast, and run a comprehensive verification plan.

## Files Most Likely to Be Modified Later

*   `Areas/Admin/Views/Shared/_AdminLayout.cshtml` (Layout shell and navigation menu)
*   `wwwroot/css/input.css` (Tailwind styles, adding responsive rules and helper utilities)
*   `wwwroot/js/admin.js` (JavaScript for toggling the mobile sidebar drawer and bulk action states)
*   `Areas/Admin/Views/Reservation/Index.cshtml` (Table card transformation and filters layout)
*   `Areas/Admin/Views/Contact/Index.cshtml` (Table card transformation and filters layout)
*   `Areas/Admin/Views/MenuGroup/Index.cshtml` (Table card transformation and actions layout)
*   `Areas/Admin/Views/MenuGroup/Images.cshtml` (Card item grid layout and action buttons spacing)
*   `Areas/Admin/Views/Post/Index.cshtml` (Table card transformation and filters layout)
*   `Areas/Admin/Views/Review/Index.cshtml` (Table card layout)
*   `Areas/Admin/Views/Branch/Index.cshtml` (Table card layout)

## Files That Must Not Be Modified Directly

*   `wwwroot/css/site.css` (Compiled file. Modifications will be overwritten during Tailwind build)
*   `Areas/Admin/Controllers/*` (Audit scope only. Do not modify C# controller actions)
*   `Areas/Admin/ViewModels/*` (Audit scope only. Do not modify ViewModels or property annotations)
*   `Views/Shared/_Layout.cshtml` (Public layout; must not be modified)
*   `Views/Home/Index.cshtml` (Public home view; must not be modified)
