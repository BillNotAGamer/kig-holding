# CSS and Component Reuse Plan

## Existing Reusable Patterns

| Source | Class/Pattern | Can Reuse? | Notes |
|--------|---------------|------------|-------|
| `input.css` | `.public-editorial-page` | **Yes** | Applies the signature dark radial/linear gradient background canvas. |
| `input.css` | `.public-editorial-page__shell` | **Yes** | Standardizes vertical paddings using `clamp(3rem, 6vw, 5.5rem)`. |
| `input.css` | `.public-editorial-page__section` | **Yes** | Inserts thin dividers (`border-top: 1px solid rgba(255, 255, 255, 0.08)`) between blocks. |
| `input.css` | `.public-editorial-page__hero` | **Yes** | Unboxed structure for hero layouts. |
| `input.css` | `.public-editorial-page__eyebrow` | **Yes** | Small uppercase text with a gradient red prefix line. |
| `input.css` | `.public-editorial-page__title` | **Yes** | Dynamic typography sizing using `clamp(2.35rem, 4.8vw, 4.6rem)`. |
| `input.css` | `.public-editorial-page__lead` | **Yes** | Applies the `max-width: 44rem` readability limit. |
| `input.css` | `.public-editorial-page__button--*` | **Yes** | Applies primary (gradient red) and secondary (border transparent) button styles. |
| `input.css` | `.public-editorial-page__grid` | **Yes** | Open grid structures (stacks from 3 or 4 columns to 1 column on mobile). |
| `input.css` | `.public-editorial-page__item` | **Yes** | Open list article items separated by top borders. |
| `input.css` | `.public-editorial-page__item-index` | **Yes** | Large editorial sequence indices (`01`, `02`). |
| `input.css` | `.public-editorial-page__columns` | **Yes** | Two-column grid with a left border on desktop. |
| `input.css` | `.public-editorial-page__bullet` | **Yes** | Custom bullet list using gradient red dots. |
| `input.css` | `.public-editorial-page__steps` | **Yes** | Custom sequence step blocks. |
| `input.css` | `.public-editorial-page__cta` | **Yes** | Unboxed pre-footer call-to-action wrapper. |

## Suggested Page Class Strategy
* **Recommendation:** Reuse the generic `.public-editorial-page__*` prefix classes directly in the view markup.
* **Rationale:**
  * The class system is fully implemented and compiled. Creating a duplicate `.about-page__*` series would result in redundant styles and increase stylesheet size.
  * Reusing the generic classes ensures that `/gioi-thieu` remains consistent with other public pages like `/thanh-vien` (Member page).
  * If the About page requires minor custom layout options (such as custom aspect ratios for gallery blocks), they can be implemented using standard Tailwind utility classes directly in the markup or mapped using small modifiers.

## CSS Changes Needed

| File | Change Type | Reason | Risk |
|------|-------------|--------|------|
| [wwwroot/css/input.css](file:///f:/Coding/Web development/KIG Holding/KIGHolding/wwwroot/css/input.css) | No changes required. | The existing class rules cover all layout options. | None. |

## JS Changes Needed

| File | Change Type | Reason | Risk |
|------|-------------|--------|------|
| [wwwroot/js/uw-reveal.js](file:///f:/Coding/Web development/KIG Holding/KIGHolding/wwwroot/js/uw-reveal.js) | No changes required. | Fully supports attribute-driven animations (delays, directions, etc.). | None. |

## Avoid Changing
* **Do not edit compiled output [site.css](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/wwwroot/css/site.css) manually.**
* Do not modify light theme components that are shared with the Admin panel.
* Do not alter styling blocks on sibling views like [Views/Franchise/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Franchise/Index.cshtml) or [Views/Member/Index.cshtml](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Member/Index.cshtml).

## Tailwind / CSS Build Notes
* Any changes to the view markup will be picked up by Tailwind's content scanner (as configured in `tailwind.config.js`).
* The stylesheet is compiled by executing:
  ```powershell
  npm run build:css
  ```
  This command must compile successfully without warnings.
