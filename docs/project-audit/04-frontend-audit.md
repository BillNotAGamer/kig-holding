# 04 Frontend Audit

## Global Layout (`_Layout.cshtml`)
* **File Path**: [Views/Shared/_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml)
* **Metadata & SEO**: Automatically binds titles, meta descriptions, and open graph tags (`og:title`, `og:image`, `og:url`) from dynamic database settings using the [ISiteSettingService](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Services/ISiteSettingService.cs) if the database connection is verified. Falls back to static defaults if the DB is unconfigured.
* **CDNs Loaded**:
  * Google Fonts: `Quicksand` (400, 500, 600, 700 weights).
  * Swiper Bundle CSS/JS (`v11.0.7`). *Note: Redundant CDN load since Home Hero uses a custom script.*
  * jQuery (`v3.5.1`).
* **Animations & Reveal**: Dynamically appends [uw-reveal.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/uw-reveal.js) at the bottom.

---

## Global Header / Mobile Menu

### Desktop Header (`Partials/_Header.cshtml`)
* **Visuals**: Sticky header (`.site-header`) with a semi-transparent black background (`rgba(11, 11, 11, 0.82)`) and light backdrop-blur. Applies a solid background (`.is-scrolled`) when the scroll position is > 20px.
* **Dropdown**: Uses standard CSS hover and `:focus-within` transitions to render sub-items under "KIG Holding" (Ve KIG Holding, Thanh vien, Lien he).
* **CTAs**: A prominent red button (`bg-brand-red`) on the right directs to `/dat-ban`.

### Mobile Menu Drawer (`Partials/_MobileMenu.cshtml`)
* **Structure**: A full-height side drawer (`.mobile-drawer`) that slides in from the right (`translate-x-full` to `translate-x-0`).
* **Nested Accordion**: Uses native HTML5 `<details>` and `<summary>` elements with styled arrows to handle the KIG Holding nested navigation links without JavaScript overhead.

---

## Global Footer (`Partials/_Footer.cshtml`)
* **Visuals**: Styled in a dark charcoal theme with soft gold-tinted radial gradients.
* **App Download Section**: Renders mockup layout columns for iOS and Android app download links. Includes a verified WebP QR code asset (`wwwroot/images/general/kig-holding-qr.png`).
* **Branch & Corporate Details**: Renders legal registration details, phone lines, and address links pulled from the database or fallbacks.
* **Social Outlets**: Circular border button links mapping to Facebook, Zalo, and TikTok profiles.

---

## Public Pages Breakdown

### 1. Home Page (`Views/Home/Index.cshtml`)
Comprises a strict 7-section structure designed for premium visual appeal:

| Section | View File / Component | Purpose | Data Source | Visual Strategy |
|---------|-----------------------|---------|-------------|-----------------|
| 1. Hero Slider | `Home/Index.cshtml` | Full screen slideshow | Static links / Banner path | Conic-gradient heat overlays, sliding tickers, and ArrowLeft/Right keys via [home-hero-slider.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/home-hero-slider.js). |
| 2. Brand Story | `Home/Index.cshtml` | Brand origin summary | Static story highlights array | Alternating 2-column grid layout, Korean dish mockup graphic, and benefit icon blocks. |
| 3. Culinary Essence | `Home/Index.cshtml` | Core brands presentation | Static brands features array | Floating illustration elements (Kimchi, Soyu, Chili) animated on infinite keyframe loops. |
| 4. Menu Teaser | `Home/Index.cshtml` | Hub category cards | Static card lists | Large alternating cards linking to menu groups. Uses `onerror` image removal fallbacks. |
| 5. Take-Home | `Home/Index.cshtml` | Home dining promotion | Static takeout details | Styled layout featuring the brand mascot (`champong-mascot.webp`). |
| 6. News & Promotions | `Home/Index.cshtml` | Recent blog updates | `Model.LatestPosts` (Limit 3) | Visual priority grid: 1 large featured card with full details, and 2 smaller list items. |
| 7. Booking CTA | `Home/Index.cshtml` | Bottom booking card | `@await Component.InvokeAsync("BookingMiniForm")` | Pinned backdrop panel with reservation form fields. |

