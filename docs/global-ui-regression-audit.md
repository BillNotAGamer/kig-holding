# Global UI Regression Audit Report

## 1. Executive Summary

The public UI is in a safe state after the recent redesign phases. The application builds successfully, the audited public routes return HTTP 200 in local smoke testing, and the major recent feature contracts remain intact:

- Header navigation includes the KIG Holding dropdown/group and placeholder routes.
- Footer layout and assets remain present.
- Home keeps the approved 7-section structure, finalized hero slider, and finalized "Mang Champong ve nha" section.
- Branch locator supports real GET search, city filtering, combined filtering, suggestions, reset state, branch cards, and map section.
- Reservation preselect via `/dat-ban?branch=<slug>` remains supported.
- The new `data-uw-*` reveal animation contract remains active.
- No old reveal classes were found in source views, JavaScript, input CSS, controllers, viewmodels, or view components.

No Critical or High regressions were found. The main follow-up risk is missing placeholder image assets referenced by older non-redesigned pages.

## 2. Build Result

Command:

```powershell
dotnet build .\KIGHolding.csproj
```

Result: Success

Warnings:

- `NU1900: Error occurred while getting package vulnerability data: Unable to load the service index for source https://api.nuget.org/v3/index.json.`
- The warning appeared during restore/build metadata checks and is consistent with restricted or unavailable NuGet vulnerability-feed access.
- No compile errors were reported.
- No code warnings were reported.

Summary:

- 2 warnings
- 0 errors
- Build output: `bin\Debug\net10.0\KIGHolding.dll`

## 3. Route Smoke Test Results

Local route smoke testing was performed against the development server. Routes rendered without runtime exceptions in the tested responses.

| Route | Status | Result | Notes |
|---|---:|---|---|
| `/` | 200 | Rendered | Home marker found: `data-home-section="hero"` |
| `/thuc-don` | 200 | Rendered | Menu page marker found |
| `/chi-nhanh` | 200 | Rendered | Branch locator marker found: `data-branch-locator` |
| `/chi-nhanh?q=quan%202` | 200 | Rendered | Search query preserved in response |
| `/chi-nhanh?city=H%E1%BB%93%20Ch%C3%AD%20Minh` | 200 | Rendered | City filter state rendered |
| `/chi-nhanh?city=H%E1%BB%93%20Ch%C3%AD%20Minh&q=quan%202` | 200 | Rendered | Combined city and search state rendered |
| `/chi-nhanh?q=zzzz-no-branch` | 200 | Rendered | Empty state rendered |
| `/dat-ban` | 200 | Rendered | Reservation page rendered |
| `/dat-ban?branch=truyen-thuyet-champong-quan-2-gogi-maru-mi-cay-bbq-han-quoc` | 200 | Rendered | Actual branch slug found in HTML and selected branch marker detected |
| `/gioi-thieu` | 200 | Rendered | About page rendered |
| `/lien-he` | 200 | Rendered | Contact page rendered |
| `/thanh-vien` | 200 | Rendered | Placeholder member route rendered |
| `/lien-he-nhuong-quyen` | 200 | Rendered | Placeholder franchise route rendered |
| `/tin-tuc` | 200 | Rendered | News page rendered |

Runtime note:

- During local server testing, ASP.NET logged `HttpsRedirectionMiddleware: Failed to determine the https port for redirect.` This is a local development configuration warning, not a route-render failure.

## 4. Header Audit

Files inspected:

- `Views/Shared/Partials/_Header.cshtml`
- `Views/Shared/Partials/_MobileMenu.cshtml`
- `ViewModels/Shared/NavItemViewModel.cs`
- `ViewComponents/LayoutFallbacks.cs`

Findings:

- Header navigation is helper-generated through `LayoutFallbacks`.
- `NavItemViewModel` supports child items through `Children` and `HasChildren`.
- Top-level header items are:
  - `Trang chu` -> `/`
  - `Thuc don` -> `/thuc-don`
  - `Dat ban` -> `/dat-ban`
  - `Chi nhanh` -> `/chi-nhanh`
  - `Tin tuc` -> `/tin-tuc`
  - `KIG Holding` group
  - `Lien he nhuong quyen` -> `/lien-he-nhuong-quyen`
- KIG Holding children are:
  - `Ve KIG Holding` -> `/gioi-thieu`
  - `Thanh vien` -> `/thanh-vien`
  - `Lien he` -> `/lien-he`
