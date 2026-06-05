# 14 Safe Contribution Guide

This guide is designed for developers and AI agents onboarding onto the project to help them make code modifications safely and maintain code quality.

---

## 1. Before You Change Code
1. **Run a Verification Build**: Ensure the application compiles without errors:
   ```powershell
   dotnet build
   ```
2. **Verify CSS Compilation**: Verify that npm assets compile correctly:
   ```powershell
   npm run build:css
   ```
3. **Verify Database Configuration**: Ensure you have configured a PostgreSQL connection in `appsettings.json` (or use local overrides in `appsettings.Development.json`).

---

## 2. Coding Rules and Conventions

### PostgreSQL UTC Offset Requirement
> [!CRITICAL]
> **Strict UTC Timestamps**
> Saving a `DateTimeOffset` with a local offset (such as `+07:00` for Vietnam) to a PostgreSQL `timestamptz` column will cause serialization errors. Always use `DateTimeOffset.UtcNow` or call `NormalizeLocalInputToUtc()` in controllers before saving dates.

### Separation of Themes
* **Public Boundary**: Keep public pages in the **Dark Premium** theme (`bg-brand-black` base with red accents).
* **Admin Boundary**: Keep admin pages in the **Light Theme** (`bg-brand-light` base with slate borders) to ensure legibility and ease of operations.

### CSS and styling conventions
* **Never edit `site.css` manually**. All styling additions must be written in [input.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/input.css) (using `@apply` directives inside `@layer components` for shared elements) and compiled using `npm run build:css`.

### Progressive JS Isolation
* Keep JavaScript light and page-specific. Do not add page-specific scripts to `site.js`.
* isolated scripts must include safety guards (`if (!document.querySelector('...')) return;`) to exit silently if loaded on unrelated pages.
* Wrap all DOM-manipulating logic inside a `DOMContentLoaded` event listener:
  ```javascript
  document.addEventListener('DOMContentLoaded', function() {
      // DOM logic
  });
  ```

---

## 3. How to Add a Database Entity
1. Create a C# entity class in [Models/Entities/](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Models/Entities). If the entity requires timestamps, implement `ICreatedAtEntity` or `IUpdatedAtEntity` to automate them.
2. Add a `DbSet<T>` property to [AppDbContext.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Data/AppDbContext.cs).
3. Create an entity configuration class in [Data/Configurations/](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Data/Configurations) to define column types, lengths, and constraints.
4. Generate the EF Core migration:
   ```powershell
   dotnet ef migrations add AddNewEntityName
   ```
5. Apply the migration to the database:
   ```powershell
   dotnet ef database update
   ```

---

## 4. How to Add a Service
1. Define the service interface in `Services/` (e.g. `INewService.cs`).
2. Implement the interface in `Services/` (e.g. `NewService.cs`), injecting `AppDbContext` for data access.
3. Register the service in [Program.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Program.cs#L57-L64) with a Scoped lifetime:
   ```csharp
   builder.Services.AddScoped<INewService, NewService>();
   ```

---

## 5. How to Work With File Uploads
1. **Apply Constraints**: Enforce size and extension checks in your upload actions:
   * Branch thumbnails <= 2MB
   * News thumbnails <= 2MB
   * MenuGroup covers <= 10MB
   * MenuPageImage sheets <= 50MB
2. **Allowed Extensions**: Validate that uploads are `.jpg`, `.jpeg`, `.png`, or `.webp`.
3. **Safe Naming**: Convert filenames to slugs, append a GUID suffix, and keep the original extension to avoid folder collisions:
   ```csharp
   var uniqueName = $"{slug}-{Guid.NewGuid():N}{extension}";
   ```
4. **Traversal Shield**: Physical file deletions must check that target paths start with the uploads directory root to prevent directory traversal exploits.
   ```csharp
   if (fullPath.StartsWith(uploadsRoot, StringComparison.OrdinalIgnoreCase)) {
       System.IO.File.Delete(fullPath);
   }
   ```

---

## 6. Common Mistakes to Avoid
* **Over-posting vulnerabilities**: Do not bind POST requests directly to database entities. Always use dedicated ViewModels for model binding.
* **Missing Anti-Forgery validation**: Forgetting the `[ValidateAntiForgeryToken]` decoration on HTTP POST routes, which creates security risks.
* **Obsolete Reveal Classes**: Using outdated CSS classes like `.scroll-reveal` or `.reveal-up`. Always use `data-uw-reveal="fade-up"` attributes.
* **DateTimeOffset crash**: Writing `DateTimeOffset.Now` (Vietnamese local timezone offsets) to database timestamptz columns, which causes Npgsql serialization errors.
