# Admin UI Map

This document maps all view files, layouts, folders, and ViewModels participating in the Admin UI `/Admin/*`.

## Admin Layout Files

| File | Purpose | Responsive Role |
|------|---------|-----------------|
| `Areas/Admin/Views/Shared/_AdminLayout.cshtml` | Core admin panel layout shell. | Defines the header, desktop sticky sidebar, notifications toaster, and scripts loading. Requires shell drawer optimization for mobile. |
| `Areas/Admin/Views/_ViewStart.cshtml` | Global Razor view start configuration. | Directs all admin views to inherit `_AdminLayout` by default. |
| `Areas/Admin/Views/_ViewImports.cshtml` | Global using imports for Views. | Loads tag helpers (`asp-controller`, `asp-action`) and shared view model namespaces. |

## Admin View Folders

| Folder | Screens | Purpose |
|--------|---------|---------|
| `Areas/Admin/Views/Auth/` | Login.cshtml | Entry authentication credentials page. |
| `Areas/Admin/Views/Dashboard/` | Index.cshtml | System summary and quick-access cards. |
| `Areas/Admin/Views/Reservation/` | Index.cshtml, Details.cshtml | Reservation tracking, moderation, and operational notes. |
| `Areas/Admin/Views/Contact/` | Index.cshtml, Details.cshtml | Customer contact submissions inbox. |
| `Areas/Admin/Views/MenuGroup/` | Index.cshtml, Create.cshtml, Edit.cshtml, Images.cshtml | Menu group categories, cover images, and flipbook page uploads. |
| `Areas/Admin/Views/Post/` | Index.cshtml, Create.cshtml, Edit.cshtml | News, announcements, and promotional blog CRUD. |
| `Areas/Admin/Views/Review/` | Index.cshtml, Create.cshtml, Edit.cshtml | Customer feedback testimonial moderation. |
| `Areas/Admin/Views/Branch/` | Index.cshtml, Create.cshtml, Edit.cshtml | Restaurant outlet directory and facility details. |
| `Areas/Admin/Views/Setting/` | Index.cshtml | Website global configurations (hotline, social links, location). |

## Admin View Files

| View File | Controller Action | Main UI Pattern | Responsive Risk |
|-----------|-------------------|-----------------|-----------------|
| `Auth/Login.cshtml` | `AuthController.Login` | Centered card form with local layout boilerplates. | **Low**: Card has `max-w-md` and margins. Works well on mobile. |
| `Dashboard/Index.cshtml` | `DashboardController.Index` | 4-column metric grid, 2-column detailed summary panel. | **Low**: Layout uses responsive breakpoints (`md:grid-cols-2`, `xl:grid-cols-4`, `xl:grid-cols-[1.1fr_0.9fr]`). |
| `Reservation/Index.cshtml` | `ReservationController.Index` | Desktop wide data table, filter/search form. | **High**: Table columns overflow. Filter form stacks vertically into 5 long stacked blocks. |
| `Reservation/Details.cshtml` | `ReservationController.Details` | 2-column metadata layout, status change form with notes. | **Medium**: Grid stacks nicely. Save/Cancel buttons need mobile wrapping polish. |
| `Contact/Index.cshtml` | `ContactController.Index` | Desktop wide data table, status filters. | **High**: Table columns overflow offscreen. Filter buttons wrap poorly. |
| `Contact/Details.cshtml` | `ContactController.Details` | 2-column metadata layout, status dropdown. | **Medium**: Symmetrical layout stacks fine. Actions require full-width touch targets. |
| `MenuGroup/Index.cshtml` | `MenuGroupController.Index` | 8-column table with images, badges, display orders, and actions. | **High**: Severe table column squeeze. Action buttons wrap into tall vertical lists. |
| `MenuGroup/Create.cshtml` | `MenuGroupController.Create` | Multi-column input form, textareas, file input dropzone. | **Medium**: Form grid layout handles stacking, but custom file dropzone needs compact text spacing. |
| `MenuGroup/Edit.cshtml` | `MenuGroupController.Edit` | Multi-column input form with current cover thumbnail. | **Medium**: Similar to Create view; current thumbnail and dropzone take up large vertical space. |
| `MenuGroup/Images.cshtml` | `MenuGroupController.Images` | Dropzone upload, selection panel, 3-column card grid with inputs and submit forms. | **Critical**: Extremely dense page. 15+ cards stack vertically on mobile. Each card has multiple forms/buttons. |
| `Post/Index.cshtml` | `PostController.Index` | 7-column table with thumbnails, categories, dates, and actions. | **High**: Large text (excerpt/title) forces table expansion. Actions and pagination cram on mobile. |
| `Post/Create.cshtml` | `PostController.Create` | Dense form, SEO fields, file input dropzone. | **Medium**: Grid handles basic column-to-row stacking. Rich text area sizing requires mobile bounds. |
| `Post/Edit.cshtml` | `PostController.Edit` | Form with existing thumbnail and file replacement dropzone. | **Medium**: Thumbnail preview and file inputs stack nicely but require clean thumb margins. |
| `Review/Index.cshtml` | `ReviewController.Index` | 5-column table with star rating icons and toggles. | **Medium**: Relatively simple columns, but actions and badges suffer on narrow screens. |
| `Review/Create.cshtml` | `ReviewController.Create` | Form inputs, numerical rating, branch select, visible checkbox. | **Low/Medium**: General inputs wrap cleanly. Submit actions need mobile centering. |
| `Review/Edit.cshtml` | `ReviewController.Edit` | Form inputs, rating, branch select, visible checkbox. | **Low/Medium**: Similar to Create view. |
| `Branch/Index.cshtml` | `BranchController.Index` | 6-column table showing thumbnails, hotline, and status badges. | **High**: Table horizontal overflow. Multiple columns force critical data out of bounds. |
| `Branch/Create.cshtml` | `BranchController.Create` | Dense multi-column form, business hours, capacity, dropzone. | **Medium/High**: Many fields in small grids (`sm:grid-cols-2`, `sm:grid-cols-3`). Stacks safely but feels cluttered. |
| `Branch/Edit.cshtml` | `BranchController.Edit` | Dense multi-column form with current branch photo. | **Medium/High**: Same dense grids as Create view; current photo cards require responsive width limits. |
| `Setting/Index.cshtml` | `SettingController.Index` | Divided sections form (Brand, Social, Location) in card layouts. | **Medium**: Grid splits fields correctly, but bottom save action button lacks mobile stretch. |

