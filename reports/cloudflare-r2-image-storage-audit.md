# Cloudflare R2 Image Storage Audit

Audit date: 2026-07-13

Scope note: this was a read-only audit of repository source, configuration, migrations, views, and documentation. No application code, schema, deployment files, external services, production systems, or database records were modified.

## 1. Executive Summary

Overall readiness for Cloudflare R2: limited. The repository already has a provider abstraction, but it is still a single `ImageStorageService` implementation that only supports `LocalVolume` and `Cloudinary` in code (`Services/IImageStorageService.cs:3-6`, `Services/ImageStorageService.cs:34-179`). The checked-in configuration now binds `ImageStorage.Provider=CloudflareR2` while the implementation still throws on any provider other than `LocalVolume` or `Cloudinary` (`appsettings.json:53-71`, `appsettings.Development.json:32-50`, `Services/ImageStorageService.cs:54-60`). That mismatch alone is a pre-implementation blocker.

- Current provider architecture: one `IImageStorageService` contract, one `ImageStorageService` class, provider branching inside the class, LocalVolume upload/delete support, Cloudinary upload/delete support, no R2 implementation (`Program.cs:76-77,94`, `Services/ImageStorageService.cs:34-179`).
- Audited image-bearing entities: 7 entities, 8 image-bearing fields.
- Upload entry points found: 7 action-level entry points.
- Replacement flows found: 3 file-based replacement flows, plus 1 unmanaged manual-URL substitution path for `MenuGroup.CoverImageUrl`.
- Delete flows found: 8 user-visible delete/replace flows, plus 10 compensating cleanup call sites.
- Direct filesystem bypasses found for dynamic image writes: 0 outside `ImageStorageService`.
- Hardcoded `/uploads` dependencies found in code/views: 6 material code/view sites.

Highest-risk findings:

1. Checked-in configuration selects `CloudflareR2`, but the service implementation does not support it. Uploads will throw `InvalidOperationException` anywhere environment overrides do not replace the repo defaults (`appsettings.json:53-71`, `appsettings.Development.json:32-50`, `Services/ImageStorageService.cs:54-60`).
2. Potential plaintext secrets are present in repository files, including `appsettings.json`, `appsettings.Development.json`, and `docs/Mail service API Key.txt`. Values are intentionally omitted from this report. Rotation is recommended before any further migration work.
3. `MenuGroup.CoverImageUrl` bypasses the storage abstraction as an arbitrary string field and can point at any `/uploads/...` path or external URL (`Areas/Admin/ViewModels/MenuGroupViewModels.cs:50-55`, `Areas/Admin/Views/MenuGroup/Create.cshtml:45-78`, `Areas/Admin/Views/MenuGroup/Edit.cshtml:46-89`, `Areas/Admin/Controllers/MenuGroupController.cs:166-168,280-282`).
4. Local delete logic accepts any path under `/uploads/` regardless of category, which means an unmanaged `CoverImageUrl` like `/uploads/posts/...` can later delete unrelated uploaded assets during menu-group replacement or permanent delete (`Services/ImageStorageService.cs:206-239`).
5. Upload size contracts are inconsistent across docs, startup limits, appsettings, controllers, and the storage service. Effective limits differ by flow and some UI/controller messages do not match the actual service limit (`Program.cs:21-33`, `Options/ImageStorageSettings.cs:5-15`, `appsettings.json:69-80`, `Areas/Admin/Controllers/BranchController.cs:24-32,352-369`, `Areas/Admin/Controllers/MenuGroupController.cs:18-20,746-790`, `docs/deployment-railway.md:15-22`).
6. There is no centralized URL resolver for mixed legacy references. Compatibility currently relies on direct pass-through rendering, a news-only Cloudinary helper, and a few per-view fallbacks (`Helpers/MediaUrlHelper.cs:32-119`, `Views/Shared/_Layout.cshtml:38-42,156-169`, `Controllers/MenuController.cs:165-197`).
7. `MediaAsset` is not an active registry. It has schema and configuration, but no readers, writers, or synchronization code in the live repository (`Data/AppDbContext.cs:22`, `Data/Configurations/MediaAssetConfiguration.cs:7-28`, repo-wide search for `MediaAsset` found no runtime usage outside EF metadata and docs).

Recommended migration approach:

- Do not start with bucket wiring.
- First fix configuration and secret-handling issues, then redesign the storage contract around managed asset identity and centralized URL resolution.
- Keep all existing `/uploads/...`, Cloudinary absolute URLs, static `/images/...`, and external absolute URLs readable during the transition.
- Treat new R2 uploads as a new managed-provider path, not as a destructive cutover of old references.

Final audit verdict:

`NOT_READY_FOR_IMPLEMENTATION`

## 2. Repository and Worktree Baseline

- Root: `F:/Coding/Web development/KIG Holding/KIGHolding`
- Branch: `main` (`git status --short --branch`)
- Commit: `afaa12e86ce549ed0ae976d4fe4882b1ed539300` (`git rev-parse HEAD`)
- Recent history: `afaa12e (HEAD -> main, origin/main) Refactor homepage news section layout`, `1631b5d Repair news index editorial layout`, `47e8461 update reservation rule`, `875257a Add gated TikTok pixel and public event tracking`, `bc1fc93 add tiktok ads code`
- Initial worktree state: not clean
- Pre-existing changes before the audit:
  - ` D docs/local-development.md`
  - ` M wwwroot/uploads/.gitkeep`
- Final worktree state:
  - ` D docs/local-development.md`
  - ` M wwwroot/uploads/.gitkeep`
  - `?? reports/`
  - Local directory inspection confirmed `reports/` contains only `reports/cloudflare-r2-image-storage-audit.md`.

Repository structure observed:

- Solution file: `KIGHolding.sln`
- Project file: `KIGHolding.csproj`
- Test projects: none found in the solution or repository root file list
- Documentation directories/files: `docs/`, `Areas/Admin/README.md`
- Admin controllers: `Areas/Admin/Controllers/*.cs`
- Public controllers: `Controllers/*.cs`
- Services: `Services/*.cs`
- Options/settings classes: `Options/*.cs`
- Entities: `Models/Entities/*.cs`
- ViewModels: `ViewModels/*.cs`, `Areas/Admin/ViewModels/*.cs`
- Razor views: `Views/**/*.cshtml`, `Areas/Admin/Views/**/*.cshtml`
- Startup: `Program.cs`
- Deployment documentation: `docs/deployment-railway.md`
- Current image upload directories present in source tree: `wwwroot/uploads/.gitkeep`, plus runtime defaults mapping to `App_Data/uploads` or a configured external root (`Program.cs:149-166`, `Options/ImageStorageSettings.cs:5-8`)

## 3. Architecture Observed

Confirmed architecture from source:

- Public reads are service-backed for branches, menu groups, site settings, and news (`Services/BranchService.cs:22-68`, `Services/MenuGroupService.cs:16-86`, `Services/SiteSettingService.cs:22-38`, `Services/NewsService.cs:29-340`).
- Admin mutations for image-bearing entities are controller-driven and write directly to `AppDbContext`; they do not go through domain services for persistence (`Areas/Admin/Controllers/BranchController.cs:87-346`, `PostController.cs:150-395`, `MenuGroupController.cs:112-612`, `SettingController.cs:43-101`, `ReviewController.cs:45-140`).
- Image upload/delete is the only mutation concern abstracted behind a service, via `IImageStorageService` (`Services/IImageStorageService.cs:3-6`).
- Startup config binds `CloudinarySettings` and `ImageStorageSettings`, registers a single `ImageStorageService`, maps `/uploads` to a physical directory, and unconditionally creates that directory on startup (`Program.cs:76-95,149-166`).
- Public rendering is decentralized. Most views emit stored strings directly into `<img src="...">`, with only a few special cases:
  - news pages use `MediaUrlHelper` to add Cloudinary transformations (`Helpers/MediaUrlHelper.cs:32-119`, `Views/News/Index.cshtml:126,177,241`, `Views/News/Detail.cshtml:119,208`, `Views/Home/Index.cshtml:565,626`)
  - `_Layout.cshtml` converts relative image references to absolute OpenGraph URLs (`Views/Shared/_Layout.cshtml:38-42,156-169`)
  - `MenuController` falls back to static webroot assets if a menu group has no cover image (`Controllers/MenuController.cs:165-197`)
  - `LayoutFallbacks` injects fallback logos and other static assets (`ViewComponents/LayoutFallbacks.cs:127-217`)

Documentation status against source:

| Document | Status | Evidence |
| --- | --- | --- |
| `docs/architecture.md` | Partially outdated | Says dynamic uploads live under `wwwroot/uploads` (`docs/architecture.md:46`) but runtime upload mapping is external/root-path-based (`Program.cs:149-166`). |
| `docs/backend-logic.md` | Partially outdated | Claims controllers should not directly execute external API calls (`docs/backend-logic.md:21-29`), but admin controllers directly call `_imageStorageService.UploadAsync/DeleteAsync` and write through `AppDbContext`. |
| `docs/coding-rules.md` | Partially current | Mostly true that admin uploads route through `IImageStorageService` (`docs/coding-rules.md:82-93`), but `MenuGroup.CoverImageUrl` allows unmanaged manual URL bypasses. |
| `docs/db-schema.md` | Outdated in part | Describes `MediaAssets` as tracking uploaded files (`docs/db-schema.md:128-136`), but no runtime code reads or writes it. |
| `docs/deployment-railway.md` | Partially current | LocalVolume deployment guidance is still relevant for env overrides (`docs/deployment-railway.md:5-24`), but checked-in repo config now binds an R2-shaped `ImageStorage` section instead (`appsettings.json:53-71`). |
| `docs/feature-state.md` | Partially outdated | Says the image upload pipeline is LocalVolume-ready and current (`docs/feature-state.md:47-52`), but checked-in `ImageStorage.Provider` is `CloudflareR2` with no supporting code. |
| `Areas/Admin/README.md` | Contradictory/outdated | Says admin CRUD should not exist yet (`Areas/Admin/README.md:15`), but the repo contains active CRUD controllers and views for branch, post, menu group, review, reservations, and settings. |
| `docs/admin-roadmap.md` | Generic guide only | Useful for conventions, but not source-of-truth for current implementation. |

## 4. Image Data Inventory

| Entity.Property | Type | DB mapping | Current stored representation | Writer | Reader | Delete owner | Shared usage risk | Migration impact |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `Branch.ThumbnailUrl` | `string` non-null | `Branches.ThumbnailUrl`, max 500, required (`Data/Configurations/BranchConfiguration.cs:36`) | Mixed: seeded static `/images/...` (`Data/DbInitializer.cs:155,181,207`), uploaded relative `/uploads/branches/...` (`Services/ImageStorageService.cs:194-195`), legacy Cloudinary absolute URL possible (`Services/ImageStorageService.cs:54-111`) | `Areas/Admin/Controllers/BranchController.cs:87-150,194-277` | Public branch cards via `BranchCardViewModel.FromBranch` and `Views/Shared/Components/BranchCard/Default.cshtml:30-37`; admin list/edit views | Branch edit replacement and permanent delete (`Areas/Admin/Controllers/BranchController.cs:270-272,338-340`) | Medium if DB values are manually changed; upload flow itself generates unique names | Needs backward-compatible resolver for static/local/cloudinary; object-key storage not currently supported |
| `MenuGroup.CoverImageUrl` | `string?` nullable | `MenuGroups.CoverImageUrl`, max 500 (`Data/Configurations/MenuGroupConfiguration.cs:23`) | Mixed: null, uploaded relative `/uploads/menu-groups/covers/...`, Cloudinary absolute URL, arbitrary manual URL, arbitrary relative `/uploads/...` (`Areas/Admin/Views/MenuGroup/Create.cshtml:45-78`, `Edit.cshtml:46-89`) | `Areas/Admin/Controllers/MenuGroupController.cs:112-188,217-304` | Public menu landing/detail (`Controllers/MenuController.cs:87-100,158-180`, `Views/Menu/Index.cshtml:52-60`), admin index/edit | File-based replacement and permanent delete only; manual URL changes do not clean previous managed assets (`Areas/Admin/Controllers/MenuGroupController.cs:297-300,366-368`) | High because field is free-form and unmanaged | Must classify managed vs unmanaged references before delete/migration |
| `MenuPageImage.ImageUrl` | `string` non-null | `MenuPageImages.ImageUrl`, max 500, required (`Data/Configurations/MenuPageImageConfiguration.cs:17`) | Usually uploaded relative `/uploads/menu-pages/...`; Cloudinary absolute possible if provider switched (`Services/ImageStorageService.cs:194-195`) | `Areas/Admin/Controllers/MenuGroupController.cs:387-503` | Public menu flipbook and fallback HTML (`Views/Menu/Group.cshtml:19-28,234-240`), admin images view (`Areas/Admin/Views/MenuGroup/Images.cshtml:147-166`) | Single delete, bulk delete, menu-group permanent delete (`Areas/Admin/Controllers/MenuGroupController.cs:549-608,371-373`) | Low in normal flow because filenames are unique; no registry prevents accidental shared references | Needs R2-safe rendering and safe delete ownership metadata |
| `Post.ThumbnailUrl` | `string` non-null | `Posts.ThumbnailUrl`, max 500, required (`Data/Configurations/PostConfiguration.cs:23`) | Mixed: seeded static `/images/posts/...` (`Data/DbInitializer.cs:311`), uploaded relative `/uploads/posts/...`, Cloudinary absolute URL | `Areas/Admin/Controllers/PostController.cs:150-224,255-349` | Public news lists/details/home via `NewsService.cs:292-318`, `Views/News/Index.cshtml:126,177,241`, `Views/News/Detail.cshtml:119,208`, `Views/Home/Index.cshtml:565,626`; admin list/edit views | Post edit replacement and permanent delete (`Areas/Admin/Controllers/PostController.cs:343-346,388-390`) | Medium if values are manually changed outside app; upload flow itself uses unique names | Needs centralized resolver if moving to object keys; current direct URL storage supports mixed legacy values |
| `Review.AvatarUrl` | `string?` nullable | `Reviews.AvatarUrl`, max 500 (`Data/Configurations/ReviewConfiguration.cs:21`) | Unknown; seeds set `null` (`Data/DbInitializer.cs:280`) | No active writer found | No active reader found | No delete owner found | Unknown | Requires production-data inventory query before migration; field is currently dead in source |
| `SiteSetting.LogoUrl` | `string` non-null | `SiteSettings.LogoUrl`, max 500, required (`Data/Configurations/SiteSettingConfiguration.cs:25`) | Seeded static relative `/images/brand/logo.svg` (`Data/DbInitializer.cs:77`), may be empty via `EnsureSettingsAsync` fallback (`Areas/Admin/Controllers/SettingController.cs:82-99`) | No active admin upload or edit path for the image field | Header/footer/mobile menu/floating buttons and OG image path (`ViewComponents/LayoutFallbacks.cs:127-217`, `Views/Shared/_Layout.cshtml:38-42`, `_Header.cshtml:5-13`, `_Footer.cshtml:23-28`, `_MobileMenu.cshtml:11-13`) | No delete owner found | Medium if changed out-of-band because many public surfaces depend on it | Requires future upload UI or out-of-band migration path; existing readers already accept full URLs |
| `SiteSetting.FaviconUrl` | `string` non-null | `SiteSettings.FaviconUrl`, max 500, required (`Data/Configurations/SiteSettingConfiguration.cs:26`) | Seeded static `/favicon.ico` (`Data/DbInitializer.cs:78`) | No active writer found | No active reader found; `_Favicons.cshtml` is fully hardcoded (`Views/Shared/Partials/_Favicons.cshtml:1-27`) | No delete owner found | Low | Migration impact is low until the field is actually used; currently dead data |
| `MediaAsset.Url` | `string` non-null | `MediaAssets.Url`, max 500, required, unique (`Data/Configurations/MediaAssetConfiguration.cs:18-24`) | Unknown | No active writer found | No active reader found | No active delete owner found | Unknown | Cannot be trusted as inventory; treat as legacy/unused until proven otherwise |

`MediaAsset` assessment:

- Confirmed status: legacy/unused table in current code.
- Evidence:
  - DbSet exists (`Data/AppDbContext.cs:22`)
  - configuration and migration exist (`Data/Configurations/MediaAssetConfiguration.cs:7-28`, `Migrations/20260508045623_FirstDBMigration.cs:68-86`)
  - repo-wide runtime search found no code that creates, updates, queries, or deletes `MediaAsset` records outside EF metadata/docs
- Conclusion: `MediaAsset` is not synchronized with the entity image fields and cannot be treated as a complete upload registry for R2 migration planning.

## 5. Current Storage Configuration

Providers and settings observed:

- `ImageStorageSettings` only defines:
  - `Provider`
  - `RootPath`
  - `PublicBasePath`
  - `MaxFileSizeBytes`
  - `AllowedExtensions`
  - `AllowedContentTypes`
  - Evidence: `Options/ImageStorageSettings.cs:3-15`
- `CloudinarySettings` defines:
  - `CloudName`
  - `ApiKey`
  - `ApiSecret`
  - `FolderPrefix`
  - Evidence: `Options/CloudinarySettings.cs:3-8`

DI and startup:

- `Program.cs` binds `CloudinarySettings` and `ImageStorageSettings` and registers `IImageStorageService` to `ImageStorageService` (`Program.cs:76-95`).
- Startup always creates a physical upload root and maps `/uploads` via `PhysicalFileProvider`, regardless of the configured provider (`Program.cs:149-166`).
- Startup does not validate that `ImageStorage.Provider` is a supported runtime value; unsupported-provider failure occurs only when an upload happens (`Services/ImageStorageService.cs:54-60`).

Checked-in configuration state:

- `appsettings.json` and `appsettings.Development.json` both contain:
  - an `ImageStorage` section that is R2-shaped and sets `Provider=CloudflareR2` (`appsettings.json:53-71`, `appsettings.Development.json:32-50`)
  - an empty-string top-level section that contains the old LocalVolume keys (`appsettings.json:74-80`, `appsettings.Development.json:23-29`)
