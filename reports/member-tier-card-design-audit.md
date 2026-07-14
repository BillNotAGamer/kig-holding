# Member Tier Card Design Audit

## 1. Audit Verdict
`TIER_CARD_DESIGN_READY_WITH_ASSET_BLOCKERS`

## 2. Executive Summary
- Current tier-component architecture is fully static: four rate cards render together, four detailed tier cards render together, and no tier-selection state exists in source. Evidence: `Views/Member/Index.cshtml:54-75`, `Views/Member/Index.cshtml:121-269`, `wwwroot/js/uw-reveal.js:1-257`.
- The approved redesign should replace the current duplicated four-tier presentation with one merged tabbed module:
  - selected tier visual card
  - horizontally scrollable selector pills
  - selected tier benefit list
  - selected tier invoice example and status text
- The rate-card grid and the detailed tier-card grid should be merged into one component. Keeping both would preserve the current duplication and still fail the screenshot contract.
- Recommended interaction model: accessible button tabs with minimal vanilla JavaScript and server-rendered tab panels kept in the DOM.
- Asset readiness is incomplete for pixel fidelity:
  - the repo has no screenshot-matching metallic KIG mark
  - the repo has no screenshot-matching benefit-row icon set
  - the repo has no faceted diamond texture matching the reference
- Expected implementation complexity: medium. Markup and CSS are straightforward; fidelity work is constrained by missing assets and by the need to preserve all approved business content.
- Highest regression risks:
  - hiding approved invoice/status content while simplifying the module
  - touching shared editorial button styles
  - rewriting generated CSS during a task that should stay scoped to the member component

## 3. Repository and Worktree Baseline
Initial baseline:
- branch: `main...origin/main`
- root: `F:/Coding/Web development/KIG Holding/KIGHolding`
- HEAD: `19f682c6603b4145ea7c8db6d27b256d909b1cde`
- pre-existing modified files:
  - `Views/Menu/Index.cshtml`
  - `wwwroot/css/site.css`

Initial commands:
- `git status --short --branch` -> showed only the two files above
- `git rev-parse --show-toplevel` -> succeeded
- `git rev-parse HEAD` -> succeeded
- `git log -5 --oneline --decorate` -> succeeded
- `git diff --name-status` -> showed the two files above
- `git diff --stat` -> showed 2 files changed, 2 insertions, 2 deletions
- `git diff --check` -> clean apart from LF/CRLF warnings only

Final baseline expectation for this audit:
- the only audit-created repository file is `reports/member-tier-card-design-audit.md`
- no source implementation changes were made

## 4. Current Membership-Tier DOM Inventory

| Item | Current source | Current behavior | Keep | Replace | Risk |
| ---- | -------------- | ---------------- | ---- | ------- | ---- |
| Rate overview card grid | `Views/Member/Index.cshtml:54-75` | Four rate cards render simultaneously for Bronze/Silver/Gold/Diamond | No | Yes | Duplicates tier information and conflicts with single-selected-card design |
| Point validity block | `Views/Member/Index.cshtml:77-82` | Single validity panel under rate cards | Yes | No | Low |
| Tier threshold grid | `Views/Member/Index.cshtml:93-110` | Four threshold cards render simultaneously | Yes | No | Low |
| Detailed tier card grid | `Views/Member/Index.cshtml:121-269` | Four large detail cards render simultaneously with benefits, invoice example, and note | No | Yes | High; all approved detailed content currently lives here |
| Tier-progress rule cards | `Views/Member/Index.cshtml:280-298` | Separate two-card rule section | Yes | No | Low |
| Birthday table | `Views/Member/Index.cshtml:312-347` | Separate semantic table | Yes | No | Low |
| Upgrade/downgrade rule cards | `Views/Member/Index.cshtml:362-378` | Separate two-card rule section | Yes | No | Low |
| Important terms | `Views/Member/Index.cshtml:389-406` | Separate four-card summary | Yes | No | Low |

Confirmed source behavior:
- All four tiers are rendered simultaneously twice:
  - rate summary: `Views/Member/Index.cshtml:54-75`
  - detailed cards: `Views/Member/Index.cshtml:121-269`
- Current rate cards duplicate content already expressed in the detailed tier cards:
  - Bronze/Silver/Gold/Diamond names and rates appear in both sections
