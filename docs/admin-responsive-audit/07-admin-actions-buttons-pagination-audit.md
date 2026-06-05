# Admin Actions, Buttons, and Pagination Audit

This document audits interactive elements (buttons, filters, pagination, alerts, and empty states) in the Admin panel.

## Button Patterns

The Admin area uses a consistent set of button classes:
*   **`.btn-primary`**: Black background, white text, uppercase tracking (`tracking-[0.14em]`), rounded-button (`999px` pill).
*   **`.btn-secondary`**: White background, dark border, red hover effect, uppercase tracking.
*   **`.btn-ghost`**: White background, light border, gray text, red hover.
*   **`.admin-table-action`**: Compact pill buttons used inside tables.
    *   `--primary`: Light red background, dark red text.
    *   `--muted`: White background, border, dark text, red hover.
    *   `--danger`: Light rose background, rose text.

## Row Actions

*   Row action lists are wrapped inside the `.admin-actions` container (`flex flex-wrap items-center gap-3`).
*   **Mobile Risk**: When tables are converted to mobile cards, these actions stack or wrap. When there are 3-4 buttons in a row (e.g. `MenuGroup/Index.cshtml`), they wrap into multiple rows, taking up half the card height.
*   **Recommendation**: Group secondary actions into a compact drop-up or drop-down menu, or display them side-by-side using icon buttons rather than verbose text buttons.

## Filter/Search Bars

*   Filter grids in index pages use custom column definitions:
    *   `Reservation/Index`: `xl:grid-cols-[minmax(0,20rem)_minmax(0,14rem)_minmax(0,18rem)_auto_auto]`
    *   `Contact/Index`: `xl:grid-cols-[minmax(0,16rem)_auto_auto]`
    *   `MenuGroup/Index`: `xl:grid-cols-[minmax(0,1.6fr)_minmax(0,0.95fr)_auto_auto]`
    *   `Post/Index`: `xl:grid-cols-[minmax(0,1.6fr)_minmax(0,0.95fr)_minmax(0,0.85fr)_auto_auto]`
*   **Mobile Risk**: Below the `xl` breakpoint, these grids collapse into a single column. All search queries, dropdowns, and filter buttons stack vertically. This makes the filter card taller than the actual data table.
*   **Recommendation**: Implement a 2-column grid layout for filters on tablet (`md:grid-cols-2`) and keep single columns on mobile. Wrap filter actions (Lọc, Xóa lọc) into a side-by-side grid (`grid grid-cols-2 gap-3`) below the inputs.

## Pagination

*   **View file**: [Post/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Post/Index.cshtml) (Lines 200-239)
*   **Layout**: Uses `flex flex-wrap items-center justify-between gap-3` to position "Trang trước", the page numbers list, and "Trang sau".
*   **Mobile Risk**: If there are 10+ pages, the numbers list (`@for (var currentPage = paginationStart; ... )`) will wrap into multiple rows, resulting in overlapping circles on narrow screens.
*   **Recommendation**: Limit the pagination window on mobile to a maximum of 3 page numbers (current page, one page before, one page after). Hide the "Trang trước" and "Trang sau" text labels, replacing them with simple arrows (`←`, `→`).

## Alerts and Validation

*   **Toast Notifications**: Styled as `.admin-toast` (success/error alerts). Dynamically disappear after 4 seconds via a vanilla script in `_AdminLayout.cshtml`.
    *   *Mobile Risk*: Positioned at `right-4 top-4 z-50`. On mobile, it covers the top-right header and the logout button, blocking interaction.
    *   *Recommendation*: Shift mobile notifications to the bottom center (`bottom-4 left-1/2 -translate-x-1/2`) to keep header controls clear.
*   **Validation Spanning**: `<span asp-validation-for="..." class="admin-validation">` renders red error text.
    *   *Mobile Risk*: Error messages wrap and push other fields down, causing form layouts to jump on validation failures.
    *   *Recommendation*: Allocate minimum height parameters or absolute-position messages where possible.

## Empty States

*   Styled via `.admin-empty`, `.admin-empty__title`, and `.admin-empty__text`.
*   These containers render a dashed border card with centered icons, title, and buttons. They are fully responsive and wrap correctly on all viewports.

## Mobile UX Issues

| Issue | File | Impact | Recommended Fix |
|-------|------|--------|-----------------|
| **Truncated Button Labels** | `Post/Index.cshtml`, `MenuGroup/Index.cshtml` | Text like "Xem công khai" wraps, causing button heights to distort. | Reduce text size to `text-xs` on mobile; use icons. |
| **Toast Blocking Taps** | `_AdminLayout.cshtml` | Blocks header links for 4 seconds during CRUD updates. | Reposition mobile toast panels to the bottom center. |
| **Stacked Filter card height** | `Post/Index.cshtml` | Filter card height exceeds table height. | Re-arrange filters into 2 columns on tablet viewports. |
| **Tight Destructive Margins** | `Branch/Index.cshtml` | Danger of accidental "Xóa mềm" clicks. | Enforce gap spacing (`gap-3`) and distinct button backgrounds. |

## Standardized Responsive Button Rules

1.  **Touch Target Size**: Enforce a minimum height of 48px (`h-12`) for all standalone mobile buttons to satisfy WCAG touch targets.
2.  **Full-Width Stacking**: Below `640px` (mobile), form action buttons (Save, Cancel, Back) must stack vertically and expand to `w-full`.
3.  **Visual Separation**: Destructive action buttons must be separated from primary confirmation buttons using margin borders or secondary styles.
4.  **No Text Wrapping**: Avoid uppercase letters with tracking on mobile table action pills. Use compact font sizes (`text-[10px]`) and short text strings.
