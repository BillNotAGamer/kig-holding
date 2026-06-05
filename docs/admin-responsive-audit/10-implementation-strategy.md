# Admin Responsive Implementation Strategy

This document details the step-by-step phased execution plan to make the Admin UI responsive across desktop, tablet, and mobile, without breaking any ASP.NET MVC backend contracts.

## Implementation Principles

1.  **Do Not Break View Contracts**: Do not touch C# types, models, controllers, route names, or action parameters. All modifications must reside within HTML structure and Tailwind CSS classes inside Razor Views.
2.  **Isolate Layout Changes**: Implement responsive updates using inline Tailwind utility classes (e.g., `hidden lg:block`) within Razor Views where possible, keeping the public site's global styles in `input.css` isolated.
3.  **Preserve Access and Visibility**: Mobile card transformations must display all necessary data fields and actions. Do not remove columns unless they are redundant or duplicate.
4.  **Touch Target Standards**: Ensure a minimum touch target size of 48x48 pixels for all interactive components (buttons, filters, checkboxes) on mobile viewports.

---

## Do Not Break Contracts

The following backend model binding and routing contracts must remain intact:

### 1. Model Property Bindings (`asp-for`)
All C# properties mapped via Tag Helpers must retain their exact target declarations:
*   **Auth**: `Username`, `Password`, `RememberMe`, `ReturnUrl`.
*   **Reservation**: `Status`, `Note`.
*   **Contact**: `Status`.
*   **MenuGroup**: `Name`, `Slug`, `ShortDescription`, `Description`, `CoverImageUrl`, `DisplayOrder`, `CoverImageFile`, `IsPublished`.
*   **Post**: `Title`, `Slug`, `Category`, `PublishedAt`, `Excerpt`, `Content`, `SeoTitle`, `SeoDescription`, `ThumbnailFile`, `IsPublished`.
*   **Review**: `CustomerName`, `Rating`, `Content`, `BranchId`, `DisplayOrder`, `IsVisible`.
*   **Branch**: `Name`, `Slug`, `Address`, `District`, `City`, `Hotline`, `Email`, `OpeningTime`, `ClosingTime`, `Capacity`, `AreaSquareMeters`, `NumberOfFloors`, `Description`, `GoogleMapUrl`, `IsActive`, `DisplayOrder`, `ThumbnailFile`.
*   **Setting**: `BrandName`, `Hotline`, `Slogan`, `Email`, `Address`, `GoogleMapUrl`, `FacebookUrl`, `ZaloUrl`, `TiktokUrl`.

### 2. Route & Form Declarations
*   **Anti-Forgery**: `@Html.AntiForgeryToken()` must be retained inside every POST form to prevent CSRF authentication failures.
*   **Enctype**: Forms handling uploads (`MenuGroup`, `Post`, `Branch`) must preserve `enctype="multipart/form-data"`.
*   **Named Routes**: Do not modify named routing parameters:
    *   `AdminMenuGroupImagesUpload`
    *   `AdminMenuGroupImagesBulkDelete`
    *   `AdminMenuGroupImagesUpdate`
    *   `AdminMenuGroupImagesDelete`

### 3. JavaScript Event Hooks (`data-*` attributes)
Keep these data attributes unchanged, as they power previews and selections in `admin.js`:
*   `data-admin-image-preview`
*   `data-admin-image-preview-target`
*   `data-admin-image-preview-meta`
*   `data-admin-file-count`
*   `data-admin-file-list`
*   `data-menu-image-select-all`
*   `data-menu-image-select`
*   `data-menu-image-selected-count`
*   `data-bulk-delete-submit`
*   `data-toast`

---

## Recommended Breakpoints

We target the standard Tailwind breakpoints for layout flows:
*   **Mobile**: `< 640px` (Default utility classes stack columns, apply `w-full` buttons).
*   **Tablet**: `640px` to `1024px` (`sm:` and `md:` prefixes trigger 2-column tables or grid filter adjustments).
*   **Desktop**: `> 1024px` (`lg:` and `xl:` prefixes restore the left sticky sidebar and side-by-side forms).

