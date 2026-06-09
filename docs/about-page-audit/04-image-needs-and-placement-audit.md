# Image Needs and Placement Audit

## Existing Provided Image

| Asset | Visual Description | Best Use | Recommended Ratio | Notes |
|-------|--------------------|----------|-------------------|-------|
| `bìa 1.png` | **Not available** in workspace. (If provided later, expected to be a high-end cover visual of KIG food/concept). | Main Hero graphic or ecosystem visual | `16:9` (landscape) | Should be stored under `/images/about/` and optimized once provided. |

## Current Page Image Slots

| Page Area | Current Image/Placeholder | Issue | Recommendation |
|-----------|---------------------------|-------|----------------|
| **Hero Right Column** | `/images/placeholders/about-hero.webp` | File is missing; `onerror` completely deletes the image element, leaving a blank column. | Replace with a high-resolution landscape image slot (`aspect-[16/9]`) showcasing the KIG brand ecosystem. |
| **Origin Left Column** | `/images/placeholders/korean-street-noodle.webp` | File is missing; the container uses a heavy `.food-image-wrap` shadow class in a light layout. | Replace with a portrait detail image (`aspect-[4/5]`) showing cooking craft (steam, noodle prep). |
| **Food Philosophy Right Column** | `/images/placeholders/champong-broth.webp` | File is missing. Has a light-themed caption block (`bg-white/92`) absolute-positioned over it. | Replace with a close-up signature broth detail image (`aspect-[4/5]`). Swap the white caption block with subtle editorial text overlays. |
| **Restaurant Space Gallery** | 4 images: `group-tables.webp`, `kitchen-chef.webp`, `food-table.webp`, `service-counter.webp` | All files are missing. Gallery contains white background captions (`bg-white/90`) that clash with dark premium styles. | Rebuild as a clean, minimal 3-image grid displaying authentic interior scenes. Remove heavy white captions. |

## Recommended Image Plan

| Image Need | Purpose | Suggested Ratio | Suggested Orientation | Suggested Visual Direction | Priority |
|------------|---------|-----------------|-----------------------|----------------------------|----------|
| **Hero Brand Visual** | Sets the premium tone of the page immediately upon loading. | `16:9` | Landscape | A clean, high-contrast flat lay of signature food items under single-spotlighting. | High |
| **Culinary Origin Craft** | Supports the brand story of traditional cooking techniques. | `4:5` | Portrait | Close-up action shot of hand-pulled noodles or a chef seasoning soup with rising steam. | Medium |
| **Signature Dish close-up** | Demonstrates quality and freshness. | `4:5` | Portrait | A dramatic close-up of sizzling BBQ meat on a grill or a hot bowl of Champong soup. | Medium |
| **Interior Atmosphere** | Highlights the dining environment and cozy tables. | `16:10` | Landscape | Warm, ambient restaurant space view showing empty, well-lit group dining tables. | Low |

## Core Questions Answered

### 1. Can `bìa 1.png` be used as the hero image?
Yes. If it is a high-resolution landscape graphic representing the restaurant group, it will fit the right-hand column of the Hero section. If the asset is a vertical graphic, it should be cropped or shifted to a `16:9` landscape aspect ratio to align with the typography columns.

### 2. Should the About page use only this image or require more images?
The page requires a curated set of 3-4 images to balance the copy. Relying on a single image would make sections like *Restaurant Space* or *Food Philosophy* look text-heavy, reducing the premium editorial feel.

### 3. What extra image types would improve the page?
* **A detailed cooking close-up:** Emphasizes quality (sizzling meat, soup steam).
* **A group interior shot:** Highlights the dining experience (cozy group tables, modern Korean design).
* **A brand collage:** Highlights the variety across the three holding brands.

### 4. Where should each image appear?
* **Main Hero (Right Column):** Wide landscape brand ecosystem visual.
* **Origin Section (Left Column):** Vertical portrait image showing cooking craft.
* **Food Philosophy (Right Column):** Vertical portrait close-up of signature dishes.
* **Space Gallery (Bottom Section):** A clean horizontal gallery grid showing restaurant interiors.

### 5. What aspect ratio is best for each image?
* **Hero Banner:** `16:9` or wide crop.
* **Story & Detail Sections:** `4:5` vertical portrait (maximizes vertical line alignment with text columns).
* **Interiors Gallery:** `16:10` or `3:2` landscape.

### 6. Which images should be generated later?
Specific restaurant interior gallery photos and signature food close-ups should be generated later once custom photography or branding assets are prepared.

### 7. Which sections should stay text-only for editorial premium feel?
* **Core Values Section:** Keeping this section text-only with large numbering (`01` to `04`) maintains a clean, spacious look.
* **Brand Ecosystem Section:** Highlighting the three brands (*Truyền Thuyết Champong*, *Gogimaru*, *KBB Cook*) in a 3-column open text layout looks more sophisticated than using a cluster of small images.
