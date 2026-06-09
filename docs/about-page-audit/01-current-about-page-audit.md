# Current About Page Audit

## Route

| URL | Controller | Action | View | Layout |
|-----|------------|--------|------|--------|
| `/gioi-thieu` | `KIGHolding.Controllers.AboutController` | `Index` | `Views/About/Index.cshtml` | `Views/Shared/_Layout.cshtml` |

## Controller Audit
* **File Path:** [Controllers/AboutController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/AboutController.cs)
* **Data Integration:** Dynamic.
* **Logic flow:**
  * Checks connection string using `HasConfiguredDatabase()` (excludes default or invalid local Neon host values).
  * If the database is connected, it attempts to load `SiteSetting` and active `Branch` entities asynchronously via service tasks (`ISiteSettingService.GetSettingsAsync` and `IBranchService.GetActiveBranchesAsync`).
  * In case of database error, exceptions are caught, logged, and default models are instantiated.
* **Model bound:** `AboutIndexViewModel`. Binds:
  * `SiteSetting`
  * `FeaturedBranches` (selects top 3 active branches converted to `BranchCardViewModel`)
  * `SeoTitle = "Giới thiệu"`
  * `SeoDescription = "Khám phá câu chuyện Truyền Thuyết Champong..."`
* **Dependencies:** Clean service abstractions (`ISiteSettingService`, `IBranchService`, `ILogger`). No tight database coupling.

## Razor View Audit
* **File Path:** [Views/About/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/About/Index.cshtml)
* **Model Type:** `@model AboutIndexViewModel`
* **Metadata declaration:** Maps view data:
  * `ViewData["Title"] = Model.SeoTitle;`
  * `ViewData["MetaDescription"] = Model.SeoDescription;`
* **Markup structure:** Consists of 8 distinct sections mapped to basic page elements.

## Current Page Sections

| Section | Current Markup/Classes | Purpose | Issue |
|---------|------------------------|---------|-------|
| **Hero** | `hero-frame`, `hero-card`, `public-page-title mt-5 text-brand-dark` | Header introduction & main title | Wrapped in boxed container cards, light theme background and text, uses old gold badge style. |
| **Origin** | `section-shell`, `food-image-wrap aspect-[4/5]`, `SectionHeading` VC | Brand origins narrative | Light theme styling, hardcoded copy, image doesn't exist and fails. |
| **Values** | `section-band`, `surface-panel`, `card-hover` | Four core brand values | Boxed cards with light background panels (`surface-panel`), old gold soft icons. |
| **Food Philosophy** | `section-shell`, `surface-muted` | Nước dùng, Gia vị, Nguyên liệu, Cùng thưởng thức | Uses `.surface-muted` panels. Contains an absolute positioned badge with bright background (`bg-white/92`) violating the dark aesthetic. |
| **Signature Menu** | `section-band`, `surface-panel` | Redirects to menu groups | Uses white boxed card panels. Text is hardcoded. |
| **Restaurant Space** | `section-shell`, `food-image-wrap`, `figcaption bg-white/90` | Image grid of interiors | Uses white background figure captions. Placed in light theme layout. All images fail to load. |
| **Featured Branches** | `section-band`, `BranchCard` VC | active branches overview | Renders clean dynamic view components, but layout is trapped in light context. |
| **CTA** | `section-shell`, `surface-panel` | Call-to-action booking block | Boxed in `.surface-panel` with light background and text. |

## Current Styling Sources
* Component classes like `hero-frame`, `hero-card`, `surface-panel`, `surface-muted`, `btn-primary`, and `btn-secondary` are defined inside [wwwroot/css/input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) under `@layer components`. These classes use light backgrounds (`#FAF8F3`, `#FFFFFF`) and dark text colors (`#111827`, `#6B7280`).

## Current Image Usage
* References local placeholder images:
  * `/images/placeholders/about-hero.webp`
  * `/images/placeholders/korean-street-noodle.webp`
  * `/images/placeholders/champong-broth.webp`
  * `/images/placeholders/group-tables.webp`
  * `/images/placeholders/kitchen-chef.webp`
  * `/images/placeholders/food-table.webp`
  * `/images/placeholders/service-counter.webp`
* **Visual failure:** These files are not present in the project. The tag has `onerror="this.remove();"`, which deletes the image node from the DOM, causing empty layout column gaps.

## Current Responsive Behavior
* Uses standard Tailwind grid configurations (`lg:grid-cols-[0.92fr_1.08fr]`, `md:grid-cols-3`, `sm:grid-cols-2 lg:grid-cols-4`) which stack to 1 column on mobile.
* **Responsive failures:** Button action groups do not stack correctly or stretch full-width on screens below 640px, violating mobile optimization guidelines.

## Current Animation Usage
* Uses JS-driven `data-uw-reveal="fade-up"` on sections (such as Hero and Origin).
* **Limitation:** Does not utilize delays (`data-uw-delay`) or entry directions to guide user focus through long copy.

## Current SEO Metadata
* Meta descriptions and titles are configured dynamically in the controller and bound in the view. They integrate seamlessly with [Shared/_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml) header parsing.

## Backend Contract Notes
* The page expects a valid database connection for loading settings and active branches.
* Model requirements: `AboutIndexViewModel` must contain a non-null `FeaturedBranches` collection (even if empty).
* Component dependencies: Invokes view components `SectionHeading` and `BranchCard`.
* Form post contracts: None. The page is read-only.

## Issues Found
1. **Bright Theme Canvas:** Renders on default white/sand background instead of premium dark gradients.
2. **Heavy Boxed Containers:** Heavy cards (`surface-panel` / `surface-muted`) block content flow.
3. **Broken Image Placeholders:** Missing image references break the visual layout.
4. **Skeleton Content:** Copy consists of descriptive placeholders for development layout testing rather than real brand details.
