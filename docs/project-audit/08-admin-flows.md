# 08 Admin Flows

## Admin Route & Security Structure
* **Area Name**: `Admin`
* **Route Template**: `/Admin/{Controller}/{Action}/{id?}`
* **Authorization**: Global `[Authorize]` filter decorated on the base class [AdminBaseController](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Controllers/AdminBaseController.cs). Unauthenticated requests are redirected to `/Admin/Auth/Login`.
* **Session Lifetime**: Cookie authentication (`KIGHolding.AdminAuth`) with an 8-hour sliding expiration window.
* **CSRF Protection**: Every HTTP POST action inside the admin area must use the `[ValidateAntiForgeryToken]` attribute.

---

## Admin Controllers

| Controller | Main Actions | Purpose | Primary Risk / Notes |
|------------|--------------|---------|----------------------|
| `AuthController` | `Login`, `Logout` | Handles admin login and logout sessions. | Bypasses global auth filter via `[AllowAnonymous]` on Login. Uses BCrypt-compatible PasswordHasher. |
| `DashboardController` | `Index` | Admin dashboard listing key metrics. | Safe read-only statistics query. |
| `ReservationController` | `Index`, `Edit`, `ToggleStatus`, `Delete` | Customer reservations manager. | Changing reservation status (Pending/Confirmed/Cancelled). |
| `ContactController` | `Index`, `Details`, `ToggleRead`, `Delete` | Admin inbox for customer messages. | Unsafe deleting of message entries. |
| `MenuGroupController` | `Index`, `Create`, `Edit`, `Delete`, `Images`, `Upload`, `Update`, `DeleteImage`, `BulkDelete` | Menu groups and uploader manager. | Physical file uploads and deletions. |
| `PostController` | `Index`, `Create`, `Edit`, `Delete` | Blog article news updates manager. | Physical file uploads and HTML raw renders. |
| `ReviewController` | `Index`, `ToggleVisible`, `Delete` | Customer review visible toggler. | Direct deletion of customer reviews. |
| `BranchController` | `Index`, `Create`, `Edit`, `Delete` | Restaurant branches CRUD. | Branch parameters edits (Active hours, maps, hotline). |
| `SettingController` | `Index` (GET/POST) | System settings record editor. | Saves global branding configuration. |

---

## Admin Forms & Binding

| Form | View File | POST Action | Model Binding | Anti-Forgery | Notes |
|------|-----------|-------------|---------------|:------------:|-------|
| Login Form | `Auth/Login.cshtml` | `AuthController.Login` | `AdminLoginViewModel` | **Yes** | Authenticates user username and checks PasswordHash. |
| Site Config Form | `Setting/Index.cshtml` | `SettingController.Index` | `SiteSettingViewModel` | **Yes** | Updates global hotlines, logos, and business registration numbers. |
| Create MenuGroup | `MenuGroup/Create.cshtml` | `MenuGroupController.Create` | `MenuGroupFormViewModel` | **Yes** | Binds group details and covers uploader files. |
| Edit MenuGroup | `MenuGroup/Edit.cshtml` | `MenuGroupController.Edit` | `MenuGroupFormViewModel` | **Yes** | Updates group details and replaces cover images. |
| Multi-Image Uploader | `MenuGroup/Images.cshtml` | `MenuGroupController.Upload` | `MenuPageImageUploadViewModel` | **Yes** | Multi-file form upload. Checks sizes and extensions. |
| Page Image Edit | `MenuGroup/Images.cshtml` | `MenuGroupController.Update` | `MenuPageImageUpdateViewModel` | **Yes** | Edits alt text descriptions and display order. |
| Create Branch | `Branch/Create.cshtml` | `BranchController.Create` | `BranchFormViewModel` | **Yes** | Sets opening hours, capacities, and Google Maps iframe urls. |
| Create Post | `Post/Create.cshtml` | `PostController.Create` | `PostFormViewModel` | **Yes** | Sets categories, slugs, published dates, and rich HTML text. |

---

## Key Administrative Behaviors

### 1. Auto-Alert Message System (`AdminBaseController`)
The base controller intercepts all action execution results:
* Successful POST redirects automatically populate a success alert in `TempData["SuccessMessage"]`.
* Validation failures that return a `ViewResult` automatically populate `TempData["ErrorMessage"] = "Vui lòng kiểm tra lại dữ liệu đã nhập."`.
* The admin layout renders these alerts as temporary toast notifications at the top right of the viewport.

### 2. File Upload Safety
* **Max Sizes**: News post/branch thumbnails <= 2MB, MenuGroup covers <= 10MB, and MenuPageImage sheets <= 50MB.
* **Allowed Extensions**: `.jpg`, `.jpeg`, `.png`, `.webp`.
* **Allowed Content Types**: `image/jpeg`, `image/png`, `image/webp`.
* **Safe naming**: Converts filenames to slugs, appends a clean GUID suffix, and retains the original extension to avoid physical folder collisions.
* **Delete Traversal Shield**: Checks physical file paths using `.StartsWith` against the uploads root folder to prevent directory traversal exploits.

### 3. Display Sorting Logic
* **Menu Groups**: Ordered by the integer field `DisplayOrder`, falling back to `Name`.
* **Menu Page Images**: Ordered by `DisplayOrder`, falling back to `CreatedAt`.
* **Manual Reordering**: Administrators can edit the `DisplayOrder` integer field in the images listing page to reorder pages. Newly uploaded batch files automatically inherit the maximum display order incremented by 1.

### 4. Delete & Hide Policies
* **Menu Groups**: Hard deletions are disabled. Clicking delete calls `MenuGroupController.Delete` which sets `IsPublished = false` to hide the group from the public site while preserving it in the admin panel.
* **Menu Page Images**: Hard deletion. Deletes the database record and deletes the physical file from the server.
* **Reservations/Contacts**: Hard deletion. Directly deletes records from the database.
* **Branches**: Toggles active status (`IsActive = !IsActive`) to disable booking forms without deleting historical data.
