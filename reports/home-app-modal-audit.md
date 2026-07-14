# Homepage App Download Modal Audit

## 1. Audit Verdict

HOME_APP_MODAL_READY_WITH_COORDINATION_REQUIREMENTS

Implementation may begin after explicitly handling coexistence with the existing homepage booking modal auto-open timer. The current "Mang Champong ve nha" CTA is simple enough to convert safely, but the booking modal is private, auto-scheduled on the homepage, and currently has no shared modal coordinator.

## 2. Executive Summary

- The target section is rendered directly in `Views/Home/Index.cshtml:472`.
- The current "Dat ngay" CTA is a normal anchor at `Views/Home/Index.cshtml:493` with `href="/dat-ban"`, no modal data attribute, and no dedicated JavaScript listener.
- The homepage sets `ViewData["AutoOpenBookingModal"] = true` at `Views/Home/Index.cshtml:7`; `_Layout` converts that into booking-modal auto-open body data attributes at `Views/Shared/_Layout.cshtml:102`.
- The booking modal is globally rendered after the footer at `Views/Shared/_Layout.cshtml:114`, uses private JavaScript functions in `wwwroot/js/booking-modal.js`, auto-opens after 1000ms, and auto-closes after 5000ms if untouched.
- The app download source values are static in `Views/Shared/Partials/_Footer.cshtml:84` through `Views/Shared/Partials/_Footer.cshtml:104` and duplicated in the current `/thanh-vien` page at `Views/Member/Index.cshtml:522` through `Views/Member/Index.cshtml:557`.
- Recommended implementation: static Razor modal markup on the homepage plus a small page-scoped `wwwroot/js/home-app-modal.js`, following the existing custom modal pattern rather than reusing the booking modal container.

The main regression risk is stacked or competing modals: the future app modal must suppress or coordinate the homepage booking modal auto-open so the booking modal does not open over the app modal or immediately after it closes.

## 3. Initial Worktree State

Command baseline:

- `git status --short --branch`: branch `main...origin/main`.
- `git rev-parse --show-toplevel`: `F:/Coding/Web development/KIG Holding/KIGHolding`.
- `git rev-parse HEAD`: `19f682c6603b4145ea7c8db6d27b256d909b1cde`.
- Recent commits:
  - `19f682c (HEAD -> main, origin/main) update image uploading logic and membership page content`
  - `afaa12e Refactor homepage news section layout`
  - `1631b5d Repair news index editorial layout`
  - `47e8461 update reservation rule`
  - `875257a Add gated TikTok pixel and public event tracking`

Pre-existing tracked changes before this audit:

- `M Views/Member/Index.cshtml`
- `M Views/Menu/Index.cshtml`
- `M docs/feature-state.md`
- `M docs/ui-architecture.md`
- `M wwwroot/css/input.css`
- `M wwwroot/css/site.css`

Pre-existing untracked changes before this audit:

- `?? reports/member-app-promo-implementation.md`
- `?? reports/member-assets-fix-report.md`
- `?? reports/member-tier-card-design-audit.md`
- `?? reports/member-tier-card-implementation.md`
- `?? wwwroot/images/member/`
- `?? wwwroot/js/member-tier-tabs.js`

Initial `git diff --check` returned no whitespace errors. It printed line-ending warnings only.

## 4. Homepage Section and CTA Inventory

Rendering location:

- `Views/Home/Index.cshtml:472`: `<section class="champong-takehome section-shell" data-home-section="take-home">`
- `Views/Home/Index.cshtml:476`: visible eyebrow text `Mang Champong ve nha`.
- `Views/Home/Index.cshtml:477`: section heading `Mang Champong ve nha`.
- `Views/Home/Index.cshtml:485`: visual wrapper uses `data-media-parent`, `data-uw-reveal="fade-up"`, `data-uw-delay="100"`, and `data-uw-once="true"`.
- `Views/Home/Index.cshtml:486`: mascot image path `/images/home/images/champong-mascot.webp`.
- `Views/Home/Index.cshtml:493`: CTA anchor starts.
- `Views/Home/Index.cshtml:493`: current `href="/dat-ban"`.
- `Views/Home/Index.cshtml:494`: classes include `champong-takehome__button inline-flex ... bg-brand-red ... hover:bg-brand-redDark`.
- `Views/Home/Index.cshtml:495`: `aria-label="Dat Champong mang ve"`.
- `Views/Home/Index.cshtml:496`: visible CTA text `Dat ngay`.

