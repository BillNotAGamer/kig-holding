# 11 ViewModels and Data Binding

## ViewModel List

| ViewModel | Used By | Purpose | Key Properties | Validation Attributes |
|-----------|---------|---------|----------------|-----------------------|
| `HomeIndexViewModel` | `HomeController.Index` | Home page layout | `LatestPosts`, `ActiveBranches`, `ReservationForm` | None. |
| `MenuGroupLandingViewModel` | `MenuController.Index` | Menu landing hub card list | `Groups` (list of `MenuGroupCardViewModel`) | None. |
| `MenuGroupDetailViewModel` | `MenuController.Group` | Flipbook menu page spread view | `Name`, `Slug`, `Images`, `TotalPages`, `FirstImageUrl` | None. |
| `BranchIndexViewModel` | `BranchController.Index` | Branch locator filters | `Branches`, `SelectedCity`, `SearchQuery`, `CityCounts` | None. |
| `ReservationCreateViewModel` | `ReservationController.Index` | Public reservation booking form | `CustomerName`, `PhoneNumber`, `BranchId`, `ReservationDate`, `ReservationTime`, `GuestCount` | `[Required]`, `[StringLength]`, `[Phone]`, `[Range]` (guest counts) |
| `ReservationSuccessViewModel` | `ReservationController.Success` | Booking confirmation page | `ReservationId`, `CustomerName`, `BranchName`, `ReservationDate`, `ReservationTime` | None. |
| `NewsIndexViewModel` | `NewsController.Index` | News listings filters | `Posts`, `CurrentPage`, `TotalPages`, `SelectedCategory` | None. |
| `NewsDetailViewModel` | `NewsController.Detail` | Blog detail read view | `Post`, `RelatedPosts` | None. |
| `ContactCreateViewModel` | `ContactController.Index` | Public contact submission form | `Name`, `Email`, `Subject`, `Content`, `BranchId` | `[Required]`, `[EmailAddress]`, `[StringLength]` |
| `AdminLoginViewModel` | `AuthController` (Admin) | Operator credentials form | `Username`, `Password`, `RememberMe`, `ReturnUrl` | `[Required]` |
| `MenuGroupFormViewModel` | `MenuGroupController` (Admin) | MenuGroup CRUD form | `Id`, `Name`, `Slug`, `ShortDescription`, `Description`, `CoverImageFile` | `[Required]`, `[StringLength]` |
| `MenuPageImageUploadViewModel` | `MenuGroupController` (Admin) | Drag-and-drop file upload | `MenuGroupId`, `ImageFiles` (list of `IFormFile`) | `[Required]` |
| `MenuPageImageUpdateViewModel` | `MenuGroupController` (Admin) | Edit uploader details | `Id`, `MenuGroupId`, `AltText`, `DisplayOrder`, `IsPublished` | `[Required]`, `[StringLength]` |

---

## Form Binding Audit

| Form UI | ViewModel | POST Action Target | Binding Elements | Primary Risk / Notes |
|---------|-----------|--------------------|------------------|----------------------|
| **Public Reservation Form** | `ReservationCreateViewModel` | `ReservationController.Index` | Standard form body fields | Mismatch between query branch slug vs form Guid ID resolved in controller. |
| **Quick Booking Card** | `ReservationCreateViewModel` | `ReservationController.Quick` | AJAX POST body payload | Returns validation error dictionaries serialized as JSON instead of views. |
| **Public Contact Form** | `ContactCreateViewModel` | `ContactController.Index` | Standard form body fields | Direct foreign key binding to Branches. |
| **Admin Login Form** | `AdminLoginViewModel` | `AuthController.Login` | Standard form credentials | Local path redirect check protects against external fishing redirect redirects. |
| **Create MenuGroup** | `MenuGroupFormViewModel` | `MenuGroupController.Create` | Multipart form uploader | Validates slug inputs for duplicates before saving. |
| **Page Sheets Uploader** | `MenuPageImageUploadViewModel` | `MenuGroupController.Upload` | Multi-file form payload | Custom controller loops handle and save multiple file streams. |
| **Page Sheet Update** | `MenuPageImageUpdateViewModel` | `MenuGroupController.Update` | Standard form body fields | Uses hidden fields for IDs and parent MenuGroupId. |
| **Bulk Deletion Form** | *None* (List of Guids) | `MenuGroupController.BulkDelete` | Form data list `selectedImageIds` | Validates list contains active image Guids before deleting files. |

---

## Key Data Binding Behaviors

### 1. Route Slug to ID Resolution
When a user accesses `/dat-ban?branch=truyen-thuyet-champong-quan-2`, the public controller receives the string parameter `branch`.
* The controller calls `IBranchService.GetActiveBranchesAsync()` to fetch active locations.
* It uses `ResolveSelectedBranch` to match the slug against the active branch list.
* It binds the corresponding `BranchId` Guid to the model, which pre-selects the correct dropdown option in the UI.

### 2. Multi-File Upload Binding
Inside `MenuGroupController.Upload`, the model `MenuPageImageUploadViewModel` uses the `IFormFile` type:
```csharp
public List<IFormFile> ImageFiles { get; set; } = [];
```
* The ASP.NET model binder parses multipart/form-data requests and binds them to the files list.
* The controller validates each file's size and extension type before writing the stream to the physical filesystem.

### 3. Date & Time Binding
* **Reservation Date**: Binds as a `DateOnly?` value. Forms use HTML5 date input pickers (`yyyy-MM-dd`) which map cleanly.
* **Reservation Time**: Binds as a `TimeOnly?` value. Forms use HTML5 time pickers (`HH:mm`) which map cleanly.
* **Database UTC conversions**: Custom save interceptors handle DateTimeOffset properties. Admin-submitted dates (like `PublishedAt` for news posts) are normalized to UTC via `NormalizeLocalInputToUtc` in controllers to comply with PostgreSQL timestamptz rules.