- No tier-specific conditional rendering exists in Razor.

Recommended replacement scope:
- Merge both the rate grid and detailed tier grid into one tabbed module.
- Preserve the threshold grid, birthday table, rule sections, terms, hero, and CTA outside that module.

## 5. Current CSS and JavaScript Inventory
Authoritative CSS source:
- `package.json:5-7` defines `build:css` from `wwwroot/css/input.css` to `wwwroot/css/site.css`
- `wwwroot/css/input.css:1933-2364` contains the current member-page component source block
- `wwwroot/css/site.css:3087-3473` contains the compiled member-page selectors

Current tier-related selectors:
- shared glass-card shell: `wwwroot/css/input.css:1960-1973`
- four-column rate/threshold/terms grids: `wwwroot/css/input.css:1999-2004`
- two-column detailed tier grid: `wwwroot/css/input.css:2060-2067`
- invoice row layout: `wwwroot/css/input.css:2118-2148`
- birthday table wrapper and table: `wwwroot/css/input.css:2168-2213`
- tier color variants only via border colors:
  - Bronze: `wwwroot/css/input.css:2220-2223`
  - Silver: `wwwroot/css/input.css:2225-2228`
  - Gold: `wwwroot/css/input.css:2230-2233`
  - Diamond: `wwwroot/css/input.css:2235-2238`
- mobile collapse to one column: `wwwroot/css/input.css:2331-2353`
- tablet two-column summary and one-column detail: `wwwroot/css/input.css:2355-2365`

Current visual behavior from CSS:
- the member module is not card-tabs; it is a dark glass editorial grid
- the selected-tier screenshot look is not present:
  - no landscape membership card
  - no logo placement rule inside a card
  - no progress bar
  - no selector pill row
  - no single-panel switching behavior

Current JavaScript:
- the only member-page JS influence is scroll reveal via `data-uw-reveal` attributes in `Views/Member/Index.cshtml:5`, `17`, `55-70`, `94-106`, `122-232`, `281-290`, `312`, `349`, `363-402`, `410`
- reveal engine source: `wwwroot/js/uw-reveal.js:1-257`
- no member-tier interaction script exists
- no member-tier tab semantics exist in current markup or JS

Reusable interaction evidence elsewhere:
- `wwwroot/js/home-hero-slider.js:204` and `wwwroot/js/home-hero-slider.js:210` handle arrow keys for a different component
- `wwwroot/js/branch-locator.js:95` sets `aria-selected` in a different autocomplete widget
- there is no reusable tab utility for the member page

Shared-class safety:
- `member-program*` selectors appear only in `Views/Member/Index.cshtml` and `wwwroot/css/input.css` / `wwwroot/css/site.css`
- shared button selectors used by reservation success remain external to the member namespace:
  - `Views/Reservation/Success.cshtml:65-66`
  - `wwwroot/css/input.css:1756-1798`

## 6. Reference Screenshot Analysis
Reference files found locally:
- `C:\Users\Admin\Downloads\1783999497943_38635729333297575_5324825542437930187_914cde999e6d31d8e7863d3fd98410d8.jpg`
- `C:\Users\Admin\Downloads\1783999498006_38635729333297575_5324825542437930187_9bd0299c65d602d3bc3df55a76c0033a.jpg`
- `C:\Users\Admin\Downloads\1783999498069_38635729333297575_5324825542437930187_1344801f06b29418e931ded50707daa9.jpg`
- `C:\Users\Admin\Downloads\1783999498154_38635729333297575_5324825542437930187_d8ef2952902e90af8abf2589c4a8ff39.jpg`

Confirmed image dimensions:
- all four are `1260 × 2800`

Visual observations from the screenshots:
- the module is embedded in a white app-like page surface
- a single landscape tier card is shown at a time
- a pill-row selector sits directly under the card
- only the selected tier’s benefits are shown
- the benefit count varies by tier:
  - Bronze screenshot shows 1 row
  - Silver screenshot shows 5 rows
  - Gold screenshot shows 2 rows
  - Diamond screenshot shows 5 rows

