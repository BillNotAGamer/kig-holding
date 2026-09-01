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

## 3.1 Reservation Calendar Policy UI

The public reservation page (`Views/Reservation/Index.cshtml`) and quick booking modal (`Views/Shared/Partials/_BookingModal.cshtml` with `wwwroot/js/booking-modal.js`) consume the same server-generated calendar-policy payload from `Views/Shared/Partials/_ReservationCalendarPolicy.cshtml`.

The payload is emitted as JSON plus a small shared browser helper exposed as `window.kigValidateReservationDate`. It contains the Vietnam-local minimum date, active dates loaded from `BlockedReservationDates`, and Vietnamese validation messages. Date input values are treated as `yyyy-MM-dd` and parsed with `Date.UTC(...)` only to validate calendar shape; JavaScript must not infer weekend, holiday, or maximum-year restrictions.

The frontend policy is advisory. It provides immediate inline feedback and blocks avoidable fetch/form submissions, but `ReservationService.CreateReservationAsync` remains the authoritative server-side enforcement point.

The Admin reservation policy view (`Areas/Admin/Views/Reservation/Policy.cshtml`) uses a native month grid with Monday-through-Sunday columns. White dates are allowed, red dates are blocked, past dates are disabled, and today/future dates can be toggled locally until the Admin submits the update form. The server parses the hidden `yyyy-MM-dd` set, rejects malformed input, ignores past submitted dates, deduplicates values, and reconciles the active DB set in one update.

---

## 4. Homepage App Download Modal

The homepage `Mang Champong về nhà` CTA uses a page-scoped app-download modal loaded by `wwwroot/js/home-app-modal.js`. The modal is static server-rendered markup in `Views/Home/Index.cshtml`, keeps `/dat-ban` as the no-JavaScript fallback, and reuses the same footer QR and store badge/link values.

### Cross-Modal Coordination

The app modal and homepage booking modal follow a strict single-active-modal policy. `home-app-modal.js` dispatches:

```javascript
document.dispatchEvent(
  new CustomEvent("kig:booking-modal:suppress-auto-open", {
    detail: { source: "home-app-modal" }
  })
);
```

Consumer: `wwwroot/js/booking-modal.js`.

Purpose: cancel the pending automatic homepage booking prompt, mark that automatic prompt as handled for the current browser session, and prevent stacked dialogs. Manual booking triggers remain unaffected and continue to open the booking modal normally.

---

## 5. News Details & Related Posts

*   **Article Hero Images**: The main detail image on `/tin-tuc/{slug}` uses a natural aspect ratio (via `.news-detail-media--hero`) to prevent cropping important image content.
*   **News Grid Cards**: Listing cards and related post cards intentionally keep fixed/cropped thumbnail behavior (`object-cover`) for visual grid consistency. Future changes must not mutate shared `.news-detail-media` base behavior without checking both detail hero and card usages.
*   **Article Content Styling**: Public article bodies use `.article-content` for scoped semantic HTML typography. Visual/plain-text posts still render encoded paragraphs; HTML-mode posts render only sanitized stored HTML inside this container.
*   **Admin Blog Editor Tabs**: Admin Create/Edit uses shared Visual/HTML tabs in `_ContentEditor.cshtml`, enhanced by `wwwroot/js/admin-post-editor.js`. The script owns ARIA state, keyboard navigation, textarea spellcheck/class changes, and the warning before switching custom HTML back to Visual. It does not sanitize, parse, or rewrite article content.

---

## 5.1 Admin Real-Time Reservation Notifications

The Admin shell owns the dynamic reservation notification host in `_AdminLayout.cshtml`. The host uses `data-admin-reservation-toast-host` and supplies same-origin configuration for the SignalR Hub, reservation details URL template, and notification audio URL. `wwwroot/js/admin-reservation-notifications.js` owns the guarded connection start loop, SignalR automatic reconnect, terminal-close restart, duplicate suppression, dynamic toast creation, sound toggle behavior, and visible-tab audio coordination.

The sound control uses `aria-pressed` and persists the Admin preference in `localStorage` under `kig.admin.reservationNotifications.soundEnabled`. Page unlock remains runtime-only: each new document prepares the MP3, waits for browser permission, and silently attempts unlock on the Admin's first normal interaction when the preference is enabled. Reservation playback is attempted automatically; if the browser blocks it, the preference stays enabled and a compact activation button is shown. Managed browsers may allow playback without another interaction, but the client does not bypass autoplay policy. Server-originated toast values are rendered with DOM APIs and `textContent`.

Every connected Admin tab may show the toast. Audio is attempted only by tabs where the sound preference is enabled and `document.visibilityState` is `visible`; hidden tabs do not play audio. When Web Locks are available, the script requests an exclusive per-reservation lock and holds it briefly after playback starts so a slightly delayed visible tab does not immediately play the same sound. Browsers without Web Locks fall back to visible-tab playback and may play duplicate audio if multiple visible tabs receive the same reservation event.

The vendored SignalR browser client and the notification MP3 are local deployment source assets under `wwwroot`. ASP.NET Core publish includes them from the static web root; keep them version-controlled with the rest of the feature.

---

## 6. Membership Program Editorial Page

The public route `/thanh-vien` uses the existing editorial shell (`.public-editorial-page`) with a dedicated member-page namespace layered on top (`.member-program*`). This keeps the shared page chrome and button system stable while allowing the membership page to present denser policy content safely.

### Content Architecture
The page is intentionally server-rendered and static. It presents:

1. A Truyền Thuyết Champong-specific hero
2. Point-earning overview
3. Tier thresholds
4. One accessible tabbed tier module with Bronze selected by default
5. Tier-progress rules
6. Birthday-week benefits in a semantic table
7. Upgrade and downgrade rules
8. Important point terms
9. Final practical CTAs

### Styling Rules
*   Shared button selectors such as `.public-editorial-page__button` remain untouched because they are reused by other pages including reservation success.
*   Membership-specific layout and card rules must stay under `.member-program` selectors to avoid affecting `/gioi-thieu`, reservation success, or other editorial views.
*   The tier module uses a dedicated `.member-tier*` namespace for the selected visual card, scrollable tab pills, benefit rows, invoice block, and status text.
*   The tier module is progressively enhanced by `wwwroot/js/member-tier-tabs.js`: all tier content is server-rendered first, then JavaScript hides inactive panels and visual cards, updates `aria-selected`, supports arrow/Home/End keys, and scrolls the active pill into view.
*   The selected membership-card track is decorative/informational only. It must not display fabricated personalized progress unless a future backend member-progress source is implemented.
*   The birthday-benefit table is rendered once with semantic `<table>` markup and may scroll horizontally on smaller screens rather than duplicating the DOM for mobile.

### Maintenance Rule
Any future change to approved membership tiers, thresholds, point rates, invoice examples, birthday benefits, upgrade/downgrade rules, tier-tab behavior, or point-validity rules must update:

*   `/thanh-vien`
*   the member-page tests
*   the synchronized documentation
