# 15 Next Development Roadmap

## Immediate Fixes

### 1. Fix Missing Placeholder Images
* **Task**: Add verified placeholder images on disk or replace broken references.
* **Why**: Older public pages reference static placeholder files under `/images/placeholders/*` that are missing from disk, causing 404 console errors.
* **Files Likely Involved**: `Views/About/Index.cshtml`, `Views/News/Detail.cshtml`, `Views/Shared/_Layout.cshtml`
* **Risk Level**: **Low**
* **Suggested Order**: 1st

### 2. Update dynamic sitemaps for MenuGroups
* **Task**: Append new MenuGroup detail routes to sitemap generator.
* **Why**: The dynamic `sitemap.xml` does not include route listings for the new menu group route `/thuc-don/nhom/{slug}`, reducing search engine indexing.
* **Files Likely Involved**: [SeoController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/SeoController.cs)
* **Risk Level**: **Low**
* **Suggested Order**: 2nd

---

## Backend Stabilization

### 3. Register & Connect the Reservation Policy Engine
* **Task**: Register policy services in Dependency Injection and call them during booking creation.
* **Why**: The detailed validation checks (minimum guests, blackout windows, break times) defined in `ReservationPolicyService` are currently bypassed.
* **Files Likely Involved**: [Program.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Program.cs), [ReservationService.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Services/ReservationService.cs)
* **Risk Level**: **Medium**
* **Suggested Order**: 3rd

### 4. Consolidated Media Upload Service
* **Task**: Refactor uploader logic from controllers into a shared utility service.
* **Why**: Validation rules and safe-name generation logic are duplicated across `PostController` and `MenuGroupController`.
* **Files Likely Involved**: `MenuGroupController.cs`, `PostController.cs`, `BranchController.cs`, `New IMediaUploadService`
* **Risk Level**: **Medium**
* **Suggested Order**: 4th

---

## Frontend/UI Improvements

### 5. Transition Menu Group to Book-Only Experience
* **Task**: Simplify MenuGroup details into a book-only spread layout.
* **Why**: The current page is cluttered with scroll lists and mode toggles. Moving to a book-only design will match the premium restaurant branding.
* **Files Likely Involved**: `Views/Menu/Group.cshtml`, `wwwroot/js/menu-flipbook.js`, `wwwroot/css/input.css`
* **Risk Level**: **High**
* **Suggested Order**: 5th

### 6. Remove Obsolete Swiper CDNs
* **Task**: Remove global Swiper JS and CSS tags from Layout files.
* **Why**: Reduces page load times and eliminates unused network requests since the hero carousel uses custom JS.
* **Files Likely Involved**: [Views/Shared/_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml)
* **Risk Level**: **Low**
* **Suggested Order**: 6th

---

## Security Improvements

### 7. Sanitize blog post Content HTML
* **Task**: Sanitize rich text inputs using an HTML sanitizer before saving.
* **Why**: Editorial posts render HTML raw using `@Html.Raw`, which creates XSS vulnerabilities if administrative access is compromised.
* **Files Likely Involved**: `PostController.cs` (Admin)
* **Risk Level**: **Low**
* **Suggested Order**: 7th

### 8. Physical File Cleanups on Deletion
* **Task**: Ensure uploader files are deleted from the disk when their records are deleted.
* **Why**: Deleting menu groups or news posts deletes database records but leaves orphan image files in uploads folders, causing server storage bloat.
* **Files Likely Involved**: `MenuGroupController.cs`, `PostController.cs`, `BranchController.cs`
* **Risk Level**: **Medium**
* **Suggested Order**: 8th

---

## Nice-to-Have Enhancements

### 9. Expose Dedicated Autocomplete Endpoint
* **Task**: Build a controller action that returns active branch names for search suggestions.
* **Why**: The current autocomplete scraper is limited to branches rendered in the DOM, which restricts suggestions when filters are active.
* **Files Likely Involved**: `BranchController.cs` (Public), `wwwroot/js/branch-locator.js`
* **Risk Level**: **Low**
* **Suggested Order**: 9th
