# UI Architecture & Layout Patterns

This document details the frontend visual design systems, custom layout grids, and interactive animation scripts of the **Truyền Thuyết Champong** website.

---

## 1. Alternating Checkerboard Grid Pattern

To keep readers engaged, the website uses an alternating layout pattern (image-left/text-right alternating with text-left/image-right) for storytelling and menu showcases.

### Layout Implementation
We implement this cleanly using Tailwind's CSS Grid and flex ordering. This ensures the logical DOM structure remains sequential for screen readers, while large screens display the alternating grid:

```html
<!-- Row 1: Image Left, Text Right (Default DOM Flow) -->
<article class="grid gap-8 lg:grid-cols-[1fr_1fr] lg:items-center">
    <div class="media-container">
        <img src="image1.jpg" alt="Description" />
    </div>
    <div class="text-container">
        <h2>Concept Title One</h2>
        <p>Description text here...</p>
    </div>
</article>

<!-- Row 2: Text Left, Image Right (Reordered visually using lg:order) -->
<article class="grid gap-8 lg:grid-cols-[1fr_1fr] lg:items-center">
    <!-- Displays on the left in large viewports, but remains second in mobile flows -->
    <div class="text-container lg:order-1">
        <h2>Concept Title Two</h2>
        <p>Description text here...</p>
    </div>
    <!-- Displays on the right in large viewports, but remains first in mobile flows -->
    <div class="media-container lg:order-2">
        <img src="image2.jpg" alt="Description" />
    </div>
</article>
```

### Visual Adaptations
*   **Menu Hub (`/Menu`)**: Uses dynamic grid sizing `lg:grid-cols-[0.48fr_0.52fr]` with alternating orders resolved via Razor loops:
    ```cshtml
    var isReversed = index % 2 == 1;
    var mediaOrderClass = isReversed ? "lg:order-2" : string.Empty;
    var contentOrderClass = isReversed ? "lg:order-1" : string.Empty;
    ```
*   **Brand Story**: Alternates sections to separate historical text columns from culinary display illustrations.

---

## 2. 3-Concept Culinary Essence Showcase

The core home page layout centers on the three main dining concepts under KIG Holding, styled with distinct sub-grids and interactive behaviors:

1.  **Truyền Thuyết Champong**:
    *   Features a large display image paired with a **4-column feature sub-grid** (`sm:grid-cols-2`) mapping signature credentials (group sizes, raw materials, fresh handmade noodles, authentic Korean spices).
2.  **Gogi Maru (Dry Aged BBQ)**:
    *   Visual text details Gogi Maru's 15-day meat fermentation processes alongside a **3-column feature grid** (`sm:grid-cols-3`) detailing banchan tables and outdoor sky bar options.
3.  **KBB Cook (Buffet & Hotpot)**:
    *   Integrates an interactive, lightweight custom slider driven by [kbb-cook-slider.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/kbb-cook-slider.js), showcasing promotional dining price packages (e.g. 178K, 278K, 378K packages).

---

## 3. Scroll-Reveal System (IntersectionObserver)

Animations are driven by a high-performance native JavaScript engine [uw-reveal.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/uw-reveal.js) rather than heavy external libraries. This guarantees 60fps transitions and zero layout shifting (CLS).

### Configuration Attributes
HTML elements configure their animation type, delays, and repeat limits using HTML5 `data-*` attributes:

```html
<div data-uw-reveal="fade-up" 
     data-uw-delay="150" 
     data-uw-duration="800" 
     data-uw-distance="40" 
     data-uw-once="true">
    <h3>Animate me on scroll!</h3>
</div>
```

| Attribute | Options / Format | Default | Description |
| :--- | :--- | :--- | :--- |
| `data-uw-reveal` | `fade-up`, `fade-down`, `fade-left`, `fade-right`, `zoom-up`, `fade-in` | `fade-up` | Direction or type of entrance transition |
| `data-uw-delay` | Integer (milliseconds) | `0` | Delay before animation triggers |
| `data-uw-duration`| Integer (milliseconds) | `850` | Transition duration |
| `data-uw-distance`| Integer (pixels) | `56` | Starting offset translation |
| `data-uw-once` | `true`, `false` | `false` | If true, stops observer tracking after first entrance |

### JavaScript Engine Implementation
The script initializes by applying starting translation offsets and setting `opacity = '0'` to hidden targets. It then tracks elements using a viewport observer:

```javascript
const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
        const element = entry.target;
        const state = getState(element);
        const config = state.config;

        if (entry.isIntersecting) {
            if (state.phase === 'visible' || state.phase === 'entering') return;
            animateIn(element, state, config, observer);
            return;
        }

        if (config.once || state.phase === 'hidden' || state.phase === 'exiting') return;
        animateOut(element, state, config);
    });
}, {
    root: null,
    threshold: [0, 0.15, 0.35],
    rootMargin: '0px 0px -10% 0px' // Triggers early just before entering viewport
});
```

### Accessibility & Fallbacks (Reduced Motion)
The engine respects system accessibility choices. If the browser or operating system has reduced motion flags enabled, or does not support modern Web Animation APIs, the script automatically returns, bypassing observer logic and revealing all components instantly:

```javascript
const supportsReducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
const supportsObserver = 'IntersectionObserver' in window;
const supportsAnimate = typeof Element.prototype.animate === 'function';

if (shouldReduceMotion || !supportsObserver || !supportsAnimate) {
    revealAllImmediately(); // Strips inline styles, restoring default CSS layout
    return;
}
```

### Animation Ownership
*   **Homepage Hero Slider**: Elements inside the hero slider (titles, descriptions, buttons) are exclusively animated by `home-hero-slider.js` using the `data-champong-hero-reveal` attribute. They must **not** also use `data-uw-reveal`.
*   **General Page Content**: `uw-reveal.js` is reserved for general scroll-reveal sections outside the hero slider. Using both systems on the same element will cause transition conflicts (e.g., hidden content on mobile viewports).

---

## 4. News Details & Related Posts

*   **Article Hero Images**: The main detail image on `/tin-tuc/{slug}` uses a natural aspect ratio (via `.news-detail-media--hero`) to prevent cropping important image content.
*   **News Grid Cards**: Listing cards and related post cards intentionally keep fixed/cropped thumbnail behavior (`object-cover`) for visual grid consistency. Future changes must not mutate shared `.news-detail-media` base behavior without checking both detail hero and card usages.
