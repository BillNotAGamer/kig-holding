# Database Schema & Data Dictionary

This document details the database schema, entity configurations, relationships, and Entity Framework Core migrations for the **Truyền Thuyết Champong** repository.

---

## 1. Active EF Core Migrations

The database schema has evolved through the following migrations:

1.  **`20260508045623_FirstDBMigration`**: Establishes basic tables for Branches, Reservations, Posts, Reviews, and Contact Messages.
2.  **`20260508052050_AddAdminUserTable`**: Introduces the `AdminUsers` table and basic site settings configuration metadata.
3.  **`20260525103020_AddMenuGroupsAndMenuPageImages`**: Creates group-based catalog capabilities for digital PDF/Flipbook menu pages.
4.  **`20260530041829_RemoveLegacyMenuItems`**: Cleans up schema by removing old `MenuItems` in favor of the unified `MenuPageImages` flipbook design.
5.  **`20260608082004_AddReservationDiningOptions`**: Integrates dining occasion checkboxes (such as Occasion/Celebration).
6.  **`20260608104636_RemoveReservationDiningGroupOptions`**: Refactors dining options to use delimited strings, avoiding junction tables.
7.  **`20260613073215_AddAdminAccountSecurityFoundation`**: Adds account lockouts, failed attempts tracking, and password timestamps.
8.  **`20260617210701_AddBranchLunchBreakHours`**: Introduces optional `LunchBreakStart` and `LunchBreakEnd` times for individual branches. (Note: Handcrafted migration; intentionally does not have a companion `.Designer.cs` file).
9.  **`20260710204624_AddBranchAllowsReservations`**: Adds per-branch online reservation availability.
10. **`20260811075846_AddPostHtmlModeAndSeoMetadata`**: Adds Blog content mode and SEO metadata fields.
11. **`20260812123110_Update`**: Snapshot alignment migration.
12. **`20260901170736_AddBlockedReservationDatePolicy`**: Creates the DB-backed reservation closure-date table and seeds the 23 remaining future legacy blocked dates. This migration is created for review and has not been applied by implementation work.
13. **`20261019052426_AddBranchReservationDatePolicies`**: Additively creates the branch/date policy table, its invariants, revision token, restricted Branch FK, and cleanup index.
14. **`20261020140141_AddBranchWeekendLunchBreakSupport`**: Adds `LunchBreakAppliesOnWeekend` boolean column to `Branches` (default `false`) and activates it for the Tân Phú branch.
15. **`20261020210843_AddBranchWeeklyGuestCapacityLimits`**: Adds `WeekdayMinGuests` and `WeekendMinGuests` nullable integer columns to `Branches`, with migration SQL setting `WeekdayMinGuests = 6` and `WeekendMinGuests = 10` for existing HCMC branches.

---

## 2. Active DbSets & Data Dictionary

The `AppDbContext` manages **12 DbSets**:

### 1. AdminUsers (`DbSet<AdminUser>`)
Stores credentials and security markers for the admin back-office dashboard.
*   **Id** (`Guid`, PK): Unique identifier.
*   **Username** (`string`): Unique username.
*   **PasswordHash** (`string`): Secure PBKDF2 hash of the password.
*   **Email** (`string?`): Admin email address.
*   **NormalizedEmail** (`string?`): Normalized email search index.
*   **EmailConfirmed** (`bool`): Email confirmation state.
*   **SecurityStamp** (`string`): Cryptographic stamp checked for session invalidation.
*   **Role** (`string`): Defaults to `"SuperAdmin"`.
*   **IsActive** (`bool`): Active state check.
*   **CreatedAt** (`DateTimeOffset`): Generation timestamp.
*   **UpdatedAt** (`DateTimeOffset`): Timestamp of the last update.

