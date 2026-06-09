# About Page Implementation Strategy

## Implementation Principles
1. **CSS Compilation Integrity:** Modifying the compiled output `site.css` manually is strictly prohibited. All CSS enhancements must go through `input.css` and be built using the Tailwind compiler.
2. **Zero Backend Regression:** Rebuilding `/gioi-thieu` must require no changes to routes, controller actions, database contexts, or existing view models.
3. **Design Blueprint Alignment:** Strictly enforce the public Dark Premium Editorial style guide rules (dark canvas, open hero, unboxed grids, responsive layout collapse, and scroll reveal animations).
4. **Graceful Degradation:** Placeholders and image frames must fall back cleanly if assets fail to load.

## Backend Contract Preservation
* **Preserve ViewData binds:** Keep standard headers (`ViewData["Title"] = Model.SeoTitle;` and `ViewData["MetaDescription"] = Model.SeoDescription;`).
* **Preserve Model references:** Do not change `@model AboutIndexViewModel`.
* **Preserve view component calls:** Keep calling `BranchCard` and `SectionHeading` where appropriate.
* **Keep routing static:** The route is handled by attribute routing on `AboutController` and must not be altered.

## Proposed Files to Modify

| File | Planned Change | Risk |
|------|----------------|------|
| [Views/About/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/About/Index.cshtml) | Rewrite the view to replace light/boxed components with dark editorial markup blocks, integrate new copy themes, map image nodes, and configure scroll reveal parameters. | Low. View-only change that can be tested locally and rolled back immediately. |

## Do Not Modify
* [Controllers/AboutController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/AboutController.cs)
* [ViewModels/AboutIndexViewModel.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/ViewModels/AboutIndexViewModel.cs)
* [Views/Shared/_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml)
* [wwwroot/css/site.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/site.css)

## Proposed Page Structure
* **Container Wrapper:** `<main class="public-editorial-page">`
* **Shell Wrapper:** `<div class="public-editorial-page__shell">`
  * **Section 1: Hero Banner** (Open layout: kicker, dynamic clamped title, lead paragraph, primary red gradient button and secondary transparent button).
  * **Section 2: Khởi nguồn & Sứ mệnh (Origins)** (Two columns: story text on the left, portrait vertical image on the right).
  * **Section 3: Giá trị cốt lõi (Values)** (4-column text grid, no boxed borders, large red index numbers `01`-`04`).
  * **Section 4: Triết lý ẩm thực (Culinary Philosophy)** (Two columns: details on broth/ingredients on the left, portrait food close-up on the right).
  * **Section 5: Hệ sinh thái thương hiệu (Ecosystem)** (3-column grid introducing Truyền Thuyết Champong, Gogimaru, and KBB Cook).
  * **Section 6: Hệ thống chi nhánh** (Loads the dynamic view component `BranchCard` collections).
  * **Section 7: Booking CTA** (Centered dark pre-footer conversion block with quick booking buttons).

---

## Phase 1 — Replace Skeleton Markup
* **Goal:** Remove the light card frames and prepare the Razor view architecture.
* **Files:** [Views/About/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/About/Index.cshtml)
* **Changes:** Replace `.hero-frame`, `.hero-card`, and boxed columns with the `.public-editorial-page` container.
* **Risks:** Broken HTML structure.
* **Acceptance Criteria:** Razor compiles successfully without errors.

## Phase 2 — Apply Dark Premium Editorial Styling
* **Goal:** Implement the dark canvas overrides, typography layout, and select KIG Red accent markers.
* **Files:** [Views/About/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/About/Index.cshtml)
* **Changes:** Use `.public-editorial-page__*` style tokens (e.g. `__eyebrow`, `__title`, `__lead`, `__grid`, `__item-index`).
* **Risks:** Low text contrast against dark backgrounds.
* **Acceptance Criteria:** Page canvas is dark thẳm (`#080808` to `#090909`), text is readable and uses cream/white colors, gold accents are replaced with red/cream theme tokens.

## Phase 3 — Integrate Provided Image and Future Image Slots
* **Goal:** Integrate image elements with defined aspect-ratio containers.
* **Files:** [Views/About/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/About/Index.cshtml)
* **Changes:** Embed `bìa 1.png` (or placeholder equivalent) with appropriate landscape wrappers. Map portrait vertical slots for culinary detail images.
* **Risks:** Broken layouts if images fail to load.
* **Acceptance Criteria:** Images conform to responsive layout constraints, and fallbacks handle errors gracefully.

## Phase 4 — Responsive and Animation Pass
* **Goal:** Ensure smooth scroll reveal behaviors and mobile layout stacking.
* **Files:** [Views/About/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/About/Index.cshtml)
* **Changes:** Attach `data-uw-reveal`, `data-uw-delay`, and `data-uw-once="true"` attributes to page elements. Configure CTA action groups to stack and scale to 100% on mobile.
* **Risks:** Overlapping layout elements or sluggish scroll rendering on low-end devices.
* **Acceptance Criteria:** Scroll transitions render smoothly at 60fps. Mobile layouts collapse to a single column cleanly.

## Phase 5 — Build and Browser QA
* **Goal:** Final build checks and smoke tests.
* **Commands:**
  * Build the solution:
    ```powershell
    dotnet build KIGHolding.sln
    ```
  * Compile the CSS:
    ```powershell
    npm run build:css
    ```
* **Pages to check:**
  * `/gioi-thieu` (verify dark premium styling)
  * `/lien-he-nhuong-quyen` (verify franchise styles are unaffected)
  * `/thanh-vien` (verify member styles are unaffected)
* **Acceptance Criteria:** Compilation passes without warnings, public pages load successfully, header/footer elements remain aligned.

---

## Manual QA Checklist
- [ ] Page background is deep black with subtle red/cream radial glows.
- [ ] Text uses Quicksand font with dynamic clamp scaling.
- [ ] Main hero uses an open layout with no bounding box borders.
- [ ] No `.surface-panel` or `.surface-muted` panels are used.
- [ ] Grid items use thin horizontal border lines (`border-top: 1px solid rgba(255, 255, 255, 0.08)`) and large numbering (`01`, `02`).
- [ ] Action buttons display KIG Red gradients with soft shadows.
- [ ] All columns stack cleanly to 1 column on screens below 1024px.
- [ ] Text lines are restricted to `max-width: 44rem` to optimize readability.

## Rollback Plan
Create a copy of the original [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/About/Index.cshtml) file in a backup folder:
`backups/Views/About/Index.cshtml.bak`
In case of layout regressions or build failures, restore the original view immediately by executing:
```powershell
Copy-Item "backups/Views/About/Index.cshtml.bak" "Views/About/Index.cshtml" -Force
```
This guarantees zero downtime on production environments.
