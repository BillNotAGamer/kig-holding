# Admin Area Roadmap & Developer Guide

This document describes the design, layout architecture, global mechanisms, and step-by-step procedures for extending the administrative back-office portal of the **Truyền Thuyết Champong** web application.

---

## 1. Architectural Overview

The Admin portal is implemented as an ASP.NET Core Area (`[Area("Admin")]`). All controllers inside this Area (except the authentication actions in [AuthController](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Controllers/AuthController.cs)) derive from [AdminBaseController](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Controllers/AdminBaseController.cs), which enforces authorization policies globally.

### Key Visual Shell Layout
The visual frame is managed by the unified master layout file [_AdminLayout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Shared/_AdminLayout.cshtml). It provides:
1. **Responsive Dual Pane Structure**: A fixed dark-theme sidebar on large viewports (`lg:sticky bg-brand-black text-white`) and a light-theme main content panel (`bg-brand-light text-brand-dark`).
2. **Mobile Drawer Fallbacks**: Adaptive headers (`lg:hidden`) and a slide-over mobile drawer (`-translate-x-full lg:translate-x-0`) that respects accessibility guidelines via ARIA states and backdrop click close behaviors.

---

## 2. Active Sidebar Routing Logic

To preserve state and show the user where they are, [_AdminLayout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Shared/_AdminLayout.cshtml) parses the route data of the current request and adds active markers to corresponding sidebar links.

### Route Evaluation
At the top of the layout file, the active controller is extracted from `ViewContext`:

```cshtml
@{
    var currentController = ViewContext.RouteData.Values["controller"]?.ToString();
}
```

### Visual State Highlight
In the navigation panel, each module anchor is configured with an active marker check. We resolve classes using helper functions defined at the bottom of the layout:

```cshtml
<a href="/Admin/Reservation"
   aria-current="@(IsActive(currentController, "Reservation") ? "page" : null)"
   class="@NavLinkClass(currentController, "Reservation")">
    Quản lý Đặt bàn
</a>
```

### NavLink Helper Methods
The active classes append KIG Red tokens (`border-brand-red bg-brand-red/10 text-white`) to visually distinguish active tabs from inactive hover actions (`text-white/80 hover:bg-white/10`):

```cshtml
@functions {
    private static bool IsActive(string? currentController, string controllerName)
    {
        return string.Equals(currentController, controllerName, StringComparison.OrdinalIgnoreCase);
    }

    private static string NavLinkClass(string? currentController, string controllerName)
    {
        var baseClass = "block rounded-xl border-l-2 px-3 py-3 text-sm font-semibold transition duration-200";
        return IsActive(currentController, "ControllerName")
            ? $"{baseClass} border-brand-red bg-brand-red/10 text-white shadow-soft"
            : $"{baseClass} border-transparent text-white/80 hover:bg-white/10 hover:text-white";
    }
}
```

---

## 3. Global Notification Pipeline (Toast Alerts)

The application utilizes a temporary memory store (`TempData`) to pass operational state notifications (such as "Save Successful" or "Access Denied") across controller redirects.

### Controller Implementation
Controllers set standard toast keys after processing data edits successfully or encountering model state faults:

```csharp
TempData["SuccessMessage"] = "Đã cập nhật cấu hình website thành công.";
// or
TempData["ErrorMessage"] = "Có lỗi xảy ra khi lưu thông tin. Vui lòng kiểm tra lại.";
```

### UI Render Template
The master layout reads these values and builds floating Toast alerts inside a fixed absolute portal at the bottom of the screen:

```cshtml
<div class="pointer-events-none fixed bottom-4 left-1/2 z-[60] w-[min(24rem,calc(100vw-2rem))] -translate-x-1/2 space-y-3 sm:bottom-auto sm:left-auto sm:right-4 sm:top-4 sm:translate-x-0">
    @if (!string.IsNullOrWhiteSpace(successMessage))
    {
        <div class="admin-toast pointer-events-auto rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-800 shadow-soft transition-all duration-300" data-toast>
            <!-- Checkmark icon and message -->
            <p class="font-semibold text-emerald-900">Thành công</p>
            <p class="mt-1 leading-6">@successMessage</p>
        </div>
    }
    <!-- Error alert template follows similar patterns using bg-red-50/border-red-200 -->
</div>
```

### Micro-Dismiss Animation JS
A local script initializes inside the HTML shell, automatically fading out and clean-removing notification components after **4 seconds**:

```javascript
(function () {
    var toasts = document.querySelectorAll('[data-toast]');
    if (!toasts.length) return;

    toasts.forEach(function (toast) {
        window.setTimeout(function () {
            toast.classList.add('opacity-0', 'translate-y-2');
            window.setTimeout(function () {
                toast.remove();
            }, 300);
        }, 4000);
    });
})();
```

### Real-Time Reservation Toasts