### 2. Branches (`DbSet<Branch>`)
Describes branch locations, active times, and coordinates.
*   **Id** (`Guid`, PK): Unique identifier.
*   **Name** (`string`): Branch name (e.g. "Phạm Hữu Lầu").
*   **Slug** (`string`): URL slug (Unique, e.g. "champong-quan-7").
*   **Address** (`string`): Full street address.
*   **District** / **City** (`string`): Location segmentation values.
*   **Hotline** / **Email** (`string`): Branch specific contacts.
*   **OpeningTime** / **ClosingTime** (`TimeOnly`): Active working hours.
*   **LunchBreakStart** / **LunchBreakEnd** (`TimeOnly?`): Optional branch break time windows (defaults to 14:30 - 16:30).
*   **LunchBreakAppliesOnWeekend** (`bool`): Flag determining whether lunch break hours apply on Saturdays and Sundays (enabled for Tân Phú branch, default `false`).
*   **WeekdayMinGuests** (`int?`): Minimum guests required for reservations Monday–Friday (e.g., 6 for HCMC branches).
*   **WeekendMinGuests** (`int?`): Minimum guests required for reservations Saturday–Sunday (e.g., 10 for HCMC branches).
*   **Capacity** (`int`): Maximum dining capacity.
*   **AreaSquareMeters** (`decimal?`) / **NumberOfFloors** (`int?`): Structural sizes.
*   **ThumbnailUrl** (`string`): Cloudinary/Local asset path for index displays.
*   **GoogleMapUrl** (`string`): Embed map coordinate string.
*   **IsActive** (`bool`): Toggle display on public branch finders.
*   *Relationships*: Has Many `Reservations`, `Reviews`, and `BranchReservationDatePolicies`.

### 3. Reservations (`DbSet<Reservation>`)
Manages table bookings submitted by customers.
*   **Id** (`Guid`, PK): Unique identifier.
*   **BranchId** (`Guid`, FK): Reference to parent `Branch`.
*   **CustomerName** / **PhoneNumber** (`string`): Primary customer contacts (Required).
*   **Email** (`string?`): Optional email for booking confirmations.
*   **GuestCount** (`int`): Number of seats reserved (validated in range 1-100).
*   **ReservationDate** (`DateOnly`): Reserved date. The service rejects dates before Vietnam-local today and rejects exact dates stored in `BlockedReservationDates`.
*   **ReservationTime** (`TimeOnly`): Reserved time.
*   **DiningOccasionCodes** (`string?`, max 256): Semicolon-delimited string (e.g. `family;birthday`) storing normalized semantic dining occasion codes (`family`, `friends`, `business`, `couple`, `birthday`, `anniversary`, `other`). Stored and parsed via `ReservationOptionCatalog`.
*   **DiningOccasionOtherNote** (`string?`, max 200): Customer explanation text required when `DiningOccasionCodes` contains `"other"`; cleared to `null` otherwise.
*   **Note** (`string?`): Additional customer remarks.
*   **Status** (`Enum`): Booking status (Pending, Confirmed, Seated, Cancelled, Completed).
*   **Source** (`Enum`): Origin of booking (Website, Backoffice).
*   *Relationships*: Belongs to `Branch` (required relationship).

### 3.1 BlockedReservationDates (`DbSet<BlockedReservationDate>`)
Authoritative Admin-managed reservation closure dates.
*   **Date** (`DateOnly`, PostgreSQL `date`, PK): Exact calendar date that cannot be booked. There is no surrogate `Id`.
*   **CreatedAt** (`DateTimeOffset`, PostgreSQL `timestamptz`, default `now()`): Audit timestamp for when the blocked date row was created.
*   **Policy semantics**: Saturday, Sunday, holidays, and future years are allowed unless the exact date exists in this table. The `AddBlockedReservationDatePolicy` migration seeds only the 23 future legacy closure dates at the September 2026 cutover; Admin users may later unblock or add dates through `/Admin/Reservation/Policy`.
*   **Cleanup**: `ReservationPolicyCleanupHostedService` and Admin policy access/update remove rows where `Date < VietnamToday`. Reservation correctness does not depend on physical cleanup because past dates are rejected before blocked-date lookup.

