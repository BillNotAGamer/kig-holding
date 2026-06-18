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

---

## 4. Step-by-Step Developer Guide: Adding a New Admin Module

Follow this workflow to safely extend the admin interface with a new module (e.g. a "Branch" management screen).

### Step 1: Define C# Model
Create a database entity in `Models/Entities/` (e.g. `Branch.cs`). Annotate primary keys, relational mapping tables, and required limits.
```csharp
namespace KIGHolding.Models.Entities;

public class Branch
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
}
```

### Step 2: Register DbSet & Run Migrations
1. Open [AppDbContext.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Data/AppDbContext.cs) and expose your new collection:
   ```csharp
   public DbSet<Branch> Branches { get; set; }
   ```
2. Run database migration commands in your terminal:
   ```powershell
   dotnet ef migrations add AddBranchesTable
   dotnet ef database update
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
Create your controller in `Areas/Admin/Controllers/` inheriting from `AdminBaseController` to leverage unified layout bindings:
```csharp
using KIGHolding.Areas.Admin.Controllers;

namespace KIGHolding.Areas.Admin.Controllers;

public class BranchController : AdminBaseController
{
    private readonly IBranchService _branchService;

    public BranchController(IBranchService branchService)
    {
        _branchService = branchService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var branches = await _branchService.GetAllAsync();
        return View(branches);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BranchEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        await _branchService.CreateAsync(model);
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