CSS:

- `wwwroot/css/input.css:6470`: `.champong-takehome` section background.
- `wwwroot/css/input.css:6495`: `.champong-takehome__visual` layout.
- `wwwroot/css/input.css:6503`: visual min-height `clamp(24rem, 40vw, 34rem)`.
- `wwwroot/css/input.css:6525`: `.champong-takehome__mascot`.
- `wwwroot/css/input.css:6547`: `.champong-takehome__button` shadow.
- `wwwroot/css/input.css:6551`: mobile-specific visual sizing.

Duplication:

- No separate desktop/mobile duplicate for this CTA was found.
- `rg` found this specific `champong-takehome__button` only in `Views/Home/Index.cshtml` and CSS selectors. Future targeting by this class is low risk.

Existing automated tests:

- No homepage-specific tests were found in `KIGHolding.Tests`.
- Existing tests are currently focused on membership page and storage behavior.

## 5. Current CTA Behavior

Today, activating the "Dat ngay" CTA performs native navigation:

- It is an `<a>` at `Views/Home/Index.cshtml:493`.
- It navigates to `/dat-ban`.
- It does not use `target="_blank"`.
- It does not use `data-booking-modal-trigger`.
- It is keyboard-operable as a native link.
- No dedicated JavaScript click listener was found for `.champong-takehome__button`.
- The parent visual wrapper has only reveal animation attributes at `Views/Home/Index.cshtml:485`.

Destination:

- `/dat-ban` is handled by `ReservationController` because `Controllers/ReservationController.cs:12` declares `[Route("dat-ban")]`.
- `Controllers/ReservationController.cs:60` constructs the full reservation form model for the GET route.

Current behavior is predictable and reversible. Future implementation should preserve a non-JavaScript fallback. Recommended fallback is to keep an `href` that remains useful, either `/dat-ban` or a store/universal app link if one is explicitly approved. If the visible label stays `Dat ngay`, the `aria-label` should clarify the modal action, for example `Mo tai ung dung Truyen Thuyet Champong`.

## 6. Existing Booking Modal Architecture

DOM and rendering:

- `Views/Shared/_Layout.cshtml:44`: reads `ViewData["AutoOpenBookingModal"]`.
- `Views/Home/Index.cshtml:7`: sets `ViewData["AutoOpenBookingModal"] = true`.
- `Views/Shared/_Layout.cshtml:102`: `<body>` receives booking auto-open attributes.
- `Views/Shared/_Layout.cshtml:103`: `data-booking-modal-auto-open="true"` on pages that set the flag.
- `Views/Shared/_Layout.cshtml:104`: `data-booking-modal-auto-open-delay="1000"`.
- `Views/Shared/_Layout.cshtml:105`: `data-booking-modal-auto-close-delay="5000"`.
- `Views/Shared/_Layout.cshtml:114`: globally renders `<partial name="Partials/_BookingModal" />`.

Modal markup:

- `Views/Shared/Partials/_BookingModal.cshtml:44`: root `.booking-modal.booking-modal--compact`.
- `Views/Shared/Partials/_BookingModal.cshtml:45`: root selector `data-booking-modal`.
- `Views/Shared/Partials/_BookingModal.cshtml:46`: initial `aria-hidden="true"`.
- `Views/Shared/Partials/_BookingModal.cshtml:47`: backdrop `data-booking-modal-backdrop`.
- `Views/Shared/Partials/_BookingModal.cshtml:49`: panel `data-booking-modal-panel`.
- `Views/Shared/Partials/_BookingModal.cshtml:51`: `role="dialog"`.
- `Views/Shared/Partials/_BookingModal.cshtml:52`: `aria-modal="true"`.
- `Views/Shared/Partials/_BookingModal.cshtml:53`: labelled by `booking-modal-title`.
- `Views/Shared/Partials/_BookingModal.cshtml:54`: described by `booking-modal-description`.
- `Views/Shared/Partials/_BookingModal.cshtml:55`: panel has `tabindex="-1"`.
- `Views/Shared/Partials/_BookingModal.cshtml:75`: form posts to `/dat-ban`.
- `Views/Shared/Partials/_BookingModal.cshtml:80`: AJAX action `data-booking-modal-action="/dat-ban/quick"`.
- `Views/Shared/Partials/_BookingModal.cshtml:81`: anti-forgery token is rendered.

JavaScript behavior:

- `wwwroot/js/booking-modal.js:2`: selects `[data-booking-modal]`.
- `wwwroot/js/booking-modal.js:10`: binds all `[data-booking-modal-trigger]` once on load with `querySelectorAll`, not delegated.
- `wwwroot/js/booking-modal.js:37`: session key is `kig:booking-modal:auto-shown`.
- `wwwroot/js/booking-modal.js:48`: auto-open flag is read from `document.body.dataset.bookingModalAutoOpen`.
- `wwwroot/js/booking-modal.js:49`: auto-open delay defaults to 1000ms.
- `wwwroot/js/booking-modal.js:50`: auto-close delay defaults to 5000ms.
- `wwwroot/js/booking-modal.js:53`: stores last manual trigger for focus return.
- `wwwroot/js/booking-modal.js:54`: tracks `isSubmitting`.
- `wwwroot/js/booking-modal.js:55`: tracks `openSource`.
- `wwwroot/js/booking-modal.js:56` through `wwwroot/js/booking-modal.js:60`: private auto-open/auto-close and interaction state.
- `wwwroot/js/booking-modal.js:210`: auto-open focus can target the panel through `focusMode === 'dialog'`.
- `wwwroot/js/booking-modal.js:234`: schedules auto-close.
- `wwwroot/js/booking-modal.js:246` through `wwwroot/js/booking-modal.js:257`: refuses auto-close when validation, success, submit, interaction, or closed state exists.
- `wwwroot/js/booking-modal.js:265`: private `setOpen`.
- `wwwroot/js/booking-modal.js:288`: toggles `.is-open`.
- `wwwroot/js/booking-modal.js:289`: updates `aria-hidden`.
- `wwwroot/js/booking-modal.js:290`: toggles `document.body.classList.toggle('booking-modal-open', open)`.
- `wwwroot/js/booking-modal.js:323`: private `openModal`.
- `wwwroot/js/booking-modal.js:329`: private `closeModal`.
- `wwwroot/js/booking-modal.js:511`: manual trigger click prevents default navigation, clears auto timers, marks session shown, and opens as manual.
- `wwwroot/js/booking-modal.js:528`: backdrop click closes.
- `wwwroot/js/booking-modal.js:640`: keydown handler is attached to the modal root.
- `wwwroot/js/booking-modal.js:645`: Escape closes the booking modal.
- `wwwroot/js/booking-modal.js:651`: Tab focus trap is implemented while open.
- `wwwroot/js/booking-modal.js:675`: syncs conditional fields.
- `wwwroot/js/booking-modal.js:676`: schedules auto-open at initialization.

CSS:

- `wwwroot/css/input.css:508`: `body.booking-modal-open { overflow: hidden; }`.
- `wwwroot/css/input.css:512`: `.booking-modal` is fixed, full viewport, `z-index: 90`, hidden by opacity/visibility/pointer-events.
- `wwwroot/css/input.css:525`: `.booking-modal.is-open` enables visibility and pointer events.
- `wwwroot/css/input.css:531`: backdrop fills the viewport with blur.
- `wwwroot/css/input.css:540`: panel is scrollable, bounded to `width: min(calc(100vw - 2rem), 74rem)` and `max-height: calc(100dvh - 2rem)`.

