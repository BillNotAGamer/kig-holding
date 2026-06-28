# Local Development Guide

## 1. Secrets Management
Secrets such as database connection strings and API keys **must never** be stored in `appsettings.json` or `appsettings.Development.json`. Committing secrets to the repository is a critical security risk. Instead, we use `.NET User Secrets` for local development.

## 2. Initializing User Secrets
If you haven't already, initialize user secrets for this project by running:
```bash
dotnet user-secrets init
```
This command adds a `UserSecretsId` to the `.csproj` file, enabling local secret storage outside the project directory.

## 3. Required Local Secrets
Configure the following required secrets using the `.NET CLI`:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your-local-or-neon-connection-string>"
dotnet user-secrets set "ResendSettings:ApiKey" "<your-resend-api-key>"
dotnet user-secrets set "AdminBootstrap:Username" "<your-admin-bootstrap-username>"
dotnet user-secrets set "AdminBootstrap:Password" "<your-admin-bootstrap-password>"
dotnet user-secrets set "ImageStorage:Provider" "LocalVolume"
dotnet user-secrets set "ImageStorage:RootPath" "App_Data/uploads"
dotnet user-secrets set "ImageStorage:PublicBasePath" "/uploads"
```

## 4. Optional Cloudinary Secrets
If you need to test legacy provider behavior or Cloudinary deletions, you can optionally configure these secrets:

```bash
dotnet user-secrets set "CloudinarySettings:CloudName" "<your-cloudinary-cloud-name>"
dotnet user-secrets set "CloudinarySettings:ApiKey" "<your-cloudinary-api-key>"
dotnet user-secrets set "CloudinarySettings:ApiSecret" "<your-cloudinary-api-secret>"
```

## 5. Local Upload Behavior
*   **Storage Location:** By default, locally uploaded images will be saved to `App_Data/uploads`.
*   **Database Record:** The database stores the relative public path prefix, e.g., `/uploads/posts/my-image.jpg`.
*   **Git Ignore:** The `App_Data/` folder is explicitly ignored in `.gitignore`, ensuring local test uploads are never committed.

## 6. Local Verification Checklist
When testing local development setup, ensure you run through the following checklist:
- [ ] Run `dotnet build KIGHolding.csproj` and ensure it succeeds.
- [ ] Run the app using `dotnet run --project KIGHolding.csproj`.
- [ ] Log into the `/Admin` dashboard using your configured bootstrap credentials.
- [ ] Upload a **post thumbnail**.
- [ ] Upload a **branch thumbnail**.
- [ ] Upload a **menu group cover**.
- [ ] Upload **menu flipbook pages** (if the UI supports it).
- [ ] Replace an existing image and verify the old one is deleted locally.
- [ ] Delete an image and verify removal from the local filesystem.
- [ ] Verify the public pages render all uploaded images correctly.
- [ ] Verify the physical files exist under the configured `ImageStorage:RootPath` (e.g., `App_Data/uploads`).
- [ ] Verify the database stores the `/uploads/...` prefix, not a physical disk path.

## 7. Troubleshooting
*   **Startup Crash:** Ensure `ConnectionStrings:DefaultConnection` and `ResendSettings:ApiKey` are set correctly.
*   **Login Failing:** Ensure `AdminBootstrap:Password` is set and satisfies the password policy.
*   **Upload Folder Permission Issue:** Ensure your local user has write access to the project's root folder so `App_Data/uploads` can be created.
*   **Images Return 404:** Check if `ImageStorage:RootPath` matches the folder created, and `ImageStorage:PublicBasePath` aligns with `/uploads`.
*   **Cloudinary Errors:** If you accidentally set `ImageStorage:Provider` to `Cloudinary` but lack credentials, the app will throw configuration errors upon upload.
