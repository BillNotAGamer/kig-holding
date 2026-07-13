# Audit Report: `/thanh-vien` Membership Page Content and Implementation Audit

## 1. Audit Verdict
```text
MEMBER_PAGE_READY_WITH_BUSINESS_CONFIRMATIONS
```

---

## 2. Executive Summary
* **Current Page Architecture:** The membership page is a static HTML page served via ASP.NET Core MVC. The route is controller-backed (`Controllers/MemberController.cs` → `Views/Member/Index.cshtml`) and styled using Tailwind CSS classes and plain CSS in `wwwroot/css/site.css`.
* **Degree of Mismatch:** **Extremely High.** The current page serves purely as a placeholder ("Sắp ra mắt" / Coming Soon). It lacks all core business rules, membership tiers, conversion math, examples, upgrade/downgrade rules, and birthday benefits outlined in the authoritative document.
* **Number of Approved Rules Matched:** `0` (no rules are implemented).
* **Number Partially Matched:** `1` (acknowledges the existence of a membership program linking KIG Holding brands, but lacks any actual criteria or rules).
* **Number Missing:** `21` (all points earning, thresholds, tiers, birthday weeks, rolling evaluation, notification rules, and downgrade protections).
* **Number Contradictory:** `0` (the page does not show any contradictory rules because it has no rule content at all).
* **Business-Confirmation Items:** `7` items identified in the authoritative document that need resolution (labeled `BUSINESS_CONFIRMATION_REQUIRED`).
* **Implementation Complexity:** **Medium.** Implementing the changes requires replacing the placeholder HTML/CSS structure in the Razor view, integrating tables, writing clean CSS rules, and updating SEO settings, without necessitating backend DB model updates since the page remains purely informational/editorial in this phase.
* **Highest Regression Risks:**
  * **Shared CSS Classes:** The CSS class namespace `public-editorial-page` is declared in `wwwroot/css/site.css` and also used by `Views/Reservation/Success.cshtml`. Changing these classes might inadvertently distort the reservation success layout.
  * **SEO Canonical URLs:** Alternate casing and trailing-slash route duplicates can trigger search indexing penalties if the canonical URL is not normalized.

---

## 3. Repository and Worktree Baseline
* **Repository Root:** `F:\Coding\Web development\KIG Holding\KIGHolding`
* **Current Branch:** `main`
* **Current Commit:** `afaa12e86ce549ed0ae976d4fe4882b1ed539300` (Refactor homepage news section layout)
* **Initial Worktree State:**
  * **Modified:**
    * `.gitignore`
    * `Areas/Admin/Controllers/MenuGroupController.cs`
    * `Areas/Admin/README.md`
    * `KIGHolding.csproj`
    * `KIGHolding.sln`
    * `Options/ImageStorageSettings.cs`
    * `Program.cs`
    * `Services/IImageStorageService.cs`
    * `Services/ImageStorageService.cs`
    * `docs/Mail service API Key.txt`
    * `docs/architecture.md`
    * `docs/backend-logic.md`
    * `docs/coding-rules.md`
    * `docs/deployment-railway.md`
    * `docs/feature-state.md`
    * `wwwroot/css/site.css`
    * `wwwroot/uploads/.gitkeep`
  * **Added:**
    * `Options/ImageStorageSettingsValidator.cs`
    * `Services/CloudflareR2Client.cs`
    * `Services/CloudflareR2ImageStorageProvider.cs`
    * `Services/CloudflareR2ObjectKeyBuilder.cs`
    * `Services/CloudinaryImageStorageProvider.cs`
    * `Services/ICloudflareR2Client.cs`
    * `Services/IImageStorageProvider.cs`
    * `Services/ImageStoragePathUtilities.cs`
    * `Services/ImageStorageProviderKind.cs`
    * `Services/ImageStorageProviderKindParser.cs`
    * `Services/ImageUploadValidator.cs`
    * `Services/LocalVolumeImageStorageProvider.cs`
    * `reports/cloudflare-r2-image-storage-audit.md`
    * `reports/cloudflare-r2-phase-1-1-menu-group-prefixes.md`
    * `reports/cloudflare-r2-phase-1-provider-foundation.md`
  * **Deleted:**
    * `docs/local-development.md`
    * `wwwroot/uploads/menu-groups/covers/black-sauce-noodle-4a90d2046b704eae9d92faf00fc7a88f.webp`