Authenticated Admin pages using `_AdminLayout.cshtml` also host dynamic reservation notifications from SignalR. The layout provides a stable `data-admin-reservation-toast-host` container, the `/admin/hubs/reservations` Hub URL, the reservation detail URL template, and the local MP3 notification URL. The client script appends new-reservation toasts into the same visual stack and removes them after approximately 4 seconds, preserving existing TempData toast behavior. Its guarded start loop retries initial connection failures and restarts after SignalR automatic reconnect reaches a terminal close.

The Admin header includes a compact sound enable/disable control. The sound preference persists in `localStorage`, but every new document remains subject to browser autoplay policy. When the preference is enabled, the page prepares the audio and silently attempts to unlock it on the Admin's first normal interaction; reservation playback is also attempted automatically. If the browser blocks playback, the preference remains enabled, the toast still appears, and a small activation control is shown. Multiple Admin tabs all show toasts. Hidden tabs do not play audio; visible tabs use an exclusive per-reservation Web Locks claim when supported, while older browsers fall back to visible-tab playback and may play duplicate audio if multiple visible tabs receive the same event.

---

## 4. Step-by-Step Developer Guide: Adding a New Admin Module

Follow this workflow to safely extend the admin interface with a new module (e.g. a "Branch" management screen).

### Step 1: Define C# Model
Create a database entity in `Models/Entities/` (e.g. `Branch.cs`). Annotate primary keys, relational mapping tables, and required limits.
```csharp
namespace KIGHolding.Models.Entities;

public class Branch : IUpdatedAtEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

### Step 2: Register DbSet & Run Migrations
1. Open [AppDbContext.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Data/AppDbContext.cs) and expose your new collection:
   ```csharp
   public DbSet<Branch> Branches => Set<Branch>();
   ```
2. Run database migration commands in your terminal:
   ```powershell
   dotnet ef migrations add AddBranchesTable --project KIGHolding.csproj
   dotnet ef database update --project KIGHolding.csproj
   ```

### Step 3: Implement Business Logic Layer
Create service interfaces and cached logic layers inside `/Services` (e.g. `IBranchService.cs` and `BranchService.cs`).
Register the implementation inside [Program.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Program.cs):
```csharp
builder.Services.AddScoped<IBranchService, BranchService>();
```

### Step 4: Add Area ViewModels
Create ViewModels in `Areas/Admin/ViewModels/` (e.g. `BranchEditViewModel.cs`) containing only UI data transfer details.
> [!IMPORTANT]
> Always apply mandatory `DataAnnotations` (like `[Required]`, `[StringLength]`, `[EmailAddress]`) on ViewModels to assert model integrity before processing database commits.

### Step 5: Implement Admin Controller
Create your controller in `Areas/Admin/Controllers/` inheriting from `AdminBaseController` to leverage unified layout bindings. In the KIG Holding architecture, Admin controllers query `AppDbContext` directly with database-level paging, bypassing public caches while explicitly invalidating public caches on write:
```csharp
using KIGHolding.Areas.Admin.Controllers;
using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Areas.Admin.Controllers;

public class BranchController : AdminBaseController
{
    private const int PageSize = 10;
    private readonly AppDbContext _dbContext;
    private readonly IBranchService _branchService;

    public BranchController(AppDbContext dbContext, IBranchService branchService)
    {
        _dbContext = dbContext;
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var requestedPage = Math.Max(page, 1);
        var query = _dbContext.Branches.AsNoTracking();
        var totalItems = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));
        var currentPage = Math.Min(requestedPage, totalPages);

        var branches = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return View(new BranchIndexViewModel
        {
            Branches = branches,
            Page = currentPage,
            PageSize = PageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BranchCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var branch = new Branch
        {
            Name = model.Name.Trim(),
            Address = model.Address.Trim(),
            IsActive = model.IsActive
        };

        _dbContext.Branches.Add(branch);
        await _dbContext.SaveChangesAsync();

        // Invalidate public memory cache so website visitors see changes immediately
        _branchService.InvalidateActiveBranchesCache();

        TempData["SuccessMessage"] = "Đã thêm chi nhánh mới!";
        return RedirectToAction(nameof(Index));
    }
}
```

### Step 6: Design Razor Templates
Create folder `Areas/Admin/Views/Branch/`. Initialize basic CRUD templates using the project design tokens:
*   `Index.cshtml`: Admin tables (`.admin-table-wrap`) and actions buttons (`.admin-table-action--primary`).
*   `Create.cshtml`/`Edit.cshtml`: Responsive form columns (`.admin-form-grid`) styled with standard fields (`.admin-field`).

### Step 7: Update Sidebar Menu Links
Finally, insert the navigation hook inside [_AdminLayout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Shared/_AdminLayout.cshtml):
```cshtml
<a href="/Admin/Branch"
   aria-current="@(IsActive(currentController, "Branch") ? "page" : null)"
   class="@NavLinkClass(currentController, "Branch")">
    Quản lý Chi nhánh
