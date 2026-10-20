# System Architecture

This document outlines the system architecture, folder structure, and dependency injection services of the **Truyền Thuyết Champong** web application.

---

## 1. Technology Stack

The application is built on a modern, scalable web stack:
*   **Framework**: ASP.NET Core 10.0 (MVC)
*   **Database**: PostgreSQL
*   **ORM**: Entity Framework Core 10.0 (Npgsql.EntityFrameworkCore.PostgreSQL)
*   **CSS Utility Framework**: Tailwind CSS (compiled via `@tailwindcss/cli` via PostCSS)
*   **Email Dispatch**: Resend API Integration (via Resend .NET SDK)
*   **Image Storage**: Configurable provider architecture supporting `CloudflareR2` for new uploads, `LocalVolume` for legacy/local `/uploads` compatibility, and Cloudinary for legacy absolute URLs.

---

## 2. Directory Structure (Post-Cleanup)

The project has been cleaned of redundant legacy structures (such as `BookingMiniForm` and public review grids). The simplified directory structure is as follows:

```text
KIGHolding/
├── Areas/
│   └── Admin/
│       ├── Controllers/      # Admin portal controllers (Setting, MenuGroup, Branch, Post, Reservation, etc.)
│       ├── ViewModels/       # ViewModels for data transfer in Admin views
│       └── Views/            # Razor views for Admin dashboard management
├── Controllers/              # Public controllers (Home, About, Branch, Contact, Franchise, Member, Menu, News, Reservation, Seo)
├── Data/                     # EF Core DbContext, entity configurations, and DbInitializer
├── Migrations/               # PostgreSQL schema migrations
├── Models/
│   ├── Entities/             # C# classes mapped to DB tables (Branch, MenuGroup, MenuPageImage, Reservation, Post, etc.)
│   ├── Enums/                # Enums for statuses and reservation sources
│   └── PagedResult.cs        # Generic pagination helper
├── Options/                  # Options pattern settings classes (ResendSettings, CloudinarySettings, etc.)
├── Properties/               # launchSettings.json configurations
├── Services/                 # Business logic interfaces, clocks/policies (VietnamClock.cs, ReservationBlockedDateService.cs, IdentityNormalizer.cs) & cached implementations
├── ViewComponents/           # ViewComponents for reusable components (SiteHeader, SiteFooter, LayoutFallbacks, etc.)
├── ViewModels/               # ViewModels for public-facing forms and detail views
├── Views/                    # Razor Views for public routes (Home, Menu, News, Reservation, Branch, Contact, Shared)
└── wwwroot/                  # Client-side assets
    ├── css/                  # input.css (source) and site.css (Tailwind compiled output)
    ├── js/                   # Site scripts (site.js, uw-reveal.js, kbb-cook-slider.js, etc.)
    └── uploads/              # Legacy/local uploaded files served through /uploads when present
```

---

## 3. Active Dependency Injection Services

The services configured in `Program.cs` isolate database access and external API integrations from the controller actions:

| Service | Lifetime | Purpose |
| :--- | :--- | :--- |
| `AppDbContext` | Scoped | Entity Framework database context |
| `ISiteSettingService` | Scoped | Cached retrieval of global site settings (hotline, email, brand info) |
| `IMenuGroupService` | Scoped | Cached service to manage menu groups and flipbook menu pages |
| `IBranchService` | Scoped | Cached service for branch operations and visible customer reviews |
| `INewsService` | Scoped | Service for news categories, articles, and suggestions |
| `IBlogHtmlSanitizer` | Scoped | Project-owned boundary for sanitizing admin-authored HTML post content before persistence |
| `IReservationService` | Scoped | Logic for creating, listing, and transitioning reservations |
| `IReservationBlockedDateService` | Scoped | Authoritative DB-backed reservation closure-date policy and cleanup operations |
| `IBranchReservationDatePolicyService` | Scoped | Exact branch/date policy queries, validated optimistic mutations, impact counts, and cleanup |
| `IReservationPolicyEvaluator` | Scoped | Composes past-date, global, branch, and guest-count rules with global-first precedence |
| `IReservationPolicyLockService` | Scoped | Transaction-scoped PostgreSQL advisory locks shared by booking and policy writers |
| `IContactService` | Scoped | Logic for storing franchise and general contact messages |
| `IEmailService` | Scoped | Dispatches emails via Resend integration for notifications |
| `IImageStorageService` | Scoped | Orchestrates image uploads/deletes across Cloudflare R2, LocalVolume, and Cloudinary providers |
| `IImageStorageProvider` implementations | Scoped | Provider-specific upload/delete behavior for LocalVolume, Cloudinary, and Cloudflare R2 |
| `ICloudflareR2Client` | Singleton | S3-compatible Cloudflare R2 client wrapper; created lazily and used only for R2 operations |
| `IPasswordHasher<AdminUser>` | Scoped | Secure hashing using PBKDF2 for admin credentials |
| `AdminCookieAuthenticationEvents`| Scoped | Validates admin session cookie and security stamp consistency |
| `IMemoryCache` | Singleton | Bounded memory cache (`SizeLimit = 50_000` in `Program.cs`) for rate limit gating |

