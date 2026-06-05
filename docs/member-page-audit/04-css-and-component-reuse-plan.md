# CSS and Component Reuse Plan

## Existing Reusable Patterns

| Source | Class/Pattern | Can Reuse? | Notes |
|--------|---------------|------------|-------|
| [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Franchise/Index.cshtml) | `.franchise-page` | Yes (Abstracted) | This defines the premium dark radial background and black color base. We should abstract it to `.public-editorial-page`. |
| [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Franchise/Index.cshtml) | `.franchise-page__shell` | Yes (Abstracted) | Provides vertical padding clamp spacing. |
| [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Franchise/Index.cshtml) | `.franchise-page__section` | Yes (Abstracted) | Handles thin section dividers (`rgba(255, 255, 255, 0.08)`). |
| [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Franchise/Index.cshtml) | `.franchise-page__eyebrow` | Yes (Abstracted) | Handles red gradient kicker lines and text scaling. |
| [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Franchise/Index.cshtml) | `.franchise-page__button--primary` | Yes (Abstracted) | Handles red gradient background and soft red shadow. |
| [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Franchise/Index.cshtml) | `.franchise-page__grid` | Yes (Abstracted) | Controls column grids on desktop, stacking to 1 column on mobile. |
| [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Franchise/Index.cshtml) | `.franchise-page__steps` / `.step` | Yes (Abstracted) | Circular steps timeline layout. |

## Suggested New Page Class Namespace
We strongly recommend implementing the generic **`public-editorial-page__*`** namespace in [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) instead of page-specific `member-page__*` classes.

### Why `public-editorial-page__*` is safer and cleaner:
1. **DRY Principle (Don't Repeat Yourself)**: Using `member-page__*` requires duplicating the exact same CSS declarations already defined for `/lien-he-nhuong-quyen` (`franchise-page__*`), bloating the compiled stylesheet.
2. **Scalability**: By adding a single generic `.public-editorial-page` class structure, future public-facing editorial pages (such as a redesigned Story or Partner page) can immediately reuse the style rules without requiring additional CSS modifications.
3. **Semantic Correctness**: It is semantically incorrect to write `<main class="franchise-page">` on a page representing membership details (`/thanh-vien`). A generic namespace cleanly solves this.

## CSS Changes Needed

| File | Change Type | Reason | Risk |
|------|-------------|--------|------|
| [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) | Add `.public-editorial-page` namespace definitions | Provide styling for the dark radial background, shells, sections, titles, and grid components. | Low. Scoping styles inside `.public-editorial-page` ensures no pollution of light-themed pages. |
| [site.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/site.css) | Auto-generated compile output | Update compiled output file to include the new generic editorial styles. | Low, provided Tailwind's compiler runs successfully. |

## JS Changes Needed

| File | Change Type | Reason | Risk |
|------|-------------|--------|------|
| [uw-reveal.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/uw-reveal.js) | None | The reveal script is already generic, listening to `[data-uw-reveal]` attributes. | None. |

## Avoid Changing
* **Global Base Utilities**: Do not change base elements like `body` or standard layout helpers (e.g. `.container-page`, `.section-shell`) because they are shared by light corporate pages like About, Contact, and Branch lists.
* **Light Theme Component Classes**: Avoid altering `.surface-panel` and `.surface-muted` in [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css). These classes must continue rendering white/grey panels for the admin dashboard and light pages.

## Tailwind / CSS Build Notes
1. **No Manual Edits to site.css**: Never edit [site.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/site.css) directly; it will be overwritten on the next compile cycle.
2. **Compilation Command**: To compile the stylesheet after editing [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css), run:
   ```powershell
   npm run build:css
   ```
3. **Global Share Impact**: Admin templates and public pages share the compiled [site.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/site.css). Always verify the admin dashboard compiles and renders correctly after any CSS rebuild.
