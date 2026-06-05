# 13 Risks, Gaps, and Unused Code

## Risks and Issues Inventory

| Severity | Area | Issue | Evidence / File | Impact | Recommendation |
|----------|------|-------|-----------------|--------|----------------|
| **High** | Security / XSS | Unsanitized rich-text HTML rendering. | [Views/News/Detail.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/News/Detail.cshtml) | Operators could insert malicious scripts via admin news content fields, leading to XSS vulnerabilities. | Sanitize rich text inputs using an HTML sanitizer library before saving to the database. |
| **Medium** | Backend | Unregistered booking policy services. | [Program.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Program.cs) | Detailed validation policies (weekday guest minimums, blackout hours) are bypassed in the active reservation service. | Register `ReservationPolicyService` and `ConfiguredSpecialDateProvider` in DI, and call `ValidateAsync` inside `ReservationService`. |
| **Medium** | Assets | Missing placeholder images on disk. | `Views/About/Index.cshtml`, `Views/News/Detail.cshtml` | Older public pages reference placeholder images under `/images/placeholders/*` that are missing from disk, causing 404 errors. | Add verified placeholder images to the filesystem or replace references with existing graphics/CSS fallbacks. |
| **Medium** | Cleanup | Orphan uploader files on deletion. | [MenuGroupController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Controllers/MenuGroupController.cs) | Deleting a MenuGroup cascade deletes database records, but the physical cover and page sheet files remain on the server, causing storage bloat. | Add a loop in `MenuGroupController.Delete` to delete the physical files from the server when a group is deleted. |
| **Low** | SEO | Missing menu group sitemap entries. | [SeoController.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Controllers/SeoController.cs) | The dynamic `sitemap.xml` does not include route listings for the new menu group route `/thuc-don/nhom/{slug}`, reducing SEO indexing. | Query active `MenuGroups` in `SeoController.Sitemap` and append their routes to the XML output. |
| **Low** | Asset bloat | Obsolete Swiper JS/CSS CDN loads. | [Views/Shared/_Layout.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Shared/_Layout.cshtml) | Page weight is inflated by global Swiper CDN references even though the hero slider uses a custom vanilla script. | Remove Swiper JS/CSS tags from `_Layout.cshtml` and load them only on specific pages if needed. |
| **Low** | Code Duplication | Duplicated file upload logic. | `PostController.cs` & `MenuGroupController.cs` | Image validation and unique name generation logic are duplicated across admin controllers. | Refactor uploader helpers into a single shared service (e.g. `IMediaUploadService`). |
| **Observation** | Code quality | Empty legacy view directories. | `Areas/Admin/Views/MenuCategory`, `MenuItem` | Empty folders remain after the legacy tables were dropped, which can confuse new developers. | Delete the empty directories. |

---

## Technical Debt Breakdown

### 1. Unregistered Policy Engine
`ReservationPolicyService` contains validations for guest counts and holiday dates (based on `appsettings.json` configurations). However, because it is not registered in the DI container in `Program.cs`, it is completely bypassed. The active `ReservationService` uses simple validations and does not check for weekend holiday exceptions or afternoon district break windows.

### 2. Orphan Media Files
When an administrator deletes a news post or branch, the database record is removed. However, the physical image file is left on the server. Over time, this leads to storage bloat.

### 3. Desktop Header Accessibility
The desktop header dropdown is CSS-driven and does not dynamically update `aria-expanded` attributes. Adding a small vanilla script to manage aria states will improve screen reader accessibility.
