# 09 Public Pages

## Public Pages Summary

| Page Name | URL | Controller / Action | View File | Main Purpose | Current Status |
|-----------|-----|---------------------|-----------|--------------|----------------|
| Home Page | `/` | `HomeController.Index` | `Views/Home/Index.cshtml` | Renders Home page showing brand intro and booking triggers. | **MVP Completed**. Uses custom hero slider and bobbing illustrations. |
| Menu Hub | `/thuc-don` | `MenuController.Index` | `Views/Menu/Index.cshtml` | Renders active menu groups catalog landing cards. | **MVP Completed**. Displays Cover images and sheet page counts. |
| Menu Group | `/thuc-don/nhom/{slug}` | `MenuController.Group` | `Views/Menu/Group.cshtml` | Renders specific menu group pages in Scroll or Book flipbook views. | **MVP Completed**. Dual-mode active; plan exists to migrate to book-only. |
| Branch Locator | `/chi-nhanh` | `BranchController.Index` | `Views/Branch/Index.cshtml` | Directory for finding active branches and viewing Google maps. | **MVP Completed**. Supports city filtering, GET search, suggestions, and maps. |
| Reservation | `/dat-ban` | `ReservationController.Index` | `Views/Reservation/Index.cshtml` | Public reservation booking form. | **MVP Completed**. Preselects branch via query and handles validations. |
| Booking Success | `/dat-ban/thanh-cong` | `ReservationController.Success` | `Views/Reservation/Success.cshtml` | Confirms successful table booking. | **MVP Completed**. Displays reservation ID and contact details. |
| News Listing | `/tin-tuc` | `NewsController.Index` | `Views/News/Index.cshtml` | Paginated index of blog posts and announcements. | **MVP Completed**. Editorial hierarchy grid with category selectors. |
| News Read | `/tin-tuc/{slug}` | `NewsController.Detail` | `Views/News/Detail.cshtml` | Detailed read page for a specific article. | **MVP Completed**. Displays full paragraphs and booking promo cards. |
| Contact Box | `/lien-he` | `ContactController.Index` | `Views/Contact/Index.cshtml` | Customer contact request forms and corporate hotline list. | **MVP Completed**. Saves entries to database. |
| About KIG | `/gioi-thieu` | `AboutController.Index` | `Views/About/Index.cshtml` | Corporate branding highlights and company story. | **MVP Completed**. Static page; uses older layout placeholders. |
| Membership | `/thanh-vien` | `MemberController.Index` | `Views/Member/Index.cshtml` | Membership details preview. | **Placeholder**. Return static view only. |
| Franchise | `/lien-he-nhuong-quyen` | `FranchiseController.Index` | `Views/Franchise/Index.cshtml` | Franchise opportunities. | **Placeholder**. Return static view only. |

---

## Detailed Page Catalog

### 1. Home Page
* **URL**: `/`
* **Controller/Action**: `HomeController.Index`
* **View**: [Views/Home/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Home/Index.cshtml)
* **Layout**: `_Layout.cshtml` (Dark Premium styling elements apply)
* **ViewModel**: `HomeIndexViewModel`
* **Data Source**: Fetches the latest 3 published posts and active branch listings.
* **Sections**: Hero banner slider, Brand story description, Culinary essence bobs, Menu teaser, Take-home, News promotions list, and Booking mini-form.
* **CTAs**: "Đặt bàn ngay" (to `/dat-ban`), "Xem thực đơn" (to `/thuc-don`), and "Xem chi nhánh" (to `/chi-nhanh`).
* **Forms**: None.
* **JS Behavior**: [home-hero-slider.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/home-hero-slider.js) manages slide transitions, timer bars, pause states, and keyboard listeners.
* **CSS/Theme**: Styled in a dark premium theme. Custom styles are defined in `input.css` for bobbing illustrations and overlay colors.
* **SEO Notes**: Title: "Truyền Thuyết Champong - Trải nghiệm ẩm thực Hàn Quốc đậm vị và hiện đại".
* **Risks**: Uses mockup banner images (`banner1.webp` to `banner3.webp`) which require production quality checks.

### 2. Menu Group Detail Page
* **URL**: `/thuc-don/nhom/{slug}`
* **Controller/Action**: `MenuController.Group`
* **View**: `Views/Menu/Group.cshtml`
* **Layout**: `_Layout.cshtml` (Dark Premium)
* **ViewModel**: `MenuGroupDetailViewModel`
* **Data Source**: MenuGroup details and cascading `MenuPageImage` lists loaded from database.
* **Sections**: condensed header panel, control toggle bar, book viewer stage, pagination links, and related booking CTA banner.
* **CTAs**: "Đặt bàn ngay" (to `/dat-ban`), "Quay lại" (to `/thuc-don`).
* **Forms**: None.
* **JS Behavior**: [menu-flipbook.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/menu-flipbook.js) scrapes card images from the DOM, initializes dual-page spread spreads on desktop, handles page turns, and responds to ArrowLeft/Right keys.
* **Risks**: Deleting DOM scroll cards breaks the JS image scraper. The page loads Swiper JS/CSS globally even though it is not used here.

### 3. Branch Locator Page
* **URL**: `/chi-nhanh`
* **Controller/Action**: `BranchController.Index`
* **View**: `Views/Branch/Index.cshtml`
* **Layout**: `_Layout.cshtml` (Dark Premium)
* **ViewModel**: `BranchIndexViewModel`
* **Data Source**: Active branch list loaded from database.
* **Sections**: Locator header, real GET search inputs, city filtering chips, results list columns, and Google map iframe.
* **CTAs**: "Đặt bàn" (to `/dat-ban?branch=slug`), Map directions link (`target="_blank"`), phone hotline trigger (`tel:` link).
* **Forms**: GET search form submitting text parameters.
* **JS Behavior**: [branch-locator.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/branch-locator.js) provides search suggestions with ArrowUp/Down and Escape hooks.
* **Risks**: Autocomplete suggestions are scoped to the current filtered view, not the global dataset.

### 4. Reservation Booking Page
* **URL**: `/dat-ban`
* **Controller/Action**: `ReservationController.Index`
* **View**: `Views/Reservation/Index.cshtml`
* **Layout**: `_Layout.cshtml` (Dark Premium)
* **ViewModel**: `ReservationCreateViewModel`
* **Data Source**: Active branch dropdown options and global site hotlines loaded from database.
* **Sections**: Page intro block, booking form fields panel, and side hotline contact details card.
* **CTAs**: None.
* **Forms**: POST form for name, phone, email, branch selection, date, time, and notes.
* **JS Behavior**: [booking-modal.js](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/js/booking-modal.js) provides support for popup booking interactions.
* **SEO Notes**: Canonic URLs mapped dynamically using AppBaseUrl options.
* **Risks**: Does not apply the advanced business validation rules (blackout hours, weekday minimum guest limits) drafted in the policy service.
