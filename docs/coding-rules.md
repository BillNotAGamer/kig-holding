# Project Coding Rules & Standards

This document establishes the non-negotiable coding rules, visual tokens, and security boundaries that must be followed when contributing to the **Truyền Thuyết Champong** project.

---

## 1. Visual Design Tokens & UI Architecture

All public and administrative views must strictly adhere to the unified Light Theme system. Do not introduce custom dark surfaces, custom primary buttons, or unmapped colors.

### Base Color System
Theme tokens are managed inside [tailwind.config.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/tailwind.config.js) and mapped to global styles in [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css):

*   **HTML root setting**: `:root { color-scheme: light; }`
*   **Base Background**: `@apply bg-brand-light` (Warm off-white `#FAF8F3` with secondary background gradients).
*   **Typography**: Default font family is `sans: ['Quicksand', ...]` for a highly legible, premium editorial look.
*   **Main Text**: `text-brand-dark` (`#111827`) for body content; `text-brand-gray` (`#6B7280`) for helper texts.
*   **Selection Highlight**: `::selection { @apply bg-brand-red text-white; }`
*   **Primary Brand Red**: `#E50914` (`brand.red`) for focus highlights, selections, active indicators.
*   **Danger Accent**: `#B91C1C` (`brand.redDark`) for error notifications, invalid inputs, and action alerts.

### Hover and Focus States
Interactive buttons or fields must implement smooth transitions (`transition duration-200`) and clear visual feedback:
*   **Focus Ring**: Use `focus-visible:ring-2 focus-visible:ring-brand-red focus-visible:ring-offset-2`.
*   **Hover Styles**: Buttons must shift styles cleanly (e.g. `.btn-primary` transitions to `.hover:bg-brand-charcoal`; `.btn-secondary` transitions to `.hover:border-brand-red hover:text-brand-red`).

---

## 2. Mandatory DataAnnotations & Model Validation

All incoming form payloads must be validated on both the client and server sides.

### C# ViewModels
All ViewModels inside `ViewModels/` and `Areas/Admin/ViewModels/` directories must contain standard `System.ComponentModel.DataAnnotations` boundaries. Never pass bare entity classes straight from forms to database context:

```csharp
public class ReservationEditViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập họ tên của bạn.")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá {1} ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng cung cấp số điện thoại liên hệ.")]
    [Phone(ErrorMessage = "Số điện thoại không đúng định dạng.")]
    [RegularExpression(@"^(0[3|5|7|8|9])+([0-9]{8})$", ErrorMessage = "Số điện thoại Việt Nam không hợp lệ.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số lượng khách.")]
    [Range(1, 100, ErrorMessage = "Số lượng khách phải từ {1} đến {2} người.")]
    public int GuestCount { get; set; }
}
```