Estimated measurements from the screenshots:
- card width: about `91%–93%` of viewport width
- card side margins: about `3.5%–4.5%` per side
- card aspect ratio: about `1.8:1` to `1.9:1`
- card corner radius: about `7%–9%` of card height
- internal horizontal padding: about `6%–7%` of card width
- title block top offset: about `14%–17%` of card height
- logo box width: about `16%–18%` of card width
- progress track width: about `84%–88%` of card width
- progress track thickness: visually slim, roughly `2%–3%` of card height
- gap between card and selector: about `4%–5%` of card height
- selector pill height: about `5%` of viewport width on the screenshot scale, equivalent to a large mobile touch target
- selector gap: about `2%` of viewport width
- benefit row vertical padding: about `1.25rem–1.5rem` in CSS-equivalent spacing
- benefit icon column: visually fixed and narrow, about `2rem–2.5rem`

Tier-specific visual observations:
- Bronze: warm taupe/bronze field with a peach-white glow at the right edge
- Silver: cool blue-gray field with a cyan-white glow at the right edge
- Gold: strong orange-gold gradient with a bright yellow-white glow at lower right
- Diamond: red gradient with a polygon/faceted texture overlay plus bright red glow

## 7. Visual Fidelity Matrix

| Design element | Reference expectation | Current state | Required change | Asset blocker | Verification method |
| -------------- | --------------------- | ------------- | --------------- | ------------- | ------------------- |
| Bronze background | warm bronze gradient + right glow | only dark card with bronze border `wwwroot/css/input.css:2220-2223` | rebuild selected card background | No for approximation | screenshot comparison |
| Silver background | blue-gray + cyan glow | only dark card with silver border `wwwroot/css/input.css:2225-2228` | rebuild selected card background | No for approximation | screenshot comparison |
| Gold background | orange/gold gradient + bright glow | only dark card with gold border `wwwroot/css/input.css:2230-2233` | rebuild selected card background | No for approximation | screenshot comparison |
| Diamond background | red + faceted texture | only dark card with blue-toned border `wwwroot/css/input.css:2235-2238` | rebuild selected card background and texture | Yes for exact fidelity | screenshot comparison |
| Logo | metallic KIG mark top-right | no logo rendered in member module | add logo slot and tier-safe sizing | Yes for exact fidelity | screenshot comparison |
| Progress bar | wide rounded light track near bottom of card | no progress bar exists | add static/non-personalized track treatment | No | browser QA |
| Selector pills | horizontal scrollable pills, active black pill | no selector exists | add mobile-first tab list | No | browser QA |
| Benefit rows | icon rows on light surface with dividers | current benefits are inside dark cards as plain bullet lists `Views/Member/Index.cshtml:127-130`, `164-167`, `201-204`, `238-242` | replace with list-row layout | Yes for exact icon fidelity | browser QA |
| Card depth | soft premium glow, no hard border | current cards use glass border and shadow `wwwroot/css/input.css:1968-1973` | redesign card shell | No | screenshot comparison |
| Card layout | one selected tier only | four detailed cards shown at once `Views/Member/Index.cshtml:121-269` | replace with tabs | No | source QA |

## 8. Asset Inventory and Blockers
Existing assets:
- KIG circular logo: `wwwroot/images/general/kig-no-bg-logo.png` (`1080 × 1080`)
  - unsuitable for exact fidelity because it is a circular black logo badge, not the metallic KIG mark shown in the screenshots
- no other KIG logo candidate found under `wwwroot/images`
- generic icons exist under `wwwroot/images/home/icons/*`
  - examples: `team-icon.png`, `icons8-shop-64.png`, `icons8-origin-50.png`
  - none visually match the pink/red badge/hand icons from the screenshots
- generic dark texture exists: `wwwroot/images/home/images/texture.png`
  - unsuitable for exact diamond fidelity because it is a watercolor/grunge texture, not a red faceted polygon texture

Blockers:
- `ASSET_REQUIRED`: screenshot-matching metallic KIG logo or a vector/transparent equivalent
- `ASSET_REQUIRED`: screenshot-matching benefit-row icon set, or owner approval to approximate them with inline SVG
- `ASSET_REQUIRED` for exact fidelity only: red faceted diamond texture asset, or owner approval to approximate the effect with CSS-only polygon overlays