- Because `Program.cs` binds `builder.Configuration.GetSection("ImageStorage")` (`Program.cs:77,150`), the empty-string section is ignored. The effective checked-in binding is:
  - `Provider=CloudflareR2`
  - `RootPath` falls back to `ImageStorageSettings.RootPath = App_Data/uploads`
  - `PublicBasePath` falls back to `ImageStorageSettings.PublicBasePath = /uploads`
  - `MaxFileSizeBytes=5242880`
- Impact: the repository default configuration is internally inconsistent. It still maps `/uploads` to a local folder, but upload code rejects `CloudflareR2` as unsupported.

LocalVolume logic:

- Public path mapping: `/uploads` (`Program.cs:127-130,158-165`, `Options/ImageStorageSettings.cs:7`)
- Physical root resolution: absolute path if `RootPath` is rooted; otherwise `ContentRootPath + RootPath` (`Program.cs:151-153`, `Services/ImageStorageService.cs:223-228,358-364`)
- Startup creates the resolved directory (`Program.cs:155-156`)
- Local upload returns a relative public URL string (`Services/ImageStorageService.cs:181-201`)

Cloudinary legacy support:

- Package reference present (`KIGHolding.csproj:11`)
- upload support exists inside `ImageStorageService.UploadAsync` for provider `Cloudinary` (`Services/ImageStorageService.cs:58-111`)
- delete support exists for absolute Cloudinary URLs whose `public_id` can be derived from the configured folder prefix (`Services/ImageStorageService.cs:128-178,295-340`)
- news rendering adds Cloudinary transformations only for absolute Cloudinary URLs (`Helpers/MediaUrlHelper.cs:47-119`)

Upload limits actually enforced today:

| Layer | Branch | Post | MenuGroup cover | Menu pages | Evidence |
| --- | --- | --- | --- | --- | --- |
| Kestrel/FormOptions request body | 60MB | 60MB | 60MB | 60MB | `Program.cs:21-33` |
| `ImageStorageSettings.MaxFileSizeBytes` from checked-in config | 5MB | 5MB | 5MB | 5MB | `appsettings.json:69-80`, `ImageStorageService.cs:38-40` |
| Controller-side validation | 2MB | none | 10MB | 50MB | `BranchController.cs:24-32,352-369`; `MenuGroupController.cs:18-20,746-790`; no equivalent validation in `PostController.cs:148-349` |
| UI/help text | 2MB | no size text in controller, file input present | 10MB | 50MB | `Areas/Admin/Views/Branch/Create.cshtml:167-189`, `Post/Create.cshtml:69-89`, `MenuGroup/Create.cshtml:58-78`, `MenuGroup/Images.cshtml:38-55` |
| Docs | 50MB | 50MB | 50MB | 50MB | `docs/deployment-railway.md:15-22`, `docs/coding-rules.md:90-94` |

Filename strategy:

- Local uploads sanitize the base name, strip accents/special chars, lower-case it, then append a GUID and original extension (`Services/ImageStorageService.cs:431-466`)
- Cloudinary uploads keep filename semantics but force `UniqueFilename=true` and `Overwrite=false` (`Services/ImageStorageService.cs:70-77`)

Failure behavior:

- Upload failures throw `InvalidOperationException` to the caller for user-facing handling (`Services/ImageStorageService.cs:38-52,81-110,197-200`)
- Delete is best-effort:
  - local delete swallows exceptions and only logs (`Services/ImageStorageService.cs:215-244`)
  - Cloudinary delete logs errors and does not throw (`Services/ImageStorageService.cs:155-178`)

## 6. `IImageStorageService` Contract Audit

Exact current contract:

```csharp
Task<string> UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default);
Task DeleteAsync(string? imageUrlOrPath, ImageCategory category, CancellationToken cancellationToken = default);
```

Evidence: `Services/IImageStorageService.cs:3-6`

Contract behavior:

- Upload input:
  - `IFormFile file`
  - `ImageCategory category`
  - optional `CancellationToken`
- Upload output:
  - returns a single `string`
  - that string is always a renderable URL/path, not a structured object identifier
  - for local uploads: relative public path such as `/uploads/posts/...` (`Services/ImageStorageService.cs:194-195`)
  - for Cloudinary uploads: absolute `SecureUrl` string (`Services/ImageStorageService.cs:90-101`)
- Delete input:
  - one nullable string that may be a relative path, absolute URL, or arbitrary string
  - the category is required, but local delete still accepts any `/uploads/...` path under the uploads root regardless of category (`Services/ImageStorageService.cs:206-239`)
- CancellationToken support:
  - supported on both methods
  - propagated into local file copy and caller-supplied delete loops (`Services/ImageStorageService.cs:181-193,114-179`)
- Provider routing:
  - single class with provider branching
  - `LocalVolume` branch
  - `Cloudinary` branch
  - any other provider throws (`Services/ImageStorageService.cs:54-60`)
- Folder/category semantics:
  - no free-form folder/prefix argument
  - category is an enum with four cases only: `Branches`, `Posts`, `MenuPages`, `MenuGroupCovers` (`Services/ImageCategory.cs:3-9`)
- Error behavior:
  - upload throws on invalid size/content/provider or provider failure
  - delete intentionally does not throw for missing/unknown/unmanaged targets

Provider leakage:

- Controllers know `ImageCategory` and pass storage-facing categories directly (`Areas/Admin/Controllers/BranchController.cs:101-104`, `PostController.cs:181-184`, `MenuGroupController.cs:142-145,445-448`)
- Views store and re-display raw managed URLs (`Areas/Admin/Views/Branch/Edit.cshtml:172-175`, `Post/Edit.cshtml:71-74`, `MenuGroup/Edit.cshtml:60-63`, `MenuGroup/Images.cshtml:148-166`)
- News rendering contains Cloudinary-specific URL logic in a view helper (`Helpers/MediaUrlHelper.cs:47-119`)

Fit for R2:

- Current contract is not sufficient for safe object-store ownership.
- R2 can be forced into the current shape only if the application stores full public URLs and gives up object-key ownership in the contract.
- Safe R2 support requires either:
  - a redesigned contract that returns structured upload metadata including provider + object key + public URL
  - or a sidecar registry that records provider/object ownership while legacy entity fields continue storing strings

## 7. Upload Entry-Point Inventory

| Flow | Route / action | Method | Auth / anti-forgery | ViewModel file field | Validation observed | Storage prefix | DB field written | Save order | Cache invalidation | Old image delete | Classification |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Branch create | `Admin/Branch/Create` (`Areas/Admin/Controllers/BranchController.cs:85-152`) | POST | `AdminBaseController` `[Authorize]` + `[ValidateAntiForgeryToken]` (`AdminBaseController.cs:8-10`, `BranchController.cs:85-86`) | `BranchCreateViewModel.ThumbnailFile` (`BranchCreateViewModel.cs:82-83`) | ext allowlist, MIME allowlist, 2MB controller limit (`BranchController.cs:352-369`) plus service validation (`ImageStorageService.cs:38-52`) | `branches` (`ImageCategory.Branches`) | `Branch.ThumbnailUrl` (`BranchController.cs:113-137`) | upload -> create entity -> `SaveChangesAsync` -> invalidate cache (`BranchController.cs:96-150`) | yes (`BranchController.cs:150`) | n/a | Conditionally safe |
| Branch edit | `Admin/Branch/Edit` (`BranchController.cs:192-277`) | POST | Same | `BranchEditViewModel.ThumbnailFile` (`BranchEditViewModel.cs:5-8`) | same as branch create | `branches` | `Branch.ThumbnailUrl` only if new upload succeeds (`BranchController.cs:250-253`) | validate -> upload -> update entity -> save -> delete previous if new upload exists (`BranchController.cs:215-275`) | yes (`BranchController.cs:275`) | yes, file-based replacement only | Conditionally safe |
| Post create | `Admin/Post/Create` (`PostController.cs:148-224`) | POST | `[Authorize]` via base + `[ValidateAntiForgeryToken]` | `PostCreateViewModel.ThumbnailFile` (`PostViewModels.cs:87-88`) | no controller-side file validation; service-only ext/MIME/5MB validation (`ImageStorageService.cs:38-52`) | `posts` | `Post.ThumbnailUrl` (`PostController.cs:194-210`) | upload -> create entity -> save -> invalidate news cache (`PostController.cs:175-224`) | yes (`PostController.cs:223`) | n/a | Conditionally safe |
| Post edit | `Admin/Post/Edit` (`PostController.cs:253-349`) | POST | Same | `PostEditViewModel.ThumbnailFile` (`PostViewModels.cs:135-136`) | service-only ext/MIME/5MB validation | `posts` | `Post.ThumbnailUrl` only if new upload succeeds (`PostController.cs:320-323`) | upload -> update entity -> save -> invalidate cache -> delete previous if new upload exists (`PostController.cs:293-349`) | yes (`PostController.cs:343`) | yes, file-based replacement only | Conditionally safe |
| MenuGroup create cover | `Admin/MenuGroup/Create` (`MenuGroupController.cs:110-188`) | POST | `[Authorize]` via base + `[ValidateAntiForgeryToken]` | `MenuGroupFormViewModel.CoverImageFile` (`MenuGroupViewModels.cs:54-55`) or manual `CoverImageUrl` (`MenuGroupViewModels.cs:50-52`) | controller-side ext/MIME/10MB for file (`MenuGroupController.cs:762-790`), service ext/MIME/5MB after that (`ImageStorageService.cs:38-52`) | `menu-groups/covers` | `MenuGroup.CoverImageUrl` as uploaded URL or manual string (`MenuGroupController.cs:166-168`) | normalize -> optional upload -> create entity -> save (`MenuGroupController.cs:114-188`) | none | n/a | Unsafe for unmanaged manual URLs |
| MenuGroup edit cover | `Admin/MenuGroup/Edit` (`MenuGroupController.cs:215-304`) | POST | Same | `MenuGroupFormViewModel.CoverImageFile` or manual `CoverImageUrl` | same as create | `menu-groups/covers` | `MenuGroup.CoverImageUrl` as uploaded URL or manual string (`MenuGroupController.cs:280-282`) | normalize -> optional upload -> update entity -> save -> delete previous only if a new file was uploaded (`MenuGroupController.cs:253-300`) | none | yes for file replacement; no for manual string replacement | Unsafe for unmanaged manual URLs |
| Menu page batch upload | `Admin/MenuGroup/Images/{menuGroupId}/Upload` (`MenuGroupController.cs:387-503`) | POST | `[Authorize]` via base + `[ValidateAntiForgeryToken]` (`MenuGroupController.cs:387-388`) | `MenuPageImageUploadViewModel.ImageFiles` (`MenuGroupViewModels.cs:91-92`) | max 10 files, ext/MIME, controller says 50MB (`MenuGroupController.cs:410-420,746-760`), service still enforces 5MB (`ImageStorageService.cs:38-52`) | `menu-pages` | `MenuPageImage.ImageUrl` (`MenuGroupController.cs:472-480`) | upload each -> collect entities -> save all -> redirect (`MenuGroupController.cs:439-503`) | none | n/a | Conditionally safe, but limit messaging is inconsistent |