- No old top-level `Gioi thieu` item was found in the nav construction.
- No old top-level `Lien he` item was found in the nav construction.
- Desktop dropdown uses hover/focus-within behavior and regular anchor child links.
- Mobile menu uses native `<details>/<summary>` for the KIG Holding nested group.

Risk:

- Desktop dropdown is CSS-driven and does not dynamically update `aria-expanded`. Keyboard access is still present through focus-within, but dynamic `aria-expanded` would be a future accessibility polish item.

## 5. Footer Audit

File inspected:

- `Views/Shared/Partials/_Footer.cshtml`

Findings:

- Footer layout remains the completed dark, Haidilao-inspired direction.
- Footer logo path falls back to `/images/general/kig-no-bg-logo.png`, which exists.
- QR image path `~/images/general/kig-holding-qr.png` exists.
- App button placeholders are text/CSS based and do not introduce missing image risk.
- Branch and contact sections remain present.
- Footer still uses the new `data-uw-*` animation attributes.

Risks:

- Footer quick links include `/khuyen-mai` and `/tuyen-dung`; these routes were not part of the requested smoke route list and should be verified or replaced when those pages are implemented.

## 6. Home Audit

Files inspected:

- `Views/Home/Index.cshtml`
- `Controllers/HomeController.cs`
- `ViewModels/HomeIndexViewModel.cs`
- `wwwroot/js/home-hero-slider.js`

Findings:

- Home route `/` renders successfully.
- The 7 official sections remain present:
  1. Hero banner slider
  2. `Cau chuyen thuong hieu`
  3. `Tinh hoa am thuc`
  4. `Thuc don`
  5. `Mang Champong ve nha`
  6. `Chuong trinh khuyen mai`
  7. `Dat ban ngay`
- Hero slider markup uses the scoped `champong-hero-*` naming and `data-champong-hero-*` attributes.
- `wwwroot/js/home-hero-slider.js` exists and is loaded from the Home view through a page script section with `asp-append-version`.
- Hero CTA links remain:
  - `/dat-ban`
  - `/thuc-don`
  - `/chi-nhanh`
- Home uses existing `HomeIndexViewModel` data for dynamic menu/posts/branches/reservation content where applicable.
- HomeController and HomeIndexViewModel were not damaged by the UI phases.
- No old reveal classes were found in the Home view.
- Home uses `data-uw-*` reveal attributes.
- No missing image paths were introduced by the approved hero/take-home sections; they use CSS/gradient compositions rather than unverified images.

Risk:

- The Home hero currently relies on CSS visual treatments rather than production photography. This is acceptable and intentional, but real verified assets may improve final polish later.

## 7. Branch Page Audit

Files inspected:

- `Controllers/BranchController.cs`
- `ViewModels/BranchIndexViewModel.cs`
- `Views/Branch/Index.cshtml`
- `Views/Shared/Components/BranchCard/Default.cshtml`
- `wwwroot/js/branch-locator.js`

Findings:

- Public route `/chi-nhanh` renders successfully.
- `BranchController.Index` accepts `string? city` and `string? q`.
- Search is real GET search through `name="q"`.
- Search filtering is case-insensitive and diacritic-insensitive.
- Search matches branch name, address, city, district, and hotline.
- City filtering remains query-based with `?city=...`.
- Combined filtering works with `?city=...&q=...`.
- `BranchIndexViewModel` exposes filter state and counts:
  - `SelectedCity`
  - `SearchQuery`
  - `TotalCount`
  - `FilteredCount`
  - `CityCounts`
  - `HasActiveFilters`
- The Branch view includes:
  - compact locator header
  - real GET search form
  - area dropdown
  - city filter links
  - active filter chips
  - reset filter link
  - grouped result sections
  - compact branch card grid
  - empty state
  - map section below results
  - branch suggestions dataset and script load
- `wwwroot/js/branch-locator.js` is isolated and no-ops outside `[data-branch-locator]`.
- Autocomplete supports:
  - typed suggestions
  - diacritic-insensitive matching
  - click selection
  - ArrowUp/ArrowDown keyboard navigation
  - Enter selection
  - Escape close
  - outside click close
- BranchCard preserves:
  - root `id="@Model.Slug"`
  - `/dat-ban?branch=@Model.Slug`
  - Google Maps external link with `target="_blank"` and `rel="noopener"`
  - `tel:` hotline link
  - image fallback visual