---

## Phase 1 — Admin Shell

*   **Goal**: Replace the vertical stacked layout on mobile with a slide-out hamburger menu drawer and mobile header toolbar.
*   **Files**:
    *   `Areas/Admin/Views/Shared/_AdminLayout.cshtml`
    *   `wwwroot/js/admin.js`
*   **Changes**:
    *   Hide the sidebar on mobile (`hidden lg:flex`) and wrap it with transition classes (`fixed inset-y-0 left-0 z-50 w-72 -translate-x-full transition-transform duration-300 lg:translate-x-0`).
    *   Insert a fixed mobile header toolbar at the top of the viewport (`lg:hidden fixed top-0 inset-x-0 h-16 z-40 bg-brand-black text-white flex items-center justify-between px-4`).
    *   Insert a transparent/backdrop overlay `div` toggled via drawer open state.
    *   Write a vanilla JavaScript listener in `admin.js` to toggle `-translate-x-full` and backdrop visibility.
*   **Risks**: Keyboard traps on drawer transition.
*   **Acceptance Criteria**: Sidebar menu is completely hidden on mobile/tablet screens. Clicking the hamburger button slides the menu in smoothly. Clicking the backdrop or hitting `Escape` closes the drawer. Page content is padded to avoid header overlaps.

---

## Phase 2 — Tables and Lists

*   **Goal**: Redesign all desktop HTML tables to automatically transform into clean card components on mobile viewports.
*   **Files**:
    *   `Areas/Admin/Views/Reservation/Index.cshtml`
    *   `Areas/Admin/Views/Contact/Index.cshtml`
    *   `Areas/Admin/Views/MenuGroup/Index.cshtml`
    *   `Areas/Admin/Views/Post/Index.cshtml`
    *   `Areas/Admin/Views/Review/Index.cshtml`
    *   `Areas/Admin/Views/Branch/Index.cshtml`
*   **Changes**:
    *   Wrap table heads (`<thead>`) with `.hidden.md:table-header-group` (or `lg:`).
    *   Transform table row grids (`<tr>`) into responsive cards (`flex.flex-col.border-b.md:table-row`).
    *   Format table cells (`<td>`) into labeled list items (`flex.justify-between.px-4.py-2.md:table-cell`).
    *   Wrap row actions into horizontal bars (`.admin-actions.mt-3.md:mt-0`).
*   **Risks**: Horizontal styling layout issues on custom table grid alignments.
*   **Acceptance Criteria**: Columns fit within small mobile viewports without forcing horizontal scrolling. Buttons are large and separated, preventing mis-taps.

---

## Phase 3 — Forms

*   **Goal**: Polish forms to enforce stacked column controls, enlarged tap elements, and responsive action zones.
*   **Files**:
    *   `Areas/Admin/Views/Branch/Create.cshtml`, `Edit.cshtml`
    *   `Areas/Admin/Views/MenuGroup/Create.cshtml`, `Edit.cshtml`
    *   `Areas/Admin/Views/Post/Create.cshtml`, `Edit.cshtml`
    *   `Areas/Admin/Views/Setting/Index.cshtml`
    *   `Areas/Admin/Views/Review/Create.cshtml`, `Edit.cshtml`
*   **Changes**:
    *   Verify nested sub-grids (`sm:grid-cols-3` capacity cards, `sm:grid-cols-2` operating hours grids) have row gaps (`gap-y-4`).
    *   Reposition form submit buttons at the bottom of pages from `text-right` to a centered block (`text-center sm:text-right`) and stretch buttons to `w-full` on mobile.
    *   Ensure all checkboxes (e.g. `IsActive`, `IsPublished`) are wrapped in touch-safe block shells (`.admin-post-checkbox-shell`).
*   **Risks**: Model validation error elements rendering incorrectly when fields stack.
*   **Acceptance Criteria**: Form labels and validation spans align cleanly. Inputs are easy to select and type into on touchscreens.

---

## Phase 4 — Upload and Media Manager