Reusability assessment:

- The booking modal has the right interaction primitives, but they are private inside an IIFE and tied to reservation form state.
- Reusing the booking container would mix unrelated app-download content with reservation form behavior and auto-close/session logic.
- The app modal should not copy the entire booking modal blindly; it should reuse the pattern and only coordinate state.

## 7. Overlay Conflict Inventory

| Overlay | Selector | Z-index | Scroll lock | Escape handling | Conflict risk |
| --- | --- | ---: | --- | --- | --- |
| Booking modal | `[data-booking-modal]`, `.booking-modal` | 90 | `body.booking-modal-open` at `wwwroot/css/input.css:508` | Root keydown closes at `wwwroot/js/booking-modal.js:645` | High, because homepage auto-open may fire while the app modal is active |
| Mobile drawer | `[data-mobile-menu]`, `[data-mobile-menu-overlay]` | overlay 60, drawer 70 | `body.classList.add('overflow-hidden')` at `wwwroot/js/site.js:21` | Document Escape closes at `wwwroot/js/site.js:64` | Medium, because it also owns body overflow but is lower z-index |
| Floating contact controls | `[data-floating-contact]` | 50 | None | Document Escape closes at `Views/Shared/Partials/_FloatingContactButtons.cshtml:205` | Low to medium, because expanded actions could remain behind a modal |
| Site header/dropdowns | `.site-header`, nav dropdown | 50 | None | CSS hover/focus | Low, modal z-index should exceed it |
| Back-to-top button | `[data-back-to-top]` | 40 | None | None | Low |
| Menu flipbook overlay | menu-specific JS/classes | Not homepage-specific | `menu-viewer-overlay-open` in `wwwroot/js/menu-flipbook.js` | Menu-page context | Low for homepage; do not load or coordinate unless future modal becomes global |

The future app modal should use a z-index above the mobile drawer and floating contact controls. It can match booking modal `z-index: 90` only if a strict single-active-modal policy prevents overlap. Otherwise it should use a named layer such as 95, but stacking should still be prevented.

## 8. App Download Source Verification

Footer rendering chain:

- `Views/Shared/_Layout.cshtml:112`: renders `SiteFooter` view component.
- `Views/Shared/Components/SiteFooter/Default.cshtml:3`: renders `~/Views/Shared/Partials/_Footer.cshtml`.
- `Views/Shared/Partials/_Footer.cshtml:74`: footer connect section.

Static app values in footer:

- QR card: `Views/Shared/Partials/_Footer.cshtml:84`.
- QR image path: `~/images/general/kig-holding-qr-app.webp` at `Views/Shared/Partials/_Footer.cshtml:85`.
- QR alt: `Quet ma QR de tai ung dung KIG Holding` at `Views/Shared/Partials/_Footer.cshtml:86`.
- App Store URL: `https://apps.apple.com/vn/app/truy%E1%BB%81n-thuy%E1%BA%BFt-champong-%EC%A0%88%EC%84%A4%EC%9D%98%EC%A7%AC%EB%BD%BB/id6753157313?l=vi` at `Views/Shared/Partials/_Footer.cshtml:99`.
- App Store attributes: `target="_blank" rel="noopener noreferrer" aria-label="Tai ung dung tren App Store"` at `Views/Shared/Partials/_Footer.cshtml:99`.
- App Store badge path: `~/images/general/icons/app-store-download.svg` at `Views/Shared/Partials/_Footer.cshtml:100`.
- Google Play URL: `https://play.google.com/store/apps/details?id=com.eggstech.champong.app&pcampaignid=web_share` at `Views/Shared/Partials/_Footer.cshtml:103`.
- Google Play attributes: `target="_blank" rel="noopener noreferrer" aria-label="Tai ung dung tren Google Play"` at `Views/Shared/Partials/_Footer.cshtml:103`.
- Google Play badge path: `~/images/general/icons/google-play-download.svg` at `Views/Shared/Partials/_Footer.cshtml:104`.