- No decorative disabled search input remains.
- Map is below results and not beside the branch card.

Risk:

- Autocomplete suggestions are generated from the branches rendered in the current view. When filters are active, suggestions may be limited to the filtered result set. This is acceptable for the current data size, but a future all-branch suggestion dataset or endpoint may be needed if UX expectations grow.

## 8. Reservation Flow Audit

Files inspected:

- `Controllers/ReservationController.cs`
- `Views/Reservation/Index.cshtml`
- `Views/Reservation/Success.cshtml`
- `Views/Shared/Components/BookingMiniForm/Default.cshtml`

Findings:

- `/dat-ban` returns HTTP 200.
- `ReservationController.Index` supports branch preselect through `[FromQuery] string? branch`.
- `ResolveSelectedBranch` resolves branch slug and branch ID.
- Smoke test with actual branch slug returned HTTP 200 and rendered the selected branch marker.
- Branch card links to `/dat-ban?branch=@Model.Slug` remain compatible with reservation preselect logic.
- BookingMiniForm still renders through the existing public layout.
- No reservation backend behavior was changed by this audit.

## 9. Animation Audit

Files inspected:

- `wwwroot/js/uw-reveal.js`
- `Views/Shared/_Layout.cshtml`
- Relevant views using reveal attributes

Findings:

- `wwwroot/js/uw-reveal.js` exists.
- `_Layout.cshtml` loads `~/js/uw-reveal.js` with `asp-append-version="true"`.
- The new animation contract remains in use:
  - `data-uw-reveal`
  - `data-uw-delay`
  - `data-uw-duration`
  - `data-uw-distance`
  - `data-uw-once`
- `uw-reveal.js` retains reduced-motion behavior and the localStorage override behavior.
- `home-hero-slider.js` and `branch-locator.js` use scoped selectors and do not directly conflict with `uw-reveal.js`.
- Search across source views, JS, input CSS, controllers, viewmodels, and view components found no old reveal class regressions:
  - `scroll-reveal`
  - `reveal-up`
  - `reveal-left`
  - `reveal-right`
  - `delay-100`
  - `delay-200`
  - `is-visible`

## 10. Asset Audit

Verified existing assets:

| Asset | Exists? | Used/Relevant Area | Notes |
|---|---:|---|---|
| `wwwroot/images/general/kig-no-bg-logo.png` | Yes | Header/footer/logo fallback | Verified on disk |
| `wwwroot/images/general/kig-holding-qr.png` | Yes | Footer QR | Verified on disk |
| `wwwroot/images/home/bg-hero.webp` | Yes | Home asset pool | Verified on disk |
| `wwwroot/uploads/branches/` | Yes | Branch uploads | Contains branch upload files |
| `wwwroot/uploads/branches/champong-quan-5-4b410349ba84408988a1a0895f6cca32.png` | Yes | Branch upload asset | Present but currently untracked in git status |

Missing or risky placeholder references:

| Reference | File | Risk |
|---|---|---|
| `/images/placeholders/news-hero.webp` | `Views/News/Index.cshtml` | Placeholder asset may 404 |
| `/images/placeholders/post-card.webp` | `Views/News/Detail.cshtml` | Placeholder asset may 404 |
| `/images/placeholders/about-hero.webp` | `Views/About/Index.cshtml` | Placeholder asset may 404 |
| `/images/placeholders/korean-street-noodle.webp` | `Views/About/Index.cshtml` | Placeholder asset may 404 |
| `/images/placeholders/champong-broth.webp` | `Views/About/Index.cshtml` | Placeholder asset may 404 |
| `/images/placeholders/group-tables.webp` | `Views/About/Index.cshtml` | Placeholder asset may 404 |
| `/images/placeholders/kitchen-chef.webp` | `Views/About/Index.cshtml` | Placeholder asset may 404 |
| `/images/placeholders/food-table.webp` | `Views/About/Index.cshtml` | Placeholder asset may 404 |
| `/images/placeholders/service-counter.webp` | `Views/About/Index.cshtml` | Placeholder asset may 404 |
| `/images/placeholders/champong-hero.webp` | `Views/Shared/_Layout.cshtml` | Open Graph fallback image may 404 |
| `/images/placeholders/food-card.webp` | `Views/Menu/Detail.cshtml` | Placeholder asset may 404 |
| `/images/placeholders/menu-hero.webp` | `Views/Menu/Index.cshtml` | Placeholder asset may 404 |
| `/images/placeholders/booking-table.webp` | `Views/Menu/Index.cshtml` | Placeholder asset may 404 |

