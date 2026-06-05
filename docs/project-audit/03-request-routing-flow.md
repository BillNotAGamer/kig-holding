# 03 Request Routing Flow

## Routing Setup in Program.cs
The application maps incoming requests using a combination of **Conventional Routing** (for area controllers) and **Attribute Routing** (for public endpoints).

Inside [Program.cs](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Program.cs#L88-L100):
```csharp
app.MapControllerRoute(
    name: "admin-root",
    pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "Admin" });

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

---

## Public Routes

| URL | Controller | Action | View | Purpose | Notes |
|-----|------------|--------|------|---------|-------|
| `/` | `HomeController` | `Index` | `Views/Home/Index.cshtml` | Renders Home page | Loads hero slider, brand story, essence, skeletons, news teasers, and reservation components. |
| `/thuc-don` | `MenuController` | `Index` | `Views/Menu/Index.cshtml` | Menu Group hub | Renders active menu group card items (Gogimaru, Champong, etc.). |
| `/thuc-don/nhom/{slug}` | `MenuController` | `Group` | `Views/Menu/Group.cshtml` | Menu Group detail | Loads list of images for scroll or flipbook rendering. |
| `/thuc-don/{slug}` | `MenuController` | `Detail` | *None* | Preserved legacy route | Redirects GET requests immediately back to `/thuc-don` (retained for SEO preservation). |
| `/chi-nhanh` | `BranchController` | `Index` | `Views/Branch/Index.cshtml` | Branch Locator | Renders interactive search grids, autocomplete dropdowns, and map frame. |
| `/dat-ban` | `ReservationController` | `Index` (GET/POST) | `Views/Reservation/Index.cshtml` | Booking page / Submission | Renders reservation forms, validates posts, and redirects to success views. |
| `/dat-ban/quick` | `ReservationController` | `Quick` (POST) | *None* | Async reservation POST | Accepts AJAX submissions from home page booking cards. Returns JSON. |
| `/dat-ban/thanh-cong` | `ReservationController` | `Success` (GET) | `Views/Reservation/Success.cshtml` | Booking confirmation page | Displays customer name, reservation ID, selected branch, and hotline. |
| `/tin-tuc` | `NewsController` | `Index` | `Views/News/Index.cshtml` | News listing | Paginated grid (9 items/page) with category filtering. |
| `/tin-tuc/{slug}` | `NewsController` | `Detail` | `Views/News/Detail.cshtml` | News read page | Displays full article paragraphs, thumbnail frames, and booking CTA cards. |
| `/lien-he` | `ContactController` | `Index` (GET/POST) | `Views/Contact/Index.cshtml` | Contact box | Allows users to submit messages to the administration inbox. |
| `/gioi-thieu` | `AboutController` | `Index` | `Views/About/Index.cshtml` | About page | Content detail page showcasing KIG Holding origins. |
| `/thanh-vien` | `MemberController` | `Index` | `Views/Member/Index.cshtml` | Membership page | Informational page details. |
| `/lien-he-nhuong-quyen` | `FranchiseController` | `Index` | `Views/Franchise/Index.cshtml` | Franchise page | Partner benefit descriptions and CTAs redirecting to contact forms. |
| `/sitemap.xml` | `SeoController` | `Sitemap` | *None* | SEO Sitemap generator | Serves sitemap XML stream. |
| `/robots.txt` | `SeoController` | `Robots` | *None* | SEO crawling rules | Serves robot configurations. |
| `/error/{statusCode}` | `ErrorController` | `Index` | `Views/Error/Index.cshtml` | Error response pages | Intercepts HTTP 404, 500, etc. |

---

## Admin Routes (Area: Admin)

| URL | Controller | Action | View | Purpose | Notes |
|-----|------------|--------|------|---------|-------|
| `/Admin/Dashboard` | `DashboardController` | `Index` | `Views/Dashboard/Index.cshtml` | Main Overview | Displays key statistics (total reservations, unread contacts, active branches). |
| `/Admin/Auth/Login` | `AuthController` | `Login` (GET/POST) | `Views/Auth/Login.cshtml` | Admin login page | Form submission, password verification, and authentication cookie setup. |
| `/Admin/Auth/Logout` | `AuthController` | `Logout` (POST) | *None* | Sign out | Cleans authentication cookies and redirects to Login page. |
| `/Admin/Reservation` | `ReservationController` | `Index` | `Views/Reservation/Index.cshtml` | Bookings manager | List, search, pagination, status toggles (Confirm/Cancel), and manual edits. |
| `/Admin/Contact` | `ContactController` | `Index` | `Views/Contact/Index.cshtml` | Inbox list | View customer requests, mark as read/unread, and delete logs. |
| `/Admin/MenuGroup` | `MenuGroupController` | `Index` | `Views/MenuGroup/Index.cshtml` | Menu groups list | CRUD list of Gogimaru, Champong BBQ menu groups. |
| `/Admin/MenuGroup/Images/{id}` | `MenuGroupController` | `Images` | `Views/MenuGroup/Images.cshtml` | Page sheet upload manager | Drag-and-drop uploader interface, display ordering edits, and visibility toggles. |
| `/Admin/Post` | `PostController` | `Index` | `Views/Post/Index.cshtml` | Editorial manager | Create, draft, edit thumbnail paths, publish, and delete posts. |
| `/Admin/Review` | `ReviewController` | `Index` | `Views/Review/Index.cshtml` | Customer reviews | View testimonials, toggle visibility flags, and delete reviews. |
| `/Admin/Branch` | `BranchController` | `Index` | `Views/Branch/Index.cshtml` | Restaurant locations | CRUD for name, address, hotline, active hours, capacity, and coordinates. |
| `/Admin/Setting` | `SettingController` | `Index` (GET/POST) | `Views/Setting/Index.cshtml` | Global config | Edits hotline numbers, legal entity names, branding names, logo links, and social URLs. |

---

## Special Routes / Query Parameters

| Route | Parameter | Used By | Behavior |
|-------|-----------|---------|----------|
| `/chi-nhanh` | `q` | `BranchController.Index` | Performs case/diacritic-insensitive matching against Name, Address, District, and Hotline. |
| `/chi-nhanh` | `city` | `BranchController.Index` | Filters branches by exact City name (e.g. "Hồ Chí Minh"). |
| `/dat-ban` | `branch` | `ReservationController.Index` | Pre-selects a branch dropdown menu in the booking form using the branch's slug. |
| `/dat-ban` | `guestCount` / `guests` | `ReservationController.Index` | Pre-populates the Guest Count field (defaults to 2 if empty). |
| `/tin-tuc` | `category` | `NewsController.Index` | Filters news grid posts by static category slug keys. |
| `/tin-tuc` | `page` | `NewsController.Index` | Changes active pagination page number offset (default: page 1). |

---

## Request Lifecycle Walkthrough

```txt
1. Browser sends GET to `/dat-ban?branch=truyen-thuyet-champong-quan-2`
2. Program.cs maps the URL pattern `/dat-ban` to ReservationController, Index action.
3. ReservationController.Index receives query parameter branch="truyen-thuyet-champong-quan-2".
4. Action calls IBranchService.GetActiveBranchesAsync() to load branch records.
5. Action runs ResolveSelectedBranch() to match the slug "truyen-thuyet-champong-quan-2".
6. Action populates ReservationCreateViewModel carrying branch dropdowns and selected flags.
7. Action compiles and passes model to Views/Reservation/Index.cshtml.
8. Razor engine compiles HTML, loads styles from site.css, animations from uw-reveal.js, and serves back to the browser.
```
