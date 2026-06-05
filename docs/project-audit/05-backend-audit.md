# 05 Backend Audit

## Controllers List

### Public Boundary Controllers

| Controller | Main Actions | Dependencies | Purpose | Notes |
|------------|--------------|--------------|---------|-------|
| `HomeController` | `Index` | `INewsService`, `IBranchService` | Renders Home page | Queries latest 3 news posts and active branches for the landing model. |
| `MenuController` | `Index`, `Group`, `Detail` | `IMenuGroupService`, `IWebHostEnvironment`, `IConfiguration` | Public Menu Hub and Group viewers | `Detail` action redirects GET `/thuc-don/{slug}` to `/thuc-don`. `Group` fetches images for book viewer. |
| `BranchController` | `Index` | `IBranchService`, `IConfiguration` | Branch locator page | Filters by city and search string. Searches are case and diacritic-insensitive. |
| `ReservationController` | `Index` (GET/POST), `Quick` (POST), `Success` (GET) | `IReservationService`, `IBranchService`, `ISiteSettingService`, `IEmailService`, `IOptions<ResendSettings>` | Manages reservation booking requests | `Quick` processes async home requests and returns JSON. `Success` renders reservation details. |
| `NewsController` | `Index`, `Detail` | `INewsService` | Handles paginated news listings and read views | Compiles related updates using shared categories, falling back to latest posts. |
| `ContactController` | `Index` (GET/POST) | `IContactService`, `IBranchService`, `IEmailService` | Handles contact form submissions | Validates inputs, saves to DB, and sends mail notifications. |
| `AboutController` | `Index` | `ISiteSettingService` | Renders About page | Static views displaying corporate brand origins. |
| `MemberController` | `Index` | *None* | Renders Membership placeholder page | Return View only. |
| `FranchiseController` | `Index` | *None* | Renders Franchise placeholder page | Return View only. |
| `SeoController` | `Sitemap`, `Robots` | `INewsService`, `IMenuGroupService`, `IBranchService` | Serves search-crawler documents | Serves sitemap XML stream and robots.txt. |
| `ErrorController` | `Index` | *None* | Intercepts status code redirects | Renders system error pages. |

---

### Admin Boundary Controllers (Area: Admin)
*All admin controllers derive from [AdminBaseController](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Controllers/AdminBaseController.cs) and are protected by `[Authorize]` with sliding cookie session validation.*

| Controller | Actions | Dependencies | Purpose | Notes |
|------------|---------|--------------|---------|-------|
| `DashboardController` | `Index` | `AppDbContext` | Main admin panel dashboard | Calculates total bookings, unread contacts, active branches, and news counts. |
| `AuthController` | `Login` (GET/POST), `Logout` (POST) | `AppDbContext` | Handles admin sessions | Uses `PasswordHasher<AdminUser>` and sets claims values. Allows `[AllowAnonymous]` on Login. |
| `ReservationController` | `Index`, `Edit` (GET/POST), `ToggleStatus`, `Delete` | `AppDbContext` | Manages customer reservation lists | Supports status updates (Confirmed/Pending/Cancelled) and searches. |
| `ContactController` | `Index`, `Details`, `ToggleRead`, `Delete` | `AppDbContext` | Manages site inbox messages | Allows marking messages as read/unread or deletion. |
| `MenuGroupController` | `Index`, `Create` (GET/POST), `Edit` (GET/POST), `TogglePublished`, `Delete`, `Images` (GET), `Upload` (POST), `Update` (POST), `DeleteImage` (POST), `BulkDelete` (POST) | `AppDbContext`, `IWebHostEnvironment` | MenuGroup CRUD and multi-image sheet page uploads | Large controller (800+ lines) handling Cover images and cascading page sheets. |
| `PostController` | `Index`, `Create` (GET/POST), `Edit` (GET/POST), `TogglePublished`, `Delete` | `AppDbContext`, `IWebHostEnvironment` | Editorial news blog manager | Coordinates thumbnail uploads, slug validation, and date normalization. |
| `ReviewController` | `Index`, `ToggleVisible`, `Delete` | `AppDbContext` | Manages testimonial reviews | Controls which reviews are shown publicly. |
| `BranchController` | `Index`, `Create` (GET/POST), `Edit` (GET/POST), `ToggleActive`, `Delete` | `AppDbContext`, `IWebHostEnvironment` | Restaurant branch manager | CRUD for capacities, maps links, telephone info, and operating time limits. |
| `SettingController` | `Index` (GET/POST) | `AppDbContext` | Single global settings record editor | Updates Brand Name, Slogans, Hotlines, Emails, and Social links. |

