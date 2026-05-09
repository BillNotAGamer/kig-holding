# Architecture Overview

## High-Level Tech Stack
* **Framework**: ASP.NET Core MVC (C#)
* **Database**: PostgreSQL (via Npgsql Provider)
* **Styling**: Tailwind CSS
* **Frontend Interactivity**: Vanilla JavaScript with native APIs (e.g., IntersectionObserver)

## Directory Structure
* `/Controllers/`: Contains all public-facing application controllers (e.g., `HomeController`, `MenuController`).
* `/Areas/Admin/`: Encapsulates the entire secure Admin panel, including its own `Controllers/`, `Views/`, and `ViewModels/`.
* `/Services/`: Houses core business logic and caching implementation to decouple from controllers.
* `/Models/`: Contains Entity Framework core domain models (`Entities/`).
* `/wwwroot/`: Serves static assets, including Tailwind compiled CSS, vanilla JS scripts, and images.
* `/ViewModels/`: Holds data transfer objects with strict validation rules for views.

## Dependency Injection Configuration
In `Program.cs`, the DI container is meticulously structured:
* **Controllers & Views**: `builder.Services.AddControllersWithViews()`
* **Database**: Entity Framework configured to use PostgreSQL via `AppDbContext`.
* **Services**: Scoped logic services are registered (`IMenuService`, `IBranchService`, `INewsService`, `IReservationService`, `IContactService`, `ISiteSettingService`, `DbInitializer`).
* **Caching**: In-memory caching (`AddMemoryCache()`) is configured, heavily utilized by services.
* **Optimization**: Response compression is enabled for network efficiency.

## Authentication Architecture
* The application employs **Cookie Authentication** configured via `CookieAuthenticationDefaults.AuthenticationScheme`.
* The secure cookie is named `KIGHolding.AdminAuth`.
* Sliding expiration is active, set to 8 hours.
* Unauthorized access redirects to `/Admin/Auth/Login`.
* User authorization relies on the `AdminUser` table. Passwords are securely stored and verified via ASP.NET Core Identity's `PasswordHasher`.