Membership page currently duplicates the same values:

- QR path at `Views/Member/Index.cshtml:523`.
- Fallback factual copy at `Views/Member/Index.cshtml:536`.
- App Store link and badge at `Views/Member/Index.cshtml:542` through `Views/Member/Index.cshtml:548`.
- Google Play link and badge at `Views/Member/Index.cshtml:553` through `Views/Member/Index.cshtml:559`.

Source-of-truth recommendation:

- For the immediate homepage modal, use page-local reuse with the exact static footer values. This avoids touching the accepted footer and accepted `/thanh-vien` page during a modal feature.
- A future cleanup can extract a small shared app-download partial if the owner accepts changing three render sites at once. That is not required for this modal and would increase regression surface.

## 9. Recommended Modal Architecture

Recommended option: new page-scoped static Razor modal with minimal vanilla JavaScript, following the existing custom modal pattern.

Comparison:

- Native `<dialog>`: acceptable in modern browsers, but the project already uses custom modal CSS/JS and the booking modal is not `<dialog>`. Mixing patterns would increase focus/backdrop edge cases.
- Existing project modal pattern: best visual/interaction fit, but use the pattern only. Do not reuse booking modal internals directly because they are reservation-specific.
- New page-scoped modal with minimal vanilla JavaScript: recommended. It keeps scope narrow, preserves static server-rendered content, and allows explicit coordination with the booking modal.
- Reusing the booking modal container: not recommended. It would mix app download content with reservation form validation, anti-forgery, submit state, auto-close, and success rendering.
- Dynamically constructing modal HTML in JavaScript: not recommended. Static Razor is safer, easier to test, and avoids string-injection mistakes.

Future implementation should:

- Add modal markup to `Views/Home/Index.cshtml` near the take-home section or near the end of the page.
- Change the take-home CTA from pure navigation into a modal trigger while retaining a useful fallback `href`.
- Add isolated CSS under `.home-app-modal`.
- Add `wwwroot/js/home-app-modal.js` and load it in the existing homepage `@section Scripts` after `home-hero-slider.js` and `kbb-cook-slider.js` or before them if it does not depend on slider code.
- Keep the modal content static and server-rendered.

## 10. Modal Coordination Policy

Use a strict single-active-modal policy.

Scenario A: user clicks "Dat ngay" before the booking modal auto-opens.

- Recommended behavior: open the app modal and suppress the pending booking auto-open for the current session.
- Reason: the user has intentionally engaged with a CTA. Showing a reservation modal over the app modal 1 second later is disruptive.
- Implementation note: because booking modal auto-open state is private, add the smallest coordination hook to `booking-modal.js`, such as listening for a document event from the app modal that clears auto timers and marks `kig:booking-modal:auto-shown`. Do not expose form internals.

Scenario B: booking modal is already open when user attempts to open the app modal.

- Recommended behavior: do not stack. Refuse app modal open while `[data-booking-modal].is-open` exists.
- Practical note: normal user clicks behind an open booking modal are blocked by the booking backdrop, but scripts should still guard this state.
- Do not close a submitting booking modal.

Scenario C: app modal closes while booking modal timer is still pending.

- Recommended behavior: the booking auto-open remains suppressed for the session. Manual booking triggers still work.
- Reason: otherwise the booking modal can appear immediately after the app modal closes, which feels like a modal loop.

Scenario D: mobile navigation or floating contact panel is open.

