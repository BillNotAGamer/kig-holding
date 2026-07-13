# Admin Area Notes

The Admin area contains authenticated CRUD flows for branches, posts, menu groups, menu page images, reservations, reviews, contacts, and site settings.

## Image Upload Contract

Admin controllers must continue using `IImageStorageService` for image operations:

```csharp
Task<string> UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default);
Task<string> UploadAsync(IFormFile file, ImageCategory category, string storageScope, CancellationToken cancellationToken = default);
Task DeleteAsync(string? imageUrlOrPath, ImageCategory category, CancellationToken cancellationToken = default);
```

Controllers must not reference AWS SDK, Cloudflare R2, Cloudinary, local filesystem storage, or provider credentials directly.

Menu page uploads are the only admin flow that supplies `storageScope`. The scope must come from the persisted `MenuGroup.Slug`; client input, menu group name, alt text, or uploaded filename must never select the storage folder.

## Current Provider Behavior

* `CloudflareR2`: active target provider for new uploads when configured. Returns absolute public URLs.
* `LocalVolume`: retained for local development and legacy `/uploads/...` assets.
* `Cloudinary`: retained for recognized legacy absolute URLs.

Delete routing is based on the stored image reference and category. An active R2 upload provider must still be able to delete recognized LocalVolume and Cloudinary legacy references during replacement or permanent-delete operations.

For new menu page uploads, R2 object keys are grouped by menu group slug:

```text
menu-pages/<menu-group-slug>/<safe-unique-filename>
```

Existing root-level `menu-pages/<filename>` objects remain supported for rendering and deletion. Slug changes do not move old objects or rewrite stored URLs.

## Admin Upload Limits

Storage service canonical limit:

```text
ImageStorage:MaxFileSizeBytes = 52428800
```

Feature-specific limits still apply before the storage service:

* Branch thumbnail: 2 MB
* Post thumbnail: 2 MB
* Menu group cover: 10 MB
* Menu page image: 50 MB

## Pending Lifecycle Hardening

Phase 1 does not add managed asset ownership records. Manual URL fields, shared references, legacy migration, and delayed cleanup semantics remain future hardening work. Be conservative when changing create/edit/delete flows.
