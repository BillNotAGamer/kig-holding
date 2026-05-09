# Admin Placeholder Foundation

This area is intentionally limited to a placeholder dashboard for the client MVP.

Future admin implementation notes:

- Add ASP.NET Core Identity before implementing admin CRUD.
- Add role-based authorization policies before exposing management actions.
- Planned roles:
  - `SuperAdmin`
  - `Manager`
  - `Editor`
  - `Receptionist`
- Keep public client routes and admin routes isolated.
- Do not add reservation, menu, branch, post, media, or contact CRUD screens until the admin phase is explicitly requested.
- Do not create fake authentication or hidden bypasses for staging/demo.