Observed bypasses of the storage abstraction:

- `MenuGroup.CoverImageUrl` manual input is a direct bypass and can store:
  - relative `/uploads/...`
  - Cloudinary absolute URL
  - other external absolute URLs
  - arbitrary strings that pass length validation
- Evidence: `Areas/Admin/Views/MenuGroup/Create.cshtml:45-78`, `Edit.cshtml:46-89`, `Areas/Admin/Controllers/MenuGroupController.cs:166-168,280-282`

Direct filesystem writes bypassing `IImageStorageService`:

- None found for dynamic image creation or replacement outside `ImageStorageService`.

## 8. Edit and Replacement Flow Inventory

### Branch thumbnail replacement

- Source: `Areas/Admin/Controllers/BranchController.cs:194-277`
- Order:
  1. validate branch model and file constraints
  2. upload new thumbnail
  3. write new URL to `branch.ThumbnailUrl`
  4. save database
  5. delete old thumbnail
  6. invalidate branch cache
- Compensation:
  - DB save failure deletes the newly uploaded thumbnail (`BranchController.cs:260-267`)
- Risks:
  - no explicit transaction across storage + DB
  - old-image delete is best-effort only
  - no concurrency token; concurrent edits can orphan a newly uploaded image
- Classification: conditionally safe

### Post thumbnail replacement

- Source: `Areas/Admin/Controllers/PostController.cs:255-349`
- Order:
  1. validate business fields
  2. upload new thumbnail
  3. write new URL to `post.ThumbnailUrl`
  4. save database
  5. invalidate news cache
  6. delete old thumbnail
- Compensation:
  - DB save failure deletes the newly uploaded thumbnail (`PostController.cs:333-340`)
- Risks:
  - same non-transactional orphan risk as branch replacement
  - controller defines file-limit constants but does not use them; actual file validation comes only from `ImageStorageService`
- Classification: conditionally safe

### Menu-group cover replacement by file upload

- Source: `Areas/Admin/Controllers/MenuGroupController.cs:217-304`
- Order:
  1. normalize form
  2. validate file if present
  3. upload new cover file
  4. write uploaded URL to `group.CoverImageUrl`
  5. save database
  6. delete previous cover only if a file was uploaded
- Compensation:
  - DB save failure deletes the newly uploaded cover (`MenuGroupController.cs:287-294`)
- Risks:
  - no explicit transaction
  - old-image delete is best-effort
  - no ownership check before deleting previous URL
- Classification: conditionally safe only for managed file-based replacement

### Menu-group cover replacement by manual `CoverImageUrl`

- Source: `Areas/Admin/Controllers/MenuGroupController.cs:276-300`
- Order:
  1. normalize manual string
  2. save new string to `group.CoverImageUrl`
  3. save database
  4. no managed cleanup of the previous image unless a new file upload occurred
- Risks:
  - replacing a managed uploaded cover with a manual URL leaves the old managed object orphaned
  - setting a manual `/uploads/...` path can later cause deletion of unrelated local assets
- Classification: unsafe

No replacement flow was found for:

- `MenuPageImage.ImageUrl`
- `Review.AvatarUrl`
- `SiteSetting.LogoUrl`
- `SiteSetting.FaviconUrl`
- `MediaAsset.Url`

## 9. Delete Flow Inventory

| Flow | Source | Order | Missing-object behavior | Storage-failure behavior | Shared/ownership risk | Path traversal protection | Cache invalidation | Classification |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Branch soft delete | `Areas/Admin/Controllers/BranchController.cs:280-298` | mark `IsActive=false` -> save -> invalidate cache | HTTP 404 if branch missing | no storage delete attempted | image remains referenced in DB but branch hidden | n/a | yes | Safe |
| Branch permanent delete | `BranchController.cs:300-345` | remove DB row -> save -> invalidate cache -> delete thumbnail | HTTP 404 if row missing; local missing file is ignored by storage service | delete logs and returns, DB delete already committed | no reference-counting; assumes single ownership | local delete stays within uploads root (`ImageStorageService.cs:228-233`) | yes | Conditionally safe |
| Post soft delete | `Areas/Admin/Controllers/PostController.cs:352-369` | mark `IsPublished=false` -> save -> invalidate cache | HTTP 404 if post missing | no storage delete attempted | image remains stored | n/a | yes | Safe |
| Post permanent delete | `PostController.cs:372-395` | remove DB row -> save -> invalidate cache -> delete thumbnail | HTTP 404 if row missing | best-effort delete only | no reference-counting | uploads-root check exists | yes | Conditionally safe |
| MenuGroup soft delete | `MenuGroupController.cs:306-323` | mark `IsPublished=false` -> save | HTTP 404 if row missing | no storage delete attempted | cover/page images remain stored | n/a | none | Safe |
| MenuGroup permanent delete | `MenuGroupController.cs:325-377` | load group+pages -> remove group (cascade page rows) -> save -> delete cover -> delete distinct page URLs | HTTP 404 if row missing | best-effort deletes only; DB already committed | no check for shared references across other rows/entities | uploads-root check exists, but any `/uploads/...` path is accepted regardless of category | none | Conditionally safe |
| Menu page single delete | `MenuGroupController.cs:549-568` | remove row -> save -> delete image | HTTP 404 if row missing | best-effort delete only | no check for shared references | uploads-root check exists | none | Conditionally safe |
| Menu page bulk delete | `MenuGroupController.cs:571-612` | remove rows -> save -> delete each stored URL | if selected rows missing, returns error and redirects | best-effort delete only | no reference-counting | uploads-root check exists | none | Conditionally safe |

Compensating cleanup delete paths:

- Branch create save failure: `Areas/Admin/Controllers/BranchController.cs:139-147`
- Branch edit save failure / old-image cleanup: `BranchController.cs:260-272`
- Post create save failure: `PostController.cs:212-220`
- Post edit save failure / old-image cleanup: `PostController.cs:333-346`
- MenuGroup create save failure: `MenuGroupController.cs:175-183`
- MenuGroup edit save failure / old-image cleanup: `MenuGroupController.cs:287-299`
- Menu page batch upload partial cleanup after per-file failure: `MenuGroupController.cs:450-468`
- Menu page batch upload DB-save cleanup: `MenuGroupController.cs:487-497`

Delete semantics and risks in `ImageStorageService`:

- Relative paths beginning with `/uploads/` or the configured `PublicBasePath` are treated as local managed assets (`Services/ImageStorageService.cs:121-125`).
- Absolute URLs are deleted only if:
  - they parse as absolute URIs
  - host contains `cloudinary.com`
  - a folder-prefixed `public_id` can be derived (`Services/ImageStorageService.cs:128-178,295-340`)
