# About Page Audit Summary

## Current Status
The current `/gioi-thieu` (About) page is a placeholder skeleton page. It utilizes a light-themed background and boxed panels that violate the dark premium aesthetic of the public brand site. Additionally, it references non-existent food images that fail to load, resulting in an empty and unfinished appearance. It lacks the customer's actual brand narrative, values, and cohesive layout structure.

## Current Route and Files
* **URL Route:** `/gioi-thieu` (mapped via attribute routing `[Route("gioi-thieu")]`)
* **Controller:** [AboutController](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/AboutController.cs) (`Index` action)
* **Razor View:** [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/About/Index.cshtml)
* **Default Layout:** [Shared/_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml) (injected via `_ViewStart.cshtml`)

## Current Visual Problem
* **Theme Incompatibility:** The page renders on the default light theme background (`bg-brand-light` / `#FAF8F3` with beige gradients) instead of the signature dark theme.
* **Boxed Card Styling:** It wraps content inside container elements like `.hero-frame`, `.hero-card`, `.surface-panel`, and `.surface-muted`.
* **Outdated Accent Styles:** It uses heavy gold-focused badges and pills (`brand-goldDeep`, `brand-goldSoft`) that contradict the minimalist layout philosophy.
* **Broken Image Placeholders:** Missing WebP images are removed via JavaScript `onerror="this.remove();"`, causing significant visual shifts and empty layout columns.

## Available Content Sources
* **Project Plan:** The plan document [Kế hoạch xây dựng...docx](file:///f:/Coding/Web%20development/KIG%20Holding/Doc/K%E1%BA%BF%20ho%E1%BA%A1ch%20x%C3%A2y%20d%E1%BB%B1ng%20website%20Truy%E1%BB%81n%20Thuy%E1%BA%BFt%20Champong.docx) outlines the core section definitions (Hero, Origins, Core Values, Food Philosophy, Gallery, dynamic branches list, and CTA block).
* **Brand Ecosystem context:** Sibling public pages (such as the Member plan [03-content-structure-plan.md](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/docs/member-page-audit/03-content-structure-plan.md)) confirm the three-brand structure of KIG Holding: *Truyền Thuyết Champong*, *Gogimaru*, and *KBB Cook*.
* **Customer Copy Note:** The specific file `kigholding.docx` is not available in the workspace or parent folders. Essential brand themes and values are extracted directly from the system plan files.

## Available Image Sources
* **No specific About images** currently exist in the local workspace folders.
* Sibling folders like [wwwroot/uploads/branches/](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/uploads/branches/) and [wwwroot/images/home/images/](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/images/home/images/) contain general assets (e.g. logos, icons, branch banners) that are referenced dynamically.
* **Customer Image Note:** The asset `bìa 1.png` is not available. Suggestions for image aspect ratios and future placement strategies have been mapped out to guide content creation.

## Target Visual Direction
* **Deep Dark Canvas:** Deep black background `#080808` to `#090909` with radial glow accents in the corners (KIG Red `#E50914` at top-left and warm cream `#E7D9BD` at top-right).
* **Open Layouts:** Open typography-led sections with large dynamic text, removing boxed cards and outlines.
* **Minimalist Accents:** Thin dividers (`border-top: 1px solid rgba(255, 255, 255, 0.08)`), selective red kickers, and large light index numbers (`01`, `02`) to structure sections.
* **Standard Transitions:** Smooth animations using JS-driven `data-uw-reveal` attributes.

## Top Findings
1. **Existing Design Tokens:** The complete styling definitions for the Dark Premium Editorial theme (under the `.public-editorial-page__*` prefix) are already implemented in [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) and compiled in `site.css`. They can be reused immediately.
2. **Clean Backend Controller:** The controller logic is fully async and binds branch details through clean services. The route mapping does not need changes.
3. **Preserved Layout:** The shared layout [_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml) handles standard HTML5 scaffolding, fonts, and meta tags correctly, meaning only the view markup itself needs revision.

## Top Risks
* **Missing Assets:** Proceeding to implementation without resolving the missing customer copy and hero image could result in another generic placeholder page.
* **Style Leakage:** Editing the shared `site.css` manually is prohibited; all CSS updates must occur in `input.css` and compile correctly without breaking the Admin panel.

## Recommended Implementation Direction
Apply a structural rewrite to [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/About/Index.cshtml) by wrapping it in `<main class="public-editorial-page">` to enable the dark theme canvas. Format all layout sections into the editorial columns, values grid, and sequence steps based on the verified CSS class system.

## Files Likely Modified Later
* [Views/About/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/About/Index.cshtml) (rewrite markup and layouts)
* [wwwroot/css/input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) (if additional custom aspect ratio classes are needed)

## Files That Must Not Be Modified
* [Controllers/AboutController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/AboutController.cs)
* [ViewModels/AboutIndexViewModel.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/ViewModels/AboutIndexViewModel.cs)
* [Views/Shared/_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml)
* [wwwroot/css/site.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/site.css) (compiled output)