---

## Services & Business Layer

| Service | Interface | DI Lifetime | Registered? | Dependencies | Purpose | Notes |
|---------|-----------|-------------|-------------|--------------|---------|-------|
| `SiteSettingService` | `ISiteSettingService` | Scoped | **Yes** | `AppDbContext`, `IMemoryCache` | Loads and caches site variables | Caches site settings for 20 minutes to reduce database queries. |
| `MenuGroupService` | `IMenuGroupService` | Scoped | **Yes** | `AppDbContext` | Loads menu groups and page images | Queries active sheets sorted by order and creation dates. |
| `BranchService` | `IBranchService` | Scoped | **Yes** | `AppDbContext` | Loads active restaurant details | Exposes lists and retrieves detailed branch models. |
| `NewsService` | `INewsService` | Scoped | **Yes** | `AppDbContext` | Manages editorial articles and related reads | Compiles pages and queries related items by category matching. |
| `ReservationService` | `IReservationService` | Scoped | **Yes** | `AppDbContext` | Creates new reservations | Validates booking times, guest counts, and checks active branches. |
| `ContactService` | `IContactService` | Scoped | **Yes** | `AppDbContext` | Saves customer requests | Creates inbox contact message records. |
| `EmailService` | `IEmailService` | Scoped | **Yes** | `IResend&nbsp;Client`, `IOptions<ResendSettings>` | Sends notification emails | Sends booking confirmation emails to restaurant staff via Resend. |
| `DbInitializer` | *None* | Scoped | **Yes** | `AppDbContext` | Database migrations and seeding | Installs initial values, categories, branches, and seeds SuperAdmins. |
| `ReservationPolicyService` | `IReservationPolicyService` | *None* | **NO** | `IOptions<ReservationPolicyOptions>`, `ISpecialDateProvider`, `ILogger` | Detailed business rule checks | **Unused**. Bypassed by controllers. |
| `ConfiguredSpecialDateProvider` | `ISpecialDateProvider` | *None* | **NO** | `ILogger` | Holiday dates helper | **Unused**. Loaded inside `ReservationPolicyService`. |

---

## Runtime Infrastructure

### 1. Dependency Injection Audit
* All active services are registered in [Program.cs](file:///f:/Coding/Web%20development/KIG Holding/KIGHolding/Program.cs#L57-L64) with a **Scoped** lifetime.
* **Unregistered Services**: `IReservationPolicyService` / `ReservationPolicyService` and `ISpecialDateProvider` / `ConfiguredSpecialDateProvider` are not registered in the DI container. This prevents them from being resolved at runtime, which is why they are currently unused.

### 2. Error Handling & Redirects
* In production environments, the app registers `UseExceptionHandler("/error")` to catch unhandled exceptions.
* Status code redirects are processed via `UseStatusCodePagesWithReExecute("/error/{0}")` to handle errors like 404 NotFound.
* The `ErrorController` resolves these redirects and renders a user-friendly error view.

### 3. Validation Gaps
* While the presentation layer validates inputs via ViewModel Data Annotations, controllers (such as `PostController` and `MenuGroupController`) perform manual validation for file uploads (file size and extension checks) instead of encapsulating this logic in custom validation attributes or services.
