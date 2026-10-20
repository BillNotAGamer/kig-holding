# Backend Logic & Separation of Concerns

This document details the backend architectural design of the **Truyền Thuyết Champong** application, focusing on layer separation, cached service patterns, and validation gateways.

---

## 1. Separation of Concerns (Layered Design)

The application separates execution into three primary layers to guarantee testability, maintainability, and clean boundaries:

```mermaid
graph TD
    A[Public & Admin Controllers] -->|Validates Payloads| B[Business Service Layer]
    B -->|Cached Fetch / Mutation| C[EF Core AppDbContext]
    B -->|Third-Party APIs| D[Resend SDK / Cloudinary API / Cloudflare R2 S3 API]
    C -->|PostgreSQL Driver| E[(Neon Database)]
```

### Layer Responsibilities

1.  **Presentation / Routing Layer (Controllers)**:
    *   Expose endpoints for public clients and administrative screens.
    *   Coordinate requests, validate binding models, and manage UI redirection.
    *   Write session updates to cookies and notification states to `TempData`.
    *   *Constraint*: Controllers must never write raw SQL queries, direct EF Linq context filters, or execute external API calls directly.
2.  **Service Layer (Business Logic)**:
    *   Act as the exclusive source of business calculations, validation rules, formatting, and operations.
    *   Integrate external dependencies (e.g. Resend SDK inside `EmailService`, image-storage providers behind `IImageStorageService`).
    *   Handle memory cache configurations to optimize query times.
3.  **Data Access Layer (EF Core & PostgreSQL)**:
    *   Provide relational persistence via `AppDbContext`.
    *   Track modifications and write transactions to the PostgreSQL server.

---

## 2. Validation Gateways via ModelState

Before a request propagates to the business service tier, the controller checks the structural integrity of the input data using `ModelState`.