- Recommended behavior: close lightweight overlays before opening the app modal.
- Mobile drawer: dispatch a click to `[data-mobile-menu-close]` or add a narrow custom close event if implementation touches `site.js`.
- Floating contact: close open roots by removing `.is-open`, setting `aria-expanded="false"`, `aria-hidden="true"`, and `tabindex="-1"` using the same attributes as `Views/Shared/Partials/_FloatingContactButtons.cshtml:127` through `Views/Shared/Partials/_FloatingContactButtons.cshtml:146`.
- Do not stack app modal with any other modal/dialog.

## 11. Recommended Modal Content

Static content structure:

- Close button.
- Eyebrow: `Ung dung Truyen Thuyet Champong`.
- Heading: `Mang Champong ve nha thuan tien hon`.
- Body: `Quet ma QR hoac lua chon nen tang phu hop de tai ung dung.`
- QR code using `~/images/general/kig-holding-qr-app.webp`.
- App Store badge/link using the exact footer URL and `~/images/general/icons/app-store-download.svg`.
- Google Play badge/link using the exact footer URL and `~/images/general/icons/google-play-download.svg`.
- Optional secondary close action: `Tiep tuc tren website`.

Do not claim:

- points synchronization,
- member login,
- booking management,
- online redemption,
- app-only discounts,
- real-time member balance,
- delivery/order backend availability,
unless those claims are approved elsewhere.

CTA label:

- Keeping visible text `Dat ngay` is acceptable if the design owner wants continuity.
- Accessibility should improve the trigger label to communicate the modal result, for example `aria-label="Mo thong tin tai ung dung Truyen Thuyet Champong"`.
- If copy changes are allowed later, `Dat ngay qua ung dung` is clearer than `Dat ngay`.

## 12. Accessibility Requirements

Future modal must include:

- `role="dialog"` and `aria-modal="true"`.
- Stable unique title ID and `aria-labelledby`.
- Stable description ID and `aria-describedby`.
- Close button with a clear accessible name.
- Focus moved into the modal on open, preferably close button or dialog panel, not external store links.
- Focus restored to the take-home CTA on close.
- Escape closes only the active app modal.
- Backdrop click closes according to the same policy as the booking modal.
- Focus containment while open.
- Body scroll locking that does not fight `booking-modal-open` or mobile menu `overflow-hidden`.
- QR image with meaningful alt text.
- Store links with accessible names and footer-matching `target="_blank" rel="noopener noreferrer"`.
- Visible focus indicators.
- Touch-friendly close button and store links.
- Reduced-motion support.

Known accessibility gaps to avoid:

- Do not hide modal content with CSS while it remains focusable.
- Do not let the booking modal receive focus while the app modal is open.
- Do not create duplicate IDs with footer or membership app promo markup.

## 13. Responsive Requirements

375px:

- Modal width should be `calc(100vw - 2rem)` or similar.
- Single-column content.
- QR around 140-160px.
- Store badges can stack.
- Close button visible without scrolling.

390px:

- Same as 375px with no horizontal overflow.
- Ensure long Vietnamese heading wraps cleanly.

430px:

- QR may grow toward 160-180px.
- Store badges may remain stacked or become two-column only if each remains tappable.

768px:

- Modal can use a two-column inner layout if height permits.
- QR around 180px.
- Store badges can sit in one row or compact stack.

1024px:

- Bounded modal width, not full viewport.
- Two-column layout preferred: QR card plus text/store actions.
- Modal centered vertically when viewport height permits.

1440px:

- Keep modal compact and premium; do not scale QR excessively.
- Modal should remain visually distinct from footer and membership app promo.

All breakpoints:

- No page-level horizontal overflow.
- Internal scrolling if viewport height is short.
- Floating contact controls must remain visually below/inactive behind modal overlay.

## 14. CSS and JavaScript File Map

Expected future files:

- `Views/Home/Index.cshtml`: CTA trigger attributes and static modal markup.
- `wwwroot/css/input.css`: isolated `.home-app-modal*` styles.
- `wwwroot/css/site.css`: generated through `npm run build:css`.
- `wwwroot/js/home-app-modal.js`: page-scoped modal behavior.
- `KIGHolding.Tests/...`: homepage/source tests. A new `HomePageTests.cs` is preferable to adding unrelated assertions to member tests.
- `docs/feature-state.md`: concise feature note.
- `reports/home-app-modal-implementation.md`: implementation report.

