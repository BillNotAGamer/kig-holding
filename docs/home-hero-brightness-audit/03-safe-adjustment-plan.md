# Safe Adjustment Plan

## Preferred Approach

- Adjust CSS variable/class in `input.css`

## Recommended Exact Change

Do not implement yet.

Most precise low-risk adjustment:

1. Keep the hero markup and inline image paths unchanged.
2. Keep the slider JS unchanged.
3. In `wwwroot/css/input.css`, tune the hero-specific selectors only:
   - `.champong-hero__media`
   - `.champong-hero__media::before`
   - `.champong-hero__media::after`
   - `.champong-hero__slide--two .champong-hero__media::after`
   - `.champong-hero__slide--three .champong-hero__media::after`
   - the mobile overlay override under `@media (max-width: 767px)`
4. First pass recommendation:
   - increase `brightness(0.92)` to approximately `brightness(0.98)` or `brightness(1)`
   - reduce the strongest black alpha stops in the hero overlay gradients modestly
   - preserve the existing gradient directions so the text alignment strategy still works per slide
5. Rebuild `site.css` from `input.css`.

Do not start with image asset editing.

Reason:

- the current assets already fit the dark premium mood,
- the darkening logic is centralized in hero-specific CSS,
- CSS is safer than editing assets that may be reused or later swapped.

## Files to Modify Later

| File | Planned Change | Risk |
|------|----------------|------|
| `wwwroot/css/input.css` | Tune `.champong-hero__media` brightness and reduce hero overlay opacity values slightly | Low if limited to `champong-hero` selectors |
| `wwwroot/css/site.css` | Generated output after rebuilding CSS only; no manual edits | Low |

Fallback only if CSS tuning proves inconsistent across slides:

| File | Planned Change | Risk |
|------|----------------|------|
| `Views/Home/Index.cshtml` | Add per-slide custom properties for overlay strength or brightness | Medium, because markup becomes more configuration-heavy |

## Acceptance Criteria

- Food image is brighter than the current state.
- Text remains readable.
- CTA buttons remain visible.
- Slider still works.
- No layout shift.
- No public page regression.
- Mobile still looks good.

## QA Checklist

Check:

- `/` at `375px`
- `/` at `430px`
- `/` at `768px`
- `/` at `1024px`
- `/` at `1440px`

Verify:

- slide 1 left-aligned text still reads clearly,
- slide 2 centered text remains readable,
- slide 3 right-aligned text remains readable on desktop and left-aligned on mobile,
- ghost button border remains visible,
- food looks slightly brighter, not washed out,
- hero still feels dark premium rather than flat or light.
