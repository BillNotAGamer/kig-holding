# 00 Executive Summary

## Project Context
* **Project Name**: KIGHolding / Truyền Thuyết Champong
* **Business/Domain Purpose**: A high-end, premium Korean culinary brand platform representing several restaurant concepts under KIG Holding: **Truyền Thuyết Champong** (signature hand-made seafood noodle bowls), **Gogimaru** (15-day dry-aged premium BBQ), and **KBB Cook** (modern table grilling). The public site delivers an atmospheric, editorial dark dining aesthetic, while the admin area provides operational CRUD controls.
* **Main Tech Stack**: 
  * ASP.NET Core 10.0 MVC
  * C# 13
  * Entity Framework Core 10.0.6
  * PostgreSQL (via `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.1)
  * Tailwind CSS (using Tailwind CLI 3.4.19 and CLI 4.3.0 in PostCSS config)
  * Resend for transactional email notifications (v0.5.1)
  * Custom Vanilla JavaScript (progressive enhancement model)
* **Database Provider**: PostgreSQL (remotely hosted on a Neon database instance).
* **Authentication/Authorization**: Cookie-based authentication (`KIGHolding.AdminAuth`) configured in [Program.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Program.cs#L17-L27) with an 8-hour sliding expiration window redirecting to `/Admin/Auth/Login`.
* **Current Maturity Level**: **MVP (Minimum Viable Product)**. The core routing, layout systems, data-access layers, email templates, and admin managers are fully functional and build cleanly. However, the site contains placeholder copy and assets, and requires a production database content seed before official rollout.

---

## Top 10 Things a New Contributor Must Know

1. **Separation of Themes**: Public-facing pages strictly use a custom **Dark Premium** theme (`bg-brand-black` / `#0B0B0B` base), whereas all admin boundary pages (`/Admin/*`) use a highly readable, clean **Light Theme** (`bg-brand-light` / `#FAF8F3` base).
2. **PostgreSQL UTC offset rule**: PostgreSQL `timestamp with time zone` (timestamptz) columns strictly require date writes to have a zero offset (UTC). All datetime operations must be committed as UTC. Check the uploader/post logic for `NormalizeLocalInputToUtc` or write via `DateTimeOffset.UtcNow`.
3. **Legacy Menu System Deletion**: The legacy item-centric menu system (`MenuItems`, `MenuCategories`, `MenuItemImages`) was **removed** from the database and C# code via EF migration `20260530041829_RemoveLegacyMenuItems`. The site now uses a `MenuGroup`-centric (flipbook catalog) system.
4. **Active/Unregistered Services**: `ReservationPolicyService` and `ConfiguredSpecialDateProvider` exist in the code but are **not registered** in Dependency Injection inside `Program.cs` and are currently unused.
5. **Animation System**: Scroll reveals must strictly use the attribute-driven engine `data-uw-reveal="..."` defined in [uw-reveal.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/uw-reveal.js). Classic class-based triggers (e.g., `scroll-reveal`, `reveal-up`) are **obsolete** and forbidden.
6. **No manual edits to site.css**: [site.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/site.css) is compiled dynamically from [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) using the Tailwind CLI. Any direct changes to `site.css` will be lost.
7. **Anti-Traversal Protection**: All admin file deletion operations must use strict `StartsWith` checks against root folder bounds to prevent directory traversal exploits.
8. **Anti-Forgery & Auth**: Every mutating HTTP POST route inside the admin area must use the `[ValidateAntiForgeryToken]` decoration. All admin controllers (except `AuthController`) inherit from `AdminBaseController` and are secured under `[Authorize]`.
9. **Image Fallback Strategy**: Public components (like branch cards) use inline `onerror` scripts to remove missing image tags and swap parent CSS layout classes (`.is-image-missing`) to render styled brand-colored backup blocks.
10. **Swiper CDN Dependencies**: Global `_Layout.cshtml` still queries Swiper JS and CSS CDNs, but the homepage hero has successfully migrated to a custom vanilla slider ([home-hero-slider.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/home-hero-slider.js)).

---

## Top 10 Risks or Uncertainties

1. **Stale/Stray Documentation**: Existing docs in `docs/` (such as `backend-project-context.md` and `db-schema.md`) still claim that legacy `MenuItem` and `MenuCategory` models exist and that their controllers are intact. They do not exist.
2. **Unregistered Policy Engine**: The `ReservationPolicyService` contains rich business rules (break times, minimum guests, weekday rules), but the active `ReservationService` uses simple validations and bypasses this policy entirely.
3. **Broken Image Fallback Coverage**: Older views that have not been redesigned (like `About/Index.cshtml`) still hardcode links to `/images/placeholders/*.webp` which do not exist on disk, risking 404 errors for assets.
4. **Missing Menu Groups in Sitemap**: [SeoController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/SeoController.cs) generates sitemaps dynamically, but it does not map the new menu group route `/thuc-don/nhom/{slug}`.
5. **No Size/Type Check in Post Thumbnail Uploads**: While `MenuGroupController` enforces file sizes (cover <= 10MB, page sheets <= 50MB), some upload controllers lack explicit file-type header validation.
6. **Shared Uploader Duplication**: File saving and safe-name generation logic is duplicated across `PostController` and `MenuGroupController` instead of being consolidated in a shared service.
7. **CSS/JS CDN Bloat**: Global pages load Swiper CSS/JS files from external CDNs, adding network overhead even though Swiper is not used by the redesigned custom hero slider.
8. **Limited Suggestions Scope**: [branch-locator.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/branch-locator.js) scrapes suggestions from active cards in the DOM; if a city filter is active, it only suggests branches in that city rather than globally.
9. **No Automated HTML Sanitization for Blog Posts**: News posts render HTML raw (`@Html.Raw`). Admins can insert arbitrary scripts unless content is sanitized on save.
10. **Local SSL Warning**: Dev builds output `Failed to determine the https port for redirect`, which requires setting the HTTPS port variable in launch settings.
