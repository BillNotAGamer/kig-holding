# Current Member Page Audit

## Route

| URL | Controller | Action | View | Layout |
|-----|------------|--------|------|--------|
| `/thanh-vien` | [MemberController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/MemberController.cs) | `Index()` | [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Member/Index.cshtml) | [_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml) |

## Controller Audit
The [MemberController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/MemberController.cs) handles the routing.
* **Attributes**: `[Route("thanh-vien")]` at class level, `[HttpGet("")]` at method level.
* **Functionality**: Simple and static. Sets title and description via `ViewData["Title"]` and `ViewData["MetaDescription"]`, then returns `View()`.
* **Statefulness**: Completely stateless. No database services or ViewModels are loaded.

## Razor View Audit
The Razor view is located at [Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Member/Index.cshtml).
* **View Model**: None declared (no `@model` directive).
* **Layout**: Standard layout is inherited from [_ViewStart.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/_ViewStart.cshtml) (`Layout = "_Layout";`).
* **HTML Tags**: Uses regular section components (`section`, `div`, `article`, `a`, `p`, `h1`, `h2`, `h3`).

## Current Page Sections

| Section | Current Markup/Classes | Purpose | Issue |
|---------|------------------------|---------|-------|
| **1. Hero Section** | `<section class="hero-frame">` wrapping `<div class="hero-card">` | Welcome and introduce the membership program placeholder. | Boxed structure with `.hero-frame` and `.hero-card`. Uses gold kicker badge and dark text color. |
| **2. Status Panel (Right of Hero)** | `<div class="surface-panel p-6 sm:p-7">` containing articles with `.surface-muted` | "Sắp ra mắt" information grid list of future membership benefits. | Uses light cards (`bg-brand-white` and `bg-brand-light`) which break the dark theme look. |
| **3. Roadmap Section** | `<section class="section-band section-shell">` using `SectionHeading` component and a grid of `.surface-panel` articles | Show stages of the membership program launch. | Boxed cards with light backgrounds (`.surface-panel`). Uses the default `SectionHeading` component which is not tailored for dark editorial layouts. |
| **4. CTA Banner** | `<section class="section-shell pt-0 lg:pt-0">` wrapping a `.surface-panel` container | Call to action for next steps (Explore Menu, Contact). | Boxed shadow card on light theme, mismatching the desired open typography CTA. |

## Current Styling Sources
* **Base Styles**: Inherits `.bg-brand-light` and `.text-brand-dark` on `<body>` from [_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml).
* **Component Styles**: Uses `.surface-panel` (white background, border, panel shadow) and `.surface-muted` (light grey background, border, soft shadow) defined in [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) lines 117-123.
* **Layout Utilities**: Uses `.hero-frame`, `.section-shell`, and `.container-page` from Tailwind classes.
* **Accents**: Uses Tailwind utility classes like `text-brand-goldDeep` (`#9E7F43`), `bg-brand-goldSoft/45`, and `border-brand-gold/25`.

## Current Responsive Behavior
* On screens `< 1024px`, the grid layouts stack vertically (`lg:grid-cols-...` rules drop back to single columns).
* Inside the Hero, the grid splits 50/50 on desktop and stacks to 1 column on mobile, which functions correctly but looks clustered due to card box padding.
* Buttons stack and stretch into a flex column layout on mobile screens (`flex flex-col gap-3 sm:flex-row`).

## Current Animation Usage
* Uses the modern attribute-driven animation system defined in [uw-reveal.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/uw-reveal.js).
* Selectors: `data-uw-reveal="fade-up"` is configured on the hero wrapper card and the roadmap cards.
* *Note*: No legacy reveal classes (e.g. `.scroll-reveal`, `.reveal-up`) are present, which adheres to style guide rules.

## Current SEO Metadata
* Configured correctly at the top of the Razor view:
  ```razor
  ViewData["Title"] = "Thành viên KIG Holding";
  ViewData["MetaDescription"] = "Trang thành viên KIG Holding đang được hoàn thiện, với các thông tin ưu đãi, tích điểm và cập nhật chương trình mới.";
  ```
* These are correctly mapped by [_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml) into `<title>` and `<meta name="description">` tags.

## Backend Contract Notes
* The page does not send form requests (POST) or interact with backend controllers using AJAX.
* It only contains absolute internal anchors like `/thuc-don` (Menu) and `/lien-he` (Contact) which must be preserved to prevent dead links.

## Issues Found
1. **Light Background**: The page is fully light-themed, which directly contradicts the Dark Premium Editorial aesthetic defined in the style guide.
2. **Heavy Boxed Cards**: Uses framed card boxes (`.hero-card`, `.surface-panel`, `.surface-muted`) which clutter the layout and limit breathing room.
3. **Gold Accent Excess**: Uses gold badge elements and text instead of subtle KIG Red gradients.
4. **Poor Typography Contrast**: The heading fonts and styles do not match the magazine-like editorial look of Franchise (`Views/Franchise/Index.cshtml`).
5. **No Layout Isolation**: Wrapping elements are styled inline, and lack a page-specific wrapper class (e.g. `.public-editorial-page`) to override the body background.