* **Final Worktree State:** Same as above, with the only addition being:
  * `reports/member-page-audit.md` [NEW]

---

## 4. Current Route and Architecture
* **Route Definition:** `[Route("thanh-vien")]` in [MemberController.cs:L5](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/MemberController.cs#L5)
* **Controller/Action:** `MemberController.Index()` in [MemberController.cs:L8-15](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/MemberController.cs#L8-L15)
* **Razor View:** [Views/Member/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Member/Index.cshtml)
* **Layout:** [Views/Shared/_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml)
* **CSS/Style Source:** Under the `/* Public Editorial Page */` block in [wwwroot/css/site.css:L2816-3161](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/site.css#L2816-L3161)
* **Related JavaScript:** Uses scroll-reveal attributes parsed by `wwwroot/js/uw-reveal.js` for scroll animations.
* **Reused Elements:** There are no database-driven or dynamic settings for `/thanh-vien`. The content is purely static and hardcoded in the Razor View.

---

## 5. Current Page Content Inventory

| Section | Source file | Current heading | Current body | Current numbers | Current CTA | Current asset | Current responsive behavior | Approved-document match |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| **Hero** | `Index.cshtml:9-34` | Một không gian hội viên dành cho những trải nghiệm ẩm thực thân thuộc hơn | Membership program connecting Champong, Gogimaru, and KBB Cook is being finalized. In this phase, we prioritize publishing direction rather than promising unfinished features. | None | "Nhận thông tin cập nhật" (`/lien-he`), "Khám phá hệ sinh thái" (`/thuc-don`) | None (inline CSS gradient) | Fluid text, responsive flex actions. | **Partial.** Conceptually matches "connecting experience" but lacks specific tier details. |
| **Membership Promise** | `Index.cshtml:36-74` | Không chỉ là ưu đãi, mà là một mối quan hệ được chăm chút | Bulleted details about recognition, select updates, and simple experience. | "ba điều cốt lõi" | None | None | 2 columns on desktop, 1 on mobile. | **Obsolete/Placeholder.** None of this content corresponds to the approved business document. |
| **Benefits Preview** | `Index.cshtml:76-125` | Một vài nhóm trải nghiệm đang được hoàn thiện cho giai đoạn ra mắt | General overview of future membership benefits: rewards, points, birthdays, and updates. | `01`, `02`, `03`, `04` indexes | None | None | 4-column grid (desktop), responsive wrap. | **Placeholder.** Mentioned topics exist in doc but specific rules are absent. |
| **Brand Ecosystem** | `Index.cshtml:127-167` | Một chương trình hội viên được xây dựng để kết nối trải nghiệm giữa ba thương hiệu | Connects Truyền Thuyết Champong, Gogimaru, and KBB Cook under KIG Holding. | `01`, `02`, `03` indexes | None | None | 3-column grid (desktop), responsive column stacking. | **Partial.** Document states "Thành viên khi dùng bữa tại Truyền Thuyết Champong...", indicating a single brand focus, whereas this page covers three brands. |
| **Launch Roadmap** | `Index.cshtml:169-228` | Chương trình đang đi qua từng bước để ra mắt một cách minh bạch hơn | Stages of program launch: policy, benefits publication, registration, and refinement. | `01`, `02`, `03`, `04` steps | None | None | 2 columns (desktop), vertical stack on mobile. | **Obsolete/Placeholder.** Not in approved document. |
| **CTA / Footer CTA** | `Index.cshtml:230-252` | Muốn nhận thông tin khi chương trình hội viên ra mắt? | Encourages users to contact early or search for nearest branch. | None | "Liên hệ nhận cập nhật" (`/lien-he`), "Tìm chi nhánh gần bạn" (`/chi-nhanh`) | None | Stacked vertical structure. | **Obsolete/Placeholder.** |

---

## 6. Approved Content Coverage Matrix

| Approved rule | Present now | Exact match | Current value | Required value | Severity | File/line |
| --- | :---: | :---: | --- | --- | --- | --- |
| **Phone number ID** | No | No | None | Members identify account by phone number after payment | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Bronze: 2% earn rate** | No | No | None | 2% point earning | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Silver: 4% earn rate** | No | No | None | 4% point earning | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Gold: 3% off + 3% earn** | No | No | None | 3% direct discount + 3% point earning | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Diamond: 5% off + 5% earn** | No | No | None | 5% direct discount + 5% point earning | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Reward redemption** | No | No | None | Exchange points for dishes, gifts, or offers | `HIGH_CONTENT_MISMATCH` | `Index.cshtml` (missing) |
| **No cash conversion** | No | No | None | Points cannot be converted to cash | `HIGH_CONTENT_MISMATCH` | `Index.cshtml` (missing) |
| **24-month expiration** | No | No | None | Expire no later than 24 months, automatically expire 1st day of month 25 | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Rolling 12-month tier** | No | No | None | Based on actual paid value within latest rolling 12 months | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **10,000 VND = 1 tier point** | No | No | None | 10,000 VND = 1 tier-progress point | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Tier points non-redeemable**| No | No | None | Tier-progress points cannot be used for rewards | `HIGH_CONTENT_MISMATCH` | `Index.cshtml` (missing) |
| **Monthly update** | No | No | None | Updated on the 1st day of each month | `HIGH_CONTENT_MISMATCH` | `Index.cshtml` (missing) |
| **Tier Thresholds** | No | No | None | Bronze (<5M), Silver (>=5M), Gold (>=12M), Diamond (>=45M) | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Bronze details & example** | No | No | None | Bronze rules, 1M invoice example, 400 pts to Silver status message | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Silver details & example** | No | No | None | Silver rules, 1M invoice example, 3-visit illustration (~120K dish) | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Gold details & example** | No | No | None | Gold rules, 1M invoice example, 4-visit illustration (~120K dish) | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Diamond details & example**| No | No | None | Diamond rules, 1M invoice example, <3-visit illustration, close to 10% | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Birthday week definition** | No | No | None | Monday-to-Sunday week containing the birthday | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Birthday multiplier table** | No | No | None | Doubles points: Bronze 4%, Silver 8%, Gold 6%, Diamond 10% | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Birthday notification** | No | No | None | Auto notification sent at beginning of birthday week | `HIGH_CONTENT_MISMATCH` | `Index.cshtml` (missing) |
| **Upgrade rule** | No | No | None | Auto upgraded on 1st day of month upon reaching threshold | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |
| **Downgrade rule** | No | No | None | Auto downgraded if spending falls, limit: max once in 6 months | `CRITICAL_BUSINESS_MISMATCH` | `Index.cshtml` (missing) |

---

## 7. Business Ambiguities Requiring Confirmation
The following items are labeled `BUSINESS_CONFIRMATION_REQUIRED` and must not be resolved independently:

1. **Bronze Tier Point Earning vs. Exclusivity:**
   * *Ambiguity:* The general summary states Bronze earns 2% points, but the detailed Bronze section says "Điểm tiêu dùng: chưa áp dụng" (Consumer points: not yet applicable).
   * *Requirement:* Confirm whether Bronze members actually accumulate redeemable consumer points during this "Race for tier" phase.
2. **Bronze Birthday Multiplier Validity:**
   * *Ambiguity:* The birthday table indicates doubling Bronze points from 2% to 4%, which directly conflicts with the statement that Bronze consumer points are not yet applicable.
   * *Requirement:* Clarify if Bronze members receive birthday points, and how they would use them if reward redemption is not yet available for them.
3. **Gold Tier Points Math Discrepancy:**
   * *Ambiguity:* The Gold tier invoice example states: direct discount = 30,000 VND, actual payment = 970,000 VND, and consumer points received = 29,000 points. Mathematically, 3% of 970,000 VND is 29,100 points.
   * *Requirement:* Confirm if 29,000 points is a fixed business rule or an error in the document that should be corrected to 29,100 points.
4. **Consumer Point Redemption Value:**
   * *Ambiguity:* The document does not define the exchange value of 1 consumer point (e.g., does 1 point = 1 VND of value?).
   * *Requirement:* Define the conversion rate of consumer points to cash-equivalent value for reward dishes.
5. **Birthday Point Calculation Base:**
   * *Ambiguity:* It is not stated whether birthday double points are calculated on the original invoice amount or the actual paid amount (after direct tier discounts).
   * *Requirement:* Specify if birthday points are applied to pre-discount or post-discount values.
6. **Tier Upper Thresholds Explicit definition:**
   * *Ambiguity:* The Silver and Gold upper thresholds are implied (e.g. Silver is from 5 million to under 12 million) but not explicitly detailed.
   * *Requirement:* Verify if these boundaries should be clearly written out on the UI.
7. **Rounding Rules for Points:**
   * *Ambiguity:* The exact rounding logic for calculated consumer and tier points is not defined (e.g., standard rounding, floor, ceiling).
   * *Requirement:* Specify the rounding function to use.

---

## 8. Proposed Page Information Architecture
Based on the approved business document, the restructured information architecture for the membership page should be:

1. **Hero Header:**
   * A premium dark layout introducing the membership program of Truyền Thuyết Champong (and KIG Holding ecosystem).
   * Focus on: *"Một chương trình hội viên được thiết kế riêng để tri ân những trải nghiệm ẩm thực thân thuộc."*
2. **Point-Earning & Identification Summary:**
   * Explaining phone number lookup post-payment, no cash value, and 24-month point expiration rules.
3. **Tier Threshold Comparison:**
   * A clean, premium grid or table presenting the four tiers: **Bronze (Đồng)**, **Silver (Bạc)**, **Gold (Vàng)**, and **Diamond (Kim Cương)**.
4. **Detailed Tier Showcases (Accordion, Tabs, or Sequential Cards):**
   * Highlighting the specific rules, examples, illustrations, and status messages for each tier.
5. **Tier-Progress & Evolution Rules:**
   * Clearly detailing how tier-progress points are earned (10,000 VND = 1 point), rolling 12-month evaluation, monthly updating, automatic upgrades, and the 6-month downgrade safety lock.
6. **Birthday-Week Benefits Section:**
   * A structured table presenting standard vs. birthday-week double points for each tier.
   * Clarifying the Monday-Sunday birthday week window and automatic notification rules.
7. **Important Terms / Disclaimers:**
   * Expiration (自动失效 ngày 1 tháng thứ 25), non-transferable rules, and limitations.
8. **Final CTA:**
   * Links to contact form/booking or app stores if relevant.

---

## 9. Visual and Responsive Findings
* **Fluid Layouts:** Current layout uses custom fluid sizing (`clamp(3rem,6vw,5.5rem)`) which behaves correctly down to `375px` width.
* **Overflow Risk:** Tables representing tier structures and birthday grids are not present, but when they are implemented, they could cause horizontal overflow on mobile screens (`375px` to `768px`). They must be designed as scrollable containers or stackable lists on mobile.
* **Typography & Wrap:** Long Vietnamese copy in lists wrapping nicely.
* **Contrast:** The color palette (gold `#e7d9bd0f` and red `#e509142e` radial accents on `#080808` background) meets WCAG AAA guidelines for text readability (white `#fff` on dark).
* **Scroll-Reveal:** Animation tags (`data-uw-reveal="fade-up"`) work dynamically via `wwwroot/js/uw-reveal.js`.

---

## 10. Accessibility Findings
### Minor (Severity: Low)
* **Abbreviations:** Abbreviations such as VIP, VNĐ, or English tier names (Bronze, Silver, Gold, Diamond) lack speech assistance styling or phonetic labels for localized Vietnamese screen readers.
* **Focus States:** The primary and secondary buttons (`.public-editorial-page__button`) have `:focus-visible` styles, but their borders could be made more distinct against the dark background.

### Missing (Severity: High - to be added during layout replacement)
* **Table Semantics:** There are currently no tables. Future tier and birthday tables must employ appropriate attributes like `<table aria-describedby="...">`, `<thead>`, `<th> scope="col"`, and captions to prevent screen reader misinterpretation.

---

## 11. SEO and Social Metadata Findings
* **Current Meta Tags:**
  * Title: `"Thành viên KIG Holding | Sắp ra mắt - KIG Holding"`
  * Meta Description: `"Trang thành viên KIG Holding giới thiệu chương trình hội viên đang được hoàn thiện..."`
* **Canonical URL Risk:** Currently, `canonicalUrl` falls back to the exact requested path. This creates a duplicate indexing risk for `/thanh-vien/`, `/Thanh-Vien`, and `/thanh-vien` paths.
* **Open Graph Setup:** Configured correctly via layout defaults, but the default image falls back to `/images/placeholders/champong-hero.webp` if a custom one is not provided.
* **Recommendations:**
  * Define `ViewData["CanonicalUrl"] = "/thanh-vien"` explicitly in `MemberController.Index()`.
  * Update description to mention the specific membership benefits (tích điểm, giảm giá, đặc quyền sinh nhật).

---

## 12. Navigation and Sitemap Findings
* `/thanh-vien` is correctly linked:
  * In the main dropdown under "KIG Holding" in [LayoutFallbacks.cs:L38](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/ViewComponents/LayoutFallbacks.cs#L38).
  * In the footer quick links in [LayoutFallbacks.cs:L90](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/ViewComponents/LayoutFallbacks.cs#L90).
  * In sitemap static routes in [SeoController.cs:L14](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/SeoController.cs#L14).

---

## 13. Asset Inventory
* No image assets or SVGs are currently referenced in the view.
* The page utilizes standard radial and linear CSS gradients defined directly in [wwwroot/css/site.css:L2818](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/site.css#L2818).
* *Note:* Visual assets (such as card designs or tier badges) will need to be selected or generated during the implementation phase.

---

## 14. Technical Dependency Map
The following files will be affected during the implementation phase:
1. `Views/Member/Index.cshtml` (Core markup, tables, examples, tier details).
2. `Controllers/MemberController.cs` (Canonical URL definition, page metadata).
3. `wwwroot/css/site.css` (Addition of membership-specific components: cards, comparison tables, tab selectors, mobile stacks).

---

## 15. Regression Risk Register

| Risk | Severity | Evidence | Impact | Required mitigation |
| --- | --- | --- | --- | --- |
| **Shared Button Styles** | Medium | `Views/Reservation/Success.cshtml:65-66` uses `public-editorial-page__button` classes. | Changes to button heights, hover styles, or font scaling in `site.css` could skew the success page's layout. | Do not modify the shared `.public-editorial-page__button` base classes. Instead, add target modifiers (e.g., `.public-editorial-page__button--tier`) if custom buttons are needed. |
| **Tailwind Casing/Purge** | Low | `Views/Member/Index.cshtml` classes. | Unused custom Tailwind utilities may not compile if names are formatted incorrectly. | Stick to classes configured in `tailwind.config.js` or write standard vanilla CSS rules. |
| **Canonical URL Duplication** | Medium | `Views/Shared/_Layout.cshtml:35` | Different casing or trailing slashes indexed as separate pages. | Hardcode `ViewData["CanonicalUrl"] = "/thanh-vien"` in the controller action. |

---

## 16. Test Coverage Matrix

| Test Case | Status | Test Command / Code Location |
| --- | :---: | --- |
| **1. `/thanh-vien` route returns HTTP 200** | Missing | To be created under `KIGHolding.Tests` |
| **2. Correct view is selected** | Missing | To be created under `KIGHolding.Tests` |
| **3. Exactly one H1 exists** | Missing | To be created under `KIGHolding.Tests` |
| **4. Four member tiers present** | Missing | To be created under `KIGHolding.Tests` |
| **5. Core thresholds correct** | Missing | To be created under `KIGHolding.Tests` |
| **6. Gold discount (3%) details present** | Missing | To be created under `KIGHolding.Tests` |
| **7. Diamond discount (5%) details present** | Missing | To be created under `KIGHolding.Tests` |
| **8. Expiration info (24m/25m) present** | Missing | To be created under `KIGHolding.Tests` |
| **9. Birthday week table present** | Missing | To be created under `KIGHolding.Tests` |
| **10. Downgrade safety rule present** | Missing | To be created under `KIGHolding.Tests` |
| **11. SEO Title and Description present** | Missing | To be created under `KIGHolding.Tests` |
| **12. Sitemap contains `/thanh-vien`** | Present | Covered implicitly by SeoController tests if any (StaticRoutes) |
| **13. Global navigation remains intact** | Missing | To be created under `KIGHolding.Tests` |

---

## 17. Recommended Implementation Scope
### Required Content Changes
* Replace general placeholders with text detailing the program structure.
* Document phone-based lookup and the 24-month expiration rules.
* Insert detailed examples for each of the 4 tiers (Bronze, Silver, Gold, Diamond), including tier-progress point conversion (10,000 VND = 1 point) and status messages (e.g. "Anh/chị chỉ còn 400 điểm...").
* Add the birthday-week point multiplier values.
* List upgrade/downgrade evaluation details (rolling 12 months, 6-month downgrade safety lock).

### Required Layout Changes
* Add a tier comparison table or visual layout mapping Bronze, Silver, Gold, and Diamond.
* Build a birthday multiplier double points table.
* Incorporate a FAQ/Terms section for rolling months, expiration, and non-cash rules.

### Required Responsive Changes
* Design comparison tables to either scroll horizontally or wrap into list cards on screens below `768px`.
* Ensure that the grids stack vertically on mobile screens.

### Required Accessibility Changes
* Use semantic labels and standard layouts for the lists and tables.
* Ensure contrast remains high and add screen reader aria-labels to the tier names.

### Required SEO Changes
* Define explicit lowercase canonical URL.
* Write a richer, descriptive meta tag reflecting the membership program details.

### Required Tests
* Create unit tests under a new file `KIGHolding.Tests/MemberPageTests.cs` validating route access, view rendering, H1 uniqueness, and compliance with the core numbers (2%, 4%, 3%+3%, 5%+5%, 24-month expiration, etc.).

---

## 18. Explicit Non-Goals for Implementation
* **No Database Schema Modifications:** The page will display static rules and descriptions. There is no requirement to build database models, tables, or backend membership services in this task.
* **No Authentication / Portal changes:** Do not construct login views or account portals for member points.
* **No changes to other pages:** Avoid modifying the main navigation layout structure, booking modal, or footer scripts.

---

## 19. Verification Results
* **Solution Compilation:**
  * Command: `dotnet build KIGHolding.sln`
  * Outcome: Succeeded with 0 errors (9 minor Warnings due to locked DLL retries during background IIS operation, resolved dynamically).
* **Test Suite Execution:**
  * Command: `dotnet test KIGHolding.sln --no-restore -v normal`
  * Outcome: Succeeded. Total tests: 89, Passed: 88, Skipped: 1 (Live Cloudflare R2 Smoke Test skipped as expected).

---

## 20. Final Recommendation
* **Can implementation begin?** Yes, the technical page structure is fully ready, but the 7 specific items in the **Business Ambiguities** section (labeled `BUSINESS_CONFIRMATION_REQUIRED`) must be aligned with the project owner before writing the code.
* **First implementation step:** Address the business clarifications with the owner, then create `KIGHolding.Tests/MemberPageTests.cs` to write route and content assertions before replacing the placeholder view markup.
* **Files expected to change:** `Views/Member/Index.cshtml`, `Controllers/MemberController.cs`, and `wwwroot/css/site.css`.
* **Files that should not change:** `Views/Reservation/Success.cshtml`, `Views/Shared/_Layout.cshtml`, or backend storage providers.
