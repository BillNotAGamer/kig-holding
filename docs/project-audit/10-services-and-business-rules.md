# 10 Services and Business Rules

## Active Services Catalog

### 1. SiteSettingService
* **Interface**: `ISiteSettingService`
* **Registered in DI / Lifetime**: Yes / Scoped
* **Used by**: `HomeController`, `AboutController`, `ReservationController`, `SeoController`, `_Layout.cshtml`, `_Header.cshtml`, `_Footer.cshtml`
* **Dependencies**: `AppDbContext`, `IMemoryCache`
* **Responsibility**: Loads the singlet website configuration parameters. Caches results to optimize database calls.
* **Important Methods**: 
  * `GetSettingsAsync(cancellationToken)`: Returns the current `SiteSetting` object.
* **Business Rules**: Caches settings under `"SiteSettings_CacheKey"` for 20 minutes. Invalidates cache when settings are saved in the admin panel.
* **Risks**: None.

### 2. BranchService
* **Interface**: `IBranchService`
* **Registered in DI / Lifetime**: Yes / Scoped
* **Used by**: `HomeController`, `BranchController`, `ReservationController`, `ContactController`, `SeoController`
* **Dependencies**: `AppDbContext`
* **Responsibility**: Loads active and public-facing restaurant locations.
* **Important Methods**:
  * `GetActiveBranchesAsync(cancellationToken)`: Returns active branches sorted by `DisplayOrder`.
  * `GetBranchBySlugAsync(slug, cancellationToken)`: Returns a specific active branch.
* **Business Rules**: Filters results where `IsActive == true`. Returns results sorted by `DisplayOrder` then `Name`.

### 3. ReservationService
* **Interface**: `IReservationService`
* **Registered in DI / Lifetime**: Yes / Scoped
* **Used by**: `ReservationController` (public), `ReservationController` (admin)
* **Dependencies**: `AppDbContext`
* **Responsibility**: Handles reservation creation and detail queries.
* **Important Methods**:
  * `CreateReservationAsync(request, cancellationToken)`: Creates a new pending reservation.
  * `GetReservationByIdAsync(id, cancellationToken)`: Loads details of a booking, including the parent branch.
* **Business Rules**: 
  * Guest count must be between 1 and 100.
  * Reservation date must be greater than or equal to current local date.
  * The target branch must be active.
  * Selected booking hours must sit within the branch's `OpeningTime` and `ClosingTime`.
* **Risks**: Unused policy service. Bypasses advanced booking checks (break times, blackout zones, weekday constraints).

### 4. EmailService
* **Interface**: `IEmailService`
* **Registered in DI / Lifetime**: Yes / Scoped
* **Used by**: `ReservationController`, `ContactController`
* **Dependencies**: `IResendClient`, `IOptions<ResendSettings>`
* **Responsibility**: Coordinates transactional email notifications to restaurant staff.
* **Important Methods**:
  * `SendReservationNotificationAsync(...)` & `SendContactNotificationAsync(...)`
* **Business Rules**: Formats templates using HTML and raw text builders, sending alerts via the Resend API client.
* **Risks**: Depends on Resend network connectivity. If the Resend API key is incorrect or key limits are exceeded, notifications fail (though they fail gracefully without crashing controller submissions).

---

## Business Rule Matrix

| Business Rule | Enforcement Location | Enforced Server-Side? | Enforced Client-Side? | Primary Risk / Notes |
|---------------|----------------------|:---------------------:|:---------------------:|----------------------|
| **Booking Date in Past** | Controller & Service | **Yes** | **Yes** (Date input limits) | Low risk. Checked in both controller validation and database transaction level. |
| **Booking Guest Count Range** | `ReservationService` (1-100) | **Yes** | **Yes** (Input `min`/`max`) | Medium risk. Public view components default guests to 2. |
| **Branch Active Status** | `ReservationService` | **Yes** | *No* (Implicit dropdown filter) | Low risk. Dropdowns only populate active branches. |
| **Time within Opening Hours** | `ReservationService` | **Yes** | *No* | High risk. Users can type any value in text time inputs unless custom JS selectors restrict it. |
| **Weekday Minimum Guest Count** | `ReservationPolicyService` (Unused) | **NO** (Bypassed) | *No* | High risk. Weekday minimum guest rules are not enforced by the active reservation service. |
| **Afternoon Break Blackouts** | `ReservationPolicyService` (Unused) | **NO** (Bypassed) | *No* | High risk. Blackout hours (e.g. 14:00-16:00 in specific districts) are not currently validated. |
| **Unique Branch Slug** | `BranchController` (Admin) | **Yes** | *No* | Low risk. Admin controller slugifier appends numeric suffixes to avoid duplicates. |
| **Max Uploader File Sizes** | `MenuGroupController` (Admin) | **Yes** | **Yes** (Uploader triggers validation) | Low risk. Enforces cover size <= 10MB and sheet size <= 50MB. |
| **Post Category Validation** | `NewsCategories` | **Yes** | *No* | Low risk. Mapped using a static lookup index system. |
| **CSRF Validation** | Admin & Public POSTs | **Yes** | *No* | Low risk. Enforces `[ValidateAntiForgeryToken]` on all mutating routes. |
| **Input String Length Limits** | EF Configurations | **Yes** | **Yes** (ViewModel attributes) | Low risk. DB configurations enforce strict character limits. |
| **Email API Key fallback** | `ReservationController` | **Yes** | *No* | Low risk. Fallback email is configured as a constant if DB configurations are missing. |
