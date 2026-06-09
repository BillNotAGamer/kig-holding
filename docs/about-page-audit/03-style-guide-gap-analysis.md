# Style Guide Gap Analysis

| Style Guide Rule | Current Page Status | Gap | Recommendation |
|------------------|--------------------|-----|----------------|
| **Dark canvas** | Uses light background class `.bg-brand-light` and sand color gradients. | Direct violation of the dark premium identity. | Replace body background layout style on the page by wrapping it in `.public-editorial-page` with dark gradients. |
| **Open hero layout** | Wraps the hero contents in `.hero-frame`. | Constrained visual space. | Remove `.hero-frame` layout boundaries. |
| **No boxed hero cards** | Hero text is enclosed in `.hero-card`. | Heavy borders and layout boxing. | Remove `.hero-card` wrap, placing text directly on dark canvas. |
| **Typography-led title** | Uses `.public-page-title` with dark text color `text-brand-dark`. | Small scale on large screens, lacks editorial tone. | Apply `.public-editorial-page__title` class for clamped sizing. |
| **Eyebrow/kicker** | Uses gold badge with border `.border-brand-gold/25`. | Outdated badge pill styling. | Use `.public-editorial-page__eyebrow` class with red gradient line prefix. |
| **Lead paragraph max width** | Extends without width limits. | Poor text legibility. | Apply `max-width: 44rem` using `.public-editorial-page__lead` class. |
| **Red accent usage** | Relies on gold soft backgrounds (`bg-brand-goldSoft`). | Lacks consistent primary brand accent. | Apply KIG Red (`#E50914`) for eyebrows, bullet indicators, and primary CTAs. |
| **Thin section dividers** | Spaced with margins only, no top borders. | Lacks visual rhythm. | Add `border-top: 1px solid rgba(255, 255, 255, 0.08)` to separate sections. |
| **Open grid items** | Wraps grid items in `.surface-panel` or `.surface-muted`. | Boxed boxes block content breathing space. | Use `.public-editorial-page__item` with top border dividers instead of panels. |
| **Numbered item anchors** | Uses icons or simple headings. | Lacks clear editorial structure. | Add `.public-editorial-page__item-index` (e.g., `01`, `02`) to grid items. |
| **CTA button style** | Uses `.btn-primary` (black background) and `.btn-secondary` (white background). | Bright button styles violate dark theme look. | Apply `.public-editorial-page__button--primary` (red gradient) and `.public-editorial-page__button--secondary` (transparent with border). |
| **Image usage rules** | Placeholders fail and remove the nodes completely. | Breaks layout column balance. | Remove white/light overlay blocks; use portrait/landscape aspect ratio containers with safe fallbacks. |
| **`data-uw-reveal`** | Limited to simple fade-up declarations without delays. | Uniform enter animations feel flat. | Configure distinct delays (`data-uw-delay="80/120/160"`) to direct user eye path. |
| **Mobile stack behavior** | Columns collapse, but action buttons and borders do not adjust. | Stiff mobile layout. | Stack CTAs vertically on screens below 640px. Hide desktop vertical borders (`border-l`) on mobile. |
| **Avoid old reveal classes** | Compliant (does not use `scroll-reveal` classes). | None. | Keep using attribute-driven data markers. |
| **Avoid light surface classes** | Uses `.surface-panel` and `.surface-muted` panels. | Violates dark premium guidelines. | Remove all light panel classes entirely. |
| **Avoid gold-heavy styling** | Uses gold text, badges, and background placeholders. | Confuses KIG Red branding with gold styling. | Transition from gold-heavy elements to white/cream typography and selective red highlights. |

## Patterns to Reuse from Franchise and Member Pages
* **Franchise page (`Views/Franchise/Index.cshtml`):** Good layout model for two-column features, processes, and list layout borders.
* **Member page (`Views/Member/Index.cshtml`):** Represents the **exact** implementation blueprint for the Dark Premium Editorial layout. It defines the generic `.public-editorial-page__*` class system inside `input.css` which covers page backgrounds, headers, margins, button states, and grid layouts. This class system is completely generic and can be applied directly to rebuilding `/gioi-thieu` without adding redundant CSS.