Non-blockers:
- Bronze/Silver/Gold backgrounds can be approximated with CSS gradients and pseudo-element glow without new files

## 9. Approved Content Mapping
Approved content already mapped in the current page:
- Bronze detailed content: `Views/Member/Index.cshtml:122-155`
- Silver detailed content: `Views/Member/Index.cshtml:158-192`
- Gold detailed content: `Views/Member/Index.cshtml:195-229`
- Diamond detailed content: `Views/Member/Index.cshtml:232-266`
- thresholds: `Views/Member/Index.cshtml:93-108`
- birthday rates: `Views/Member/Index.cshtml:324-344`

Recommended future mapping into the tabbed module:
- On the visual membership card:
  - tier display name
  - optional concise subtitle
  - brand mark/logo
  - decorative progress track
  - non-personalized threshold-oriented message
- In the selected benefit list:
  - approved tier benefits
  - approved rate/discount summary where relevant
  - birthday high-level benefit row if it belongs to the tier panel summary
- Below the benefit list, inside the same tab panel:
  - full invoice example
  - approved status text

Content that should remain outside the tabbed module:
- global point-validity block `Views/Member/Index.cshtml:77-82`
- threshold overview section `Views/Member/Index.cshtml:85-110`
- tier-progress rule section `Views/Member/Index.cshtml:272-298`
- birthday table `Views/Member/Index.cshtml:301-350`
- upgrade/downgrade rules `Views/Member/Index.cshtml:354-378`
- important terms `Views/Member/Index.cshtml:381-406`

## 10. Progress-Bar Recommendation
Personalized progress is not possible truthfully.

Confirmed source constraints:
- the member page is fully static; `Views/Member/Index.cshtml:1-430` contains no user state
- controller is static metadata only: `Controllers/MemberController.cs:5-15`
- no membership identity, current tier-progress points, current spend, or logged-in member source exists anywhere in the current member route

Recommendation:
- use a threshold-oriented informational progress treatment, not a personalized remaining-points claim
- the card may keep a decorative/static track to preserve the screenshot language
- the text under the track should be non-personalized, for example:
  - Bronze: next threshold toward Silver
  - Silver: next threshold toward Gold
  - Gold: next threshold toward Diamond
  - Diamond: highest tier / current top level

Do not implement fabricated messages such as:
- `Bạn cần 500 điểm để nâng cấp`
- `Bạn cần 1200 điểm để nâng cấp`
- `Bạn cần 4500 điểm để nâng cấp`

unless a real membership-progress data source is added in a separate backend task.

## 11. Recommended Interaction Architecture
Recommended model:
- accessible button tabs with minimal JavaScript

Why this is the safest option:
- keeps one selected panel visible at a time
- supports proper `role="tablist"`, `role="tab"`, `role="tabpanel"`
- allows arrow-key navigation and `aria-selected`
- allows scroll-into-view for the active pill on mobile
- avoids the fragility of CSS-only radios for long, dynamic Vietnamese content
- avoids route changes and server round-trips

Recommended structure:
- four buttons in a horizontal scroll container
- four tab panels rendered server-side in the DOM
- JS upgrades the component after load:
  - marks default selected tab
  - hides inactive panels
  - updates `aria-selected`, `tabindex`, and `hidden`
  - scrolls active tab into view on narrow screens

Fallback behavior:
- without JS, all panels may remain visible in document order
- with JS, only one panel is visible

Recommended default:
- Bronze selected by default
  - consistent with current content ordering `Views/Member/Index.cshtml:122-155`
  - matches the first screenshot

## 12. Responsive Specification

### 375px
- card width: about `92vw`, max centered
- card aspect ratio: about `1.85:1`
- selector: horizontal scroll, no wrap, hidden dominant scrollbar styling if feasible
- benefit rows: single column with fixed icon column and text wrapping
- selected panel remains one-column

### 390px
- same structure as 375px
- tab pills sized for comfortable thumb activation
- “Thành viên Kim Cương” must remain single-line through horizontal scroll, not wrap

### 430px
- same mobile model
- card may gain slightly more internal padding
- active tab should auto-scroll into view when switched

### 768px
- keep tab interaction
- component may widen, but should still feel like a centered premium card module, not a return to four summary columns
- benefit list can remain stacked under the card

