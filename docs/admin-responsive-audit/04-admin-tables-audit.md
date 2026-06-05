# Admin Tables Audit

This document reviews the wide tables and lists present in the Admin panel and details their responsive redesign strategy.

## Table Responsive Strategy Matrix

| Admin Table | Current Problem | Recommended Pattern | Complexity | Risk |
|-------------|-----------------|---------------------|------------|------|
| **Reservation Table** | 7 columns. Pushes "Xem chi tiết" actions offscreen; horizontal overflow. | Card Transformation below 768px. | Medium | Low |
| **Contact Table** | 6 columns. Subject column stretches viewport; action buttons hidden. | Card Transformation below 768px. | Medium | Low |
| **MenuGroup Table** | 8 columns, 4 row actions. Squeezes layout; actions wrap into tall stacked items. | Card Transformation below 1024px. | High | Medium |
| **Post Table** | 7 columns. Excerpts and slugs cause excessive column width. | Card Transformation below 1024px. | Medium | Low |
| **Review Table** | 5 columns. Star icons stretch layout. Actions wrap. | Card Transformation below 640px. | Low | Low |
| **Branch Table** | 6 columns. Long addresses and hotlines break table cells. | Card Transformation below 768px. | Medium | Low |

---

## Table: Reservation List

- **View file**: [Reservation/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Reservation/Index.cshtml)
- **Data represented**: CustomerName, PhoneNumber, BranchName, ReservationDate, ReservationTime, GuestCount, Status, StatusLabel.
- **Current columns**: Khách hàng, Điện thoại, Chi nhánh, Ngày giờ, Số khách, Trạng thái, Hành động.
- **Row actions**: "Xem chi tiết" (Details link).
- **Filters/search**: Customer name or phone number (`SearchQuery`), Status (`StatusFilter`), Branch (`BranchFilter`).
- **Pagination**: None.
- **Desktop behavior**: Renders a complete grid with highlight row classes (`admin-row-highlight--warning`) for pending items.
- **Tablet risk**: Compact viewport compresses date and status columns.
- **Mobile risk**: Horizontal scroll hides the "Xem chi tiết" action.
- **Recommended responsive pattern**:
  *   **Card transformation**: Convert table rows into standalone cards (`.admin-card`) below `md` (768px).
  *   **Priority layout**: Render name, date/time, guest count, and status prominently.
- **Data that must remain visible on mobile**: CustomerName, Date/Time, GuestCount, Status, Details button.
- **Data that can be hidden on mobile**: PhoneNumber, BranchName (show branch only as a small subtitle under the customer name).
- **Actions that must remain visible**: "Xem chi tiết" button (styled as full-width secondary button at the bottom of the card).

---

## Table: Contact List

- **View file**: [Contact/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Contact/Index.cshtml)
- **Data represented**: FullName, PhoneNumber, Subject, Status, CreatedAt.
- **Current columns**: Khách hàng, Điện thoại, Nội dung (Subject), Trạng thái, Thời gian, Hành động.
- **Row actions**: "Xem chi tiết" (Details link).
- **Filters/search**: Status filter (`StatusFilter`).
- **Pagination**: None.
- **Desktop behavior**: Large layout. The `Subject` column has a `max-w-sm` container which helps, but doesn't prevent overflow on small screens.
- **Tablet risk**: Date and subject columns overlap.
- **Mobile risk**: Navigation to details is hidden on the right side.
- **Recommended responsive pattern**:
  *   **Card transformation**: Stack details as cards on mobile.
  *   **Highlights**: Apply warning borders for new messages (`ContactMessageStatus.New`).
- **Data that must remain visible on mobile**: FullName, CreatedAt, Subject, Status, Details button.
- **Data that can be hidden on mobile**: PhoneNumber (accessible inside details view).
- **Actions that must remain visible**: "Xem chi tiết" button.

---

## Table: MenuGroup List

- **View file**: [MenuGroup/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/MenuGroup/Index.cshtml)
- **Data represented**: CoverImageUrl, Name, Slug, ShortDescription, DisplayOrder, PageImageCount, IsPublished, UpdatedAt.
- **Current columns**: Ảnh bìa, Nhóm thực đơn, Mô tả ngắn, Thứ tự, Ảnh menu, Trạng thái, Cập nhật, Hành động.
- **Row actions**: "Quản lý ảnh" (Images link), "Sửa" (Edit link), "Ẩn/Hiện lại" (TogglePublished form submit), "Ẩn nhóm" (Delete form submit).
- **Filters/search**: Search query (`q`), Status (`status`).
- **Pagination**: None.
- **Desktop behavior**: Multi-action panel. Action column contains multiple buttons and inline forms.
- **Tablet risk**: Squeezes short-description text into tall, narrow columns. Action buttons wrap into double lines.
- **Mobile risk**: Table is completely unreadable; actions list wraps into a chaotic block of 4 vertical buttons.
- **Recommended responsive pattern**:
  *   **Card transformation**: Convert to cards below `lg` (1024px) for clear layout representation.
  *   **Action Bar**: Group actions at the bottom of the card into a clean flex list.