Potential minimal coordination file:

- `wwwroot/js/booking-modal.js`: only if needed to add a small custom-event listener for suppressing homepage auto-open while app modal is active. Avoid changing reservation submit/validation logic.

Files to remain untouched:

- `Views/Member/Index.cshtml`
- `wwwroot/js/member-tier-tabs.js`
- `wwwroot/images/member/`
- `Views/Shared/Partials/_Footer.cshtml`
- `Views/Shared/_Layout.cshtml`, unless page-specific script loading cannot be handled from `Views/Home/Index.cshtml`
- routes/controllers/database/R2/storage
- booking business logic and reservation controller

CSS namespace:

- Use `.home-app-modal`, `.home-app-modal__dialog`, `.home-app-modal__backdrop`, `.home-app-modal__qr`, `.home-app-modal__stores`, `.home-app-modal__store-link`, `.home-app-modal__store-badge`.
- Do not alter `.member-tier*`, `.member-app-promo*`, `.booking-modal*`, or `.public-editorial-page__button*` unless coordination proves unavoidable.

JavaScript:

- Use `wwwroot/js/home-app-modal.js`.
- Initialize on `DOMContentLoaded`.
- Be idempotent with a `data-home-app-modal-bound` guard.
- Do not throw if modal markup is absent.
- Intercept only the take-home CTA trigger, not all `/dat-ban` links.
- Store and restore the trigger focus.
- Close on close button, backdrop, and Escape.
- Trap Tab focus while open.
- Lock body scroll with an app-specific class, but preserve existing locks.
- Prefer a custom event to request booking auto-open suppression instead of reaching into booking modal internals.

## 15. Regression Risk Register

| Risk | Severity | Evidence | Impact | Required mitigation |
| --- | --- | --- | --- | --- |
| Booking modal auto-opens while app modal is active | High | Homepage enables auto-open at `Views/Home/Index.cshtml:7`; booking schedules at `wwwroot/js/booking-modal.js:676` | Stacked dialogs, broken focus, confusing UX | Add strict single-active-modal policy and suppress booking auto-open for session when app modal opens |
| Body scroll lock class conflict | High | Booking uses `booking-modal-open`; mobile drawer uses `overflow-hidden` | Closing one overlay can unlock body while another is open | Use app-specific scroll-lock class and restore only what app modal owns, or centralize lock accounting narrowly |
| Static app values drift across footer, member page, modal | Medium | Footer values are static at `_Footer.cshtml:84-104`; member duplicates at `Views/Member/Index.cshtml:522-557` | Store links/QR can diverge | Immediate exact copy from footer; future optional small shared partial |
| Trigger selector affects other `/dat-ban` links | Medium | Multiple `/dat-ban` links exist in `Views/Home/Index.cshtml:62`, `:96`, `:493`, `:709` | Wrong CTAs open app modal | Target only an explicit new data attribute on the take-home CTA |
| Focus trap misses hidden store links or close button | Medium | Booking has a custom focus trap at `booking-modal.js:651-672`; app modal will need its own | Keyboard users can leave modal | Implement tested focusable-element query and hidden state handling |
| Footer or `/thanh-vien` accepted promo changes accidentally | Medium | Existing accepted member promo is dirty in worktree | Regression of accepted feature | Do not touch footer/member files for this modal |
| TikTok/analytics behavior changes unexpectedly | Low | Current take-home CTA has no tracking data attributes | Missing or duplicate analytics | Do not add tracking unless owner explicitly approves |

## 16. Automated Test Plan

Recommended tests:

1. Homepage returns HTTP 200.
2. `Mang Champong ve nha` exists once.
3. `Dat ngay` trigger exists once inside `[data-home-section="take-home"]`.
4. Trigger remains keyboard-operable.
5. Modal markup exists exactly once.
6. QR path matches footer.
7. App Store badge path matches footer.
8. Google Play badge path matches footer.
9. App Store URL matches footer exactly.
10. Google Play URL matches footer exactly.
11. Modal has valid `aria-labelledby` and `aria-describedby`.
12. Close button has an accessible name.
13. Trigger has modal relationship attributes.
14. JavaScript open sets active state and removes `aria-hidden`/`hidden` as designed.
15. Escape closes the app modal.
16. Focus returns to the trigger.
17. Backdrop closes according to policy.
18. Repeated initialization does not duplicate listeners.
19. Booking modal does not open over app modal.
20. App modal does not stack above booking modal.
21. Body scroll lock restores correctly.
22. Footer remains unchanged.
23. `/thanh-vien` app promo remains unchanged.
24. Existing booking modal tests continue to pass.
25. No external store request occurs during tests.
26. No production/database dependency exists.

No test should call external App Store or Google Play URLs.

## 17. Recommended Implementation Scope

Razor:

- Add one modal trigger data attribute to the specific take-home CTA.
- Add one static modal block to `Views/Home/Index.cshtml`.
- Reuse exact footer QR/store values.
- Keep a useful fallback `href`.

CSS:

- Add isolated `.home-app-modal*` source rules to `wwwroot/css/input.css`.
- Generate `wwwroot/css/site.css` with the official CSS build.

JavaScript:

- Add `wwwroot/js/home-app-modal.js`.
- Load through existing homepage `@section Scripts` at `Views/Home/Index.cshtml:718`.
- If coordination requires it, add a tiny event listener to `wwwroot/js/booking-modal.js` to suppress pending auto-open and mark the auto prompt as shown.

Tests:

- Add a homepage-focused test class.
- Add source-level assertions for exact footer value reuse.
- Add JS source assertions for Escape/focus/idempotent behavior if no browser test runner exists.

Documentation:

- Update `docs/feature-state.md` only if implementation is completed.

Untouched areas:

- Footer markup.
- `/thanh-vien` page and member tier JS/assets.
- Reservation controller and reservation form behavior.
- R2/storage/database.
- Routes/sitemap.

## 18. Explicit Non-Goals

- No footer change.
- No `/thanh-vien` change.
- No app backend.
- No order backend.
- No database/schema work.
- No R2/storage work.
- No booking-flow redesign.
- No stacked modal system.
- No external library.
- No jQuery.
- No app feature claims beyond existing footer/member copy.
- No deployment.

## 19. Verification Results

Commands run:

- `dotnet build KIGHolding.sln --no-restore`
  - First attempt failed due a transient file lock on `obj\Debug\net10.0\KIGHolding.dll`, reported as locked by `VBCSCompiler`.
  - Rerun succeeded with 0 warnings and 0 errors.
- `dotnet test KIGHolding.sln --no-restore -v normal`
  - Passed.
  - Total tests: 104.
  - Passed: 103.
  - Skipped: 1 live Cloudflare R2 smoke test gated by `RUN_LIVE_R2_TESTS=true`.

No `npm run build:css` was run during this audit because it would rewrite generated CSS.

## 20. Final Recommendation

Proceed with implementation using a new page-scoped app modal, but require explicit booking-modal coordination in the implementation plan.

The safest implementation is:

1. Add static app modal markup to `Views/Home/Index.cshtml`.
2. Add a dedicated `data-home-app-modal-trigger` to only the take-home CTA.
3. Add `wwwroot/js/home-app-modal.js` for open/close/focus/trap behavior.
4. Add `.home-app-modal*` CSS only.
5. Add a minimal booking-modal suppression hook if needed so the homepage booking auto-open timer cannot fire during or after app modal engagement.
6. Keep footer and `/thanh-vien` unchanged.

This avoids broad refactors and preserves current reservation behavior while preventing stacked modal regressions.
