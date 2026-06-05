# Style Guide Gap Analysis

| Style Guide Rule | Current Page Status | Gap | Recommendation |
|------------------|--------------------|-----|----------------|
| **Dark canvas** | Inherits `.bg-brand-light` (#FAF8F3) from body | Page is light, not deep dark | Wrap content in `.public-editorial-page.public-editorial-page--dark` with custom radial gradients (KIG Red/Cream on black canvas). |
| **Hero open layout** | Uses `.hero-frame` container and `.hero-card` box | Text is boxed inside a border card panel | Remove `.hero-frame` and `.hero-card`. Place content directly on the dark canvas. |
| **No boxed cards** | Content is split into `.surface-panel` and `.surface-muted` card panels | Visually heavy borders and white/grey card backgrounds | Remove all boxed cards. Let the content breathe directly on the dark background. |
| **Typography-led title** | Uses `.public-page-title` with `.text-brand-dark` | Missing the custom editorial styling and warm kem white color | Style the `<h1>` title using `.public-editorial-page__title` with warm kem white `#fff7ed` and clamp sizing. |
| **Eyebrow/kicker** | Uses standard inline flex badge with gold borders | Missing the signature gradient red line and typography | Use `.public-editorial-page__eyebrow` with uppercase letter spacing and left gradient red border. |
| **Lead paragraph max width** | Uses `.text-lead.text-brand-gray` with no max-width constraints | Lines can stretch too wide on large viewports | Apply `.public-editorial-page__lead` with `max-width: 44rem` to improve readability. |
| **Red accent usage** | Uses gold colors for badges and kickers | No red accents are present on the page | Replace all gold colors with KIG Red (`#E50914`) for eyebrows, bullet marks, and primary buttons. |
| **Thin section dividers** | Uses empty space or background band colors | Missing the subtle horizontal section borders | Separate sections using `border-top: 1px solid rgba(255, 255, 255, 0.08)`. |
| **Open grid items** | Uses `.surface-panel` card boxes for grid items | Items are enclosed inside borders and solid background blocks | Render items inside a borderless grid using `.public-editorial-page__grid` and `.public-editorial-page__item`. |
| **Numbered item anchors** | Items are introduced with gold subheadings | Lacks the large faded numbered indices | Prepend grid items with an index tag `<span class="public-editorial-page__item-index">01</span>` using large, faded red coloring. |
| **CTA button style** | Uses `.btn-primary` (black background) and `.btn-secondary` (white background) | Standard business buttons instead of premium editorial ones | Use `.public-editorial-page__button--primary` (Red gradient with red glow) and `.public-editorial-page__button--secondary` (transparent with red hover outline). |
| **`data-uw-reveal`** | Configured on large container divs | Animations are coarse and delay patterns are default | Refine `data-uw-reveal="fade-up"` on individual items with staggered delays (`data-uw-delay="80"`, `120`, `160`). |
| **Mobile stack behavior** | Stacks vertically but buttons do not stretch | Buttons are small and off-center on mobile screens | Make buttons full-width (`w-full`) and stacked vertically on viewports `< 768px`. |
| **Avoid old reveal classes** | Adheres to this rule (no legacy reveal classes present) | None | Continue using `data-uw-reveal` attributes only. |
| **Avoid light surface classes** | Violates this rule by using `.surface-panel` and `.surface-muted` | Light background blocks disrupt the premium dark aesthetic | Avoid `.surface-panel` and `.surface-muted` completely in the new layout. |
| **Avoid gold-heavy styling** | Violates this rule by using multiple gold border/text utilities | Gold coloring clashes with the KIG Red accent standard | Strip out all `brand-gold` classes. |

## Reusable Patterns from Franchise View
The Franchise page ([Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Franchise/Index.cshtml)) contains highly relevant layout structures that can be safely abstracted:
* **Radial Gradient background**: The base container `.franchise-page` has a premium dark radial gradient. We can reuse this exact styling setup for our generic `.public-editorial-page` container.
* **Open Grid Layout**: The `.franchise-page__grid` and `.franchise-page__item` border-top structure works perfectly for listing membership benefits.
* **CTA Block**: The two-column side-by-side CTA layout in `.franchise-page__cta` is ideal for the final section of `/thanh-vien`.
* **Step Process**: The circular step process `.franchise-page__steps` can be reused to showcase the membership roadmap.
