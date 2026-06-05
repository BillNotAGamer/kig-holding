# Member Page Audit Summary

## Current Status
The Membership page (`/thanh-vien`) is currently a light skeleton placeholder page. It uses the light corporate aesthetic of the repository rather than the premium Dark Editorial style used on pages like Franchise (`/lien-he-nhuong-quyen`).

## Current Route and Files
* **Route**: `/thanh-vien`
* **Controller**: [MemberController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/MemberController.cs)
* **Razor View**: [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Member/Index.cshtml)
* **Layout**: [_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml) (resolved via [_ViewStart.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/_ViewStart.cshtml))

## Current Visual Problem
The page violates several core design guidelines of the Dark Premium Editorial style:
* It has a light canvas background (`bg-brand-light` / `#FAF8F3`) inherited from [_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml).
* It groups content into boxed cards using classes like `.hero-frame`, `.hero-card`, `.surface-panel`, and `.surface-muted`.
* It utilizes gold accents (`text-brand-goldDeep`, `bg-brand-goldSoft/45`, `border-brand-gold/25`) instead of the selective KIG Red (`#E50914`) gradient system.
* It lacks editorial typography, open grids, item dividers, and large number indices.

## Target Visual Direction
Align `/thanh-vien` with the **Dark Premium Editorial** design system:
* Set a deep dark canvas background (`#080808` to `#090909`) with red/cream radial gradients.
* Implement an open Hero layout instead of boxed frames.
* Replace card panels with thin borders (`border-top: 1px solid rgba(255, 255, 255, 0.08)`).
* Introduce large faded red index numbers (`01`, `02`, `03`) for lists.
* Use typography-led hierarchy with warm kem white text (`#fff7ed`) and selective KIG Red accents.
* Retain the attribute-driven `data-uw-reveal` animations.

## Top Findings
1. **No Backend Logic**: The [MemberController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/MemberController.cs) is fully static and returns `View()` without any model, service call, or form submission. It is extremely safe to modify the view markup since there is no backend state to preserve.
2. **Missing CSS Namespace**: The target `public-editorial-page__*` namespace proposed in the style guide does not exist in [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) yet. However, the identical `.franchise-page__*` style blocks are fully implemented.
3. **No ViewStart Override**: The member view relies on the main public layout, which defaults to light theme styles on the `<body>` element. Wrapping the page in a `.public-editorial-page--dark` container will override layout text and background styles successfully without changing [_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml).

## Top Risks
* **Admin Styling Contamination**: Since public pages and the admin panel share compiled CSS via [site.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/site.css), any global base changes or modifications to helper classes like `.surface-panel` or `.surface-muted` would break the light theme in admin and other pages (e.g. `/about`).
* **Semantic pollution**: Duplicating styling rules as `.member-page__*` inside [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) leads to code bloat. The cleanest approach is implementing the proposed generic `.public-editorial-page__*` styles.

## Recommended Implementation Direction
1. **Extend CSS**: Add the `.public-editorial-page__*` rules in [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) matching the rules defined for `.franchise-page__*` but scoped for generic editorial layouts.
2. **Re-compile CSS**: Run `npm run build:css` to generate the new [site.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/site.css).
3. **Rewrite Razor View**: Completely replace the placeholder skeleton in [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Member/Index.cshtml) with the new layout following the Dark Premium Editorial rules, referencing the Vietnamese copywriting structure.

## Files Likely Modified Later
* [Views/Member/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Member/Index.cshtml)
* [wwwroot/css/input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css)
* [wwwroot/css/site.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/site.css) (compiled result of CSS build)

## Files That Must Not Be Modified
* [Controllers/MemberController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/MemberController.cs) (already returns standard routing and ViewData)
* [Views/Shared/_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml) (shared with light pages)
* [Views/_ViewStart.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/_ViewStart.cshtml)
* [Views/_ViewImports.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/_ViewImports.cshtml)
