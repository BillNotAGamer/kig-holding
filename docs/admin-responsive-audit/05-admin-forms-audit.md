# Admin Forms Audit

This document audits the forms present in the Admin panel and details their responsive design adjustments.

## Form Responsive Strategy Matrix

| Form | Main Problem | Recommended Layout | Complexity | Backend Binding Risk |
|------|--------------|--------------------|------------|----------------------|
| **Login Form** | None. Standard centered viewport block. | Keep current centered card format. | Low | None |
| **Reservation Status Form** | Stacked buttons inside `space-y-4` wrap well, but lack mobile stretch. | Stretch submit buttons to `w-full` on mobile. | Low | None |
| **Contact Status Form** | Identical button wrapping issues. | Stretch submit buttons to `w-full` on mobile. | Low | None |
| **MenuGroup Form** | Uses `admin-post-*` classes. Spacing on mobile feels loose. | Standardize margins and input padding. | Low | None |
| **Post Form** | Large content textarea is difficult to scroll past on mobile. | Set textarea dynamic heights and touch-scroll safeguards. | Medium | None |
| **Branch Form** | Dense sub-grids (`sm:grid-cols-3` capacity block) stack but lack vertical separation. | Add row spacing helper classes. Convert right-aligned submit to full width. | Medium | None |
| **Site Settings Form** | Separate form sections stack, but bottom save button is right-aligned. | Convert bottom actions block to full-width centered layout on mobile. | Low | None |

---

## Form: Login Form