### Gatekeeper Pattern
By using MVC Model Binding, incoming HTTP request payloads are automatically parsed and matched against validation schemas. The controller evaluates `ModelState.IsValid` as a strict gatekeeper:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Index(ReservationCreateViewModel model, CancellationToken cancellationToken)
{
    // 1. Check schema & controller-level validity immediately
    ApplyControllerValidation(model);
    if (!ModelState.IsValid)
    {
        await PopulateFormMetadataAsync(model, cancellationToken);
        return View(model);
    }

    // 2. Delegate valid request payload to business service
    var result = await CreateReservationAsync(model, cancellationToken);
    if (!result.Succeeded)
    {
        AddServiceErrorsToModelState(result);
        await PopulateFormMetadataAsync(model, cancellationToken);
        return View(model);
    }

    // 3. Trigger post-commit notifications and redirect to success view
    await TrySendReservationNotificationAsync(result.ReservationId!.Value, model);
    return RedirectToAction(nameof(Success), new { id = result.ReservationId!.Value });
}
```

### Benefits of the Validation Block
*   **Database Guard**: Blocks malformed, incomplete, or corrupted records from ever reaching PostgreSQL.
*   **Resource Protection**: Prevents resource-intensive database queries or third-party email API calls on inputs that are fundamentally flawed.
*   **Consistent Client Feedback**: Generates predictable error feedback using standard Razor helper tags `<span asp-validation-for="...">`.

### Multi-Tier Server-Side Validation Chain
Once a request passes the initial `ModelState` gateway, the business service layer ([ReservationService.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Services/ReservationService.cs)) processes it through a strict sequential validation pipeline:
1. **Preliminary Policy Evaluation**: Resolves Vietnam-local today, rejects past/global blocked dates, validates the selected active/reservable branch, and evaluates its exact `(BranchId, Date)` rule against the submitted `GuestCount`.
2. **In-Memory Rate Limiting Gate**: Checks the memory cache for active locks on the normalized phone number to block spam submissions within a 10-minute window.
3. **Database Parameter Validation**: Assures validity of branch IDs, guest-count bounds, and session slots prior to storage execution. Branch capacity is not used as a booking rejection rule.
4. **Coordinated Final Policy Evaluation**: Opens an explicit `READ COMMITTED` transaction, acquires the policy date advisory lock, and performs fresh `AsNoTracking` global and branch policy reads. A rejection rolls back before the reservation is added or saved.
5. **Persistence**: Takes the existing phone advisory lock after the policy lock, creates the `Reservation`, saves, commits, stamps the rate-limit success key, and publishes post-commit notifications only after all validation succeeds.

The effective precedence is `PastDate`, `GlobalBlockedDate`, `InvalidBranch`, `InvalidGuestCount`, `BranchClosedDate`, `MinimumGuestsNotMet`, then `Allowed`. A global row cannot be overridden. Branch `MinimumGuests` compares directly with the validated/persisted `GuestCount` (1-100). No branch row means Allowed by the branch layer. Saturday, Sunday, holidays, and future years receive no inferred restriction.

The submitted `DateOnly` is evaluated directly and, when allowed, stored unchanged. Frontend date restrictions mirror active database rows for user experience only; the service layer remains authoritative if client-side checks are bypassed.

Preliminary failures return early. A rule committed during an in-flight request is caught by the locked final check; final-check rejection rolls back before `Reservations.Add`, reservation `SaveChangesAsync`, rate-limit success stamping, SignalR notification, and controller-owned email dispatch.

`BlockedReservationDates` remains the global closure collection. `BranchReservationDatePolicies` stores only exceptional branch rules (`Closed` or `MinimumGuests`); Allowed is represented by no row. Neither layer restores handwritten holiday logic. Migration `20261019052426_AddBranchReservationDatePolicies` has passed the disposable PostgreSQL Phase 1 gate but has not been applied to staging or production.

---

## 3. Cached Service Implementations

To enhance response times and minimize database workloads, key services utilize Microsoft's `IMemoryCache` to buffer static or slow-changing configurations.

### 1. Active Branch Caching ([BranchService](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Services/BranchService.cs))
*   **Key**: `branches:active`
*   **Duration**: 10 minutes absolute expiration.
*   **Query Performance**: Fetches branch lists with `.AsNoTracking()` to avoid Entity Framework change-tracker overhead.

```csharp
public async Task<IReadOnlyList<Branch>> GetActiveBranchesAsync(CancellationToken cancellationToken = default)
{
    return await _cache.GetOrCreateAsync(ActiveBranchesCacheKey, async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
        entry.Size = 1;
        return await _dbContext.Branches
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }) ?? [];
}
```

### 2. Website Configurations Caching ([SiteSettingService](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Services/SiteSettingService.cs))
*   **Key**: `site-setting:active`
*   **Duration**: 10 minutes absolute expiration (`entry.Size = 1`).
*   **Context**: Prevents loading key-value site configurations (hotline numbers, social links, footer notes) on every single page render, invalidated immediately via `InvalidateCache()` on admin edits.

### 3. News Feed Aggregations ([NewsService](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Services/NewsService.cs))
*   **Key pattern**: Versioned composite keys generated by `BuildCacheKey(scope, segments)` matching `news:{scope}:v{version}:{segments}` (e.g. `news:list:v1:take:all`, `news:page:v1:category:all:page:1:page-size:9`, `news:detail:v1:slug:{slug}`, `news:suggested-categories:v1:...`).
*   **Duration**: 5 minutes absolute / 2 minutes sliding expiration for public news cards and post details; 15 minutes absolute for suggested categories.
*   **Cache Bounding & Invalidation**: Every entry explicitly sets `entry.SetSize(1)` or `entry.Size = 1`. Invalidation increments the volatile in-memory version counter atomically via `Interlocked.Increment(ref _cacheVersion)`, instantly rendering all prior news cache keys obsolete without blocking concurrent reads.

### 3.1 Blog Content and SEO Persistence Boundary
Admin post create/edit continues to use `Areas/Admin/Controllers/PostController.cs` with EF Core-backed persistence. The Blog HTML + SEO implementation uses `PostContentMode` as the public rendering boundary:

*   **Visual mode**: `Post.Content` remains plain text. Existing posts default to `Visual` and are not rewritten into HTML.
*   **HTML mode**: submitted HTML is sanitized server-side through `IBlogHtmlSanitizer` before persistence. The original unsanitized HTML must never become the canonical stored post body.
*   **Rendering boundary**: `Views/News/Detail.cshtml` renders raw post content only in the `ContentMode=Html` branch. Visual posts still use the encoded paragraph renderer.
*   **SEO output**: `SeoTitle` and `SeoDescription` fall back to `Title` and `Excerpt`. `OgTitle`, `OgDescription`, `OgImageUrl`, `CanonicalUrl`, `RobotsIndex`, and `RobotsFollow` feed the shared layout metadata and sitemap decisions. `FocusKeyword` is editorial metadata only; the application must not emit a legacy meta-keywords tag from it.
*   **Sitemap rule**: published posts with `RobotsIndex=false` remain routable but are excluded from `sitemap.xml` and receive `noindex` robots metadata.
*   **Security rule**: do not sanitize HTML with regular expressions. Blog HTML must pass through the project-owned sanitizer abstraction backed by a maintained .NET sanitizer library.

### 4. Reservation Rate Limiting ([ReservationService](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Services/ReservationService.cs))
*   **Key Pattern**: `res_lock:phone:{NormalizedPhone}` where `{NormalizedPhone}` is normalized by [IdentityNormalizer.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Services/IdentityNormalizer.cs).
*   **Duration**: 10 minutes absolute expiration.
*   **Memory Defense**: Configured via `options.SizeLimit = 50_000` in `Program.cs` to prevent memory exhaustion, with each cache lock entry registered with a size of `1`.

---

## 4. Cache Invalidation Patterns

To prevent administrators from seeing outdated dashboard info after saving updates, services must expose cache invalidation routines.

### Manual Cache Clears
When write commands occur inside the Admin area, the respective service clears the cache keys immediately.

```csharp
// Inside BranchController.cs after successful Create or Update:
_branchService.InvalidateActiveBranchesCache();
```

Inside `BranchService.cs`, this triggers a clean-up:
```csharp
public void InvalidateActiveBranchesCache()
{
    _cache.Remove("branches:active");
}
```

This guarantees that subsequent client requests bypass the cache, query PostgreSQL for fresh records, and re-cache the updated data.

---

## 5. PostgreSQL Advisory Lock Concurrency Control

To prevent race conditions where a client submits multiple overlapping reservations simultaneously at the exact same millisecond, the application employs a transaction-bound, write-exclusive locking mechanism:

### Dual-Gate Anti-Spam & Concurrency Architecture

To eliminate both rapid client spam and concurrent millisecond race conditions, the application enforces a dual-gate protection pipeline:

1. **Gate 1: Fail-Fast In-Memory Rate Limiting**:
   * Prior to opening any database connection or transaction, the service normalizes the incoming phone number via `IdentityNormalizer.NormalizePhone` and checks `_cache.TryGetValue(phoneLockKey, out _)`.
   * If a record exists (10-minute cooldown), the request fails immediately with a rate limit message, protecting PostgreSQL connection pools from spam floods.
2. **Transaction-Scope Advisory Date Lock (Atomic Policy Coordination)**:
   * Inside an explicit `IsolationLevel.ReadCommitted` transaction, the service executes:
     ```sql
     SELECT pg_advisory_xact_lock(1263092560, date.DayNumber)
     ```
   * This locks the specific calendar date in a dedicated two-integer advisory lock namespace (`1263092560`), guaranteeing that concurrent bookings, global closure modifications, and branch policy edits on that date are strictly serialized. Fresh policy checks are re-evaluated within this locked boundary.
3. **Gate 2: Transaction-Scope Phone Advisory Lock**:
   * Next, the service serializes concurrent requests for the identical phone hash:
     ```sql
     SELECT pg_advisory_xact_lock(hashtext(@NormalizedPhone))
     ```
   * This operates in the 64-bit bigint lock space, serializing concurrent DB sessions for the same phone number across all running application instances.
4. **Persistence & Commit**:
   * The `Reservation` entity is added and persisted via `_dbContext.SaveChangesAsync()`, and the transaction commits. PostgreSQL automatically releases all transaction-scoped advisory locks on commit or rollback.
5. **Post-Commit Cache Stamping**:
   * Only after the transaction is successfully committed, Gate 1 stamps the in-memory cache:
     ```csharp
     _cache.Set(phoneLockKey, true, new MemoryCacheEntryOptions
     {
         AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
         Size = 1
     });
     ```

### Reservation policy coordination

Policy coordination uses the separate two-integer PostgreSQL advisory-lock key space implemented by `IReservationPolicyLockService`:

* Date lock: fixed namespace `1263092560` plus `DateOnly.DayNumber`.
* Global reconciliation mutex: fixed namespace `1263092563` plus key `1`.
* Booking order: begin `READ COMMITTED` transaction, date lock, fresh global/branch evaluation, existing phone lock, save, commit.
* Branch mutation order: begin `READ COMMITTED` transaction, date lock, expected-version check, generate a new UUID revision, write, commit.
* Global reconciliation order: begin `READ COMMITTED` transaction, global mutex, calculate affected dates, acquire ascending date locks, reconcile rows, commit.

Branch policy `Version` is an application-generated UUID concurrency token, not a counter. Create and every successful update generate a new non-empty value; delete/recreate therefore cannot reuse an earlier policy incarnation's revision. Matching expected revisions participate in EF concurrency predicates, while stale update/delete requests return `Conflict`. The entity identity remains the composite `(BranchId, Date)` key.

Migration `20261019052426_AddBranchReservationDatePolicies` is created but not applied to staging or production. It must be applied before deploying code that queries the new table, and old/new application versions must not concurrently serve policy-sensitive traffic. The isolated PostgreSQL verification passed all 12 cases; staging migration and production deployment remain separate, unauthorized gates.

The public `GET /dat-ban/policy` endpoint accepts one branch ID, reservation date, and guest count, validates the request, and delegates directly to `IReservationPolicyEvaluator`. Its compact response exposes the semantic status, customer-safe field/message, applicable minimum guest threshold, and when `MinimumGuestsNotMet` occurs, includes `branchName`, `branchHotline`, and `canCallHotline: true` to power the reactive hotline callout on client forms. Tier 3 evaluates city-level capacity rules for HCMC branches (min 6 guests on Mon–Fri, min 10 guests on Sat–Sun), while branches like Tân Phú also enforce weekend lunch breaks via `LunchBreakAppliesOnWeekend`. It is non-cacheable, read-only, and fail-closed: malformed input returns 400 and evaluator/database failure returns a safe 500 response, never `Allowed`. The full form and quick modal recheck this endpoint immediately before their existing POST contracts; the POSTs still rely on `ReservationService` for the authoritative locked evaluation and all operational rules.

If an Admin policy update commits before a booking acquires its date lock and performs the final read, the booking observes the new rule. If the booking holds the lock and commits first, a later restriction does not alter that reservation. Lock failures propagate and fail closed. The source protocol does not make older deployed instances participate; mixed-version deployments require a controlled rollout and relational concurrency verification.

The Phase 2 Admin editor is exposed separately at `/Admin/Reservation/BranchPolicy`; the global reconciliation endpoint is unchanged. GET month/branch navigation is read-only. POST mutations are anti-forgery protected, validate branch/date/rule/minimum values server-side, and call `IBranchReservationDatePolicyService` for one `(BranchId, Date)` only. A stale UUID maps to a Vietnamese conflict/reload response and is never retried automatically.

Before `Closed` or `MinimumGuests` is applied where reservations already exist, the controller obtains authoritative counts by status and requires an explicit signed confirmation. The protected payload binds the proposed mutation and the count fingerprint. Counts are fetched again on the confirmed POST; a changed snapshot requires confirmation again. This guard is impact acknowledgement only: it never cancels, edits, or deletes an existing reservation, and it does not broaden the established advisory-lock protocol.

---

## 5.1 Post-Commit Admin Reservation Notifications

Public website reservations publish the `AdminReservationCreated` SignalR event only after `ReservationService.CreateReservationAsync` has completed `SaveChangesAsync`, committed the PostgreSQL transaction, and stamped the in-memory rate-limit key. The notification is sent through `IAdminReservationNotifier`, not by using `IHubContext` directly from the reservation service or a controller.

Notification publishing is best-effort and independent from email dispatch. If SignalR publishing fails, the service logs the reservation ID and exception, keeps the committed reservation successful, and returns the existing success result so the public controller can continue the existing best-effort email workflow. The reservation database remains the source of truth; missed real-time events are recovered operationally through the Admin reservation list and pending dashboard count.

The server does not persist notification history or guarantee durable delivery in Phase 1. The Admin browser client handles transient disconnects with SignalR automatic reconnect plus a guarded manual restart after terminal close; if no Admin page is connected, the committed reservation remains visible through the normal Admin reservation views.

---

## 6. Image Storage Provider Flow

Admin image uploads must enter through `IImageStorageService`; controllers do not know Cloudinary, LocalVolume, AWS SDK, or Cloudflare-specific types.

```text
Admin form upload
-> controller validation
-> IImageStorageService
-> active IImageStorageProvider
-> physical LocalVolume path or Cloudflare R2 object key
-> returned public URL/path stored on the entity
-> Razor renders the stored URL/path directly
```

`ImageStorage:Provider` is parsed centrally and supports `LocalVolume`, `Cloudinary`, and `CloudflareR2`. New R2 uploads return absolute public URLs. Delete routing is based on the stored reference, not only on the current active upload provider:

* `/uploads/...` remains LocalVolume-compatible.
* Recognized Cloudinary URLs remain Cloudinary-compatible.
* Configured R2 public URLs or safe R2 object keys are deleted only when they are under the expected category prefix.
* External unmanaged URLs and static assets are ignored.

Menu page uploads add one scoped storage segment from the persisted `MenuGroup.Slug`:

```text
MenuGroup.Slug
-> validated single path segment
-> IImageStorageService.UploadAsync(file, ImageCategory.MenuPages, slug)
-> menu-pages/<slug>/<safe-unique-filename>
```

Invalid persisted slugs stop the upload before any object is written or `MenuPageImage` row is saved. Existing root-level `menu-pages/<filename>` objects remain supported for deletes, and slug renames do not move previously uploaded objects.

Upload size uses `ImageStorage:MaxFileSizeBytes` as the canonical storage-service limit. The current value is 50 MB. Kestrel and multipart transport limits remain 60 MB to allow form-data overhead. Feature-specific controller checks may be stricter: branch thumbnails are 2 MB, menu group covers are 10 MB, menu pages are 50 MB, and post thumbnails are 2 MB.