### 1024px
- preserve tabs
- selected card may widen moderately
- recommended layout:
  - selected visual card on top
  - selector below
  - benefit list and invoice/status inside one selected panel
- do not revert to the old four-card desktop grid

### 1440px
- preserve a centered bounded component rather than stretching to the full editorial width
- the selected card can expand modestly, but the visual target remains a mobile-derived premium module

## 13. Accessibility Specification
Required future changes:
- add real tab semantics:
  - `role="tablist"`
  - `role="tab"`
  - `role="tabpanel"`
  - `aria-selected`
  - `aria-controls`
- keyboard support:
  - Left/Right arrow navigation
  - Enter/Space activation
  - visible focus on pills
- keep exactly one page `h1`
- keep benefit items as semantic lists, not only styled paragraphs
- hide decorative icons from screen readers
- ensure tier identity is not color-only:
  - text label always visible
- do not trap keyboard focus inside the horizontal scroller
- ensure hidden panels are actually hidden from screen readers when inactive
- retain reduced-motion compatibility with `uw-reveal.js`

## 14. Implementation File Map
Expected files to modify later:
- `Views/Member/Index.cshtml`
- `wwwroot/css/input.css`
- `wwwroot/css/site.css` via the official build command only
- `KIGHolding.Tests/MemberPageTests.cs`
- likely new JS file: `wwwroot/js/member-tier-tabs.js`
- relevant docs:
  - `docs/feature-state.md`
  - `docs/ui-architecture.md`
  - only if needed, `docs/architecture.md`

Expected files to keep untouched:
- `Controllers/MemberController.cs` unless script loading strategy requires a minimal page-only hook
- `Views/Reservation/Success.cshtml`
- `Views/Shared/_Layout.cshtml` unless there is no safer per-page script-loading path
- all R2/storage files
- all database/entity/migration files
- header/footer components

## 15. Regression Risk Register

| Risk | Severity | Evidence | Impact | Required mitigation |
| ---- | -------- | -------- | ------ | ------------------- |
| Approved content loss during merge | High | current invoice examples and status text live only inside the detailed tier cards `Views/Member/Index.cshtml:132-155`, `169-192`, `206-229`, `244-266` | redesign may simplify visuals but drop approved legal/business content | keep invoice example and note inside each tab panel; test for all approved strings |
| Shared button regression | High | shared button selectors are global `wwwroot/css/input.css:1756-1798`, used by reservation success `Views/Reservation/Success.cshtml:65-66` | changing them would alter another accepted page | keep button changes scoped under `.member-program` only |
| Generated CSS overwrite risk | Medium | `package.json:6` rewrites `wwwroot/css/site.css`; the file is already modified in the worktree | unscoped rebuilds can hide unrelated changes in review | edit `input.css` only, rebuild once, review member-only diff carefully |
| Hidden panel accessibility failure | High | no current tabs/ARIA exist | inactive content could remain focusable or invisible to AT in the wrong state | implement true tabs with `hidden`, `aria-selected`, roving tab stop or managed `tabindex` |
| Asset mismatch | High | only circular KIG logo exists `wwwroot/images/general/kig-no-bg-logo.png`; no screenshot-matching benefit icons or diamond texture found | implementation may be structurally correct but visually off | obtain owner-approved assets or document CSS-only approximations explicitly |
| Personalized progress misrepresentation | High | no member state source in `Controllers/MemberController.cs:5-15` or `Views/Member/Index.cshtml:1-430` | showing “Bạn cần X điểm...” would be false | use threshold-oriented static text only |
| No-JS usability gap | Medium | tab behavior will require JS | without fallback, only one tier could be inaccessible | render all panels server-side and let JS progressively hide inactive panels |

## 16. Automated Test Plan
Required future tests:
1. exactly four tier tabs exist
2. Bronze is selected by default
3. only one tier panel is visible after JS initialization
4. selecting Silver updates active card and benefits
5. selecting Gold updates active card and benefits
6. selecting Diamond updates active card and benefits
7. `aria-selected` updates correctly
8. keyboard Left/Right navigation works
9. Enter/Space activation works
10. active tab scrolls into view on narrow screens
11. all approved tier content remains present in the DOM data source
12. no screenshot-derived conflicting business values are introduced
13. exactly one `h1` still exists
14. birthday table remains present
15. invoice examples remain accessible per tier
16. reservation-success shared classes are unchanged
17. no horizontal overflow at 375px
18. component remains readable at 200% zoom
19. reduced-motion behavior remains acceptable
20. JS initialization is idempotent