*   **View file**: [Auth/Login.cshtml](file:///f:/Coding/Web%20development/KIG Holding/KIGHolding/Areas/Admin/Views/Auth/Login.cshtml)
*   **POST target**: `/Admin/Auth/Login`
*   **ViewModel**: `AdminLoginViewModel`
*   **Fields**:
    *   `Username` (Text input)
    *   `Password` (Password input)
    *   `RememberMe` (Checkbox)
    *   `ReturnUrl` (Hidden field)
*   **Hidden inputs**: `ReturnUrl`
*   **File inputs**: None
*   **Validation summary**: Renders above the fields if model state is invalid.
*   **Anti-forgery**: Token included via `@Html.AntiForgeryToken()`.
*   **Current layout**: Centered container with card layout, `max-w-md` width constraint.
*   **Mobile risk**: Minimal. Fits small viewports.
*   **Recommended responsive changes**: None. Keep layout intact.
*   **Must preserve**: Exact field IDs and model bindings.

---

## Form: MenuGroup Create/Edit Form

*   **View file**: [MenuGroup/Create.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/MenuGroup/Create.cshtml) & [MenuGroup/Edit.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/MenuGroup/Edit.cshtml)
*   **POST target**: `asp-action="Create"` / `asp-action="Edit"`
*   **ViewModel**: `MenuGroupFormViewModel`
*   **Fields**:
    *   `Id` (Hidden in Edit view)
    *   `Name` (Text input)
    *   `Slug` (Text input)
    *   `ShortDescription` (Textarea)
    *   `Description` (Textarea)
    *   `CoverImageUrl` (Text input)
    *   `DisplayOrder` (Number input)
    *   `CoverImageFile` (File upload inside `.admin-upload` panel)
    *   `IsPublished` (Checkbox inside shell)
*   **Hidden inputs**: `Id`
*   **File inputs**: `CoverImageFile`
*   **Validation summary**: Target tag helper `.admin-post-summary`.
*   **Anti-forgery**: `@Html.AntiForgeryToken()` active.
*   **Current layout**: Uses `lg:grid-cols-2` for name/slug and cover/display order. The rest are full-width blocks.
*   **Mobile risk**: Textareas occupy large vertical heights. Custom image preview panel sits under the dropzone, adding to the scroll length.
*   **Recommended responsive changes**:
    *   Retain column-to-row stacking.
    *   Ensure the preview image wrapper `.admin-upload__preview` displays thumbnails at `w-32 h-20` on mobile instead of stretching.
*   **Must preserve**: Enctype attribute `enctype="multipart/form-data"`.

---

## Form: Post Create/Edit Form

*   **View file**: [Post/Create.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Post/Create.cshtml) & [Post/Edit.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Post/Edit.cshtml)
*   **POST target**: `asp-action="Create"` / `asp-action="Edit"`
*   **ViewModel**: `PostCreateViewModel` / `PostEditViewModel`
*   **Fields**:
    *   `Id` (Hidden in Edit)
    *   `Title` (Text)
    *   `Slug` (Text)
    *   `Category` (Select)
    *   `PublishedAt` (DateTime input)
    *   `Excerpt` (Textarea)
    *   `Content` (Textarea, line count: 10)
    *   `SeoTitle` (Text)
    *   `SeoDescription` (Text)
    *   `ThumbnailFile` (File input inside dropzone)
    *   `IsPublished` (Checkbox)
*   **Hidden inputs**: `Id`, `ExistingThumbnailUrl`
*   **File inputs**: `ThumbnailFile`
*   **Validation summary**: `admin-post-summary`
*   **Anti-forgery**: `@Html.AntiForgeryToken()` active.
*   **Current layout**: Stacks multiple columns into grids: name/slug (`lg:grid-cols-2`), category/date (`lg:grid-cols-2`), SEO details (`lg:grid-cols-3`).
*   **Mobile risk**: The large `Content` textarea (`min-h-56` or 10 rows) catches mobile swipe gestures, trapping touch scroll on the page.
*   **Recommended responsive changes**:
    *   Standardize input spacing on mobile using `space-y-4` inside grid containers.
    *   Limit textarea maximum heights on mobile to prevent viewport capture.
*   **Must preserve**: Model property bindings.

---

## Form: Branch Create/Edit Form

*   **View file**: [Branch/Create.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Branch/Create.cshtml) & [Branch/Edit.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Branch/Edit.cshtml)
*   **POST target**: `asp-action="Create"` / `asp-action="Edit"`
*   **ViewModel**: `BranchCreateViewModel` / `BranchEditViewModel`
*   **Fields**: Address, District, City, Hotline, Email, Times, Capacity, Area, Floors, GoogleMapUrl, Description, IsActive, display order, file.
*   **Hidden inputs**: `Id`, `ExistingThumbnailUrl`
*   **File inputs**: `ThumbnailFile`
*   **Validation summary**: Active at the top.
*   **Anti-forgery**: `@Html.AntiForgeryToken()` active.
*   **Current layout**: Split screen view (`lg:grid-cols-2`). Left contains basic text fields; right holds times, capacities, map URL, description, and thumbnail dropzone.
*   **Mobile risk**: Highly cluttered interface on mobile. Sub-grids (like District/City, Hotline/Email, Hours) stack closely without spacing.
*   **Recommended responsive changes**:
    *   Introduce grid row gap separators for sub-grids below the `sm` breakpoint.
    *   Submit action bar at the bottom must be modified from `text-right` to a centered layout:
        *   Class changes: `text-center sm:text-right`.
        *   Button changes: `w-full sm:w-auto`.
*   **Must preserve**: Datatype mappings (decimal conversion for `AreaSquareMeters`, integer fields for `Capacity`).

---

## Form: Site Settings Form

*   **View file**: [Setting/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Setting/Index.cshtml)
*   **POST target**: `asp-action="Update"`
*   **ViewModel**: `SettingViewModel`
*   **Fields**: BrandName, Hotline, Slogan, Email, Address, GoogleMapUrl, Facebook, Zalo, Tiktok.
*   **Hidden inputs**: `Id`
*   **File inputs**: None
*   **Validation summary**: Active.
*   **Anti-forgery**: `@Html.AntiForgeryToken()` active.
*   **Current layout**: Grouped sections (`.admin-form-section`) containing nested grids.
*   **Mobile risk**: Section labels and Kickers stack together. The social grid (`lg:grid-cols-3`) stacks but lacks clear margins.
*   **Recommended responsive changes**:
    *   Bottom submit button must stretch to full-width:
        *   Container changes: `.admin-actions` becomes `flex-col sm:flex-row sm:justify-end`.
        *   Button changes: `w-full sm:w-auto`.
*   **Must preserve**: Field name mappings to database config records.