### 2. Branch Locator (`Views/Branch/Index.cshtml`)
* **Layout**: Dark premium layout grid.
* **Filter System**: Query-parameter city list links (Hồ Chí Minh, Hà Nội) and a live text search bar.
* **Branch Cards**: Render capabilities like capacity flags, address details, maps query links, and direct hotline triggers.
* **Autocomplete suggester**: Managed by [branch-locator.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/branch-locator.js). Listens to inputs (minimum 2 characters) and serves local card matches. Supports keyboard inputs and clicks.
* **Google Maps**: Renders a dedicated iframe location viewer below the branches list.

### 3. News Listings (`Views/News/Index.cshtml` & `Detail.cshtml`)
* **Listings Layout**: Editorial layout structure. Renders 1 lead featured article card, 2 secondary cards, 2 compact lists, and a bottom 3-column post card grid.
* **Read Page Layout**: Single-column reading view. Renders category badges, published timestamps, paragraph lines, a bottom reservation booking banner, and a related posts suggest list.

### 4. Menu Hub & Group Viewer (`Views/Menu/Index.cshtml` & `Group.cshtml`)
* **Landing Hub**: Renders published menu group banners, cover thumbnails, page counts, and links to group detail pages.
* **Detailed Group Viewer**: Supports a dual viewing model:
  * **Xem dạng cuộn** (Vertical Scroll Grid): Standard card list of menu images.
  * **Xem dạng sách** (Book Flipbook): Desktop dual-page spread layout with page-turning animations managed by [menu-flipbook.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/menu-flipbook.js).
* *Note on transition*: Plans exist to simplify this into a premium book-only experience, which will involve removing the vertical scroll grid and toggles.

---

## Admin Pages UI (`Areas/Admin/Views/Shared/_AdminLayout.cshtml`)
* **Visual Theme**: Strict Light Theme (`bg-brand-light` / `#FAF8F3` base, clean borders, dark text) to ensure legibility and ease of operations.
* **Sidebar**: Dark left sidebar (`bg-brand-black` / `text-white`) with red accent lines indicating the active route.
* **CRUD Forms**: Styled input fields, select items, validation messages, and custom action buttons.
* **Uploader Manager**: Multi-file dropzone area allowing operators to upload, sort (by increments of 10), edit alt text description, and visibility toggle menu page sheets.

---

## Animation & JavaScript Systems

### 1. Progressive Script Isolation
Site-wide actions (like mobile drawers and scroll-to-top buttons) are kept in the global [site.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/site.js). Heavy UI animations are isolated to individual, page-specific scripts loaded in their respective templates:
* [home-hero-slider.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/home-hero-slider.js) (Loaded on Home Index)
* [branch-locator.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/branch-locator.js) (Loaded on Branch Index)
* [menu-flipbook.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/menu-flipbook.js) (Loaded on Menu Group)

### 2. Scroll Reveal Engine (`uw-reveal.js`)
* Uses `IntersectionObserver` paired with the **Web Animations API** (`Element.prototype.animate`) to trigger reveals dynamically without the CPU overhead of css classes.
* Nodes are decorated with `data-uw-reveal="fade-up"` (supports `fade-down`, `fade-left`, `fade-right`, `zoom-up`, `fade-in`), `data-uw-delay`, `data-uw-duration`, and `data-uw-once="true"`.
* Respects `prefers-reduced-motion` and supports localized testing via `localStorage.setItem('uwRevealMotion', 'off')`.

---

## Frontend Risks & Gaps

1. **Obsolete Swiper CDN Queries**: Swiper CSS and JS tags are loaded globally in `_Layout.cshtml`, which increases page load times. Since the hero slider was migrated to custom JS, these tags should be removed if no other page relies on them.
2. **Missing Disk Assets**: Older, non-redesigned views (like `About/Index.cshtml` and legacy `Menu/Index.cshtml` references) hardcode image placeholders like `/images/placeholders/about-hero.webp` that are missing from the folder structures, causing 404 console errors.
3. **No Alt Text on Logo Elements**: Brand logos and social icons inside header/footer components lack proper `alt="..."` text attributes, which poses an accessibility issue.