- Any other absolute external URL is ignored, which prevents accidental deletion of arbitrary external assets but also means future R2 public URLs would currently be unmanaged and undeletable.
- Local path traversal protection exists via `Path.GetFullPath` and root-prefix checks (`Services/ImageStorageService.cs:221-233`).
- Category isolation does not exist for any path starting with `/uploads/`, so cross-category deletes inside the upload root are possible if a DB field stores the wrong relative path.

## 10. Public Rendering and URL Resolution Inventory

Currently supported reference types:

1. Static application asset under `wwwroot`
   - direct view usage and seeded DB values (`Data/DbInitializer.cs:77-78,155-156,311`, `ViewComponents/LayoutFallbacks.cs:127-217`, `Views/Shared/Partials/_Favicons.cshtml:1-27`)
2. LocalVolume public path such as `/uploads/...`
   - served by static-file middleware (`Program.cs:158-165`)
   - rendered directly in public/admin views
3. Legacy Cloudinary absolute URL
   - rendered directly in views
   - news helpers transform these URLs in-place for responsive delivery (`Helpers/MediaUrlHelper.cs:47-119`)
4. External absolute URL
   - rendered directly in `<img src>` or turned into absolute OG image URL (`Views/Shared/_Layout.cshtml:156-169`)
5. Data URL
   - no evidence found
6. Future R2 object key
   - not supported by current renderers
7. Future R2 public absolute URL
   - renderable by most current views, but not manageable by delete logic

Important rendering locations:

- Branch thumbnails:
  - `ViewModels/Shared/BranchCardViewModel.cs:25-45`
  - `Views/Shared/Components/BranchCard/Default.cshtml:30-37`
  - admin list/edit views in `Areas/Admin/Views/Branch/*.cshtml`
- Menu-group covers:
  - `Controllers/MenuController.cs:87-100,158-180`
  - `Views/Menu/Index.cshtml:52-60`
  - admin list/edit views in `Areas/Admin/Views/MenuGroup/*.cshtml`
- Menu-page images:
  - serialized into JS in `Views/Menu/Group.cshtml:19-28`
  - rendered directly in fallback HTML in `Views/Menu/Group.cshtml:234-240`
  - rendered in admin images view in `Areas/Admin/Views/MenuGroup/Images.cshtml:147-166`
- Post thumbnails:
  - `Services/NewsService.cs:292-318`
  - `Views/News/Index.cshtml:126,177,241`
  - `Views/News/Detail.cshtml:8-15,117-125,192-208`
  - `Views/Home/Index.cshtml:544-570,624-630`
  - admin list/edit views in `Areas/Admin/Views/Post/*.cshtml`
- Site logos:
  - `_Layout.cshtml` OG image path (`Views/Shared/_Layout.cshtml:38-42`)
  - header/footer/mobile menu (`Views/Shared/Partials/_Header.cshtml:5-13`, `_Footer.cshtml:23-28`, `_MobileMenu.cshtml:11-13`)
  - floating buttons via `LayoutFallbacks` and `_FloatingContactButtons.cshtml:96-109`

Centralized resolver assessment:

- No general-purpose centralized image URL resolver exists.
- Existing special cases are fragmented:
  - `MediaUrlHelper` for news-only Cloudinary transformation (`Helpers/MediaUrlHelper.cs:32-119`)
  - `_Layout.cshtml` `BuildAbsoluteUrl` for OG metadata (`Views/Shared/_Layout.cshtml:156-169`)
  - `MenuController.ResolveMenuGroupCoverImage` for static menu-cover fallback (`Controllers/MenuController.cs:165-180`)
  - `LayoutFallbacks` for logo fallbacks (`ViewComponents/LayoutFallbacks.cs:127-217`)

Legacy compatibility findings:

- Existing `/uploads/...` references remain readable as long as `/uploads` remains mapped (`Program.cs:158-165`).
- Existing Cloudinary absolute URLs remain readable because most views pass them through untouched, and news-only helpers preserve non-Cloudinary URLs and already support Cloudinary URLs (`Helpers/MediaUrlHelper.cs:49-58`).
- Static `/images/...` and `/favicon.ico` references remain readable via `wwwroot`.
- Arbitrary external absolute URLs are rendered today if present in the DB.
- Future object-key-only R2 references would currently break because the app expects a renderable URL string in entity fields.

## 11. Hardcoded Storage and Filesystem Dependencies

| File / lines | Dependency | Impact |
| --- | --- | --- |
| `Program.cs:127-130` | hardcoded `/uploads` cache rule | existing middleware behavior assumes the local uploads request path remains `/uploads` |
| `Program.cs:149-166` | physical static-file mapping rooted at `ImageStorageSettings.RootPath` | current runtime still depends on a local filesystem mount even when `Provider=CloudflareR2` |
| `Services/ImageStorageService.cs:121-125,206-239` | hardcoded `/uploads/` delete handling and local path resolution | cross-category local deletes are possible inside the uploads root |
| `Services/ImageStorageService.cs:181-201,356-364` | local physical folder creation/writes | dynamic image uploads are not stateless under LocalVolume |
| `Controllers/MenuController.cs:183-197` | direct `WebRootPath` file existence check | menu cover fallback depends on local static files, though this is unrelated to dynamic uploaded assets |
| `Areas/Admin/Views/MenuGroup/Create.cshtml:46` | hardcoded `/uploads/...` placeholder | admin UX teaches users to enter storage-path-like values directly |
| `Areas/Admin/Views/MenuGroup/Edit.cshtml:47` | same as above | same unmanaged-path risk |
| `Areas/Admin/Views/Branch/Create.cshtml:171` | outdated `wwwroot/uploads/branches/` hint | contradicts runtime external-root mapping and may confuse operators |
| `KIGHolding.csproj:30-35` | `wwwroot\uploads\posts` folder entry | source tree still carries a repository-side uploads folder convention even though runtime local uploads target an external path |

Dynamic-image filesystem bypasses:

- No direct `FileStream`, `File.Write`, or `File.Delete` paths were found in admin/public controllers outside `ImageStorageService`.
- The only direct dynamic-image filesystem writer/deleter is `ImageStorageService` itself (`Services/ImageStorageService.cs:181-239`).

Non-image-related filesystem usage that should not be removed as part of R2 migration:

- data protection keys under `App_Data/DataProtectionKeys` (`Program.cs:64-68`)
- design-time config base path in `Data/AppDbContextFactory.cs:11-17`
- `MenuController.StaticWebFileExists` for static fallback assets (`Controllers/MenuController.cs:183-197`)

## 12. Database and Transaction Findings

Confirmed transaction boundaries:

- No explicit EF Core transaction is used in the image-bearing admin create/edit/delete flows for branch, post, menu group, or menu pages.
- Each controller relies on a single `SaveChangesAsync` call, which uses EF Core’s normal per-save transaction behavior only for the database work.
- External storage calls occur:
  - before `SaveChangesAsync` on create/edit uploads
  - after `SaveChangesAsync` on delete or old-image cleanup

Consequences:

- Upload latency does not hold database locks, because upload happens before `SaveChangesAsync`.
- Delete latency also does not hold database locks, because delete happens after `SaveChangesAsync`.
- The tradeoff is non-atomicity:
  - DB save can fail after upload, leaving a new object behind unless compensation succeeds
  - storage delete can fail after DB commit, leaving an orphaned object

Compensation status by flow:

- Branch create/edit: compensation exists for uploaded-new-file DB failure (`BranchController.cs:139-147,260-267`)
- Post create/edit: compensation exists for uploaded-new-file DB failure (`PostController.cs:212-220,333-340`)
- MenuGroup create/edit cover: compensation exists for uploaded-new-file DB failure (`MenuGroupController.cs:175-183,287-294`)
- Menu page batch upload: compensation exists for partial upload failure and DB save failure (`MenuGroupController.cs:450-468,487-497`)
- Permanent deletes: no compensation if storage delete fails after DB commit

Concurrency findings:

- No optimistic concurrency tokens or row-version fields were found in the entities or EF configurations.
- Repeated POST protection is limited to anti-forgery. There are no idempotency tokens, duplicate-submit guards, or compare-and-swap checks for image replacement flows.
- Concurrent replacement can orphan newly uploaded objects:
  - example: two editors upload two different replacements from the same initial row state; both saves can succeed and both delete only the original image
- Shared-object protection is absent; delete code assumes the current row is the only owner of a managed asset

Why PostgreSQL cannot make object-store operations atomic:

- PostgreSQL transactions only control database state.
- R2/Cloudinary/local filesystem object operations occur in external systems outside the DB transaction boundary.
- The only safe pattern is compensation:
  - upload new object
  - commit DB reference
  - best-effort old-object delete
  - or registry-backed delayed cleanup

## 13. Cache Invalidation Findings

