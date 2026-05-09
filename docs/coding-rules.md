# Coding Rules

## Strict Coding Standards

All future development must adhere to the following hard constraints:

### Frontend Standards
* **Light Theme**: The interface must strictly adhere to the Light Theme foundation (`bg-brand-light`, `text-brand-dark`). Do not introduce dark mode classes.
* **Typography**: The `Quicksand` font must be used universally. Do not overwrite font families.
* **Brand Colors**: Adhere to the defined KIG color palette. Specifically, "KIG Red" must be accessed via Tailwind tokens (e.g., `text-brand-red` which maps to `#E50914`).

### Backend Standards
* **Validation**: You MUST use `System.ComponentModel.DataAnnotations` (e.g., `[Required]`, `[StringLength]`) for ALL ViewModels. Manual form validation logic in controllers should be avoided.

### Security Standards
* **CSRF Protection**: Every `[HttpPost]` action within Admin Controllers MUST be decorated with the `[ValidateAntiForgeryToken]` attribute.
* **Passwords**: Any user authentication must utilize ASP.NET Core's `PasswordHasher`. Never store plaintext or use weak hashes.

### JavaScript Standards
* **Execution**: All DOM-manipulating vanilla JavaScript logic MUST be wrapped inside a `DOMContentLoaded` event listener to ensure safe execution.