</a>
```

---

## 5. Admin Credential Bootstrap & Authentication Architecture

To protect production deployments against default or unhardened credentials while guaranteeing zero-touch automated setup, the admin back-office uses a defense-in-depth bootstrapping architecture:

### 5.1 Credential Configuration (`AdminBootstrapSettings`)
Configured in `appsettings.json` or environment variables under the `AdminBootstrap` section:
```json
{
  "AdminBootstrap": {
    "Username": "production_admin",
    "Password": "SuperSecretPassword123!",
    "Email": "admin@kigholding.vn",
    "RemediateLegacySeed": true
  }
}
```
* **Password Policy Enforcement**: Validated by `AdminPasswordPolicy.Validate`: minimum 12 characters, requiring uppercase, lowercase, digit, and a non-alphanumeric special character.
* **Email Normalization**: Validated and normalized by `AdminEmailNormalizer` (lowercased, trimmed, max 256 characters).

### 5.2 Legacy Seed Remediation Guard (`AdminLegacyCredentialGuard`)
Migration `20260508052050_AddAdminUserTable` seeded an initial admin record (`Id: 2d7d0dd3-7d9d-4d9b-93d8-8e2d2b8ed9b1`, username `"admin"`, role `"SuperAdmin"`, seed timestamp `2026-05-08 00:00:00 UTC`).
* **Startup Enforcement**: On every application startup in `Program.cs`, `AdminLegacyCredentialGuard.EnsureSecureAsync()` inspects the database.
* **Fail-Closed Protection**: If `LegacyAdminSeedFingerprint.IsExactUnremediatedHistoricalSeed(legacyAdmin)` matches and `RemediateLegacySeed` is `false`, the application **aborts startup immediately** with an `InvalidOperationException`.
* **Automatic Remediation**: When `RemediateLegacySeed` is `true`, the guard updates the seed account with the validated bootstrap credentials, hashes the password via `IPasswordHasher<AdminUser>` (PBKDF2), generates a fresh 32-byte cryptographic security stamp, and persists the changes.

### 5.3 Session Security & Stamp Invalidation (`AdminCookieAuthenticationEvents`)
* **Sliding Session**: The admin cookie (`KIGHolding.AdminAuth`) is configured with `SlidingExpiration = true` and `ExpireTimeSpan = TimeSpan.FromHours(8)` (extended to 14 days if `RememberMe` is checked).
* **Cryptographic Security Stamp**: On login, `AdminSecurityStampGenerator.Create()` stamps the user record with 32 secure random bytes formatted as a 64-character hexadecimal string, embedded into the cookie claims via `AdminClaimTypes.SecurityStamp`.
* **Per-Request Principal Validation**: `AdminCookieAuthenticationEvents.ValidatePrincipal` intercepts every admin request, queries `AppDbContext.AdminUsers.AsNoTracking()`, and verifies `user.IsActive` and `user.SecurityStamp == cookieStamp`. If an administrator deactivates an account or rotates credentials, all active cookie sessions for that user are rejected and signed out immediately.

---

## 6. Branch Reservation Policy & Weekly Defaults Workflow

The Admin portal provides comprehensive capacity and schedule management at `/Admin/Reservation/BranchPolicy` and `/Admin/Branch`.

### 6.1 Synchronized Calendar Visualization
The branch reservation policy calendar visually reflects the unified 4-tier policy evaluation state for each calendar day:
* **Specific Date Closed (`Closed`)**: Renders in Red (`.branch-policy-state-closed`) with badge `"Không nhận đặt bàn"`.
* **Specific Date Minimum Override (`MinimumGuests`)**: Renders in Amber (`.branch-policy-state-minimum`) with badge `"Tối thiểu {X} khách (Riêng ngày)"`.
* **Inherited Weekly Default (`weeklyDefaultMin > 1`)**: When no date override exists in the database, the calendar automatically calculates `isWeekend ? branch.WeekendMinGuests : branch.WeekdayMinGuests` and renders in Amber (`.branch-policy-state-minimum`) with badge `"Tối thiểu {X} khách (Mặc định)"`.
* **Unrestricted Default**: Renders in Green (`.branch-policy-state-allowed`) with badge `"Cho phép đặt bàn"`.

### 6.2 Weekly Defaults Quick Configuration & 1-Click HCMC Sync
A dedicated configuration card is placed directly above the calendar grid on `/Admin/Reservation/BranchPolicy`:
* Allows immediate viewing and modification of `WeekdayMinGuests` (Mon–Fri) and `WeekendMinGuests` (Sat–Sun) for the selected branch.
* Form submits via POST `UpdateBranchWeeklyDefaults` with anti-forgery verification.
* **1-Click Bulk HCMC Sync**: When the selected branch is located in Ho Chi Minh City (`IsSelectedBranchHcmc`), admins can check `"Áp dụng cho tất cả chi nhánh TP.HCM"` to propagate the values to all HCMC branches in a single atomic transaction.
* Both single-branch and bulk updates trigger `_branchService.InvalidateActiveBranchesCache()` to refresh public caches immediately.

### 6.3 Branch Management Integration
* `BranchCreateViewModel` and `BranchEditViewModel` include `WeekdayMinGuests` and `WeekendMinGuests` with `[Range(1, 100)]` validation attributes.
* Fields are editable from `/Admin/Branch/Create` and `/Admin/Branch/Edit`, and automatically persisted to `Branches` on save.
