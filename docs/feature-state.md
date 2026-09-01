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
*   **Take-Home App Modal**: The homepage "Mang Champong về nhà" CTA opens a scoped app-download modal that reuses the footer QR code, App Store badge/link, and Google Play badge/link. The CTA keeps `/dat-ban` as its no-JavaScript fallback, does not claim an ordering backend, suppresses only the automatic homepage booking prompt for the current session, and leaves manual booking triggers available.

### 2. Multi-Concept Digital Menu Flipbook
*   **Flipbook Engine**: Custom mobile-friendly viewport render loop loaded from `wwwroot/js/menu-flipbook.js`.
*   **Concept Navigation**: Allows toggling between menus (e.g. Gogi Maru, KBB Cook).
*   **Backoffice File Uploads**: Allows admins to upload image plates, automatically indexing pages in display order.

### 3. Public Booking System (`/dat-ban`)
*   **Dynamic Client Validation**: Immediate input formatting (such as date and guest limit constraints). Full-page booking and the quick booking modal share the same server-generated reservation calendar-policy payload for Vietnam-local minimum date and active DB blocked-date UX feedback.
*   **District Lockouts**: Enforces backend/frontend blocks on restricted zones based on app configuration limits.
*   **Occasion Tags**: Delimited checkboxes to tag celebration milestones (Birthday, Meeting, etc.).
*   **Resend Email Alerts**: Sends automated, rich HTML notifications to the business email once a reservation is successfully created.
*   **Quick Booking Modal**: Responsive slide-over modal for reservation bookings, accessible via headers, footers, and floating contact buttons.
*   **Floating Contact Routing**: Floating Contact Buttons support brand-specific Facebook/Zalo routing. Truyền Thuyết Champong, Gogi Maru, and KBB Cook each have their own Facebook/Zalo destination. Global `SiteSettings` social URLs remain available for global/footer/contact usage.
*   **DB-Driven Reservation Date Policy**: Server-side policy rejects past dates and exact rows in `BlockedReservationDates` before branch database lookup, persistence, email, or real-time notification. Saturday and Sunday are allowed by default unless Admin blocks the exact date. There is no hard-coded holiday inference and no maximum open-calendar cap.
*   **Reservation Policy Admin State**: `/Admin/Reservation/Policy` provides an authenticated calendar for toggling today/future blocked dates. The `AddBlockedReservationDatePolicy` migration seeds 23 remaining future legacy closure dates, including `2026-09-02` and `2026-09-03`, as ordinary Admin-managed rows. The migration has been created for review but not applied by implementation work.

### 4. News & Media Hub (`/tin-tuc`)
*   **Category Filtration**: Fast AJAX and route-based classification.
*   **Editorial Excerpt Cards**: Elegant dark-styled article list cards.
*   **Suggested Reading Engine**: Dynamic categories suggestions component based on active readers.
*   **Blog HTML + SEO Phase 2**: Admin Create/Edit now use shared Visual/HTML authoring tabs over the same `Post.Content` field. Visual content remains plain text; HTML content is still sanitized server-side before persistence.
*   **Sanitized HTML Rendering**: Public article detail renders `Post.Content` as raw HTML only when `ContentMode=Html`; Visual posts continue through the encoded paragraph renderer for backward compatibility.
*   **Article SEO Output**: Blog detail pages emit SEO title/description fallbacks, canonical URLs, Open Graph, Twitter Card, article published/modified metadata, JSON-LD Article schema, and robots metadata. `FocusKeyword` remains editorial only and does not emit meta keywords.
*   **Noindex Sitemap Behavior**: Published posts with `RobotsIndex=false` remain publicly accessible but are excluded from `sitemap.xml` and receive `noindex` robots metadata.
*   **Pending Blog HTML + SEO Work**: Runtime editor polish and broader browser smoke coverage remain review items; the backend sanitizer, public mode gate, SEO output, and scoped article-content CSS are implemented.

