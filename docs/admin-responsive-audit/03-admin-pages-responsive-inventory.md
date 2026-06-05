# Admin Pages Responsive Inventory

This document inventories every admin view screen, classifying its responsive state, risks, and proposed fixes.

| Page | URL | View File | Page Type | Main UI Pattern | Desktop Status | Tablet Risk | Mobile Risk | Recommended Fix |
|------|-----|-----------|-----------|-----------------|----------------|-------------|-------------|-----------------|
| **Đăng nhập** | `/Admin/Auth/Login` | `Auth/Login.cshtml` | Auth page | Centered card form. | OK | Low | Low | Maintain centered card constraints, ensure spacing. |
| **Tổng quan** | `/Admin/Dashboard` | `Dashboard/Index.cshtml` | Dashboard | Metric cards grid & summary panels. | OK | Low | Low/Medium | Ensure grid layouts wrap correctly; no action changes. |
| **Danh sách Đặt bàn** | `/Admin/Reservation` | `Reservation/Index.cshtml` | Table/List | Data table with filters & highlight rows. | OK | Medium | High | Transform table rows into mobile cards; convert filters grid. |
| **Chi tiết Đặt bàn** | `/Admin/Reservation/Details/{id}` | `Reservation/Details.cshtml` | Detail page | Information panels and notes form. | OK | Low | Medium | Stack columns, enlarge textarea, full-width buttons. |
| **Hộp thư Liên hệ** | `/Admin/Contact` | `Contact/Index.cshtml` | Table/List | Data table with status select. | OK | Medium | High | Mobile card transformation; wrap filter buttons cleanly. |
| **Chi tiết Liên hệ** | `/Admin/Contact/Details/{id}` | `Contact/Details.cshtml` | Detail page | Metadata grids and status updater. | OK | Low | Medium | Stack fields vertically; full-width form actions. |
| **Nhóm thực đơn** | `/Admin/MenuGroup` | `MenuGroup/Index.cshtml` | Table/List | 8-column table with images & action lists. | OK | High | High | Mobile card transformation; wrap actions, group operations. |
| **Tạo nhóm thực đơn** | `/Admin/MenuGroup/Create` | `MenuGroup/Create.cshtml` | Create form | Form fields, textarea, and cover file dropzone. | OK | Low | Medium | Stack grids; ensure upload helper labels stay readable. |
| **Sửa nhóm thực đơn** | `/Admin/MenuGroup/Edit/{id}` | `MenuGroup/Edit.cshtml` | Edit form | Form fields, thumbnail, and file dropzone. | OK | Low | Medium | Stack grids; optimize current thumbnail size constraints. |
| **Quản lý ảnh thực đơn** | `/Admin/MenuGroup/Images/{id}` | `MenuGroup/Images.cshtml` | Upload manager | Multi-upload dropzone, card grid with inputs. | OK | High | Critical | Optimize preview grid; group updates, limit infinite scroll height. |
| **Danh sách bài viết** | `/Admin/Post` | `Post/Index.cshtml` | Table/List | Wide table with excerpts, category badges. | OK | High | High | Mobile card transformation; truncate excerpt text. |
| **Tạo bài viết mới** | `/Admin/Post/Create` | `Post/Create.cshtml` | Create form | SEO fields grid, dropzone, content textarea. | OK | Medium | Medium | Optimize dense sub-grids; expand content field bounds. |
| **Chỉnh sửa bài viết** | `/Admin/Post/Edit/{id}` | `Post/Edit.cshtml` | Edit form | SEO grids, cover thumbnail, file dropzone. | OK | Medium | Medium | Similar to Create; improve existing thumbnail card layout. |
| **Danh sách Đánh giá** | `/Admin/Review` | `Review/Index.cshtml` | Table/List | Star rating tables, visibility badges. | OK | Low | Medium | Mobile card transformation; wrap rating star inline lists. |
| **Tạo đánh giá mới** | `/Admin/Review/Create` | `Review/Create.cshtml` | Create form | Standard text inputs and checkbox. | OK | Low | Low | Stack grids; convert actions to responsive alignments. |
| **Chỉnh sửa Đánh giá** | `/Admin/Review/Edit/{id}` | `Review/Edit.cshtml` | Edit form | Standard text inputs and checkbox. | OK | Low | Low | Similar to Create view. |
| **Danh sách Chi nhánh** | `/Admin/Branch` | `Branch/Index.cshtml` | Table/List | 6-column table, cover images, phone numbers. | OK | Medium | High | Mobile card transformation; wrap status badges. |
| **Thêm chi nhánh mới** | `/Admin/Branch/Create` | `Branch/Create.cshtml` | Create form | Dense sub-grids, hours, map, capacity, file. | OK | Medium | High | Refactor dense sub-grids to stack cleanly on mobile screen widths. |
| **Chỉnh sửa Chi nhánh** | `/Admin/Branch/Edit/{id}` | `Branch/Edit.cshtml` | Edit form | Dense sub-grids, hours, photo, file. | OK | Medium | High | Optimize dense grids; adjust existing photo layout bounds. |
| **Cấu hình Website** | `/Admin/Setting` | `Setting/Index.cshtml` | Settings form | Categorized sections, slogan, map, social grid. | OK | Low | Medium | Stack social inputs; convert submit to stretch button. |

## Highest Priority Pages

The following pages represent the highest usability risk on mobile and tablet devices and must be addressed first:

1.  **`Areas/Admin/Views/MenuGroup/Images.cshtml` (Quản lý ảnh thực đơn)**
    *   *Why*: This is the most complex page in the admin portal. It manages multi-upload forms, bulk delete actions, and a list of cards where each card renders an individual update form (containing Alt text, Display order, IsPublished checkbox) and a separate delete form. On mobile, this stacked layout stretches infinitely and leads to UI performance issues when loading many image assets.
2.  **`Areas/Admin/Views/MenuGroup/Index.cshtml` (Nhóm thực đơn list)**
    *   *Why*: The list contains 8 columns, including a cover photo, long short-description text, display order, image counts, visibility badges, and up to 4 inline action buttons/forms. On mobile, it squeezes the table headers and wraps button groups vertically, breaking layout consistency.
3.  **`Areas/Admin/Views/Branch/Create.cshtml` & `Edit.cshtml` (Chi nhánh forms)**
    *   *Why*: Unlike other forms, these views contain compact grids like `sm:grid-cols-2` and `sm:grid-cols-3` for numerical metrics (Capacity, Area, Floors) and operating hours. Below `sm` (640px), these stack tightly, but their labels and validations require precise wrapping to prevent overlap.
4.  **`Areas/Admin/Views/Post/Index.cshtml` (Danh sách bài viết)**
    *   *Why*: Renders long titles, slugs, and a preview of article excerpts inside columns, which forces massive horizontal viewport overflow. Action items are pushed completely offscreen.
5.  **`Areas/Admin/Views/Reservation/Index.cshtml` (Quản lý Đặt bàn)**
    *   *Why*: Supervisors frequently moderating reservations on mobile devices are forced to scroll horizontally to approve or view customer details, impacting operational speed.
