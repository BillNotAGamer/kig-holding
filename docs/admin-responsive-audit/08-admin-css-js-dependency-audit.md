# Admin CSS and JS Dependency Audit

This document details the stylesheets and JavaScript files loaded by the Admin panel, their dependencies, duplication, and risk areas.

## CSS Sources

| File | Admin Relevance | Notes |
|------|-----------------|-------|
| `wwwroot/css/input.css` | **High**: Contains source Tailwind directives (`@tailwind base;` etc.) and `@layer components` definitions for all admin elements (`.admin-page`, `.admin-card`, `.admin-table`). | Compiled via Tailwind CLI. Shared by both public and admin sites. Contains a duplicate set of form styling classes. |
| `wwwroot/css/site.css` | **High**: Compiled CSS output. | **Must not be edited directly.** Any modifications will be overwritten during the next Tailwind build task. |
| `tailwind.config.js` | **Medium**: Tailwind configuration file. | Defines colors (`brand-black`, `brand-light`, `brand-red`), font family (Quicksand), custom transition animations (`drawer-in`), and content scope. |

## Admin CSS Classes / Components

| Class / Pattern | Defined In | Used By | Responsive Risk |
|-----------------|------------|---------|-----------------|
| `.lg:grid-cols-[18rem_1fr]` | `_AdminLayout.cshtml` inline | Global shell container | **Critical**: Stacks sidebar on top of content on mobile. |
| `.admin-field` / `.admin-post-field` | `input.css` | Forms (create/edit views) | **Medium**: Standardizes inputs. `admin-post-field` has `rounded-2xl` while `admin-field` has `rounded-xl`. Highly redundant. |
| `.admin-actions` | `input.css` | Action grids and headers | **Medium**: Wrap container. Stacks buttons on mobile, leading to alignment jumps. |
| `.admin-table` | `input.css` | Listing indexes | **High**: Static layout. Truncates column text or overflows the container. |
| `.admin-upload__preview` | `input.css` | Upload containers | **Medium**: Sizing is rigid. Thumbnail images can stretch beyond card width. |

## JavaScript Sources

| File | Admin Relevance | Behavior |
|------|-----------------|----------|
| `wwwroot/js/site.js` | **Low**: Public navigation drawer script. | Target selectors are absent in the admin area, so it acts as dead weight. |
| `wwwroot/js/admin.js` | **High**: Admin-specific preview utilities. | Contains two vanilla functions: `initImagePreviews` (handles preview canvas via FileReaders) and `initFileLists` (shows name lists for multi-upload inputs). |
| `wwwroot/lib/jquery/...` | **High**: jQuery core dependency. | Required by ASP.NET Core unobtrusive client-side validations (`_ValidationScriptsPartial`). |
| `_ValidationScriptsPartial` | **High**: ASP.NET MVC validation engine. | Loaded dynamically at the bottom of forms to display client-side validation errors. |

## Shared Dependency Risks

1.  **Public CSS Leakage**: Both the public site (dark premium theme) and the admin panel (light theme) load `site.css`. Modifying base rules inside `input.css` (like custom margins, headings, or scrollbar behaviors) will immediately affect the public layout and pages.
2.  **Tailwind Configuration Changes**: Editing the tailwind config breakpoints or theme extensions (`tailwind.config.js`) to fix admin shell details will affect responsive layouts on public landing pages.
3.  **Global Script Clashes**: Shared execution paths in `site.js` (e.g. keydown listener for `Escape` to close drawers) can clash with admin scripts if class naming conventions overlap.

## Recommended CSS Strategy

To implement responsive changes safely without breaking public pages:
*   **Use Inline Tailwind Utility Classes**: Implement responsive fixes (such as hidden drawer states: `hidden lg:block`, grid stacking: `grid grid-cols-1 md:grid-cols-2`) directly inside Razor files (`.cshtml`) as classes. This completely isolates modifications to the target admin screen.
*   **Scoped Components**: If CSS component rules must be modified inside `input.css`, wrap them inside the `.admin-page` or `.admin-card` parent selectors.
    *   *Example*:
        ```css
        .admin-page .btn-primary {
          @apply w-full sm:w-auto;
        }
        ```
*   **Preserve Twin Classes**: Keep both `.admin-field` and `.admin-post-field` active to avoid breaking model bindings or HTML layouts. However, map their responsive properties to the same Tailwind tokens.

## Recommended JS Strategy

*   **Isolate Admin Drawer JS**: Write the mobile sidebar toggle code inside the admin-only script file [admin.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/admin.js).
*   **Vanilla JS only**: Avoid adding third-party scripts or libraries. Use simple vanilla listeners:
    ```javascript
    // Toggle Admin Drawer
    const openBtn = document.querySelector('[data-admin-menu-open]');
    const closeBtn = document.querySelector('[data-admin-menu-close]');
    const drawer = document.querySelector('[data-admin-drawer]');
    const overlay = document.querySelector('[data-admin-menu-overlay]');

    if (openBtn && drawer) {
        openBtn.addEventListener('click', () => {
            drawer.classList.add('translate-x-0');
            drawer.classList.remove('-translate-x-full');
            overlay?.classList.remove('hidden');
        });
        // Add matching close listeners...
    }
    ```
*   **Safe-Check Selectors**: Ensure all shared scripts check if selectors are present before attaching event listeners to avoid console errors.
