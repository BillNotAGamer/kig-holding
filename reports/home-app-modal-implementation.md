# Homepage App Download Modal Implementation

## 1. Verdict

HOME_APP_MODAL_IMPLEMENTATION_NEEDS_MANUAL_QA

The implementation is complete at source, CSS, JavaScript, documentation, and automated-test level. Manual browser QA at the requested viewport matrix was not executed in this environment, so the final verdict is intentionally not `HOME_APP_MODAL_IMPLEMENTATION_ACCEPTED`.

## 2. Source Verification

Footer source inspected:

- `Views/Shared/Partials/_Footer.cshtml`

Footer app-download values reused exactly:

- QR image: `~/images/general/kig-holding-qr-app.webp`
- App Store badge: `~/images/general/icons/app-store-download.svg`
- Google Play badge: `~/images/general/icons/google-play-download.svg`
- App Store URL: `https://apps.apple.com/vn/app/truy%E1%BB%81n-thuy%E1%BA%BFt-champong-%EC%A0%88%EC%84%A4%EC%9D%98%EC%A7%AC%EB%BD%BB/id6753157313?l=vi`
- Google Play URL: `https://play.google.com/store/apps/details?id=com.eggstech.champong.app&pcampaignid=web_share`
- External-link attributes: `target="_blank"` and `rel="noopener noreferrer"`

Evidence:

- Footer QR: `Views/Shared/Partials/_Footer.cshtml:85`
- Footer App Store link/badge: `Views/Shared/Partials/_Footer.cshtml:99` and `Views/Shared/Partials/_Footer.cshtml:100`
- Footer Google Play link/badge: `Views/Shared/Partials/_Footer.cshtml:103` and `Views/Shared/Partials/_Footer.cshtml:104`
- Homepage modal QR: `Views/Home/Index.cshtml:745`
- Homepage modal App Store link/badge: `Views/Home/Index.cshtml:762` and `Views/Home/Index.cshtml:767`
- Homepage modal Google Play link/badge: `Views/Home/Index.cshtml:773` and `Views/Home/Index.cshtml:778`

## 3. Initial Worktree State

Initial branch and HEAD:

- Branch: `main...origin/main`
- HEAD: `19f682c6603b4145ea7c8db6d27b256d909b1cde`

Pre-existing tracked changes before this task:

- `Views/Member/Index.cshtml`
- `Views/Menu/Index.cshtml`
- `docs/feature-state.md`
- `docs/ui-architecture.md`
- `wwwroot/css/input.css`
- `wwwroot/css/site.css`

Pre-existing untracked paths before this task:

- `reports/home-app-modal-audit.md`
- `reports/member-app-promo-implementation.md`
- `reports/member-assets-fix-report.md`
- `reports/member-tier-card-design-audit.md`
- `reports/member-tier-card-implementation.md`
- `wwwroot/images/member/`
- `wwwroot/js/member-tier-tabs.js`

`git diff --check` at baseline reported no whitespace errors, only line-ending warnings.

## 4. Files Changed

Task source changes:

- `Views/Home/Index.cshtml`
- `wwwroot/css/input.css`
- `wwwroot/css/site.css`
- `wwwroot/js/home-app-modal.js`
- `wwwroot/js/booking-modal.js`
- `docs/feature-state.md`
- `docs/ui-architecture.md`
- `reports/home-app-modal-implementation.md`

Task test change:

- `KIGHolding.Tests/HomePageTests.cs`

Note: `KIGHolding.Tests/` is ignored by `.gitignore`, verified by `git status --short --ignored KIGHolding.Tests\HomePageTests.cs` returning `!! KIGHolding.Tests/`.

Pre-existing user-owned changes preserved:

- Existing membership page work in `Views/Member/Index.cshtml`
- Existing menu page work in `Views/Menu/Index.cshtml`
- Existing membership/docs/CSS generated changes
- Existing untracked membership assets, member-tier script, and reports

## 5. CTA Behavior

The existing `Mang Champong về nhà` CTA remains an `<a>` with `/dat-ban` fallback.

Implemented attributes:

- `href="/dat-ban"`
- `aria-label="Mở tùy chọn tải ứng dụng Truyền Thuyết Champong"`
- `aria-haspopup="dialog"`
- `aria-controls="home-app-modal"`
- `data-home-app-modal-trigger`

Evidence:

- Take-home section: `Views/Home/Index.cshtml:472`
- Modal trigger: `Views/Home/Index.cshtml:498`

No-JavaScript behavior remains navigation to `/dat-ban`.

## 6. Modal Markup

Implemented one static server-rendered modal in `Views/Home/Index.cshtml`.

Key structure:

- Root: `#home-app-modal`
- Root state: `aria-hidden="true"` and `hidden`
- Backdrop: `data-home-app-modal-backdrop`
- Dialog: `role="dialog"` and `aria-modal="true"`
- Title: `#home-app-modal-title`
- Description: `#home-app-modal-description`
- Close controls: top-right close button and `Tiếp tục trên website`

Evidence:

- Modal root: `Views/Home/Index.cshtml:722`
- Dialog labelled by title: `Views/Home/Index.cshtml:733`
- Modal title: `Views/Home/Index.cshtml:754`
- Page-specific script reference: `Views/Home/Index.cshtml:798`

The modal does not duplicate footer navigation or footer contact content.

## 7. JavaScript Behavior

Implemented `wwwroot/js/home-app-modal.js` as a page-scoped vanilla script.

Behavior:

- Initializes safely on `DOMContentLoaded`.
- No-op when modal markup or trigger is absent.
- Uses `data-home-app-modal-bound="true"` to avoid duplicate binding.
- Intercepts only `[data-home-app-modal-trigger]`.
- Prevents default navigation only when JavaScript is active and opening the modal.
- Moves focus to the first close control or dialog.
- Restores focus to the triggering CTA on close.
- Supports Escape close.
- Supports backdrop close.
- Implements focus containment with Tab/Shift+Tab.
- Locks page scroll with `body.home-app-modal-open`.
- Closes mobile menu and floating contact action groups before opening where present.
- Refuses to open if the booking modal is already open.
- Respects `prefers-reduced-motion`.

Evidence:

- Modal binding selectors and idempotency: `wwwroot/js/home-app-modal.js:3` to `wwwroot/js/home-app-modal.js:18`
- Coordination event payload: `wwwroot/js/home-app-modal.js:105`
- Body scroll lock class add/remove: `wwwroot/js/home-app-modal.js:124` and `wwwroot/js/home-app-modal.js:139`

## 8. Booking Modal Coordination

Implemented a minimal coordination hook in `wwwroot/js/booking-modal.js`.

Added behavior:

- Booking modal listens for `kig:booking-modal:suppress-auto-open`.
- When received, pending auto-open and auto-close timers are cleared.
- The homepage auto booking prompt is marked as shown for the current session.
- Manual `[data-booking-modal-trigger]` behavior remains unchanged.
- Auto-open refuses to run while `body.home-app-modal-open` is present.

Evidence:

- App modal active guard: `wwwroot/js/booking-modal.js:503`
- Coordination event listener: `wwwroot/js/booking-modal.js:520`

Scenario policy implemented:

- App modal opened before booking auto-open: booking auto-open is suppressed for the session.
- Booking modal already open: app modal refuses to open.
- App modal closes after suppressing booking auto-open: booking auto-open does not fire later in that session.
- Mobile menu or floating contact panel open: app modal attempts to close them before opening.

## 9. CSS

Added a page-scoped modal namespace under `.home-app-modal`.

Key CSS:

- `body.home-app-modal-open`
- `.home-app-modal`
- `.home-app-modal.is-open`
- `.home-app-modal__backdrop`
- `.home-app-modal__dialog`
- `.home-app-modal__close`
- `.home-app-modal__layout`
- `.home-app-modal__qr`
- `.home-app-modal__stores`
- `.home-app-modal__store-link`
- `.home-app-modal__continue`

Responsive behavior:

- Mobile: single-column modal layout, centered QR, stacked store badges.
- `min-width: 640px`: store badges can align in a row.
- `min-width: 768px`: QR and content use a two-column modal layout.
- Reduced motion: transitions disabled for modal and interactive elements.

Evidence:

- Source CSS starts at `wwwroot/css/input.css:6551`.
- Responsive CSS starts at `wwwroot/css/input.css:6749`.
- Reduced-motion CSS starts at `wwwroot/css/input.css:6777`.

Generated CSS:

- `npm run build:css` completed successfully.
- `wwwroot/css/site.css` was generated by Tailwind and was not manually edited.

## 10. Accessibility

Implemented:

- Dialog semantics: `role="dialog"` and `aria-modal="true"`.
- Title/description relationships via `aria-labelledby` and `aria-describedby`.
- Trigger relationship via `aria-controls`.
- Meaningful QR alt text.
- App Store and Google Play links have accessible labels.
- External links use safe attributes.
- Close button has a clear accessible label.
- Escape and backdrop close.
- Focus containment while open.
- Focus restoration to trigger on close.
- Visible focus uses existing global focus styles and modal-specific focus styles.

## 11. Responsive Verification

Code-level responsive coverage exists for:

- `375px`
- `390px`
- `430px`
- `768px`
- `1024px`
- `1440px`

Manual browser inspection was not run in this environment. Required manual checks remain:

- CTA opens modal at each width.
- QR remains scannable.
- Badges remain tappable and unclipped.
- Modal scrolls internally on short-height screens.
- No horizontal overflow.
- No collision with floating contact buttons.

## 12. Automated Tests

Added homepage tests covering:

- Homepage action returns default `ViewResult`.
- Take-home CTA is the only app-modal trigger in that section.
- `/dat-ban` fallback remains.
- Modal markup exists exactly once with dialog semantics.
- Footer app QR, badges, URLs, and safe link attributes are reused exactly.
- Page-specific script is referenced.
- Other `/dat-ban` booking links are not converted to app-modal triggers.
- App modal script implements idempotency, focus/keyboard handling, scroll lock, and booking-modal coordination.
- Booking modal script suppresses auto-open without removing manual trigger flow.
- CSS is scoped under `.home-app-modal`.
- Footer and `/thanh-vien` do not contain homepage modal markup.

Required verification results:

- `npm run build:css`: passed.
- `dotnet restore KIGHolding.sln`: passed.
- `dotnet build KIGHolding.sln`: passed, 0 warnings, 0 errors.
- `dotnet test KIGHolding.sln --no-restore -v normal`: passed, 112 passed, 1 skipped live R2 smoke test.

Note: a non-escalated build attempt failed due sandboxed NuGet signature endpoint access. The required build passed after running with permitted network access.

## 13. Documentation

Updated:

- `docs/feature-state.md`
- `docs/ui-architecture.md`

Documented:

- Homepage take-home app modal.
- `/dat-ban` no-JavaScript fallback.
- Footer QR/badge/link reuse.
- No ordering backend claim.
- Single-active-modal policy.
- Booking auto-open suppression event.
- Manual booking triggers remain available.

## 14. Regression Verification

Confirmed by focused diffs/searches:

- Footer markup unchanged.
- `/thanh-vien` markup not modified by this task; existing diff is pre-existing.
- `wwwroot/js/member-tier-tabs.js` unchanged by this task.
- `wwwroot/images/member/` unchanged by this task.
- No backend/controller/database/R2/storage files changed.
- No migration created.
- No production DB access.
- No deployment.

Booking behavior:

- Manual booking triggers remain bound through existing `[data-booking-modal-trigger]` flow.
- Auto booking prompt is suppressed only when the homepage app modal is opened.

## 15. Remaining Limitations

- Manual browser QA is still required for the requested viewport matrix.
- The modal uses duplicated static footer values rather than a shared app-download partial. This was chosen to avoid changing accepted footer or `/thanh-vien` rendering in this tightly scoped task.
- The CTA visible label remains `Đặt ngay` per the current page; accessibility label clarifies that it opens app-download options.
- No app-ordering backend was added or claimed.

## 16. Final Worktree State

Final `git status --short --branch`:

```text
## main...origin/main
 M Views/Home/Index.cshtml
 M Views/Member/Index.cshtml
 M Views/Menu/Index.cshtml
 M docs/feature-state.md
 M docs/ui-architecture.md
 M wwwroot/css/input.css
 M wwwroot/css/site.css
 M wwwroot/js/booking-modal.js
?? reports/home-app-modal-audit.md
?? reports/home-app-modal-implementation.md
?? reports/member-app-promo-implementation.md
?? reports/member-assets-fix-report.md
?? reports/member-tier-card-design-audit.md
?? reports/member-tier-card-implementation.md
?? wwwroot/images/member/
?? wwwroot/js/home-app-modal.js
?? wwwroot/js/member-tier-tabs.js
```

Additional task-created ignored path:

- `KIGHolding.Tests/HomePageTests.cs`

No files were staged or committed.