### Reservation Calendar Policy Boundary

`ReservationService.CreateReservationAsync` remains the booking authority. It performs an early global/branch policy check, preserves the existing operational validation, then opens an explicit PostgreSQL `READ COMMITTED` transaction and acquires the shared policy lock for the requested date. `IReservationPolicyEvaluator` re-reads current database state after that lock with precedence `PastDate -> GlobalBlockedDate -> InvalidBranch -> InvalidGuestCount -> BranchClosedDate -> MinimumGuestsNotMet -> Allowed`. Only an allowed result may proceed to the existing phone lock and reservation persistence.

`BlockedReservationDates` remains the global policy and always wins. `BranchReservationDatePolicies` adds one optional rule per `(BranchId, Date)`: `Closed` or `MinimumGuests`; absence means Allowed at the branch-policy layer. Saturday and Sunday are allowed by default, and there is no holiday, weekday, or upper-year inference.

The policy lock uses PostgreSQL's two-integer transaction advisory-lock form. A fixed policy namespace is paired with `DateOnly.DayNumber`, so every booking, global-policy mutation, and branch-policy mutation for the same date coordinates on the same stable key. Global full-set reconciliation first holds a separate global-mutation lock (`1263092563`, key `1`), determines affected dates, and acquires every date lock in ascending order before writing. Booking always takes the date lock before the existing phone advisory lock. This source-level guarantee requires all running instances to use the new protocol; a mixed-version rolling deployment temporarily retains the old-instance bypass risk, so the migration and coordinated application rollout must be planned together.

The public full reservation page and quick booking modal consume the existing global calendar payload for immediate date-only feedback and the read-only `GET /dat-ban/policy` endpoint for the effective `BranchId + ReservationDate + GuestCount` decision. The endpoint delegates to `IReservationPolicyEvaluator`; it does not duplicate policy rules or create reservations. `wwwroot/js/reservation-policy-client.js` is shared by both forms, aborts superseded requests, verifies the current input tuple before rendering a response, and fails closed when verification cannot be completed. An `Allowed` response means only that the policy layer permits the request; it is not a table-availability or booking guarantee.

The Admin policy area keeps global and branch mutations separate. `/Admin/Reservation/Policy` retains the existing global full-future-set calendar, while `/Admin/Reservation/BranchPolicy` provides an active/reservable branch selector and single-date `Allowed` (row removal), `Closed`, or `MinimumGuests` mutation. Its monthly calendar overlays inherited global blocks without deleting the dormant branch rule. Branch policy mutation contracts use an expected UUID `Version` that identifies one persisted policy state: every create and successful update generates a new non-empty UUID, including after delete/recreate, so stale editors receive `Conflict` and an explicit reload action. The composite identity remains `(BranchId, Date)`.

Restrictive Admin mutations query current reservation counts by status. When any exist, a Data Protection-signed confirmation snapshot binds branch, date, proposed rule, expected UUID revision, and the authoritative count fingerprint. The server re-queries counts before accepting confirmation; changed impact requires review again. The operation never edits existing reservations. Daily hosted cleanup removes both global and branch rows where `Date < VietnamToday`; correctness never depends on cleanup because past dates are independently rejected.

Phase 1 backend behavior completed the disposable PostgreSQL gate (12 passed, including locks, `READ COMMITTED`, constraints, UUID/ABA behavior, and rollback recovery), and Phase 2 Admin owner QA was approved. Phase 3 public reactive validation is implemented for owner QA, while `ReservationService.CreateReservationAsync` remains the final transaction-time authority for JavaScript-disabled, crafted, stale, or racing submissions. Migration `20261019052426_AddBranchReservationDatePolicies` remains unapplied to staging and production. Migration-first deployment is mandatory, mixed old/new instances must not serve policy-sensitive traffic, and neither staging migration nor production deployment is authorized.

### Blog HTML and SEO

The Blog/News model uses one canonical `Post.Content` column and a `PostContentMode` enum. Visual posts store plain text and continue to render through the encoded paragraph path. HTML posts store only server-sanitized HTML after `IBlogHtmlSanitizer` removes unsupported tags, attributes, and unsafe URL schemes. Public raw rendering is allowed only in the `ContentMode=Html` branch of `Views/News/Detail.cshtml`; it must never be used for Visual content.

Admin Create/Edit uses shared content and SEO partials under `Areas/Admin/Views/Post/`. `wwwroot/js/admin-post-editor.js` owns the Visual/HTML tab state and accessibility behavior only; server-side validation and sanitization remain authoritative.

