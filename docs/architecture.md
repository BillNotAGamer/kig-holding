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
*   **Image Storage**: Cloudinary (fallback to Local Storage for offline environments)

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
├── Controllers/              # Public controllers (Home, About, Branch, Contact, Franchise, Menu, News, Reservation, Seo)
├── Data/                     # EF Core DbContext, entity configurations, and DbInitializer
├── Migrations/               # PostgreSQL schema migrations
├── Models/
│   ├── Entities/             # C# classes mapped to DB tables (Branch, MenuGroup, MenuPageImage, Reservation, Post, etc.)
│   ├── Enums/                # Enums for statuses and reservation sources
│   └── PagedResult.cs        # Generic pagination helper
├── Options/                  # Options pattern settings classes (ResendSettings, CloudinarySettings, etc.)
├── Properties/               # launchSettings.json configurations
├── Services/                 # Business logic interfaces & cached implementations (MenuGroupService, BranchService, etc.)
├── ViewComponents/           # ViewComponents for reusable components (SiteHeader, SiteFooter, LayoutFallbacks, etc.)
├── ViewModels/               # ViewModels for public-facing forms and detail views
├── Views/                    # Razor Views for public routes (Home, Menu, News, Reservation, Branch, Contact, Shared)
└── wwwroot/                  # Client-side assets
    ├── css/                  # input.css (source) and site.css (Tailwind compiled output)
    ├── js/                   # Site scripts (site.js, uw-reveal.js, kbb-cook-slider.js, etc.)
    └── uploads/              # Dynamic uploaded files (menu-groups/covers, posts)
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
| `IImageStorageService` | Scoped | Handles uploads to Cloudinary or falls back to local storage |
| `IPasswordHasher<AdminUser>` | Scoped | Secure hashing using PBKDF2 for admin credentials |
| `AdminCookieAuthenticationEvents`| Scoped | Validates admin session cookie and security stamp consistency |

---

## 4. Key Infrastructure Features

*   **Cookie Authentication**: Session cookies are configured with `SlidingExpiration = true` and `ExpireTimeSpan = TimeSpan.FromHours(8)` using custom event validation.
*   **Data Protection**: Persists cryptography keys to a secure local folder: `App_Data/DataProtectionKeys/` to ensure persistent login states across application recycling.
*   **Response Compression**: Optimized for SVG/XML formats to enhance loading performance.
