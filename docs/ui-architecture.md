# UI Architecture

## Structural Logic

The user interface relies on strict, repeatable CSS patterns to maintain visual hierarchy.

### "Checkerboard" Brand Story Pattern
The Brand Story section (`Views/Home/Index.cshtml`) implements an alternating "Checkerboard" layout:
* Utilizing CSS Grid (`lg:grid-cols-2`).
* **Row 1**: Displays the Image on the left (`order-1 lg:order-1`) and Text on the right (`order-2 lg:order-2`).
* **Row 2**: Alternates the layout via Tailwind order utilities, putting Text on the left (`order-2 lg:order-1`) and the Image on the right (`order-1 lg:order-2`).

### "Menu Ecosystem Showcase" Pattern
The old menu grid was refactored into three massive horizontal cards:
* Each card acts as a standalone block for distinct categories (Champong, BBQ, Combo).
* The layout relies on `flex-col lg:flex-row`, stacking vertically on mobile and horizontally on desktop with 50/50 image and text split panes.

## Animations and Interactions

### Scroll Reveal Animations
* **Trigger Classes**: HTML nodes are marked with classes such as `.reveal-up`, `.reveal-left`, and `.reveal-right`.
* **Execution**: An `IntersectionObserver` in `site.js` monitors these elements. Upon intersecting the viewport, it appends an `.is-visible` class which fires CSS transition rules (transform and opacity) stored in `input.css`.

### Image Hover Zoom
To achieve dynamic visual engagement without JavaScript overhead:
* Images are wrapped in a container possessing specific group utility classes (`group`, `overflow-hidden`).
* A custom CSS rule (`.food-image-wrap:hover .food-image, .group:hover .food-image`) gracefully applies a `transform: scale(1.08)` to the child image, creating an editorial-style zoom effect.
