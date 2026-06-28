# Feature State & MVP Checklist

This document catalogs the operational state of the **Truyền Thuyết Champong** project, documenting MVP features, architectural design decisions, and build status.

---

## 1. Project Compilation & Build Verification

The application is in a stable, optimized state:
*   **Compile Status**: `Build Succeeded`
*   **Warnings**: `0 Warnings`
*   **Errors**: `0 Errors`
*   **Target Framework**: `.NET 10.0`

---

## 2. Completed MVP Feature Map (100% Functional)

### 1. Brand Home Page
*   **Dynamic Hero Slider**: Active slider supporting sliding transitions, custom backgrounds, and data-driven links. Powered by `wwwroot/js/home-hero-slider.js`.
*   **Brand Narrative Sections**: High-fidelity light-theme grids detailing KIG Holding's milestones.
*   **3-Card Concept Showcase**: Editorial teasers for the three main concepts: Truyền Thuyết Champong, Gogi Maru, and KBB Cook.
*   **Unified Booking CTAs**: Quick navigation hooks to launch reservations.

### 2. Multi-Concept Digital Menu Flipbook
*   **Flipbook Engine**: Custom mobile-friendly viewport render loop loaded from `wwwroot/js/menu-flipbook.js`.
*   **Concept Navigation**: Allows toggling between menus (e.g. Gogi Maru, KBB Cook).
*   **Backoffice File Uploads**: Allows admins to upload image plates, automatically indexing pages in display order.

### 3. Public Booking System (`/dat-ban`)
*   **Dynamic Client Validation**: Immediate input formatting (such as date and guest limit constraints).
*   **District Lockouts**: Enforces backend/frontend blocks on restricted zones based on app configuration limits.
*   **Occasion Tags**: Delimited checkboxes to tag celebration milestones (Birthday, Meeting, etc.).
*   **Resend Email Alerts**: Sends automated, rich HTML notifications to the business email once a reservation is successfully created.
*   **Quick Booking Modal**: Responsive slide-over modal for reservation bookings, accessible via headers, footers, and floating contact buttons.
*   **Floating Contact Routing**: Floating Contact Buttons support brand-specific Facebook/Zalo routing. Truyền Thuyết Champong, Gogi Maru, and KBB Cook each have their own Facebook/Zalo destination. Global `SiteSettings` social URLs remain available for global/footer/contact usage.

### 4. News & Media Hub (`/tin-tuc`)
*   **Category Filtration**: Fast AJAX and route-based classification.
*   **Editorial Excerpt Cards**: Elegant dark-styled article list cards.
*   **Suggested Reading Engine**: Dynamic categories suggestions component based on active readers.

### 5. Branch Locator (`/chi-nhanh`)
*   **Operational Break Windows**: Full formatting support for standard hours and branch lunch breaks.
*   **Google Maps Embeds**: Responsive iframe renders for individual branch coordinates.

### 6. Admin Back-office Management Portal (`/Admin`)
*   **Secure Authentication**: Standard cookie authentication flow backed by lockouts.
*   **Sidebar Routing Navigation**: Highlighted active markers using `ViewContext` paths.
*   **Global Layout Toast Alerts**: Integrated notification alerts using `TempData` banners.
*   **Image Upload Pipeline**: Railway Volume-ready local persistent storage via `LocalVolume` provider. Existing Cloudinary URLs remain completely backward-compatible.

---

## 3. Design Choice: Review Management Status

> [!NOTE]
> **Reviews Display State on the Public Website**
> Customer reviews are hidden on the public-facing homepage by design to maintain a polished, editorial look. However, the **Admin Reviews Portal (`/Admin/Review`) is fully operational and preserved**. This allows administrators to moderate, edit, and maintain customer reviews in the database, preserving the backend infrastructure for future frontend iterations.