| Module | Cache present | Image mutation invalidation | Timing | Notes |
| --- | --- | --- | --- | --- |
| Branch | yes, `branches:active` (`Services/BranchService.cs:10-35`) | create/edit/delete/permanent delete call `InvalidateActiveBranchesCache()` (`Areas/Admin/Controllers/BranchController.cs:150,275,294,336`) | after successful DB save | branch image changes are covered |
| News/Post | yes, versioned memory cache (`Services/NewsService.cs:15-18,188-190,336-340`) | create/edit/delete/permanent delete call `InvalidateNewsCache()` (`Areas/Admin/Controllers/PostController.cs:223,343,366,386`) | after successful DB save | news image changes are covered |
| Site settings | yes, `site-setting:active` (`Services/SiteSettingService.cs:10-33`) | settings update calls `InvalidateCache()` (`Areas/Admin/Controllers/SettingController.cs:67-68`) | after successful DB save | no image upload UI exists for logo/favicon, but text settings cache is correct |
| Menu groups / menu pages | no dedicated cache | none required in current source | n/a | public menu reads query DB directly (`Services/MenuGroupService.cs:16-86`) |
| Reviews | no image lifecycle in source | n/a | n/a | `Review.AvatarUrl` is unused |

Browser/CDN cache implications:

- `/uploads` responses are cached for 1 day (`Program.cs:127-130,162-165`)
- static `/css`, `/js`, `/images`, `/lib` responses are cached for 1 hour unless versioned (`Program.cs:133-145`)
- current managed uploads use unique filenames locally and `UniqueFilename=true` with `Overwrite=false` on Cloudinary, which is the safer immutable-key pattern (`Services/ImageStorageService.cs:74-77,439`)
- manual `CoverImageUrl` values can still point to mutable URLs with unknown cache behavior

## 14. Security Findings

### Critical

1. Potential plaintext secrets are committed in repository-managed files.
   - Evidence:
     - `appsettings.json:3,47,57-58`
     - `appsettings.Development.json:3,18,36-37`
     - `docs/Mail service API Key.txt`
   - Values are intentionally omitted from this report.
   - Impact: credential disclosure and unsafe audit/implementation baseline.
   - Required mitigation: rotate exposed credentials and move them out of repo-managed files before further migration work.

2. Checked-in `ImageStorage.Provider=CloudflareR2` does not match the implemented provider set.
   - Evidence:
     - `appsettings.json:53-71`
     - `appsettings.Development.json:32-50`
     - `Services/ImageStorageService.cs:54-60`
   - Impact: upload runtime failure in any environment that uses repo defaults or partial overrides.
   - Required mitigation: restore a supported provider default or implement fail-fast validation before runtime.

### High

1. Manual `MenuGroup.CoverImageUrl` can trigger cross-category deletion of unrelated local uploads.
   - Evidence:
     - `Areas/Admin/Views/MenuGroup/Create.cshtml:46`
     - `Areas/Admin/Views/MenuGroup/Edit.cshtml:47`
     - `Areas/Admin/Controllers/MenuGroupController.cs:166-168,280-282,366-368`
     - `Services/ImageStorageService.cs:206-239`
   - Impact: a menu group cover set to `/uploads/posts/...` or another managed local path can later delete an unrelated post/branch/menu-page asset.

2. File validation relies on extension, browser-reported MIME type, and size only.
   - Evidence:
     - `Services/ImageStorageService.cs:38-52`
     - `Areas/Admin/Controllers/BranchController.cs:352-369`
     - `Areas/Admin/Controllers/MenuGroupController.cs:746-790`
   - Impact: no magic-byte validation, no decoder verification, no dimension check, no decompression-bomb protection, and no SVG/script sanitization path.

### Medium

1. Effective upload limits are inconsistent and misleading across modules.
   - Evidence: see Section 5 table.
   - Impact: operators can be told 10MB or 50MB while service rejects >5MB; behavior differs by module.

2. Managed asset ownership is not tracked.
   - Evidence:
     - no runtime `MediaAsset` usage
     - delete paths accept URL strings only
   - Impact: safe shared-reference protection and future R2 object-key ownership cannot be guaranteed.

3. No concurrency tokens exist on image-bearing entities.
   - Evidence:
     - no row-version fields in entity/configuration files
     - replacement flows rely on last-write-wins controller logic
   - Impact: concurrent edits can orphan new uploads.

4. `MenuGroup.CoverImageUrl` manual replacement leaves old managed covers orphaned.
   - Evidence: `Areas/Admin/Controllers/MenuGroupController.cs:280-300`
   - Impact: switching from an uploaded managed cover to a manual string never deletes the previous managed object.

5. Future R2 public URLs would render, but current delete logic would treat them as unmanaged.
   - Evidence: `Services/ImageStorageService.cs:128-132`
   - Impact: future provider cutover would create undeleted R2 objects unless delete semantics change.

### Low

1. `SiteSetting.FaviconUrl` is stored but ignored by public rendering.
   - Evidence:
     - field exists in entity/configuration (`Models/Entities/SiteSetting.cs:16-17`, `Data/Configurations/SiteSettingConfiguration.cs:25-26`)
     - `_Favicons.cshtml` is hardcoded (`Views/Shared/Partials/_Favicons.cshtml:1-27`)

2. Branch admin UI still says files are stored under `wwwroot/uploads/branches`.
   - Evidence: `Areas/Admin/Views/Branch/Create.cshtml:171`
   - Impact: documentation/UI confusion for operators.

### Informational

1. Delete operations are intentionally non-blocking and best-effort.
   - Evidence: `Services/ImageStorageService.cs:155-178,215-244`
   - Impact: successful HTTP responses do not guarantee object deletion.

2. Menu page image URLs are serialized into client-side JSON untouched.
   - Evidence: `Views/Menu/Group.cshtml:19-28`
   - Impact: future resolver work must preserve full-URL compatibility in JavaScript payloads too.

## 15. Test Coverage Matrix

Repository finding: no test projects or test source files were found. Existing coverage for all scenarios below is effectively none.

| Scenario | Existing coverage | Missing coverage | Recommended future test |
| --- | ---: | ---: | --- |
| 1. Valid upload to R2 | None | Yes | Integration test with fake S3-compatible endpoint and structured upload result assertions |
| 2. Invalid extension | None | Yes | Controller/service validation test rejecting unsupported extension |
| 3. MIME mismatch | None | Yes | Validation test where extension is allowed but content type is not |
| 4. Oversized upload | None | Yes | Per-module tests covering branch/post/menu cover/menu page limits |
| 5. Unique object-key generation | None | Yes | Provider unit test ensuring collision-resistant keys |
| 6. Prefix normalization | None | Yes | Unit test for provider/category prefix joining rules |
| 7. Upload succeeds and DB save succeeds | None | Yes | Mutation integration test asserting DB reference and object presence |
| 8. Upload succeeds and DB save fails | None | Yes | Compensation test ensuring uploaded object cleanup is attempted |
| 9. Replacement succeeds | None | Yes | Edit-flow integration test asserting old object delete after DB commit |
| 10. Replacement upload fails | None | Yes | Edit-flow test asserting DB reference remains unchanged |
| 11. Replacement DB save fails | None | Yes | Compensation test ensuring new upload is deleted and old reference retained |
| 12. Old-object deletion fails | None | Yes | Replacement/permanent-delete test asserting DB success but orphan warning/log path |
| 13. Object already missing during delete | None | Yes | Delete test asserting idempotent/no-throw storage behavior |
| 14. LocalVolume legacy URL rendering | None | Yes | View/renderer test for `/uploads/...` references |
| 15. Cloudinary legacy URL rendering | None | Yes | News rendering test for transformed Cloudinary URLs and generic pass-through elsewhere |
| 16. External URL rendering | None | Yes | Rendering test for arbitrary absolute URLs |
| 17. Static `/images/...` rendering | None | Yes | Rendering/fallback test for seeded static paths |
| 18. R2 object-key rendering | None | Yes | Resolver test proving object-key-only references resolve correctly |
| 19. R2 absolute URL rendering | None | Yes | Rendering test for fully-qualified R2 public URLs |
| 20. Concurrent image replacement | None | Yes | Concurrency integration test showing orphan prevention or expected compensation |
| 21. Shared-image deletion protection | None | Yes | Registry/ownership test preventing delete while another row still references the asset |
| 22. Cache invalidation after mutation | None | Yes | Branch/news/site-settings cache invalidation integration tests |
| 23. Missing R2 configuration in production | None | Yes | Startup/options validation test for fail-fast config errors |
| 24. Unknown storage provider | None | Yes | Startup or service test asserting clear failure before admin upload use |
| 25. CancellationToken propagation | None | Yes | Unit/integration test proving upload/delete honor cancellation |

## 16. Legacy Asset Compatibility Assessment

`/uploads/...`

- Supported today through static-file middleware (`Program.cs:158-165`)
- Rendered directly in public and admin views
- Must remain mapped and readable until every DB field has been migrated and verified

Cloudinary absolute URLs

- Supported today as ordinary `<img src>` values in most of the app
- News pages apply Cloudinary-specific transformations through `MediaUrlHelper` (`Helpers/MediaUrlHelper.cs:47-119`)
- Delete support exists only for URLs under a `cloudinary.com` host with a derivable folder-prefixed `public_id` (`Services/ImageStorageService.cs:128-178,295-340`)
- Must remain readable until deliberate migration copies and verifies those assets

Static `wwwroot` assets