### Server Validation Blocks
Controllers must check model validity immediately prior to executing business logic. Return the view with validation feedback if the state is flawed:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(int id, ReservationEditViewModel model)
{
    if (!ModelState.IsValid)
    {
        // Re-populate layout caches or select lists if necessary
        return View(model);
    }
    
    await _reservationService.UpdateAsync(id, model);
    TempData["SuccessMessage"] = "Cập nhật đặt bàn thành công.";
    return RedirectToAction(nameof(Index));
}
```

### Localized Timezone Validation
*   **Clock Localizing Rule**: Developers are strictly prohibited from using raw `DateTime.Today`, `DateTime.Now`, or `DateTime.UtcNow` to assess localized Vietnamese calendar boundaries in business logic layers. Because hosting environments (AWS/Azure/Neon DB) operate on UTC clocks, direct server clock references cause shifting window mismatches.
*   **Required Provider**: Use `VietnamClock.GetVietnamToday(TimeProvider)` to resolve the active calendar day under the local `Asia/Ho_Chi_Minh` timezone (GMT+7). Production code should receive `TimeProvider` from dependency injection.
*   **Reservation Date Policy Boundary**: Reservation date bookability must be evaluated through `IReservationBlockedDateService` from `ReservationService`. Do not duplicate blocked-date collections or past-date checks inside controllers, Razor views, or JavaScript.
*   **Policy Precedence Rule**: Date policy precedence is `PastDate`, `BlockedDate`, then `Allowed`.
*   **DateOnly Integrity Rule**: Submitted reservation calendar dates are `DateOnly` values. Do not transform them through UTC, `DateTimeOffset`, locale-specific parsing, or server-local `.Date` conversions before policy evaluation or persistence.
*   **Frontend Advisory Rule**: Browser-side reservation date restrictions must consume the server-owned calendar-policy payload and remain advisory. `ReservationService.CreateReservationAsync` is still the final enforcement point when requests bypass JavaScript.
*   **Blocked Date Rule**: Saturday, Sunday, holidays, and future years are allowed unless the exact date exists in `BlockedReservationDates`. Do not add runtime weekend inference, hard-coded holiday lists, or a maximum open-calendar cap.
*   **Admin Policy Rule**: Authorized Admin users manage today/future blocked dates from `/Admin/Reservation/Policy`. Past submitted dates must not become active blocked dates, and cleanup must remove only rows where `Date < VietnamToday`.

---

## 3. Dynamic File Upload Architecture

Images uploaded through the administration back-office must be routed via [IImageStorageService](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Services/IImageStorageService.cs) to ensure filename sanitization, size checks, and location rules are consistently applied.

### Filename Formatting Standard
To avoid filename collisions and sanitize incoming vectors, all filenames are rewritten via `CreateSafeFileName`:
1.  **Normalize Slug**: Remove special characters, spaces, and accents. Replace spaces or consecutive hyphens with a single hyphen (e.g. `Món Ăn Ngon! 2026.png` becomes `mon-an-ngon-2026`).
2.  **Append Entropy**: Append a shortened GUID string format (`-Guid.NewGuid():N`).
3.  **Final Pattern**: Resulting path matches: `{sanitized-slug}-{guid-hex}.{extension}`.

### Storage Locations
*   **Configurable Provider**: Image routing is handled via `ImageStorageSettings.Provider`.
*   **CloudflareR2**: Supported provider for new production uploads. Files are streamed to the S3-compatible API, object keys are immutable and unique, and the database stores an absolute public URL.
*   **LocalVolume**: Supported for local development and legacy `/uploads/...` compatibility. When active, uploads are saved to an external persistent volume (e.g. `/data/uploads`) and the database stores the public relative path (e.g. `/uploads/posts/...`).
*   **Cloudinary**: Supported for legacy backward compatibility and optional future activation. Recognized Cloudinary URLs continue routing through Cloudinary delete handling.
*   **R2 Category Prefixes**: Branches -> `BranchesPrefix`; Posts -> `NewsPrefix`; Menu pages -> `MenuPagesPrefix/<MenuGroup.Slug>` for new page-image uploads; Menu group covers -> `BrandsPrefix`.
*   **Menu Page Storage Scope**: Only menu page uploads may provide a storage scope. The scope must be the persisted `MenuGroup.Slug`, validated as one safe lowercase slug segment. Do not use `MenuGroup.Name`, route text, form data, alt text, or uploaded filename to choose the folder. Existing root-level `menu-pages/<filename>` objects remain supported and must not be migrated implicitly.
*   **Upload Limits**: `ImageStorage:MaxFileSizeBytes` is the canonical storage-service limit and must remain 50 MB unless explicitly changed with documentation. Kestrel and multipart limits remain 60 MB. Do not reintroduce `MaxUploadBytes`.
*   **Validation Scope**: Extension and MIME checks are enforced, but they are not decoder or magic-byte validation. Content-signature validation remains a future hardening item.
*   **Format Constraints**: Implement WebP format standardizations for user-facing layouts (`champong-hero.webp` and `post-card.webp`) to optimize visual loading.

### Image Storage Documentation Rule

Any change to image-storage providers, configuration keys, upload limits, object-key mapping, URL resolution, managed-asset ownership, create/edit/delete behavior, deployment requirements, or migration logic must update the relevant architecture, coding, deployment, and feature-state documentation in the same change.

### Secret Configuration Rule

Real local `appsettings.json` and `appsettings.Development.json` files are intentionally ignored and untracked. They may hold local-only secrets, but they must never be force-added to Git or printed in diagnostics. Use ignored settings or .NET user-secrets locally, and Railway Variables in production.

---

## 4. Server-Side Safety Boundaries

Security checks are enforced to safeguard the site against manipulation, request forgery, and data leaks.

### Request Forgery Protection
*   **Rule**: Every action method handling `POST`, `PUT`, or `DELETE` requests **must** be marked with the `[ValidateAntiForgeryToken]` attribute.
*   **Razor Forms**: Always wrap actions inside `<form asp-action="..." method="post">` (which automatically injects hidden anti-forgery tokens) or explicitly write `@Html.AntiForgeryToken()` inside custom JavaScript fetch setups.

### Session Security & Authorizations
*   **Admin Base Controller**: All backend controllers (except Auth controllers) must inherit from `AdminBaseController` to automatically bind:
    *   `[Area("Admin")]` routing rules.
    *   `[Authorize]` filters requiring authenticated credentials.
*   **Session Validity Verification**: Standard logins utilize sliding expiration cookie setups. Session integrity is reinforced by `AdminCookieAuthenticationEvents`, checking security stamps on page transitions to disconnect revoked admin accounts instantly.
*   **Robots Limit**: Header responses inside admin portals must emit `<meta name="robots" content="noindex, nofollow" />` to block automated indexes from crawling internal routes.
*   **Anti-XSS Validation**: All outputs inside Razor files are HTML-encoded by default. `@Html.Raw(post.Content)` is allowed only inside the public Blog detail branch that first checks `post.ContentMode == PostContentMode.Html`; all other post-content output must stay encoded.
*   **Blog HTML Sanitization Boundary**: `PostContentMode.Visual` content is plain text and must not be treated as HTML. `PostContentMode.Html` content must be sanitized server-side through `IBlogHtmlSanitizer` before persistence. Never store unsanitized HTML as canonical post content, never rely on client-side sanitization, and do not sanitize HTML with regular expressions or string replacements.
*   **Blog SEO Metadata Rule**: `FocusKeyword` is editorial metadata only. Do not emit it as `<meta name="keywords">`; use explicit SEO fields and Razor encoding for head metadata. JSON-LD must be produced by a JSON serializer with safe escaping before it is written into an `application/ld+json` script.

### Input Normalization Rule
*   **Identity Normalization**: Raw customer inputs (such as phone numbers) must be normalized prior to executing in-memory cache lockups, database checks, or identity comparisons. Sanitization must run through [IdentityNormalizer.NormalizePhone()](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Services/IdentityNormalizer.cs) to strip whitespace, drop non-numeric characters, and unify country calling prefixes (e.g. converting `+84` and `84` to local `0` formatting) to prevent rate limit bypasses.

### Real-Time Admin Notification Rule
*   **SignalR Boundary**: Controllers and business services must not depend directly on `IHubContext`. Use `IAdminReservationNotifier` for Admin reservation notifications.
*   **Hub Authorization**: Admin Hubs must require the existing Admin cookie authentication scheme and must not expose anonymous endpoints.
*   **Event Naming**: SignalR event names must be centralized in `AdminReservationNotificationEvents` rather than repeated as string literals across C# code.
*   **Payload Privacy**: Real-time reservation payloads must include only operationally necessary fields. Do not send phone numbers, emails, customer notes, dining occasion notes, internal notes, API keys, cookies, or passwords.
*   **Safe DOM Rendering**: Client notification scripts must render server-originated values with DOM APIs and `textContent`; do not inject notification payload fields with `innerHTML`.
*   **Security Stamp Scope**: Security-stamp revocation is enforced on new Hub connections and reconnects. Forcibly terminating already-open WebSocket connections is outside Phase 1.
*   **Client Reconnect Scope**: Admin notification scripts must keep SignalR automatic reconnect enabled and must restart a guarded manual start loop after terminal close without registering duplicate handlers or creating overlapping connection starts.
*   **Audio Coordination Scope**: Admin notification audio may be attempted only when the persisted sound preference is enabled and the tab is visible; hidden tabs must stay silent. Keep page-level browser unlock separate from the persisted preference, and never disable that preference solely because autoplay is blocked. Use Web Locks for per-reservation cross-tab exclusion where available; older browsers may fall back to visible-tab playback with possible duplicate audio.
*   **Deployment Assets**: The vendored SignalR browser client and Admin notification MP3 live under `wwwroot` as source-controlled deployment assets and must not be replaced by serving files from `node_modules` at runtime.