## Admin ViewModels Used by UI

The following ViewModels directly bind page data to the admin controllers, defining form structures:

| ViewModel | Used By | Important Fields for Layout/Form |
|-----------|---------|----------------------------------|
| `AdminLoginViewModel` | `Auth/Login.cshtml` | `Username`, `Password`, `RememberMe`, `ReturnUrl` (Hidden). |
| `AdminDashboardViewModel` | `Dashboard/Index.cshtml` | `TotalBranches`, `ActiveBranches`, `TotalPosts`, `TotalMessages`, `PendingReservations`, `DatabaseConnected`. |
| `ReservationIndexViewModel` | `Reservation/Index.cshtml` | `Reservations` (List), `SearchQuery` (Text), `StatusFilter` (Select), `BranchFilter` (Select). |
| `ReservationDetailViewModel` | `Reservation/Details.cshtml` | `Id` (Hidden), `CustomerName`, `PhoneNumber`, `GuestCount`, `BranchName`, `Status`, `Note` (Textarea). |
| `ContactIndexViewModel` | `Contact/Index.cshtml` | `Messages` (List), `StatusFilter` (Select). |
| `ContactDetailViewModel` | `Contact/Details.cshtml` | `Id` (Hidden), `FullName`, `PhoneNumber`, `Email`, `Subject`, `Message`, `Status` (Select). |
| `MenuGroupIndexViewModel` | `MenuGroup/Index.cshtml` | `Groups` (List), `SearchQuery` (Text), `SelectedStatus` (Select). |
| `MenuGroupFormViewModel` | `MenuGroup/Create.cshtml`, `Edit.cshtml` | `Id` (Hidden), `Name`, `Slug`, `ShortDescription`, `Description`, `CoverImageUrl`, `DisplayOrder`, `CoverImageFile`, `IsPublished`. |
| `MenuGroupImagesViewModel` | `MenuGroup/Images.cshtml` | `MenuGroupId` (Hidden), `Images` (List of PageImages), `Upload` (Sub-model with AltText, IsPublished). |
| `PostIndexViewModel` | `Post/Index.cshtml` | `Posts` (List), `SearchQuery` (Text), `SelectedCategory` (Select), `SelectedStatus` (Select), `Page`, `TotalPages`. |
| `PostCreateViewModel` / `PostEditViewModel` | `Post/Create.cshtml`, `Edit.cshtml` | `Id` (Hidden), `Title`, `Slug`, `Category`, `PublishedAt`, `Excerpt`, `Content`, `SeoTitle`, `SeoDescription`, `ThumbnailFile`, `IsPublished`. |
| `ReviewIndexViewModel` | `Review/Index.cshtml` | `Reviews` (List). |
| `ReviewCreateViewModel` / `ReviewEditViewModel` | `Review/Create.cshtml`, `Edit.cshtml` | `Id` (Hidden), `CustomerName`, `Rating`, `Content`, `BranchId`, `DisplayOrder`, `IsVisible`. |
| `BranchCreateViewModel` / `BranchEditViewModel` | `Branch/Create.cshtml`, `Edit.cshtml` | `Id` (Hidden), `Name`, `Slug`, `Address`, `District`, `City`, `Hotline`, `Email`, `OpeningTime`, `ClosingTime`, `Capacity`, `AreaSquareMeters`, `NumberOfFloors`, `Description`, `GoogleMapUrl`, `IsActive`, `DisplayOrder`, `ThumbnailFile`, `ExistingThumbnailUrl`. |
| `SettingViewModel` | `Setting/Index.cshtml` | `Id` (Hidden), `BrandName`, `Hotline`, `Slogan`, `Email`, `Address`, `GoogleMapUrl`, `FacebookUrl`, `ZaloUrl`, `TiktokUrl`. |
