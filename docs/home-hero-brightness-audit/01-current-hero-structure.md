# Current Home Hero Structure

## Hero Location

| File | Section / Line Area | Purpose |
|------|---------------------|---------|
| `Views/Home/Index.cshtml` | 37-151 | Home hero slider markup, slide content, CTA buttons, nav buttons, dots |
| `wwwroot/css/input.css` | 2797-2995 | Authored hero height, image treatment, overlay gradients, text styling, responsive behavior |
| `wwwroot/css/site.css` | 3937-4478 | Compiled output that mirrors the authored hero styles |
| `wwwroot/js/home-hero-slider.js` | 1-234 | Autoplay, active-state toggling, dots, nav, keyboard support, reveal timing |

## Slide Data / Image Sources

| Slide | Image Path | Text | CTA(s) | Notes |
|-------|------------|------|--------|-------|
| 1 | `/images/home/images/banner1.webp` | `Champong cho những bữa ăn có gu.` | `/dat-ban`, `/thuc-don` | Inline style uses `--champong-hero-image-position: 72% center`; content aligned left |
| 2 | `/images/home/images/banner2.webp` | `Hương vị Hàn Quốc trong không gian hiện đại` | `/thuc-don`, `/dat-ban` | Inline style uses `center`; content aligned center |
| 3 | `/images/home/images/banner3.webp` | `Hương vị đậm đà, nâng nhịp mọi cuộc gặp.` | `/thuc-don`, `/chi-nhanh` | Inline style uses `28% center`; content aligned right on desktop, left on mobile |

## Hero Layout

The hero structure is:

1. `<section class="champong-hero ...">`
2. slider root: `[data-champong-hero-slider]`
3. per-slide `<article data-champong-hero-slide ...>`
4. background layer: `<div class="champong-hero__media">`
5. content wrapper: `.container-page.champong-hero__inner`
6. content block:
   - `.champong-hero__content--left`
   - `.champong-hero__content--center`
   - `.champong-hero__content--right`
7. CTA row: `.champong-hero__actions`
8. nav buttons: `[data-champong-hero-prev]`, `[data-champong-hero-next]`
9. dots: `[data-champong-hero-dot]`

The images are not `<img>` elements inside the hero. They are CSS background images passed through inline custom properties on each slide.

## Hero Height

Desktop/default:

- `.champong-hero { min-height: clamp(38rem, calc(100svh - 5rem), 54rem); }`

Mobile:

- `@media (max-width: 767px)`
- `.champong-hero { min-height: clamp(38rem, calc(100svh - 4rem), 48rem); }`

Content vertical padding:

- default: `py-24`
- `sm`: `py-28`
- `lg`: `py-32`

## Slider Controls

Markup:

- previous button at `Views/Home/Index.cshtml:134-138`
- next button at `Views/Home/Index.cshtml:139-143`
- dots at `Views/Home/Index.cshtml:145-148`

Behavior from `wwwroot/js/home-hero-slider.js`:

- autoplay delay: `6500ms`
- transition duration used by JS hide timing: `700ms`
- keyboard support: `ArrowLeft`, `ArrowRight`
- pauses on hover, focus, and hidden tab
- dots update `aria-current`

## JS Dependencies

Required attributes/selectors:

- `[data-champong-hero-slider]`
- `[data-champong-hero-slide]`
- `[data-champong-hero-prev]`
- `[data-champong-hero-next]`
- `[data-champong-hero-dot]`
- `[data-champong-hero-reveal]`

Important audit finding:

- JS does not change brightness or overlay opacity.
- JS only toggles slide visibility, active state, z-index, opacity, pointer events, and reveal classes.

## Responsive Behavior

Desktop:

- slide 1 content left
- slide 2 content centered
- slide 3 content right
- side nav buttons visible from `md`

Mobile under `768px`:

- all content realigns left
- hero title reduces to `text-4xl`
- action alignment resets left
- hero overlay becomes a heavier single vertical gradient
- side nav buttons are hidden
- dots remain visible at bottom center
