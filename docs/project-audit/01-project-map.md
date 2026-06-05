# 01 Project Map

## Root Files

| File | Purpose |
|------|---------|
| [Program.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Program.cs) | Unified startup file. Configures logging, DI services, cookie auth, compression, and global routing. |
| [appsettings.json](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/appsettings.json) | Core configuration settings, Neon PostgreSQL connection string, reservation policy parameters, and Resend settings. |
| [appsettings.Development.json](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/appsettings.Development.json) | Local development overrides. |
| [KIGHolding.csproj](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/KIGHolding.csproj) | MSBuild file declaring net10.0 framework, EF Core 10, Npgsql PostgreSQL, and Resend NuGet packages. |
| [KIGHolding.sln](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/KIGHolding.sln) | Visual Studio Solution file. |
| [package.json](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/package.json) | Node package file declaring Tailwind CLI 3.4.19 and PostCSS dependencies. Defines CSS compile scripts. |
| [postcss.config.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/postcss.config.js) | Configures PostCSS plugins (tailwindcss and autoprefixer). |
| [tailwind.config.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/tailwind.config.js) | Configures Tailwind scan paths, core Quicksand fonts, and custom brand color tokens (`brand-red`, `brand-black`, etc.). |

---

## Main Folders

| Folder | Purpose | Important Notes |
|--------|---------|-----------------|
| `Areas/` | Area boundary folder | Contains the secure `Admin/` area for admin CRUD management. |
| `Controllers/` | Public HTTP Controllers | Contains public controllers like `HomeController`, `MenuController`, and `ReservationController`. |
| `Data/` | EF Core Data Access | Holds `AppDbContext`, model configurations, and the `DbInitializer` seeder. |
| `Migrations/` | EF Core Migrations | Contains the PostgreSQL database migrations and schema definitions. |
| `Models/` | Domain Entities and Enums | Houses C# entity models and status enums. |
| `Services/` | Business Logic Layer | Contains services (e.g. `BranchService`, `ReservationService`) and DTO requests. |
| `ViewComponents/` | Razor View Components | Backing C# classes for reusable view blocks (like site header/footer). |
| `ViewModels/` | Page Models / Data Bundlers | ViewModels that bind page rendering and HTTP form validation. |
| `Views/` | Razor Views templates | Public facing pages and layouts. |
| `wwwroot/` | Public Static Assets | Holds compiled CSS, vanilla JS, images, icons, and uploaded media. |
| `docs/` | Project Documentation | Holds architecture guides, audit reports, and local development context files. |

---

## MVC Structure

| Area/Folder | Purpose |
|-------------|---------|
| [Controllers/](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers) | Orchestrates client routing for sitemaps, homepage, news, menus, branches, and booking forms. |
| [Areas/Admin/Controllers/](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Controllers) | Secure area controllers inheriting from `AdminBaseController` for site config, uploads, and dashboard lists. |
| [Views/Shared/](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared) | Global layouts (`_Layout.cshtml`), layout fallbacks, and dialog windows (like booking modals). |
| [Views/Shared/Components/](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/Components) | Razor sub-views for ViewComponents, housing header, footer, cards, and contact buttons. |
| [Views/Shared/Partials/](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/Partials) | Isolated partial files like `_Header.cshtml`, `_MobileMenu.cshtml`, and `_Footer.cshtml` used by ViewComponents. |

---

## Static Assets

| Folder/File | Purpose |
|-------------|---------|
| `wwwroot/css/input.css` | Canonical styling source. Defines typography, utility overrides, custom gradients, and bobbing animations. |
| `wwwroot/css/site.css` | Compiled stylesheet output. Generated automatically; must not be edited. |
| `wwwroot/js/site.js` | Site-wide scripting (drawer open/close, category filters, and back-to-top handler). |
| `wwwroot/js/home-hero-slider.js` | Handles slideshow slide cross-fades, timer progress, pause state, and keyboard actions on Home. |
| `wwwroot/js/branch-locator.js` | Autocomplete locator suggester with ArrowUp/Down and Escape hooks. |
| `wwwroot/js/uw-reveal.js` | IntersectionObserver and Web Animations API-powered scroll reveal script. |
| `wwwroot/js/menu-flipbook.js` | Renders dual-page layouts, flip transition states, page navigation, and keyboard inputs on menu groups. |
| `wwwroot/images/general/` | Contains core transparent brand logo (`kig-no-bg-logo.png`) and app QR code. |
| `wwwroot/images/home/images/` | Contains bobbing illustration icons (kimchi, soyu, noodle, chilli) and visual assets. |
| `wwwroot/uploads/` | Physical uploader folders for uploaded branch cards, news cards, menu covers, and menu page image sheets. |

---

## Folder Tree Summary
```txt
KIGHolding/
├── Areas/
│   └── Admin/
│       ├── Controllers/      <-- Admin base, dashboard, CRUD uploader
│       ├── ViewModels/       <-- Login and CRUD models
│       └── Views/            <-- Admin workspace layout and views
├── Controllers/              <-- Public page mapping (Index, Detail, XML)
├── Data/
│   ├── Configurations/       <-- EF Core model property parameters
│   ├── AppDbContext.cs       <-- SaveChanges timestamp hooks
│   └── DbInitializer.cs      <-- Initial data & superadmin creator
├── Migrations/               <-- PostgreSQL schema change history
├── Models/
│   ├── Entities/             <-- SiteSetting, Branch, Post, MenuGroup
│   └── Enums/                <-- ReservationStatus, ReservationSource
├── Services/                 <-- Branch, News, Booking, and Email services
├── ViewComponents/           <-- C# ViewComponent controllers
├── ViewModels/
│   ├── Shared/               <-- Card, header, footer viewmodels
│   └── *ViewModel.cs         <-- Form submission validation annotations
├── Views/
│   ├── Shared/
│   │   ├── Components/       <-- Reusable blocks
│   │   └── Partials/         <-- Layout pieces
│   └── */Index.cshtml        <-- Page Razor views
└── wwwroot/
    ├── css/
    │   ├── input.css         <-- Core custom styles
    │   └── site.css          <-- Compiled Tailwind stylesheet
    ├── js/                   <-- Isolated progressive ES6 scripts
    └── uploads/              <-- Physical uploader content paths
```
