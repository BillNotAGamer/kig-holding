# Admin Roadmap

## Maintaining the Admin Area

### Sidebar Active State Logic
The admin sidebar navigation dynamically highlights the current active module.
* Located in `_AdminLayout.cshtml`.
* Logic is handled via a local `@functions` block containing `IsActive(currentController, controllerName)`.
* Styling is applied using `NavLinkClass`, which returns active Tailwind classes (`border-brand-gold bg-brand-gold/15 text-brand-goldSoft`) when a match occurs.

### Toast Notification System
Feedback messages (success/error) are managed globally:
* Controllers pass data using `TempData["SuccessMessage"]` or `TempData["ErrorMessage"]`.
* `_AdminLayout.cshtml` listens for these values and injects stylized toast elements into the DOM if present.
* Vanilla JavaScript automatically applies an `opacity-0` class and removes the node after 4000ms for a seamless user experience.

## Adding a New Admin Module
To implement a new feature in the Admin panel, follow these strict steps:

1. **Create the Controller**: Create a new controller inside `Areas/Admin/Controllers/`.
2. **Inheritance**: The controller MUST inherit from `AdminBaseController` to enforce authorization and access layout helpers.
3. **ViewModels**: Create corresponding ViewModels inside `Areas/Admin/ViewModels/` utilizing `System.ComponentModel.DataAnnotations` for strict validation.
4. **Views**: Create CRUD views inside `Areas/Admin/Views/[ModuleName]/`.
5. **Sidebar Link**: Add a navigation anchor inside `Areas/Admin/Views/Shared/_AdminLayout.cshtml` ensuring the `aria-current` and `class` attributes call the `IsActive` and `NavLinkClass` helpers securely.