Notes:

- The recent Home hero and Branch locator work did not add new broken image paths.
- Branch cards include fallback behavior to reduce broken image impact.
- Older placeholder image references should be resolved before production content freeze.

## 11. CSS/JS Audit

Files inspected:

- `wwwroot/css/input.css`
- `wwwroot/css/site.css`
- `wwwroot/js/site.js`
- `wwwroot/js/home-hero-slider.js`
- `wwwroot/js/branch-locator.js`
- `wwwroot/js/uw-reveal.js`

Findings:

- `wwwroot/css/input.css` remains the canonical Tailwind source file.
- `wwwroot/css/site.css` exists as the generated stylesheet.
- This audit did not rebuild Tailwind and did not edit CSS.
- `site.js` remains focused on global UI behavior such as mobile drawer, menu filtering, and back-to-top interactions.
- Old reveal logic was not found in source JS.
- `home-hero-slider.js` is scoped to the Home hero selector and is loaded only from the Home view script section.
- `branch-locator.js` is scoped to `[data-branch-locator]` and safely exits on non-branch pages.
- `_Layout.cshtml` loads `site.js`, `uw-reveal.js`, and then page scripts.

Risk:

- `_Layout.cshtml` still includes Swiper CDN scripts/styles. The current Home hero uses a custom local slider, so this dependency should be reviewed later if no other page requires it.

## 12. Backend/Admin Safety Check

Inspected areas and git state indicate no unexpected backend/admin/database changes from the recent UI work in the current working tree.

Confirmed safe areas:

- No modified `Models/Entities` files detected in current git status.
- No modified `Data/Migrations` files detected in current git status.
- No modified `Areas/Admin` files detected in current git status.
- No modified `Services` files detected in current git status.
- Reservation behavior remains intact.

Current git status at audit time showed only UI/static-asset related changes:

- `Views/Branch/Index.cshtml`
- `wwwroot/css/site.css`
- `wwwroot/js/branch-locator.js`
- `wwwroot/uploads/branches/champong-quan-5-4b410349ba84408988a1a0895f6cca32.png`

This audit created only this documentation file.

## 13. Issues Found

| Issue | Severity | File/route | Recommended fix |
|---|---|---|---|
| Several older pages reference `/images/placeholders/*` assets that were not verified on disk. Routes still render, but users or crawlers may encounter broken images or missing OG images. | Medium | `Views/About/Index.cshtml`, `Views/Menu/Index.cshtml`, `Views/Menu/Detail.cshtml`, `Views/News/Index.cshtml`, `Views/News/Detail.cshtml`, `Views/Shared/_Layout.cshtml` | Add verified placeholder assets or replace these references with existing assets/CSS fallbacks. |
| Footer quick links include unverified routes `/khuyen-mai` and `/tuyen-dung`. | Low | `Views/Shared/Partials/_Footer.cshtml` | Add routes/pages later or temporarily point links to existing pages such as `/tin-tuc` if required. |
| Desktop header dropdown is CSS-driven and does not dynamically update `aria-expanded`. | Low | `Views/Shared/Partials/_Header.cshtml` | Optional future accessibility polish: add a tiny scoped dropdown controller or use semantics that do not imply dynamic state. |
| Branch autocomplete suggestions are based on the currently rendered branch dataset, so filtered pages may have filtered suggestions. | Info | `Views/Branch/Index.cshtml`, `wwwroot/js/branch-locator.js` | If branch count grows or UX requires global suggestions, expose a dedicated all-active-branches suggestion dataset or endpoint. |
| Build reports `NU1900` because NuGet vulnerability metadata could not be loaded. | Info | Build environment | Recheck in an environment with NuGet network access; no code fix required. |
| One branch upload file is present but untracked in git status. | Info | `wwwroot/uploads/branches/champong-quan-5-4b410349ba84408988a1a0895f6cca32.png` | Decide whether this is intended content to commit or a local upload artifact to ignore. |

Severity count:

- Critical: 0
- High: 0
- Medium: 1
- Low: 2
- Info: 3

## 14. Final Recommendation

The site is safe to proceed to the next feature/page from a UI regression standpoint.

Before production handoff, address the Medium placeholder-asset issue and verify whether footer placeholder routes should remain clickable. The remaining Low and Info items are non-blocking polish or environment/content-management follow-ups.
