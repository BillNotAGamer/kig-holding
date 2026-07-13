# Cloudflare R2 Phase 1.1 Menu Group Prefixes Report

## 1. Verdict

PHASE_1_1_ACCEPTED

## 2. Initial Worktree State

Initial HEAD:

```text
afaa12e86ce549ed0ae976d4fe4882b1ed539300
```

Pre-existing worktree changes included the uncommitted Cloudflare R2 Phase 1 provider foundation files, documentation updates, `D docs/local-development.md`, `M wwwroot/uploads/.gitkeep`, and the existing untracked `reports/cloudflare-r2-image-storage-audit.md`.

This task preserved those changes and did not overwrite the historical Phase 1 reports.

## 3. Root Cause

Phase 1 preserved the existing controller-facing upload contract:

```csharp
UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default)
```

For `ImageCategory.MenuPages`, the R2 key builder only had the category and original filename, so every menu-page object used:

```text
menu-pages/<safe-unique-filename>
```

The upload layer had no menu group context and therefore could not include `MenuGroup.Slug`.

## 4. Contract Change

`IImageStorageService` and the internal provider contract now retain the existing unscoped upload overload and add a compatible scoped overload:

```csharp
UploadAsync(IFormFile file, ImageCategory category, string storageScope, CancellationToken cancellationToken = default)
```

Null scope preserves previous behavior. Non-null scope is accepted only for `ImageCategory.MenuPages` and is validated as a single safe slug segment before reaching provider-specific path generation.

## 5. Final Object-Key Rules

New R2 menu-page uploads use:

```text
{MenuPagesPrefix}/{MenuGroup.Slug}/{safe-unique-filename}
```

Examples:

```text
menu-pages/truyen-thuyet-champong/1-<guid>.webp
menu-pages/gogi-maru/1-<guid>.webp
menu-pages/kbb-cook/1-<guid>.webp
```

The folder segment is the persisted `MenuGroup.Slug`, not the display name and not client input. Scope validation rejects empty, whitespace, multi-segment, traversal, URI-shaped, rooted, Unicode display-name, and control-character values.

## 6. Files Changed

Source:

```text
Areas/Admin/Controllers/MenuGroupController.cs
Services/IImageStorageService.cs
Services/IImageStorageProvider.cs
Services/ImageStorageService.cs
Services/ImageStoragePathUtilities.cs
Services/CloudflareR2ObjectKeyBuilder.cs
Services/CloudflareR2ImageStorageProvider.cs
Services/LocalVolumeImageStorageProvider.cs
Services/CloudinaryImageStorageProvider.cs
KIGHolding.Tests/KIGHolding.Tests.csproj
```

Tests:

```text
KIGHolding.Tests/Admin/MenuGroupControllerStorageScopeTests.cs
KIGHolding.Tests/Storage/CloudflareR2ObjectKeyBuilderTests.cs
KIGHolding.Tests/Storage/CloudflareR2ImageStorageProviderTests.cs
KIGHolding.Tests/Storage/LegacyCompatibilityTests.cs
KIGHolding.Tests/Storage/LiveCloudflareR2SmokeTests.cs
```

Documentation:

```text
Areas/Admin/README.md
docs/architecture.md
docs/backend-logic.md
docs/coding-rules.md
docs/deployment-railway.md
docs/feature-state.md
```

Report:

```text
reports/cloudflare-r2-phase-1-1-menu-group-prefixes.md
```

## 7. Upload Flow

The Admin menu-page upload flow is now:

```text
Load MenuGroup from database
-> validate persisted MenuGroup.Slug as a storage scope segment
-> validate submitted files
-> upload each file with ImageCategory.MenuPages + validated slug
-> create MenuPageImage records with returned URLs
-> save database
```

The same validated scope is used for every file in the batch. Client file names, route text, alt text, and form data cannot override the folder.

## 8. Legacy Compatibility

Confirmed behavior:

```text
old root-level R2 objects still render through stored URLs
old root-level R2 objects still delete under menu-pages/<filename>
new nested R2 objects render and delete under menu-pages/<slug>/<filename>
/uploads/... remains supported by LocalVolume delete routing
LocalVolume scoped uploads write /uploads/menu-pages/<slug>/<filename>
Cloudinary legacy URL recognition remains unchanged
```

No existing object was moved and no existing database URL was rewritten.

## 9. Test Results

Ordinary tests:

```text
dotnet test KIGHolding.sln --no-restore -v normal
Total tests: 89
Passed: 88
Skipped: 1
Warnings: 0
Errors: 0
```

The skipped test was the opt-in live R2 smoke test when `RUN_LIVE_R2_TESTS` was not enabled.

Build:

```text
dotnet build KIGHolding.sln --no-restore
Warnings: 0
Errors: 0
```

## 10. Live R2 Result

Live test command:

```text
RUN_LIVE_R2_TESTS=true dotnet test KIGHolding.Tests/KIGHolding.Tests.csproj --no-restore --filter FullyQualifiedName~LiveCloudflareR2SmokeTests -v normal
```

Result:

```text
Total tests: 1
Passed: 1
Warnings: 0
Errors: 0
```

Verified by test assertions:

```text
upload success: yes
expected nested key: menu-pages/menu-group-folder-smoke/r2-smoke-<guid>.png
object existence verification: yes
public URL verification: HTTP 200
delete success: yes
post-delete verification: object no longer exists
cleanup status: cleanup path executed; no smoke object remains
```

No credentials or secret values were printed.

## 11. Slug Rename Limitation

Existing objects are not moved when a menu group slug changes.

Example:

```text
old object: menu-pages/truyen-thuyet-champong/1-old.webp
new slug: champong-korean-noodles
future upload: menu-pages/champong-korean-noodles/2-new.webp
```

The old URL remains valid because it is stored on `MenuPageImage.ImageUrl`.

## 12. Documentation Updated

Updated documentation synchronized:

```text
Areas/Admin/README.md
docs/architecture.md
docs/backend-logic.md
docs/coding-rules.md
docs/deployment-railway.md
docs/feature-state.md
```

The docs now state the menu-page object-key structure, persisted slug source, validation constraints, root-level legacy compatibility, LocalVolume rollback path, and slug-rename limitation.

## 13. Remaining Risks

Remaining out-of-scope risks:

```text
no managed asset registry
no shared-reference ownership protection
no existing-object migration
no automatic slug-rename object migration
no full magic-byte/decoder validation beyond the current extension/content-type checks
controller lifecycle hardening remains a separate phase
```

## 14. Final Worktree State

Task-created changes are limited to the source, test, documentation, and report files listed above.

Pre-existing/unrelated worktree changes remain present and were not reverted. One unrelated tracked upload file deletion appeared in the worktree during verification:

```text
D wwwroot/uploads/menu-groups/covers/black-sauce-noodle-4a90d2046b704eae9d92faf00fc7a88f.webp
```

This task did not intentionally delete or modify runtime upload assets, and no cleanup command was run against `wwwroot/uploads`.