## 17. Visual QA Checklist
Mobile:
- selected card visually matches the screenshot proportions
- card side margins match the mobile reference feel
- active tab pill is black with white text
- inactive pills are light gray with dark text
- tab row scrolls horizontally without wrapping labels
- only one tier benefit set is visible
- benefit icon column aligns consistently
- no text clipping in long Vietnamese rows
- no page-level horizontal overflow

Desktop:
- tabs remain interactive
- component stays centered and premium, not stretched into a generic four-column grid
- card remains landscape and visually dominant
- invoice example and status note remain visible for the selected tier

Cross-check:
- compare Bronze/Silver/Gold/Diamond cards against the supplied screenshots
- verify the birthday table and threshold section still render outside the tabbed module

## 18. Recommended Implementation Scope
Razor changes:
- replace the current rate grid and detailed tier grid with one merged tabbed module
- keep thresholds, birthday table, rules, terms, and CTA outside that module

CSS changes:
- add a new scoped tabbed tier namespace under `.member-program`
- remove or stop using only the current `member-program__rate-grid`, `member-program__rate-card`, `member-program__tier-grid`, `member-program__tier-card` presentation rules
- do not touch shared editorial buttons

JavaScript changes:
- add a small page-scoped tabs controller, likely `wwwroot/js/member-tier-tabs.js`
- initialize once
- manage tabs, panels, arrow keys, and scroll-into-view

Asset changes:
- likely required owner-provided logo/icon assets for high fidelity
- if not provided, implementation must document CSS-only/icon approximation limits

Tests:
- extend `KIGHolding.Tests/MemberPageTests.cs`
- add focused JS/component behavior tests if current test infrastructure allows

Documentation:
- update `docs/feature-state.md` and `docs/ui-architecture.md`
- optionally update `docs/architecture.md` only if the implementation introduces a new page-specific JS asset contract

Explicitly untouched areas:
- business rules/content values
- controller logic unless a tiny page hook is required
- R2/storage
- database schema
- header/footer layout
- phone/status bar chrome imitation

## 19. Explicit Non-Goals
- no business-rule changes
- no membership backend
- no database schema
- no personalized points logic without data
- no R2 changes
- no global header/footer redesign
- no recreation of phone status bars or Android chrome
- no copying of conflicting screenshot business copy

## 20. Verification Results
Commands run:
- `git status --short --branch` -> succeeded
- `git rev-parse --show-toplevel` -> succeeded
- `git rev-parse HEAD` -> succeeded
- `git log -5 --oneline --decorate` -> succeeded
- `git diff --name-status` -> succeeded
- `git diff --stat` -> succeeded
- `git diff --check` -> clean apart from line-ending warnings only
- `dotnet build KIGHolding.sln --no-restore` -> succeeded, `0` warnings, `0` errors
- `dotnet test KIGHolding.sln --no-restore -v normal` -> succeeded sequentially, `97 passed`, `1 skipped`

Important verification note:
- an initial parallel `dotnet test` run failed with a transient static-web-assets file lock because `dotnet build` and `dotnet test` were launched concurrently against the same `obj` cache
- the sequential rerun passed

Command intentionally not run:
- `npm run build:css`
  - reason: `package.json:6` rewrites tracked generated CSS `wwwroot/css/site.css`
  - this audit is read-only except for the report file
  - running the CSS build would have violated the scope boundary

## 21. Final Recommendation
Implementation may begin.

Recommended starting point:
1. merge the current four rate cards and four detailed tier cards into one tabbed module
2. preserve all approved content per tier inside tab panels
3. use threshold-oriented, non-personalized progress messaging
4. keep thresholds, birthday table, and rule sections outside the new tabs

Owner-provided assets still required for screenshot-level fidelity:
- metallic KIG logo mark matching the reference
- tier-benefit icon set matching the reference
- diamond/faceted texture asset, or explicit approval for a CSS-only approximation
