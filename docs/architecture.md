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
├── Services/                 # Business logic interfaces, evaluations (VietnamHolidayEvaluator.cs, IdentityNormalizer.cs) & cached implementations (MenuGroupService, BranchService, etc.)
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
| `IReservationService` | Scoped | Logic for creating, listing, and transitioning reservations |
| `IContactService` | Scoped | Logic for storing franchise and general contact messages |
| `IEmailService` | Scoped | Dispatches emails via Resend integration for notifications |
| `IImageStorageService` | Scoped | Orchestrates image uploads/deletes across Cloudflare R2, LocalVolume, and Cloudinary providers |
| `IImageStorageProvider` implementations | Scoped | Provider-specific upload/delete behavior for LocalVolume, Cloudinary, and Cloudflare R2 |
| `ICloudflareR2Client` | Singleton | S3-compatible Cloudflare R2 client wrapper; created lazily and used only for R2 operations |
| `IPasswordHasher<AdminUser>` | Scoped | Secure hashing using PBKDF2 for admin credentials |
| `AdminCookieAuthenticationEvents`| Scoped | Validates admin session cookie and security stamp consistency |
| `IMemoryCache` | Singleton | Bounded memory cache (`SizeLimit = 50_000` in `Program.cs`) for rate limit gating |

---

## 4. Key Infrastructure Features

*   **Cookie Authentication**: Session cookies are configured with `SlidingExpiration = true` and `ExpireTimeSpan = TimeSpan.FromHours(8)` using custom event validation.
*   **Data Protection**: Persists cryptography keys to a secure local folder: `App_Data/DataProtectionKeys/` to ensure persistent login states across application recycling.
*   **Response Compression**: Optimized for SVG/XML formats to enhance loading performance.

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