### 3.2 BranchReservationDatePolicies (`DbSet<BranchReservationDatePolicy>`)
Optional special-date restrictions scoped to one branch. No row means Allowed at the branch-policy layer.

*   **BranchId** (`Guid`, PostgreSQL `uuid`, composite PK and FK): References `Branches.Id` with `ON DELETE RESTRICT`.
*   **Date** (`DateOnly`, PostgreSQL `date`, composite PK): Exact reservation date.
*   **RuleType** (`BranchReservationRuleType`, PostgreSQL `varchar(32)`): Stored as `Closed` or `MinimumGuests`, never as an ordinal.
*   **MinGuests** (`int?`): Null for `Closed`; required in the inclusive range 1-100 for `MinimumGuests`.
*   **Version** (`Guid`, PostgreSQL `uuid`, no database default): Application-generated, non-empty EF concurrency token. Every create and successful update receives a new UUID, so a delete/recreate cannot reuse a stale editor's revision.
*   **CreatedAt / UpdatedAt** (`DateTimeOffset`, PostgreSQL `timestamptz`, default `now()`): Audit timestamps using existing entity conventions.
*   **CHECK constraint**: Permits only `(Closed, NULL)` or `(MinimumGuests, 1..100)`, thereby rejecting unknown enum strings and malformed threshold combinations at the database boundary.
*   **Indexes**: Composite primary key `(BranchId, Date)` serves exact booking lookup; `IX_BranchReservationDatePolicies_Date` supports `Date < VietnamToday` cleanup.
*   **Mutation semantics**: Create requires expected absence; update/delete require a matching UUID `Version`; successful updates replace it with a new UUID; stale or missing expected states return a typed conflict. Mutations use an explicit `READ COMMITTED` transaction and the shared date advisory lock, and never modify reservations.
*   **Cleanup**: The hosted worker physically deletes only `Date < VietnamToday`. Today's policy is retained, cleanup is idempotent, and booking correctness does not depend on worker execution.
*   **Deployment state**: Migration `20261019052426_AddBranchReservationDatePolicies` is created and passed the isolated 12-case PostgreSQL verification, but it has not been applied to staging or production. Migration-first deployment and a no-mixed-version policy window are required. Phase 2 Admin UI is implemented for owner QA; Phase 3 public reactive UX is fully operational via `reservation-policy-client.js` on both `/dat-ban` and `_BookingModal.cshtml`. Staging migration and production deployment are not authorized.

### 4. MenuGroups (`DbSet<MenuGroup>`)
Concept divisions in the restaurant's menu ecosystem (e.g. "Truyền Thuyết Champong", "Gogi Maru", "KBB Cook").
*   **Id** (`Guid`, PK): Unique identifier.
*   **Name** (`string`): Group name.
*   **Slug** (`string`): URL slug (Unique).
*   **ShortDescription** (`string`): Summary shown in menu teasers.
*   **Description** (`string?`): Full markdown content about this concept.
*   **CoverImageUrl** (`string?`): Banner image path.
*   **DisplayOrder** (`int`): Sorting weight.
*   **IsPublished** (`bool`): Toggle public visibility.
*   *Relationships*: Has Many `MenuPageImages` (menu pages).

### 5. MenuPageImages (`DbSet<MenuPageImage>`)
Actual scanned or custom designed layout pages for the digital PDF/Flipbook.
*   **Id** (`Guid`, PK): Unique identifier.
*   **MenuGroupId** (`Guid`, FK): Reference to the parent `MenuGroup`.
*   **ImageUrl** (`string`): URL path of the image.
*   **AltText** (`string?`): Accessibility alt text.
*   **DisplayOrder** (`int`): Sorting order of the pages in the flipbook.
*   **IsPublished** (`bool`): Toggle visibility of pages.
*   *Relationships*: Belongs to `MenuGroup`.