- **Data that must remain visible on mobile**: CoverImage, Name, DisplayOrder, Status, Actions.
- **Data that can be hidden on mobile**: Slug, ShortDescription, UpdatedAt.
- **Actions that must remain visible**: "Quản lý ảnh", "Sửa".
- **Actions that can move into overflow menu**: "Ẩn/Hiện lại" and "Ẩn nhóm" can be combined or simplified.

---

## Table: Post List

- **View file**: [Post/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Post/Index.cshtml)
- **Data represented**: ThumbnailUrl, Title, Slug, Excerpt, CategoryDisplayName, IsPublished, PublishedAt, UpdatedAt.
- **Current columns**: Ảnh, Bài viết, Danh mục, Trạng thái, Ngày đăng, Cập nhật, Hành động.
- **Row actions**: "Xem công khai" (Public link), "Sửa" (Edit link), "Ẩn bài" (Delete form submit).
- **Filters/search**: Search query (`q`), Category (`category`), Status (`status`).
- **Pagination**: Bottom number lists.
- **Desktop behavior**: Fully-featured dashboard table with large images and text details.
- **Tablet risk**: The Excerpt preview stretches the layout.
- **Mobile risk**: Title and status columns take up the entire viewport; actions list overflows.
- **Recommended responsive pattern**:
  *   **Card transformation**: Convert to cards below `lg` (1024px).
  *   **Image Float**: Float the thumbnail to the left of the title in the card view.
- **Data that must remain visible on mobile**: Thumbnail, Title, Category, Status, Actions.
- **Data that can be hidden on mobile**: Slug, Excerpt (truncate or hide on mobile), PublishedAt, UpdatedAt.
- **Actions that must remain visible**: "Sửa" and "Xem công khai".
- **Actions that can move into overflow menu**: "Ẩn bài" (destructive action).

---

## Table: Review List

- **View file**: [Review/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Review/Index.cshtml)
- **Data represented**: CustomerName, Rating, IsVisible, DisplayOrder.
- **Current columns**: Khách hàng, Đánh giá, Hiển thị, Thứ tự, Hành động.
- **Row actions**: "Sửa" (Edit link), "Ẩn" (Delete form submit).
- **Filters/search**: None.
- **Pagination**: None.
- **Desktop behavior**: Simple, clean table with inline unicode rating stars (★).
- **Tablet risk**: Very low. Columns fit standard viewports.
- **Mobile risk**: Action buttons wrap and break row alignment on narrow screens (under 480px).
- **Recommended responsive pattern**:
  *   **Card transformation**: Convert to cards below `sm` (640px).
  *   **Stars inline**: Display customer name and rating stars side by side.
- **Data that must remain visible on mobile**: CustomerName, Rating stars, Visibility status, Actions.
- **Data that can be hidden on mobile**: DisplayOrder.
- **Actions that must remain visible**: "Sửa" and "Ẩn".

---

## Table: Branch List

- **View file**: [Branch/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/Branch/Index.cshtml)
- **Data represented**: ThumbnailUrl, Name, Slug, Address, District, City, Hotline, IsActive.
- **Current columns**: Ảnh, Chi nhánh, Thành phố, Hotline, Trạng thái, Hành động.
- **Row actions**: "Sửa" (Edit link), "Xóa mềm" (Delete form submit).
- **Filters/search**: None.
- **Pagination**: None.
- **Desktop behavior**: Clean card list featuring thumbnail images and hotline links.
- **Tablet risk**: Hotline and action columns are pushed out.
- **Mobile risk**: Table cells wrap unevenly, stretching columns.
- **Recommended responsive pattern**:
  *   **Card transformation**: Convert to cards below `md` (768px).
- **Data that must remain visible on mobile**: Thumbnail, Name, City, Hotline, Status, Actions.
- **Data that can be hidden on mobile**: Slug, detailed Address (show name + city).
- **Actions that must remain visible**: "Sửa" and "Xóa mềm".
