# Member Page Implementation Strategy

## Implementation Principles
* **Separation of Styles**: Scoping all editorial adjustments under a new `.public-editorial-page` container class to isolate changes from the rest of the application.
* **Typographic and Open Layout focus**: Ensuring we strictly follow the Dark Premium Editorial rules (no boxed cards, thin dividers, red accent kickers).
* **Audit-Only Compliance**: Producing a fully planned, step-by-step instructions roadmap for execution in the next prompt.

## Backend Contract Preservation
* The route `/thanh-vien` mapped by [MemberController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/MemberController.cs) must not be altered.
* Existing link references to other controllers/views (e.g. `/lien-he`, `/thuc-don`) must remain active.

## Proposed Files to Modify

| File | Planned Change | Risk |
|------|----------------|------|
| [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Member/Index.cshtml) | Replace skeleton markup with Dark Premium structure and Vietnamese copy. | Low. Purely structural HTML edits with no dynamic bindings to break. |
| [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) | Add rules for `.public-editorial-page` mirroring `.franchise-page`. | Low. Scoped under a specific parent class to avoid global style pollution. |
| [site.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/site.css) | Re-compile using Tailwind compiler. | Low. Ensure the compilation script executes without warnings. |

## Do Not Modify
* [Controllers/MemberController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/MemberController.cs)
* [Views/Shared/_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml)
* [Views/_ViewStart.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/_ViewStart.cshtml)
* [Views/_ViewImports.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/_ViewImports.cshtml)
* All backend model files and other controllers.

---

## Phase 1 — Replace Skeleton Markup

* **Goal**: Replace the outdated light-themed card blocks in the Razor view with a modern open-grid markup skeleton.
* **Files**: [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Member/Index.cshtml)
* **Changes**:
  * Set a wrapper `<main class="public-editorial-page public-editorial-page--dark relative isolate overflow-hidden text-white">`.
  * Define Section 1 (Hero) with kicker, large clamp-styled header, lead copy, and CTA buttons using `public-editorial-page__*` tags.
  * Define Section 2 (Membership Promise) as a two-column open comparison block.
  * Define Section 3 (Quyền lợi) as a 4-column open list grid.
  * Define Section 4 (Roadmap) using a vertical circle step layout.
  * Define Section 5 (Ecosystem) as a 3-column logo/brand detail grid.
  * Define Section 6 (Footer CTA) as a centered editorial registration block.
* **Risks**: Typographical mismatches or wrong local URLs.
* **Acceptance Criteria**:
  * No `.hero-frame`, `.hero-card`, `.surface-panel`, or `.surface-muted` classes are left in the view.
  * Structural HTML is clean and tags balance correctly.

---

## Phase 2 — Apply Dark Premium Editorial Styling

* **Goal**: Implement the generic `.public-editorial-page` styles inside [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) to support the new markup classes.
* **Files**: [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css)
* **Changes**:
  * Add the `.public-editorial-page` class containing deep black radial gradient definitions.
  * Map child components like `__shell`, `__section`, `__eyebrow`, `__title`, `__lead`, `__actions`, `__button` (both primary and secondary variants), `__grid`, `__item`, `__item-index`, `__columns`, `__bullet`, `__steps`, `__step`, and `__cta` to their corresponding properties.
* **Risks**: Typo in CSS rules causing compiler breakage.
* **Acceptance Criteria**:
  * Rules match the visual definitions from the franchise page layout.
  * No style pollution occurs on light-themed pages.

---

## Phase 3 — Responsive and Animation Pass

* **Goal**: Ensure the layout scales down beautifully to mobile viewports and registers reveal animations properly.
* **Files**:
  * [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Member/Index.cshtml)
  * [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css)
* **Changes**:
  * Add mobile media queries in CSS to stack grids and stretch buttons to 100% on small devices.
  * Add `data-uw-reveal="fade-up"` and staggered `data-uw-delay` attributes on list and grid elements in [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Member/Index.cshtml).
* **Risks**: Too many simultaneous transitions causing animation lag.
* **Acceptance Criteria**:
  * All reveal tags are attribute-driven (no legacy class overrides).
  * Page loads and animates elements smoothly as the user scrolls.

---

## Phase 4 — Build and Browser QA

* **Goal**: Recompile site CSS, build the ASP.NET Core solution, and verify layout execution across viewports.
* **Commands**:
  * Run CSS compilation: `npm run build:css`
  * Run project build: `dotnet build KIGHolding.sln`
* **Pages to check**:
  * `/thanh-vien` (target page)
  * `/lien-he-nhuong-quyen` (Franchise page - ensure it is unaffected)
  * Admin dashboard (ensure light theme panel elements are unaffected)
* **Acceptance Criteria**:
  * CSS rebuilds successfully without PostCSS warnings.
  * Dotnet build succeeds with zero errors.
  * `/thanh-vien` displays deep dark radial gradients and premium editorial typography.
  * Mobile view stacks correctly and buttons fill their containers.

---

## Manual QA Checklist
- [ ] `/thanh-vien` uses dark canvas.
- [ ] No white/gray panels are present.
- [ ] No `.hero-frame`, `.hero-card`, `.surface-panel`, or `.surface-muted` classes are loaded.
- [ ] No backend logic was changed.
- [ ] No route was changed.
- [ ] Header and footer remain intact and correctly positioned.
- [ ] Mobile view stacks correctly.
- [ ] `data-uw-reveal` animations activate smoothly on scroll.
- [ ] `dotnet build KIGHolding.sln` passes.
- [ ] `npm run build:css` passes.
- [ ] Public smoke check passes on target page.

---

## Rollback Plan
If any compile-time conflicts or layout regressions occur:
1. Revert changes to [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Member/Index.cshtml) to restore the light skeleton page.
2. Remove `.public-editorial-page` blocks from [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) and recompile using `npm run build:css`.
