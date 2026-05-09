# Backend Logic

## Separation of Concerns

The backend architecture enforces a strict separation between HTTP handling and business logic.

### Services Layer
Business logic and complex data retrieval queries are isolated within the `Services/` directory (e.g., `MenuService`, `BranchService`). 
* **Decoupling**: Controllers inject these services (e.g., `IMenuService`) via the DI container rather than querying the `AppDbContext` directly for read operations.
* **Caching**: The services layer internally handles memory caching (using `IMemoryCache`), ensuring cache invalidation and query logic remains completely hidden from the Controller.

### Validation Architecture
Controllers must act strictly as HTTP orchestrators.
* Data flowing into a POST action binds to a strongly-typed ViewModel.
* These ViewModels define rules using `System.ComponentModel.DataAnnotations`.
* The Controller simply executes `if (!ModelState.IsValid)` before performing any database transactions.
* If validation fails, the Controller repopulates dropdowns and returns the view, ensuring data integrity is never compromised at the Entity Framework level.
