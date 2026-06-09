# Overlay and Brightness Analysis

## Overlay Layers

| Layer | File | Class/Selector | Effect | Brightness Impact |
|-------|------|----------------|--------|-------------------|
| Slide image source | `Views/Home/Index.cshtml` | Inline `--champong-hero-image` on each slide | Supplies the banner asset as a CSS background image | Medium. The assets are already composed on a dark tabletop/background |
| Base image treatment | `wwwroot/css/input.css` and `wwwroot/css/site.css` | `.champong-hero__media` | `background-size: cover`, `filter: saturate(1.04) contrast(1.03) brightness(0.92)`, slight scale | High. `brightness(0.92)` dims every slide before overlays apply |
| Vertical dark wash | `wwwroot/css/input.css` and `wwwroot/css/site.css` | `.champong-hero__media::before` | Top-to-bottom black gradient | Medium |
| Default directional wash | `wwwroot/css/input.css` and `wwwroot/css/site.css` | `.champong-hero__media::after` | Left-to-right black gradient plus extra vertical wash | High on slide 1 |
| Slide 2 override | `wwwroot/css/input.css` and `wwwroot/css/site.css` | `.champong-hero__slide--two .champong-hero__media::after` | Symmetrical side darkening for centered copy | Medium to high |
| Slide 3 override | `wwwroot/css/input.css` and `wwwroot/css/site.css` | `.champong-hero__slide--three .champong-hero__media::after` | Right-to-left darkening for right-aligned desktop copy | High on slide 3 |
| Mobile override | `wwwroot/css/input.css` and `wwwroot/css/site.css` | `@media (max-width: 767px) .champong-hero__media::before/::after` | Replaces the layered overlays with a heavier single vertical black wash | Very high on mobile |

## Text Contrast Protection

Text readability is protected by multiple layers already:

- `.champong-hero__title`
  - white text
  - strong text shadow: `0 24px 55px rgba(0, 0, 0, 0.72)`
- `.champong-hero__subtitle`
  - `text-brand-goldSoft`
  - text shadow: `0 18px 38px rgba(0, 0, 0, 0.5)`
- `.champong-hero__description`
  - `text-white/80`
  - text shadow: `0 18px 38px rgba(0, 0, 0, 0.5)`
- `.champong-hero__eyebrow`
  - translucent pill
  - border
  - blur backdrop
- `.champong-hero__btn--ghost`
  - translucent background
  - white border

This means the hero has more contrast protection than just the background overlay. There is room for a moderate brightness increase without immediately breaking readability.

## Image Treatment

The source images are not flat, bright studio-white assets. They are already photographed in a dark premium style:

- black/brown tabletop backgrounds
- spotlighting on food
- substantial negative space for typography

That is appropriate for the brand mood. The problem is that the CSS stack adds extra dimming on top of already dark source photography.

## Root Cause

Root cause is combined, not singular:

1. the banners are intentionally dark premium food photos,
2. `.champong-hero__media` applies `brightness(0.92)`,
3. `::before` adds a vertical black wash,
4. `::after` adds a second directional black wash,
5. mobile replaces both with an even heavier black overlay.

The JS active state is not the cause.

## Risk if Overlay Is Reduced Too Much

If the overlay is reduced too aggressively:

- slide 2 centered text may compete with brighter food detail behind it,
- slide 3 right-aligned desktop copy may lose contrast against brighter highlights,
- the ghost CTA can lose edge contrast,
- the gold eyebrow/subtitle can wash out faster than the white title,
- mobile readability can degrade first because the mobile layout stacks more text over the image with less directional separation.

## Recommended Brightness Target

Target direction:

- the food should look clearly more appetizing than it does now,
- the background should remain dark premium,
- the overlay should remain present,
- the improvement should feel moderate, not like a redesign.

Recommended target range for a later implementation:

- raise image brightness from `0.92` to roughly `0.98` or `1.00`,
- reduce the darkest overlay stops by roughly `0.08` to `0.12` opacity,
- check mobile separately because the mobile overlay is materially darker than desktop.