SEO metadata is persisted on `Post` and rendered through the shared layout: `SeoTitle`, `SeoDescription`, `CanonicalUrl`, `OgTitle`, `OgDescription`, `OgImageUrl`, `RobotsIndex`, and `RobotsFollow`. Article detail also emits JSON-LD Article metadata. `FocusKeyword` is an editor aid, not a meta-keywords output field.

---

## 4. Key Infrastructure Features

*   **Cookie Authentication**: Session cookies are configured with `SlidingExpiration = true` and `ExpireTimeSpan = TimeSpan.FromHours(8)` using custom event validation.
*   **Data Protection**: Persists cryptography keys to a secure local folder: `App_Data/DataProtectionKeys/` to ensure persistent login states across application recycling.
*   **Response Compression**: Optimized for SVG/XML formats to enhance loading performance.
*   **Admin Reservation Real-Time Notifications**: ASP.NET Core SignalR is registered through the shared framework and exposes the authenticated Hub endpoint `/admin/hubs/reservations`. `IAdminReservationNotifier` is the application boundary used by reservation creation logic, with `SignalRAdminReservationNotifier` adapting that boundary to `AdminReservationNotificationHub`. The browser client is vendored locally at `wwwroot/lib/microsoft-signalr/signalr.min.js` through the `vendor:signalr` npm script, and that file plus `wwwroot/audio/Hem366_remix_bolero_vui_loa.mp3` are deployment source assets under `wwwroot` that ASP.NET Core publish includes automatically. The Admin script keeps automatic reconnect enabled and restarts the guarded manual start loop after a terminal SignalR close. Phase 1 is single-instance and best-effort; horizontally scaled deployments require a future SignalR backplane or equivalent fan-out.

---

## 5. Image Storage Architecture

`IImageStorageService` remains the controller-facing contract:

```csharp
Task<string> UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default);
Task<string> UploadAsync(IFormFile file, ImageCategory category, string storageScope, CancellationToken cancellationToken = default);
Task DeleteAsync(string? imageUrlOrPath, ImageCategory category, CancellationToken cancellationToken = default);
```

The service selects the active upload provider from `ImageStorage:Provider` and routes deletes by stored-reference ownership. Supported providers are:

* `CloudflareR2`: uploads through the S3-compatible API and returns absolute public URLs.
* `LocalVolume`: writes to `ImageStorage:RootPath` and returns relative `/uploads/...` URLs.
* `Cloudinary`: retained for legacy compatibility and optional future activation.

Cloudflare R2 object keys are immutable and unique. Category-to-prefix mapping:

* `ImageCategory.Branches` -> `ImageStorage:BranchesPrefix`
* `ImageCategory.Posts` -> `ImageStorage:NewsPrefix`
* `ImageCategory.MenuPages` -> `ImageStorage:MenuPagesPrefix`, plus persisted `MenuGroup.Slug` for new menu-page uploads
* `ImageCategory.MenuGroupCovers` -> `ImageStorage:BrandsPrefix`

New menu-page R2 objects use group-scoped prefixes:

```text
menu-pages/truyen-thuyet-champong/<safe-unique-filename>
menu-pages/gogi-maru/<safe-unique-filename>
menu-pages/kbb-cook/<safe-unique-filename>
```

The group folder is an R2 object-key prefix, not a physical directory. The segment is validated as one safe slug segment and is never derived from `MenuGroup.Name` or client-supplied data. Existing root-level `menu-pages/<filename>` objects remain valid and deletable. Changing a menu group slug affects only future uploads; existing object keys and database URLs are not moved or rewritten.

Existing `/uploads/...`, `/images/...`, `/favicon.ico`, Cloudinary URLs, and unmanaged external URLs remain readable. Delete operations are conservative: R2 deletes are allowed only for configured public URLs or safe object keys under the expected category prefix; `/uploads/...` is routed to LocalVolume; recognized Cloudinary URLs are routed to Cloudinary. Full managed-asset ownership tracking, shared-reference protection, legacy asset migration, and decoder/magic-byte validation remain future hardening items.

---

## 6. Static Membership Page Contract

`MemberController` serves the public route `/thanh-vien` as a static informational page describing the approved **Truyền Thuyết Champong** membership program. The page is intentionally content-driven and does not implement:

* member authentication
* account dashboards
* points calculations
* redemption workflows
* membership database tables

The route uses controller-provided SEO metadata (`Title`, `MetaDescription`, `CanonicalUrl`) and a Razor view backed only by static content and CSS. Any future change to approved membership tiers, thresholds, point rates, invoice examples, birthday benefits, upgrade/downgrade rules, or point-validity rules must update the page view, its tests, and the synchronized documentation in the same change.
