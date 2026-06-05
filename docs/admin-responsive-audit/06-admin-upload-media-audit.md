# Admin Upload and Media Audit

This document audits the file upload areas, image preview displays, reordering panels, and bulk deletion panels in the Admin area.

## Upload Surfaces

| Surface | View File | Controller Action | File Field | Max Files/Size | Current UI | Mobile Risk |
|---------|-----------|-------------------|------------|----------------|------------|-------------|
| **MenuGroup Cover** | `MenuGroup/Create.cshtml`, `Edit.cshtml` | `Create`, `Edit` (Post) | `CoverImageFile` | 1 file / 10MB | Drag-and-drop dropzone; single image preview. | **Medium**: Dropzone takes large height; preview expands off-center. |
| **Menu Pages Multi-Upload** | `MenuGroup/Images.cshtml` | `Upload` (Post via Route) | `ImageFiles` | Multiple files / 50MB per file / Max 10 files | Large dropzone; listed filenames; text counts. | **Medium**: Multi-file list overlaps if filenames are long. |
| **Post Thumbnail** | `Post/Create.cshtml`, `Edit.cshtml` | `Create`, `Edit` (Post) | `ThumbnailFile` | 1 file / 2MB | Dropzone; single image preview. | **Low**: Simple block layout. |
| **Branch Thumbnail** | `Branch/Create.cshtml`, `Edit.cshtml` | `Create`, `Edit` (Post) | `ThumbnailFile` | 1 file / 2MB | Dropzone; single image preview. | **Low**: Standard card structure. |

## Media Preview Areas

| Area | View File | Current Pattern | Mobile Risk | Recommended Pattern |
|------|-----------|-----------------|-------------|---------------------|
| **Menu Image Cards Grid** | `MenuGroup/Images.cshtml` | 3-column grid (`xl:grid-cols-3`, `md:grid-cols-2`). | **Critical**: Stacks into 1 column on mobile. 15+ cards create 7500px+ height. Each card contains 2 nested forms and 2 buttons. | Transform to 2-column compact grid below `md`. Reduce form elements inside card to compact inline icons, or use modal edit forms. |
| **Single Previews** | `MenuGroup/Edit.cshtml`, `Post/Edit.cshtml`, `Branch/Edit.cshtml` | Horizontal layout flexbox (`flex-col sm:flex-row`). | **Low**: Stacks correctly on mobile. | Keep current layout; enforce maximum thumbnail dimensions. |

## Bulk Actions

*   **Location**: [MenuGroup/Images.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/MenuGroup/Images.cshtml) (Lines 91-110).
*   **Controls**:
    *   `data-menu-image-select-all` (Checkbox to select all items).
    *   `data-menu-image-selected-count` (Display label: "Đã chọn X ảnh").
    *   `data-bulk-delete-submit` (Submit button targeting `#bulk-delete-form`).
*   **Usability Issue**: The bulk panel is statically placed at the top of the image list. On mobile, if a user scrolls down to select cards 10, 11, and 12, the delete button is pushed offscreen. The user must scroll back to the top of the page to execute the deletion.
*   **Recommendation**: Convert the bulk delete bar into a sticky bar at the bottom of the viewport (`fixed bottom-0 left-0 right-0 z-40 bg-white border-t border-brand-border p-4 shadow-panel flex justify-between items-center lg:static lg:shadow-none lg:p-0 lg:border-t-0`) when at least one checkbox is checked.

## Sorting/Reordering UI

*   **Location**: [MenuGroup/Images.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Areas/Admin/Views/MenuGroup/Images.cshtml) (Lines 185-192).
*   **Controls**: Number input `<input name="DisplayOrder" type="number" ... />` inside the individual update form for each card.
*   **Mobile Risk**: To reorder multiple images, a mobile user must tap the DisplayOrder input of card A, type a number, tap "Lưu thay đổi", wait for the page reload, scroll down, repeat for card B, and so on. This is tedious on mobile.
*   **Recommendation**: While dragging/sorting JavaScript is out of scope for layout changes, we can improve the layout by making the `DisplayOrder` input compact (inline) and placing the "Lưu thay đổi" button inline with the inputs to save vertical height.

## Delete UX

*   **Single Image Delete**: Triggers post request via `/Admin/MenuGroup/{menuGroupId}/Images/{imageId}/Delete`. Includes confirmation alert prompt.
*   **Bulk Delete**: Triggers post request via `/Admin/MenuGroup/{menuGroupId}/Images/BulkDelete` with checked IDs.
*   **Mobile Risk**: The single "Xóa ảnh" button is full-width and placed at the bottom of the card. On mobile screens, it is close to the select checkbox of the next card, leading to accidental delete triggers.
*   **Recommendation**: Keep confirmation prompts mandatory. Add spacing margins (`mt-4`) between the delete form block and the card boundary.

## Risks

1.  **Infinite Scroll Fatigue**: Mobile screens stacking 15+ cards with forms creates vertical scroll fatigue.
2.  **Accidental Image Deletions**: Single delete button placement is too close to neighboring cards.
3.  **Out-of-Viewport Bulk Actions**: Bulk delete buttons scroll out of view on mobile.
4.  **Cramped File Previews**: Image preview container `.admin-menu-image-preview` uses `height: clamp(13rem, 28vw, 18rem)`. On small screens (320px width), this forces the image to compress or scale poorly.

## Recommended Responsive Direction

Implement these mobile-first layouts for `MenuGroup/Images.cshtml`:

```txt
+-------------------------------------------------------+
|  [<- Back]   Menu Pages: Group Name                   |
+-------------------------------------------------------+
|  +-------------------------------------------------+  |
|  |  TẢI ẢNH THỰC ĐƠN                                |  |
|  |  [ Dropzone: Drag / Tap to Upload ]             |  |
|  +-------------------------------------------------+  |
+-------------------------------------------------------+
|  IMAGE PREVIEW GRID (2 columns below 768px)           |
|  +-----------------------+ +-----------------------+  |
|  | [x] Select            | | [ ] Select            |  |
|  | +-------------------+ | | +-------------------+ |  |
|  | |    Thumbnail      | | | |    Thumbnail      | |  |
|  | +-------------------+ | | +-------------------+ |  |
|  | Alt: text             | | Alt: text             |  |
|  | Order: [ 1 ]          | | Order: [ 2 ]          |  |
|  | [x] Published         | | [x] Published         |  |
|  |                       | |                       |  |
|  | [ Save ]   [ Delete ] | | [ Save ]   [ Delete ] |  |
|  +-----------------------+ +-----------------------+  |
+-------------------------------------------------------+
|  STICKY MOBILE BAR (visible if selectedCount > 0)     |
|  Selected: 1 image                     [ Xóa đã chọn ]|
+-------------------------------------------------------+
```

### CSS Modifications needed:
1.  **Preview Grid layout**: Replace `grid gap-4 p-4 md:grid-cols-2 md:p-6 xl:grid-cols-3` with `grid gap-3 p-3 grid-cols-2 md:gap-5 md:p-5 xl:grid-cols-3`. This enforces 2 columns even on small mobile screens.
2.  **Compact Cards**: Within the card, scale down the image preview container height (`height: clamp(8rem, 20vw, 12rem)`) and reduce paragraph spacing.
3.  **Button Layout**: Place "Lưu" and "Xóa" side-by-side inside the card using `grid grid-cols-2 gap-2` to save vertical space.