### 5. Membership Program Page (`/thanh-vien`)
*   **Static Informational Policy Page**: `/thanh-vien` is now a published informational page for the **Truyền Thuyết Champong** membership program, not a coming-soon placeholder.
*   **Approved Policy Content**: The page presents the approved four tiers (Đồng, Bạc, Vàng, Kim Cương), point-earning rules, point-validity rules, invoice examples, birthday-week rates, and upgrade/downgrade rules.
*   **Tabbed Tier Presentation**: The four-tier detail area is rendered as one accessible tabbed module. Bronze is selected by default after progressive enhancement, the tier visual card uses owner-approved member assets, and all tier benefits, invoice examples, and status text remain server-rendered.
*   **Progress Display Boundary**: The membership card track is informational/decorative only. The page does not fabricate personalized "points remaining" values because there is no member-account progress data source in this phase.
*   **App Download Promotion**: The page ends with a scoped app-download promotion that reuses the existing footer QR code, App Store badge/link, and Google Play badge/link.
*   **No Backend Membership Engine**: This route does not implement member login, redemption workflows, account data, automatic calculations, or a database-backed membership model.
*   **Maintenance Contract**: Any future change to approved membership tiers, thresholds, point rates, invoice examples, birthday benefits, upgrade/downgrade rules, tier-tab markup, or point-validity rules must update the member page, its tests, and the relevant documentation in the same change.

### 6. Branch Locator (`/chi-nhanh`)
*   **Operational Break Windows**: Full formatting support for standard hours and branch lunch breaks.
*   **Google Maps Embeds**: Responsive iframe renders for individual branch coordinates.

### 7. Admin Back-office Management Portal (`/Admin`)
*   **Secure Authentication**: Standard cookie authentication flow backed by lockouts.
*   **Sidebar Routing Navigation**: Highlighted active markers using `ViewContext` paths.
*   **Global Layout Toast Alerts**: Integrated notification alerts using `TempData` banners.
*   **Reservation Policy Management**: Admin Reservation includes a policy tab for managing blocked booking dates. Past rows are cleaned by a hosted service and by Admin policy access/update fallback.
*   **Real-Time Reservation Alerts Phase 1**: Public website reservations now publish a best-effort `AdminReservationCreated` SignalR event after the database transaction commits. Authenticated Admin pages show a four-second dynamic toast. The Admin sound preference persists across visits, each page silently attempts browser-compliant unlock on the first normal interaction, and visible tabs attempt the local MP3 notification sound while hidden tabs stay silent. If playback is blocked, the preference remains enabled and a compact activation control appears. The client uses SignalR automatic reconnect with guarded terminal-close restart handling. The event payload excludes phone, email, notes, and other unnecessary personal data.
*   **Image Upload Pipeline**: Provider-based storage is implemented. `CloudflareR2` can handle new uploads through the S3-compatible API and returns public absolute URLs. `LocalVolume` remains supported for local/legacy `/uploads/...` compatibility, and existing Cloudinary URLs remain backward-compatible for legacy handling.

### 8. Image Storage Phase State
*   **Implemented in Phase 1**: R2 options, provider parsing, startup validation, S3-compatible client wrapper, R2 upload/delete, conservative delete routing, safe template configuration, and opt-in live R2 smoke-test coverage.
*   **Implemented in Phase 1.1**: New menu-page uploads use group-scoped R2 prefixes based on persisted `MenuGroup.Slug`, e.g. `menu-pages/truyen-thuyet-champong/<file>`. Existing root-level `menu-pages/<file>` objects remain supported; no existing object or database URL is migrated, and slug changes affect only future uploads.
*   **Canonical Limits**: storage-service `ImageStorage:MaxFileSizeBytes` is 50 MB; Kestrel and multipart transport limits remain 60 MB. Stricter feature-level limits remain: Branch thumbnail 2 MB, Post thumbnail 2 MB, Menu group cover 10 MB, Menu pages 50 MB.
*   **Not Yet Implemented**: managed asset registry, shared-reference delete protection, centralized URL resolver, legacy asset migration, decoder/magic-byte validation, production cutover, and full create/edit/delete lifecycle ownership refactor.
*   **Out of Scope for Real-Time Reservation Alerts Phase 1**: durable notification storage, notification history, browser push notifications, service workers, Redis, SignalR backplane, polling fallback, Admin manual reservation creation, multi-instance fan-out, and guaranteed single-audio behavior in browsers without Web Locks.

---

## 3. Design Choice: Review Management Status

> [!NOTE]
> **Reviews Display State on the Public Website**
> Customer reviews are hidden on the public-facing homepage by design to maintain a polished, editorial look. However, the **Admin Reviews Portal (`/Admin/Review`) is fully operational and preserved**. This allows administrators to moderate, edit, and maintain customer reviews in the database, preserving the backend infrastructure for future frontend iterations.