### 6. Posts (`DbSet<Post>`)
Blog articles, promotions, and announcements.
*   **Id** (`Guid`, PK): Unique identifier.
*   **Title** (`string`, max 220): Article title.
*   **Slug** (`string`, max 220, unique): Unique URL segment.
*   **Excerpt** (`string`, max 600): Short snippet.
*   **Content** (`string`, PostgreSQL `text`): Article body. `ContentMode=Visual` stores plain text rendered through the encoded paragraph path; `ContentMode=Html` stores server-sanitized HTML that the public detail page may render as raw HTML only in the HTML-mode branch.
*   **ContentMode** (`PostContentMode`, integer, default `Visual`/`0`): Authoring/storage mode. Existing rows default to Visual and are not converted to HTML.
*   **ThumbnailUrl** (`string`, max 500): Image path.
*   **Category** (`string`, max 80): News category segmentation.
*   **IsPublished** (`bool`, default false): True if active.
*   **PublishedAt** (`DateTimeOffset?`): Timestamp for publication.
*   **SeoTitle** (`string?`, max 180): Optional SEO title.
*   **SeoDescription** (`string?`, max 320): Optional meta description.
*   **FocusKeyword** (`string?`, max 120): Editorial SEO keyword; it does not generate a legacy meta-keywords tag.
*   **CanonicalUrl** (`string?`, max 500): Optional canonical override. Empty means generated `/tin-tuc/{slug}` can be used by SEO rendering.
*   **OgTitle** (`string?`, max 180): Optional Open Graph title.
*   **OgDescription** (`string?`, max 320): Optional Open Graph description.
*   **OgImageUrl** (`string?`, max 500): Optional Open Graph image URL.
*   **RobotsIndex** (`bool`, default true): Whether the published article should be indexable. `false` emits `noindex` and excludes the post from `sitemap.xml`.
*   **RobotsFollow** (`bool`, default true): Whether crawlers should follow links. `false` emits `nofollow`.

### 7. Reviews (`DbSet<Review>`)
Customer feedback and rating stars.
*   **Id** (`Guid`, PK): Unique identifier.
*   **CustomerName** (`string`): Reviewer name.
*   **AvatarUrl** (`string?`): Avatar image path.
*   **Rating** (`int`): Rating value (1-5 stars).
*   **Content** (`string`): Review commentary text.
*   **BranchId** (`Guid?`, FK): Optional branch relation.
*   **IsVisible** (`bool`): Checked to authorize dashboard admin visibility.
*   **DisplayOrder** (`int`): Sort order.
*   *Relationships*: Belongs to `Branch` (Optional).

### 8. ContactMessages (`DbSet<ContactMessage>`)
General contact or franchise inquiry forms.
*   **Id** (`Guid`, PK): Unique identifier.
*   **FullName** / **PhoneNumber** / **Email** (`string`): Submitter contacts.
*   **Subject** (`string`): Category classification (e.g. Franchise Partnership).
*   **Message** (`string`): Details text.
*   **Status** (`Enum`): Processing status (New, Read, Replied, Ignored).
*   **Notes** (`string?`): Admin internal follow-up logs.

### 9. MediaAssets (`DbSet<MediaAsset>`)
Tracks uploaded files and images inside Cloudinary/Local uploads.
*   **Id** (`Guid`, PK): Unique identifier.
*   **FileName** (`string`): Original filename.
*   **Url** (`string`): Uploaded link.
*   **Category** (`string`): System folder categorization (Branches, Menu, Posts).
*   **ContentType** (`string`): MIME type check.
*   **FileSize** (`long`): File size in bytes.

### 10. SiteSettings (`DbSet<SiteSetting>`)
System configuration and global contact overrides.
*   **Id** (`Guid`, PK): Unique identifier.
*   **SiteName** / **BrandName** (`string`): Global branding names.
*   **Slogan** (`string`): Brand subtext.
*   **Hotline** / **Email** / **Address** (`string`): Site wide contacts.
*   **GoogleMapUrl** (`string`): Main map embed.
*   **FacebookUrl** / **ZaloUrl** / **TiktokUrl** (`string`): Core social icons mapping.
*   **LogoUrl** / **FaviconUrl** (`string`): Main image resources.
