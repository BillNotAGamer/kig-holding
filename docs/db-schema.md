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
8.  **`20260617210701_AddBranchLunchBreakHours`**: Introduces optional `LunchBreakStart` and `LunchBreakEnd` times for individual branches.

---

## 2. Active DbSets & Data Dictionary

The `AppDbContext` manages **10 DbSets**:

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
*   **LunchBreakStart** / **LunchBreakEnd** (`TimeOnly?`): Optional branch break time windows.
*   **Capacity** (`int`): Maximum dining capacity.
*   **AreaSquareMeters** (`decimal?`) / **NumberOfFloors** (`int?`): Structural sizes.
*   **ThumbnailUrl** (`string`): Cloudinary/Local asset path for index displays.
*   **GoogleMapUrl** (`string`): Embed map coordinate string.
*   **IsActive** (`bool`): Toggle display on public branch finders.
*   *Relationships*: Has Many `Reservations` and `Reviews`.

### 3. Reservations (`DbSet<Reservation>`)
Manages table bookings submitted by customers.
*   **Id** (`Guid`, PK): Unique identifier.
*   **BranchId** (`Guid`, FK): Reference to parent `Branch`.
*   **CustomerName** / **PhoneNumber** (`string`): Primary customer contacts (Required).
*   **Email** (`string?`): Optional email for booking confirmations.
*   **GuestCount** (`int`): Number of seats reserved (validated in range 1-100).
*   **ReservationDate** (`DateOnly`): Reserved date (restricted to $\geq$ Today).
*   **ReservationTime** (`TimeOnly`): Reserved time.
*   **DiningOccasionCodes** (`string?`): Comma-delimited strings representing celebration categories (e.g. `Occasion_Birthday,Occasion_Meeting`).
*   **DiningOccasionOtherNote** (`string?`): Text filled when "Other" is selected.
*   **Note** (`string?`): Additional customer remarks.
*   **Status** (`Enum`): Booking status (Pending, Confirmed, Seated, Cancelled, Completed).
*   **Source** (`Enum`): Origin of booking (Website, Backoffice).
*   *Relationships*: Belongs to `Branch` (required relationship).

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
*   **Title** (`string`): Article title.
*   **Slug** (`string`): Unique URL segment.
*   **Excerpt** (`string`): Short snippet.
*   **Content** (`string`): Markdown format article body.
*   **ThumbnailUrl** (`string`): Image path.
*   **Category** (`string`): News category segmentation.
*   **IsPublished** (`bool`): True if active.
*   **PublishedAt** (`DateTimeOffset?`): Timestamp for publication.

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