- Still actively used by seed data and fallbacks:
  - `Branch.ThumbnailUrl` seeds (`Data/DbInitializer.cs:155,181,207`)
  - `Post.ThumbnailUrl` seeds (`Data/DbInitializer.cs:311`)
  - header/footer/mobile-menu logo fallbacks (`ViewComponents/LayoutFallbacks.cs:127-217`)
  - hardcoded favicons (`Views/Shared/Partials/_Favicons.cshtml:1-27`)
- Must remain readable regardless of dynamic-storage migration

External absolute URLs

- Current views render them directly if stored
- Current delete logic ignores them, which is safe from accidental remote deletion but means they are unmanaged

Future R2 references

- Full public R2 URLs:
  - renderable today in most locations
  - not safely deletable/ownable by the current `DeleteAsync(string urlOrPath, ...)` contract
- Object keys only:
  - not renderable today because there is no centralized resolver

Transition requirement:

- During migration, the application must continue to read:
  - existing `/uploads/...`
  - existing absolute Cloudinary URLs
  - static `/images/...` and `/favicon.ico`
  - existing external URLs
- Only new uploads should move to R2 until old references are copied and verified.

## 17. Recommended Target Architecture

Recommended direction:

- Keep the idea of `IImageStorageService`, but redesign the contract semantics.
- Replace the single provider-branching implementation with provider-specific implementations behind a strategy/factory or keyed DI registration.

Recommended upload result structure:

```csharp
public sealed record StoredImageReference(
    string Provider,
    string ObjectKey,
    string PublicUrl,
    string? LegacySourceUrl,
    string ContentType,
    long SizeInBytes,
    bool IsManaged);
```

Why:

- the current `string` return value loses provider identity and object ownership
- R2 delete should target object keys, not infer behavior from rendered URLs
- mixed legacy URLs can still be preserved in existing entity fields during transition

Recommended stored representation:

- Short-term compatibility path:
  - continue storing renderable strings in existing entity URL fields so legacy pages do not break
  - add a managed-asset registry that maps provider/object key/public URL/owner
- Long-term preferred path:
  - object key + provider metadata as canonical managed identity
  - derived public URL resolved centrally

Object-key vs full-URL tradeoffs:

- Full URL only:
  - easiest for existing views
  - weak ownership semantics
  - fragile delete logic
- Object key + provider:
  - stronger ownership
  - safer delete
  - requires resolver
- Recommendation:
  - keep existing URL columns backward-compatible during migration
  - treat registry/provider-key metadata as the source of truth for new managed assets

Public URL resolver recommendation:

- Introduce one centralized resolver with rules for:
  - `/uploads/...`
  - static `/images/...` and `/favicon.ico`
  - Cloudinary absolute URLs
  - external absolute URLs
  - future R2 object keys
  - future R2 absolute URLs

Provider ownership detection:

- Never infer “managed by us” solely from `StartsWith("/uploads")` or host checks.
- Determine management from registry/provider metadata, or at minimum from validated provider-tagged references.

Safe delete rules:

- Delete only assets that are known-managed and still owned by the current entity/field mutation.
- Never delete:
  - arbitrary external URLs
  - manual user-entered URLs
  - relative `/uploads/...` strings whose ownership is not proven
- For manual URL fields, default to unmanaged/no-delete unless explicitly imported into a registry.

Compensation behavior:

- upload new object
- save DB reference
- enqueue or execute best-effort old-object delete after commit
- on DB failure, delete the new object if and only if it is known-managed

Cache behavior:

- keep branch/news/site-setting invalidation after successful persistence
- central resolver output should not be cached independently from the underlying DB reference unless keyed by object version

Immutable key strategy:

- continue using unique, non-overwriting object keys
- do not overwrite the same object key for image replacement

Configuration validation:

- fail fast at startup if:
  - provider is unsupported
  - required provider-specific settings are missing
  - legacy local provider requires `RootPath`/`PublicBasePath` and they are absent

Logging without secrets:

- log provider names, categories, object keys, and redacted booleans for config presence
- never log access keys, secrets, connection strings, or full signed URLs

## 18. Proposed Implementation Phases

### Phase 1 - Storage contract and options foundation

- Scope: clean up the configuration contract, fail-fast validation, and contract semantics; no provider switch yet
- Files likely affected: `Options/ImageStorageSettings.cs`, `Program.cs`, `Services/IImageStorageService.cs`, `Services/ImageStorageService.cs` or its replacement implementations
- Preconditions: rotate repo-exposed secrets and remove plaintext secrets from tracked files
- Risks: breaking current LocalVolume/Cloudinary compatibility if contract changes too aggressively
- Tests: options-binding validation tests, unknown-provider tests, legacy URL passthrough tests
- Rollback point: keep current LocalVolume runtime active and legacy renderers unchanged
- Explicit non-goals: no real R2 upload yet, no production cutover

### Phase 2 - Cloudflare R2 provider

- Scope: add an R2 implementation with upload/delete/public URL generation
- Files likely affected: new provider implementation(s), provider registration, options classes
- Preconditions: Phase 1 complete; credentials managed outside the repo
- Risks: leaking provider-specific logic into controllers/views
- Tests: mock/fake S3-compatible upload/delete tests, failure-handling tests
- Rollback point: provider remains disabled for production writes
- Explicit non-goals: no legacy data migration yet

### Phase 3 - Backward-compatible URL resolver

- Scope: introduce centralized render resolution for LocalVolume, Cloudinary, static, external, and R2 references
- Files likely affected: a new resolver service/helper, news views/helpers, layout OG logic, menu rendering, branch/logo rendering
- Preconditions: clear decision on stored representation
- Risks: breaking existing static or Cloudinary URLs if resolver normalizes incorrectly
- Tests: matrix covering `/uploads`, Cloudinary, external, static, object key, and absolute R2 URL cases
- Rollback point: old direct rendering path can remain behind a feature flag during rollout
- Explicit non-goals: no destructive delete-policy changes yet

### Phase 4 - Refactor create/edit/delete flows

- Scope: move branch/post/menu-group/menu-page flows onto safe managed/unmanaged semantics and consistent compensation
- Files likely affected: `Areas/Admin/Controllers/BranchController.cs`, `PostController.cs`, `MenuGroupController.cs`, related viewmodels
- Preconditions: Phase 1-3 complete
- Risks: regression in current admin workflows; cache invalidation order mistakes
- Tests: create/edit/delete success and failure-path integration tests
- Rollback point: keep current entity string fields unchanged until behavior is validated
- Explicit non-goals: no legacy object copy yet

### Phase 5 - Automated tests

- Scope: add coverage for storage, failure, compatibility, cache, and security scenarios
- Files likely affected: new test project(s)
- Preconditions: contract and resolver stable enough to test
- Risks: none operational; only schedule/cost
- Tests: full matrix from Section 15
- Rollback point: tests are additive
- Explicit non-goals: production data migration

### Phase 6 - Legacy asset migration tooling

- Scope: inventory and copy existing LocalVolume and Cloudinary assets to R2 without deleting sources
- Files likely affected: migration tooling, registry import code, optional admin-only verification tools
- Preconditions: resolver must already support old and new references
- Risks: duplicate copies, inconsistent ownership metadata, incomplete inventory because `MediaAsset` is unusable
- Tests: dry-run inventory tests, duplicate-detection tests, checksum/existence verification
- Rollback point: old sources remain authoritative until verification is complete
- Explicit non-goals: no immediate source deletion

### Phase 7 - Production cutover

- Scope: switch new uploads to R2 only after backward compatibility is proven
- Files likely affected: environment configuration, provider selection, startup validation
- Preconditions: Phases 1-6 complete; secrets stored securely; verification plan ready
- Risks: new uploads landing in the wrong provider, mixed render bugs
- Tests: smoke tests across all admin upload paths and public image surfaces
- Rollback point: revert provider env vars to LocalVolume while keeping resolver dual-read capable
- Explicit non-goals: no retirement of legacy sources yet

### Phase 8 - Verification and rollback window

- Scope: confirm public routes, admin edits, delete semantics, and copied legacy assets while retaining old sources
- Files likely affected: none required in code if prior phases are complete
- Preconditions: production cutover completed
- Risks: silent orphaning, stale caches, missing edge-case assets
- Tests: route crawl, sampled image existence checks, admin mutation smoke tests
- Rollback point: old LocalVolume/Cloudinary assets remain available for reversion
- Explicit non-goals: no source cleanup yet

### Phase 9 - Optional LocalVolume retirement

- Scope: retire LocalVolume only after verified migration and rollback window completion
- Files likely affected: provider config, deployment docs, possibly removal of `/uploads` mapping once no live references remain
- Preconditions: verified zero live `/uploads/...` dependencies for dynamic content
- Risks: breaking dormant records or old cached pages
- Tests: reference inventory reports prove no live local references remain
- Rollback point: retain archival backup of LocalVolume contents before removal
- Explicit non-goals: none beyond cleanup

## 19. Configuration Contract for Future Implementation

Recommended naming pattern:

```env
ImageStorage__Provider=CloudflareR2
ImageStorage__AccountId=
ImageStorage__BucketName=
ImageStorage__AccessKeyId=
ImageStorage__SecretAccessKey=
ImageStorage__ServiceUrl=
ImageStorage__PublicBaseUrl=
ImageStorage__Region=auto
ImageStorage__MaxFileSizeBytes=52428800
```

Fit against current conventions:

- The `ImageStorage__...` prefix matches current options-binding conventions (`Program.cs:77`, `Options/ImageStorageSettings.cs:3-15`).
- The current `ImageStorageSettings` class does not define:
  - `AccountId`
  - `BucketName`
  - `AccessKeyId`
  - `SecretAccessKey`
  - `ServiceUrl`
  - `PublicBaseUrl`
  - `Region`
- Therefore the naming pattern fits the section convention, but not the current options class.

Recommended compatibility additions for phased migration:

- retain support for:
  - `ImageStorage__RootPath`
  - `ImageStorage__PublicBasePath`
  - `ImageStorage__AllowedExtensions`
  - `ImageStorage__AllowedContentTypes`
- add provider-specific validation so irrelevant settings are ignored but not confused with the active provider

Important current defect to resolve first:

- The checked-in files currently place LocalVolume keys under an empty-string JSON section rather than under `ImageStorage` (`appsettings.json:74-80`, `appsettings.Development.json:23-29`), so those keys are not bound by `GetSection("ImageStorage")`.

## 20. Required Data Migration Inventory

Fields that may contain local `/uploads/...` references:

- `Branch.ThumbnailUrl`
- `MenuGroup.CoverImageUrl`
- `MenuPageImage.ImageUrl`
- `Post.ThumbnailUrl`

Fields that may contain Cloudinary absolute URLs:

- `Branch.ThumbnailUrl`
- `MenuGroup.CoverImageUrl`
- `MenuPageImage.ImageUrl`
- `Post.ThumbnailUrl`
- possibly `SiteSetting.LogoUrl` if changed out-of-band

Fields with mixed or unknown formats:

- `MenuGroup.CoverImageUrl` is explicitly mixed/unmanaged
- `Review.AvatarUrl` unknown because no active source code uses it
- `SiteSetting.LogoUrl` and `SiteSetting.FaviconUrl` can contain arbitrary strings if modified out-of-band
- `MediaAsset.Url` unknown and untrusted

Can `MediaAsset` be trusted as a complete inventory?

- No. It is not written or read by the live codebase.

Required query or migration-tool behavior recommendations:

- For each image-bearing field, classify rows by pattern:
  - `LIKE '/uploads/%'`
  - `LIKE '/images/%'`
  - `LIKE '/favicon.ico'`
  - `LIKE '%cloudinary.com%'`
  - absolute URL but not Cloudinary
  - null/empty
- For menu-group covers, also detect:
  - `/uploads/branches/%`
  - `/uploads/posts/%`
  - `/uploads/menu-pages/%`
  - `/uploads/menu-groups/%`
  - to identify cross-category unmanaged references
- Migration tooling should copy legacy assets to R2 without rewriting source references until copy verification passes

Duplicate/shared-object risks:

- no registry exists to prove one-to-one ownership
- manual `MenuGroup.CoverImageUrl` creates the highest shared-reference risk
- duplicate URLs across rows should be treated as potentially shared until proven otherwise

Verification checks recommended:

- capture per-field pre-migration counts by format category
- verify each copied object exists in R2 before rewriting any DB value
- sample public routes before and after cutover
- keep old LocalVolume/Cloudinary sources readable during the entire verification window

Rollback requirements:

- retain the old LocalVolume contents
- retain Cloudinary assets
- retain a mapping/export of old reference -> copied R2 reference
- do not delete old sources until post-cutover verification is complete

## 21. Risk Register

| Risk | Severity | Evidence | Impact | Required mitigation | Phase |
| --- | --- | --- | --- | --- | --- |
| Repo defaults point to unsupported `CloudflareR2` provider | Critical | `appsettings.json:53-71`, `Services/ImageStorageService.cs:54-60` | uploads fail in environments using repo defaults | fix config contract and add fail-fast validation | 1 |
| Plaintext secrets tracked in repo | Critical | `appsettings.json:3,47,57-58`, `appsettings.Development.json:3,18,36-37`, `docs/Mail service API Key.txt` | credential exposure | rotate and externalize secrets | 1 |
| Manual `CoverImageUrl` can delete unrelated `/uploads` assets | High | `MenuGroupController.cs:280-300,366-368`, `ImageStorageService.cs:206-239` | destructive cross-category asset deletion | treat manual URLs as unmanaged; enforce ownership checks | 4 |
| No magic-byte / decode validation | High | `ImageStorageService.cs:38-52`, `BranchController.cs:352-369`, `MenuGroupController.cs:746-790` | malformed or disguised file acceptance | add signature/decode validation | 4 |
| Size limits are inconsistent across layers | Medium | `Program.cs:21-33`, `appsettings.json:69-80`, `MenuGroupController.cs:746-790`, `docs/deployment-railway.md:15-22` | operator confusion and false acceptance/rejection | unify limits and messages | 1 |
| No managed asset registry / ownership tracking | Medium | unused `MediaAsset`, string-only storage contract | unsafe delete and weak migration inventory | introduce provider/object ownership metadata | 1-4 |
| No centralized mixed-reference resolver | Medium | direct rendering scattered across views/controllers/helpers | future object-key migration will break or proliferate ad hoc logic | add one resolver layer | 3 |
| Concurrent replacements can orphan uploads | Medium | replacement flows with no concurrency token | storage leakage under concurrent edits | add concurrency handling or background orphan reconciliation | 4 |
| `Review.AvatarUrl` and `SiteSetting.FaviconUrl` are dead fields | Low | no active readers/writers | migration blind spots if prod data exists | explicitly inventory these columns before cutover | 6 |

## 22. Open Questions

1. What exact value distributions exist in production for each image-bearing column: `/uploads/...`, static `/images/...`, Cloudinary absolute URLs, external URLs, empty strings, and nulls?
2. Are any production records intentionally sharing the same image URL across multiple rows?
3. Are there production records storing absolute site-local URLs such as `https://host/uploads/...` rather than relative `/uploads/...` paths?
4. Is the checked-in R2-shaped `ImageStorage` block an intentional pre-migration draft or an accidental committed runtime configuration?
5. Is a schema change acceptable for managed asset ownership metadata, or must migration remain string-column-only?

## 23. Verification Results

Commands run:

| Command | Outcome |
| --- | --- |
| `git status --short --branch` | Completed. Initial state was dirty: `docs/local-development.md` deleted and `wwwroot/uploads/.gitkeep` modified. |
| `git rev-parse --show-toplevel` | Completed. Root confirmed as `F:/Coding/Web development/KIG Holding/KIGHolding`. |
| `git rev-parse HEAD` | Completed. Commit `afaa12e86ce549ed0ae976d4fe4882b1ed539300`. |
| `git log -5 --oneline --decorate` | Completed. History recorded in Section 2. |
| `dotnet build KIGHolding.sln` | Passed. `0 Warning(s)`, `0 Error(s)`. |
| `dotnet test KIGHolding.sln --no-restore -v normal` | Passed at the solution level with no test projects discovered in the solution build graph. |
| `git status --short --branch` (final) | Completed. Final status remained dirty from pre-existing changes and showed one new untracked path: ` D docs/local-development.md`, ` M wwwroot/uploads/.gitkeep`, `?? reports/`. |
| `git diff --check` | Passed. No whitespace or patch-format issues were reported. |
| `git diff --name-status` | Completed. Reported only the pre-existing tracked changes: `D docs/local-development.md` and `M wwwroot/uploads/.gitkeep`. It does not list the untracked audit report. |
| `git diff --stat` | Completed. Reported only the pre-existing tracked diff stat: `docs/local-development.md` deleted and `wwwroot/uploads/.gitkeep` modified. The untracked audit report is visible in `git status`, not `git diff --stat`. |

## 24. Final Recommendation

Implementation should not begin yet.

Blocking issues to resolve first:

1. Remove plaintext secrets from tracked files and rotate exposed credentials.
2. Correct the storage configuration contract so repo defaults do not bind an unsupported `CloudflareR2` provider.
3. Decide the managed-asset ownership model before adding R2 delete semantics.
4. Eliminate the unmanaged `MenuGroup.CoverImageUrl` delete hazard or explicitly mark such values as non-deletable.
5. Unify upload size limits across config, UI, controllers, and service behavior.

Recommended first implementation phase:

- Phase 1 from Section 18: storage contract and options foundation, with no behavior switch yet.

Parts that must not be changed yet:

- Do not remove `/uploads` support.
- Do not remove Cloudinary readability.
- Do not delete LocalVolume or Cloudinary source assets during early R2 work.
- Do not trust `MediaAsset` as a migration inventory without proving it from source or data.

Production cutover conditions:

- one centralized resolver in place
- provider-specific upload/delete implementation tested
- shared/unknown ownership handled safely
- existing `/uploads/...`, Cloudinary, static, and external references verified readable
- rollback path retained for the full verification window