*   **Goal**: Optimize the menu group multi-page upload panel, alt texts, image previews, and bulk delete actions.
*   **Files**:
    *   `Areas/Admin/Views/MenuGroup/Images.cshtml`
*   **Changes**:
    *   Replace the 3-column desktop card grid (`md:grid-cols-2 xl:grid-cols-3`) with a 2-column grid on mobile (`grid-cols-2 md:grid-cols-2 xl:grid-cols-3`).
    *   Compact the image preview heights (`height: clamp(8rem, 20vw, 12rem)`) to reduce card heights by 40%.
    *   Position individual card form actions ("Lưu thay đổi" and "Xóa ảnh") side-by-side using `grid grid-cols-2 gap-2` to save vertical space.
    *   Modify the bulk delete actions panel to be fixed to the bottom of the viewport as a sticky banner on mobile when items are selected.
*   **Risks**: Network request latencies during multiple page image reloads.
*   **Acceptance Criteria**: 20+ uploaded page images fit cleanly in a dual card column on mobile. Accidental deletes are prevented by clear button borders and confirmation alerts.

---

## Phase 5 — Polish, Accessibility, and QA

*   **Goal**: Relocate toast notifications, audit focus states, check keyboard tabbing, and verify layout integrity.
*   **Files**:
    *   `Areas/Admin/Views/Shared/_Layout.cshtml` (Verify no leakage)
    *   `Areas/Admin/Views/Shared/_AdminLayout.cshtml`
*   **Changes**:
    *   Reposition mobile toast wrappers (`.admin-toast`) to the bottom-center of the viewport (`bottom-4.left-1/2.-translate-x-1/2`).
    *   Verify that any responsive CSS utility addition inside `input.css` does not break the public home page slider or booking modals.
    *   Attach keyboard focus indicators (`focus-visible:ring-2`) to drawer open/close targets.
*   **Risks**: Regression bugs on dark premium public site styles.
*   **Acceptance Criteria**: Notifications appear clearly and do not block topbar interactions. Public site styles remain untouched.

---

## Testing Checklist

### Desktop
- [ ] Sidebar sticky to left viewport, navigates to all 8 CRUD modules.
- [ ] Listing tables show full grids (excerpts, slugs, dates, categories).
- [ ] Forms display as split grids (`lg:grid-cols-2`).
- [ ] Named routes load files correctly.

### Tablet
- [ ] Sidebar collapses into mobile state below 1024px.
- [ ] Hamburger header toolbar displays with toggle.
- [ ] Index grids adapt to 2 columns where appropriate.

### Mobile
- [ ] Hamburger icon expands the sidebar drawer smoothly.
- [ ] Tapping backdrop or pressing `Escape` collapses drawer.
- [ ] Tables transform into vertical card components; no horizontal scrollbar exists.
- [ ] Toast notifications display at bottom center.
- [ ] Submit buttons expand to full width and align centered.

### Keyboard
- [ ] Tabbing shifts focus down the navigation links in order.
- [ ] Tabbing into the mobile drawer navigates internal items and locks focus when drawer is open.
- [ ] Hitting `Escape` closes the drawer.

### Screen Reader Basics
- [ ] Menu hamburger toggle has `aria-expanded` reflecting the drawer state.
- [ ] active menu item has `aria-current="page"`.
- [ ] Main landmarks (`<aside>`, `<header>`, `<main>`) are read in sequence.

### Form Submission
- [ ] Client-side validation triggers and prints error messages cleanly under inputs.
- [ ] Anti-forgery tokens submit and pass validation filters.
- [ ] Submitting forms updates database records correctly.

### Upload
- [ ] Selecting files lists filenames in count containers.
- [ ] FileReader previews thumbnails correctly.
- [ ] Multi-file forms submit and upload files to the destination directory.

### Delete/Destructive Actions
- [ ] Tapping "Xóa" or "Ẩn" triggers the browser confirmation dialog.
- [ ] Rejecting confirmation cancels the POST request.
- [ ] Accepting confirmation completes deletion/soft-deletion.
- [ ] Bulk delete selection operates correctly.
