# UI Color Audit & Migration Plan

## Executive Summary
- **Current status**: The project has initiated a migration to a modern red theme, but numerous legacy gold, amber, and yellow tokens and utility classes remain active across Razor views, CSS variables, and Tailwind configurations.
- **Main gold leak sources**: 
  - CSS Variables in `input.css` (`--color-brand-gold`, etc.)
  - Tailwind utilities in `.cshtml` files (`brand-gold`, `brand-goldDeep`, `brand-goldSoft`)
  - A few hardcoded hex codes.
- **Recommended strategy**: 
  1. Temporarily remap legacy CSS tokens to the new red variants in `input.css` to immediately fix visual leaks.
  2. Incrementally replace Tailwind utility classes in Razor components (Phase 2).
  3. Rebuild Tailwind and verify UI.
  4. Finally, remove legacy gold variables entirely.

## Token Migration Recommendation
- **Current tokens**:
  - `--color-brand-gold`
  - `--color-brand-gold-deep`
  - `--color-brand-gold-soft`
- **Proposed red tokens**:
  - `--color-brand-primary: #A30D1E`
  - `--color-brand-primary-deep: #7A0714`
  - `--color-brand-primary-soft: #FDECEE`
- **Deprecated aliases**: `brand-gold`, `brand-goldDeep`, `brand-goldSoft`
- **Removal/remap decision**: Remap temporarily in `input.css` to ensure full backward compatibility while preventing legacy colors from rendering.

## Component Impact Map
- **Header**: Navigation links, active states, buttons.
- **Mobile Menu**: Mobile links, borders, background tints.
- **Hero**: Eyebrow texts, gradients.
- **Buttons**: Hover states, background colors.
- **Cards (Food/Branch/Post)**: Text accents, border colors on hover.
- **Forms**: Reservation form focus rings, informational texts.
- **Footer**: Hover states on links.
- **Admin/Shared UI**: Some shared layout styles and badges.

## Proposed Changes (Grouped by File)

### \Areas\Admin\Views\Auth\Login.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 51 | `<div class="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(184,154,98,0.16),transparen` | B. Decorative background/gradient leak | Use red/neutral gradients (e.g., rgba(163,13,30,...)) | Medium |
| 56 | `<span class="mx-auto grid size-14 place-items-center rounded-md bg-brand-gold text-xl font-black tex` | A. Brand accent leak | brand-primary | Low |
| 57 | `<p class="mt-5 text-kicker font-black uppercase text-brand-goldDeep">Admin Authentication</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 72 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Tên đ` | A. Brand accent leak | brand-primary-deep | Low |
| 78 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Mật k` | A. Brand accent leak | brand-primary-deep | Low |
| 84 | `<input asp-for="RememberMe" class="h-4 w-4 rounded border-brand-border text-brand-goldDeep focus:rin` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Branch\Create.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 10 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Create branch</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 23 | `<label asp-for="Name" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand` | A. Brand accent leak | brand-primary-deep | Low |
| 29 | `<label asp-for="Slug" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand` | A. Brand accent leak | brand-primary-deep | Low |
| 36 | `<label asp-for="Address" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-br` | A. Brand accent leak | brand-primary-deep | Low |
| 43 | `<label asp-for="District" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-b` | A. Brand accent leak | brand-primary-deep | Low |
| 48 | `<label asp-for="City" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand` | A. Brand accent leak | brand-primary-deep | Low |
| 56 | `<label asp-for="Hotline" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-br` | A. Brand accent leak | brand-primary-deep | Low |
| 61 | `<label asp-for="Email" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-bran` | A. Brand accent leak | brand-primary-deep | Low |
| 68 | `<label asp-for="GoogleMapUrl" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] te` | A. Brand accent leak | brand-primary-deep | Low |
| 77 | `<label asp-for="OpeningTime" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] tex` | A. Brand accent leak | brand-primary-deep | Low |
| 82 | `<label asp-for="ClosingTime" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] tex` | A. Brand accent leak | brand-primary-deep | Low |
| 90 | `<label asp-for="Capacity" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-b` | A. Brand accent leak | brand-primary-deep | Low |
| 95 | `<label asp-for="AreaSquareMeters" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em` | A. Brand accent leak | brand-primary-deep | Low |
| 100 | `<label asp-for="NumberOfFloors" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] ` | A. Brand accent leak | brand-primary-deep | Low |
| 107 | `<label asp-for="DisplayOrder" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] te` | A. Brand accent leak | brand-primary-deep | Low |
| 113 | `<label asp-for="ThumbnailFile" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] t` | A. Brand accent leak | brand-primary-deep | Low |
| 120 | `<label asp-for="Description" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] tex` | A. Brand accent leak | brand-primary-deep | Low |
| 126 | `<input asp-for="IsActive" type="checkbox" class="size-4 rounded border-brand-border text-brand-goldD` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Branch\Edit.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 10 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Update branch</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 26 | `<label asp-for="Name" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand` | A. Brand accent leak | brand-primary-deep | Low |
| 32 | `<label asp-for="Slug" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand` | A. Brand accent leak | brand-primary-deep | Low |
| 38 | `<label asp-for="Address" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-br` | A. Brand accent leak | brand-primary-deep | Low |
| 45 | `<label asp-for="District" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-b` | A. Brand accent leak | brand-primary-deep | Low |
| 50 | `<label asp-for="City" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand` | A. Brand accent leak | brand-primary-deep | Low |
| 58 | `<label asp-for="Hotline" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-br` | A. Brand accent leak | brand-primary-deep | Low |
| 63 | `<label asp-for="Email" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-bran` | A. Brand accent leak | brand-primary-deep | Low |
| 70 | `<label asp-for="GoogleMapUrl" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] te` | A. Brand accent leak | brand-primary-deep | Low |
| 79 | `<label asp-for="OpeningTime" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] tex` | A. Brand accent leak | brand-primary-deep | Low |
| 84 | `<label asp-for="ClosingTime" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] tex` | A. Brand accent leak | brand-primary-deep | Low |
| 92 | `<label asp-for="Capacity" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-b` | A. Brand accent leak | brand-primary-deep | Low |
| 97 | `<label asp-for="AreaSquareMeters" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em` | A. Brand accent leak | brand-primary-deep | Low |
| 102 | `<label asp-for="NumberOfFloors" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] ` | A. Brand accent leak | brand-primary-deep | Low |
| 109 | `<label asp-for="DisplayOrder" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] te` | A. Brand accent leak | brand-primary-deep | Low |
| 115 | `<label asp-for="ThumbnailFile" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] t` | A. Brand accent leak | brand-primary-deep | Low |
| 128 | `<label asp-for="Description" class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] tex` | A. Brand accent leak | brand-primary-deep | Low |
| 134 | `<input asp-for="IsActive" type="checkbox" class="size-4 rounded border-brand-border text-brand-goldD` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Branch\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 10 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Branch CRUD</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 65 | `<a href="@Url.Action("Edit", new { id = branch.Id })" class="font-semibold text-brand-goldDeep trans` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Contact\Details.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 11 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Contact Detail</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Contact\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 10 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Contact Inbox</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 45 | `ContactMessageStatus.New => "border-amber-200 bg-amber-50 text-amber-700",` | C. Semantic warning/status color | Keep as is, or use standard yellow/amber. | Medium |
| 71 | `<a asp-action="Details" asp-route-id="@message.Id" class="font-semibold text-brand-goldDeep transiti` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Dashboard\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 9 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Welcome</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 18 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Tổng chi nhánh</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 22 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Chi nhánh hoạt động` | A. Brand accent leak | brand-primary-deep | Low |
| 26 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Bài viết</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 30 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Tin nhắn / Đặt bàn<` | A. Brand accent leak | brand-primary-deep | Low |
| 39 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Tiếp theo</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 50 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Trạng thái hệ thống</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\MenuCategory\Create.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 9 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Menu Categories</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 49 | `<input asp-for="IsActive" type="checkbox" class="size-4 rounded border-brand-border text-brand-goldD` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\MenuCategory\Edit.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 9 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Menu Categories</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 58 | `<input asp-for="IsActive" type="checkbox" class="size-4 rounded border-brand-border text-brand-goldD` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\MenuCategory\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 10 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Menu Categories</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 61 | `<a asp-action="Edit" asp-route-id="@category.Id" class="font-semibold text-brand-goldDeep transition` | A. Brand accent leak | brand-primary-deep | Low |
| 64 | `<button type="submit" class="font-semibold @(category.IsActive ? "text-brand-redDark hover:text-bran` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\MenuItem\Create.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 9 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Menu Items</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 88 | `<input asp-for="IsSignature" type="checkbox" class="size-4 rounded border-brand-border text-brand-go` | A. Brand accent leak | brand-primary-deep | Low |
| 92 | `<input asp-for="IsBestSeller" type="checkbox" class="size-4 rounded border-brand-border text-brand-g` | A. Brand accent leak | brand-primary-deep | Low |
| 96 | `<input asp-for="IsNew" type="checkbox" class="size-4 rounded border-brand-border text-brand-goldDeep` | A. Brand accent leak | brand-primary-deep | Low |
| 100 | `<input asp-for="IsCombo" type="checkbox" class="size-4 rounded border-brand-border text-brand-goldDe` | A. Brand accent leak | brand-primary-deep | Low |
| 106 | `<input asp-for="IsAvailable" type="checkbox" class="size-4 rounded border-brand-border text-brand-go` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\MenuItem\Edit.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 9 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Menu Items</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 89 | `<input asp-for="IsSignature" type="checkbox" class="size-4 rounded border-brand-border text-brand-go` | A. Brand accent leak | brand-primary-deep | Low |
| 93 | `<input asp-for="IsBestSeller" type="checkbox" class="size-4 rounded border-brand-border text-brand-g` | A. Brand accent leak | brand-primary-deep | Low |
| 97 | `<input asp-for="IsNew" type="checkbox" class="size-4 rounded border-brand-border text-brand-goldDeep` | A. Brand accent leak | brand-primary-deep | Low |
| 101 | `<input asp-for="IsCombo" type="checkbox" class="size-4 rounded border-brand-border text-brand-goldDe` | A. Brand accent leak | brand-primary-deep | Low |
| 107 | `<input asp-for="IsAvailable" type="checkbox" class="size-4 rounded border-brand-border text-brand-go` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\MenuItem\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 10 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Menu Items</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 71 | `<a asp-action="Edit" asp-route-id="@item.Id" class="font-semibold text-brand-goldDeep transition hov` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Post\Create.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 9 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Posts</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 73 | `<input asp-for="IsPublished" type="checkbox" class="size-4 rounded border-brand-border text-brand-go` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Post\Edit.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 9 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Posts</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 74 | `<input asp-for="IsPublished" type="checkbox" class="size-4 rounded border-brand-border text-brand-go` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Post\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 10 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Posts</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 61 | `<a asp-action="Edit" asp-route-id="@post.Id" class="font-semibold text-brand-goldDeep transition hov` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Reservation\Details.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 11 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Reservation Detail</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Reservation\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 10 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Reservation Ops</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 50 | `ReservationStatus.Pending => "border-amber-200 bg-amber-50 text-amber-700",` | C. Semantic warning/status color | Keep as is, or use standard yellow/amber. | Medium |
| 82 | `<a asp-action="Details" asp-route-id="@reservation.Id" class="font-semibold text-brand-goldDeep tran` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Review\Create.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 9 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Reviews</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 47 | `<input asp-for="IsVisible" type="checkbox" class="size-4 rounded border-brand-border text-brand-gold` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Review\Edit.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 9 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Reviews</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 48 | `<input asp-for="IsVisible" type="checkbox" class="size-4 rounded border-brand-border text-brand-gold` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Review\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 10 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Reviews</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 48 | `<a asp-action="Edit" asp-route-id="@review.Id" class="font-semibold text-brand-goldDeep transition h` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Setting\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 11 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Site Settings</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Areas\Admin\Views\Shared\_AdminLayout.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 84 | `<a href="/" class="btn-ghost w-full border-white/10 bg-white/[0.04] !text-white/75 hover:!border-bra` | A. Brand accent leak | brand-primary-soft | Low |
| 179 | `? $"{baseClass} border-brand-gold bg-brand-gold/15 text-brand-goldSoft shadow-soft"` | A. Brand accent leak | brand-primary-soft | Low |

### \tailwind.config.js
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 30 | `gold: "var(--color-brand-gold)",` | F. Compatibility alias / Token | Temporarily remap to var(--color-brand-primary) etc. or remove if unreferenced. | High |
| 31 | `goldDeep: "var(--color-brand-gold-deep)",` | F. Compatibility alias / Token | Temporarily remap to var(--color-brand-primary) etc. or remove if unreferenced. | High |
| 32 | `goldSoft: "var(--color-brand-gold-soft)",` | F. Compatibility alias / Token | Temporarily remap to var(--color-brand-primary) etc. or remove if unreferenced. | High |
| 57 | `ember: '0 16px 44px rgba(184, 154, 98, 0.2)',` | F. Compatibility alias / Token | Temporarily remap to var(--color-brand-primary) etc. or remove if unreferenced. | High |
| 58 | `glow: '0 0 0 1px rgba(184, 154, 98, 0.22), 0 18px 52px rgba(17, 24, 39, 0.12)',` | F. Compatibility alias / Token | Temporarily remap to var(--color-brand-primary) etc. or remove if unreferenced. | High |

### \Views\About\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 12 | `<span class="inline-flex rounded-button border border-brand-gold/25 bg-brand-goldSoft/45 px-4 py-2 t` | A. Brand accent leak | brand-primary-deep | Low |
| 79 | `<div class="mb-5 h-10 w-10 rounded-md bg-brand-goldSoft"></div>` | A. Brand accent leak | brand-primary-soft | Low |
| 122 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Signature taste</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 214 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Trải nghiệm tại bàn</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Branch\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 16 | `<span class="inline-flex rounded-button border border-brand-gold/25 bg-brand-goldSoft/45 px-4 py-2 t` | A. Brand accent leak | brand-primary-deep | Low |
| 40 | `class="btn-ghost @(allCitiesActive ? "!border-brand-gold !bg-brand-goldSoft/40 !text-brand-goldDeep"` | A. Brand accent leak | brand-primary-deep | Low |
| 48 | `class="btn-ghost @(isActive ? "!border-brand-gold !bg-brand-goldSoft/40 !text-brand-goldDeep" : stri` | A. Brand accent leak | brand-primary-deep | Low |
| 61 | `<div class="inline-flex rounded-button border border-brand-gold/25 bg-brand-goldSoft/45 px-4 py-2 te` | A. Brand accent leak | brand-primary-deep | Low |
| 132 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Google Maps</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 145 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Đang cập nhật</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Contact\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 15 | `<span class="inline-flex rounded-button border border-brand-gold/25 bg-brand-goldSoft/45 px-4 py-2 t` | A. Brand accent leak | brand-primary-deep | Low |
| 24 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Hotline</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 36 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Thông tin liên hệ</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 40 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Hotline</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 41 | `<a href="tel:@Model.Hotline.Replace(" ", string.Empty)" class="mt-1 block font-black text-brand-dark` | A. Brand accent leak | brand-primary-deep | Low |
| 44 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Email</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 45 | `<a href="mailto:@Model.EmailDisplay" class="mt-1 block font-black text-brand-dark transition hover:t` | A. Brand accent leak | brand-primary-deep | Low |
| 48 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Địa chỉ</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 52 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Giờ mở cửa</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 65 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Chi nhánh</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 75 | `<span class="font-semibold text-brand-goldDeep">@branch.OpeningHours</span>` | A. Brand accent leak | brand-primary-deep | Low |
| 76 | `<a href="tel:@branch.Hotline.Replace(" ", string.Empty)" class="font-semibold text-brand-dark transi` | A. Brand accent leak | brand-primary-deep | Low |
| 91 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Gửi tin nhắn</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 98 | `<div class="mb-6 rounded-md border border-brand-gold/25 bg-brand-goldSoft/30 p-5">` | A. Brand accent leak | brand-primary-soft | Low |
| 114 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Họ tê` | A. Brand accent leak | brand-primary-deep | Low |
| 120 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Số đi` | A. Brand accent leak | brand-primary-deep | Low |
| 128 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Email` | A. Brand accent leak | brand-primary-deep | Low |
| 134 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Chủ đ` | A. Brand accent leak | brand-primary-deep | Low |
| 141 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Nội d` | A. Brand accent leak | brand-primary-deep | Low |
| 182 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Google Maps</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 193 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Đang cập nhật</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Error\Error.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 12 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">System</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Error\NotFound.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 10 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">404</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Home\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 11 | `<div class="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(231,217,189,0.7),transparen` | B. Decorative background/gradient leak | Use red/neutral gradients (e.g., rgba(163,13,30,...)) | Medium |

### \Views\Menu\Detail.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 24 | `<a href="/thuc-don" class="transition hover:text-brand-goldDeep">Thực đơn</a>` | A. Brand accent leak | brand-primary-deep | Low |
| 26 | `<a href="/thuc-don?category=@category.Slug" class="transition hover:text-brand-goldDeep">@category.N` | A. Brand accent leak | brand-primary-deep | Low |
| 57 | `<span class="rounded-button border border-brand-gold/25 bg-brand-goldSoft/45 px-3 py-1 text-xs font-` | A. Brand accent leak | brand-primary-deep | Low |
| 64 | `<span class="rounded-button bg-brand-goldSoft px-3 py-1 text-xs font-bold uppercase tracking-wide te` | A. Brand accent leak | brand-primary-soft | Low |
| 68 | `<span class="rounded-button bg-brand-gold px-3 py-1 text-xs font-bold uppercase tracking-wide text-b` | A. Brand accent leak | brand-primary | Low |
| 85 | `<span class="text-4xl font-black text-brand-goldDeep">@item.Price.ToString("N0")đ</span>` | A. Brand accent leak | brand-primary-deep | Low |
| 94 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Độ cay</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 98 | `<span class="size-2.5 rounded-full @(i <= item.SpicyLevel ? "bg-brand-goldDeep" : "bg-brand-border")` | A. Brand accent leak | brand-primary-deep | Low |
| 103 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Khẩu phần</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 107 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Calories</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 171 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Tại bàn là ngon nhất</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Menu\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 14 | `<span class="inline-flex rounded-button border border-brand-gold/25 bg-brand-goldSoft/45 px-4 py-2 t` | A. Brand accent leak | brand-primary-deep | Low |
| 44 | `class="btn-ghost @(allCategoriesActive ? "!border-brand-gold !bg-brand-goldSoft/40 !text-brand-goldD` | A. Brand accent leak | brand-primary-deep | Low |
| 51 | `class="btn-ghost @(category.IsActive ? "!border-brand-gold !bg-brand-goldSoft/40 !text-brand-goldDee` | A. Brand accent leak | brand-primary-deep | Low |
| 74 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Món nổi bật</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 82 | `<span class="text-3xl font-black text-brand-goldDeep">@Model.FeaturedItem.Price.ToString("N0")đ</spa` | A. Brand accent leak | brand-primary-deep | Low |
| 99 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Món nổi bật</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 144 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Đặt bàn</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\News\Detail.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 18 | `<a href="/tin-tuc" class="text-sm font-semibold text-brand-goldDeep transition hover:text-brand-dark` | A. Brand accent leak | brand-primary-deep | Low |
| 21 | `<span class="rounded-button bg-brand-goldSoft px-3 py-1 text-xs font-bold uppercase tracking-wide te` | A. Brand accent leak | brand-primary-soft | Low |
| 58 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Ưu đãi tại bàn</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\News\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 14 | `<span class="inline-flex rounded-button border border-brand-gold/25 bg-brand-goldSoft/45 px-4 py-2 t` | A. Brand accent leak | brand-primary-deep | Low |
| 36 | `class="btn-ghost @(allCategoriesActive ? "!border-brand-gold !bg-brand-goldSoft/40 !text-brand-goldD` | A. Brand accent leak | brand-primary-deep | Low |
| 43 | `class="btn-ghost @(category.IsActive ? "!border-brand-gold !bg-brand-goldSoft/40 !text-brand-goldDee` | A. Brand accent leak | brand-primary-deep | Low |
| 67 | `<span class="rounded-button bg-brand-goldSoft px-3 py-1 text-xs font-bold uppercase tracking-wide te` | A. Brand accent leak | brand-primary-soft | Low |
| 85 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Tin nổi bật</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Reservation\Index.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 12 | `<span class="inline-flex rounded-button border border-brand-gold/25 bg-brand-goldSoft/45 px-4 py-2 t` | A. Brand accent leak | brand-primary-deep | Low |
| 21 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Cần đặt bàn gấp?</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 33 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Thông tin giữ bàn</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 49 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Họ tê` | A. Brand accent leak | brand-primary-deep | Low |
| 55 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Số đi` | A. Brand accent leak | brand-primary-deep | Low |
| 62 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Email` | A. Brand accent leak | brand-primary-deep | Low |
| 68 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Chi n` | A. Brand accent leak | brand-primary-deep | Low |
| 81 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Số lư` | A. Brand accent leak | brand-primary-deep | Low |
| 87 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Ngày ` | A. Brand accent leak | brand-primary-deep | Low |
| 93 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Giờ đ` | A. Brand accent leak | brand-primary-deep | Low |
| 101 | `<div class="rounded-md border border-brand-gold/25 bg-brand-goldSoft/30 p-4 text-sm leading-6 text-b` | A. Brand accent leak | brand-primary-soft | Low |
| 102 | `Nhóm trên 12 người nên gọi hotline <a href="tel:@Model.Hotline.Replace(" ", string.Empty)" class="fo` | A. Brand accent leak | brand-primary-deep | Low |
| 107 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Ghi c` | A. Brand accent leak | brand-primary-deep | Low |
| 118 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Gợi ý nhóm</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 131 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">Chi nhánh</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 140 | `<p class="mt-2 text-sm font-semibold text-brand-goldDeep">@branch.OpeningHours</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Reservation\Success.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 12 | `<span class="mx-auto grid size-14 place-items-center rounded-md bg-brand-goldSoft text-2xl font-blac` | A. Brand accent leak | brand-primary-soft | Low |
| 13 | `<p class="mt-6 text-kicker font-black uppercase text-brand-goldDeep">Đã ghi nhận</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 21 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Khách hàng</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 25 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Chi nhánh</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 29 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Ngày đến</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 33 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Giờ đến</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 37 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Số khách</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 41 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Hotline</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 42 | `<a href="tel:@Model.Hotline.Replace(" ", string.Empty)" class="mt-2 block font-black text-brand-gold` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Shared\Components\BookingMiniForm\Default.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 6 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Tên k` | A. Brand accent leak | brand-primary-deep | Low |
| 11 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Số đi` | A. Brand accent leak | brand-primary-deep | Low |
| 16 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Chi n` | A. Brand accent leak | brand-primary-deep | Low |
| 26 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Ngày<` | A. Brand accent leak | brand-primary-deep | Low |
| 31 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Giờ</` | A. Brand accent leak | brand-primary-deep | Low |
| 36 | `<span class="mb-2 block text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">Số kh` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Shared\Components\BranchCard\Default.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 13 | `<p class="text-xs font-black uppercase tracking-[0.18em] text-brand-goldSoft">@Model.City</p>` | A. Brand accent leak | brand-primary-soft | Low |
| 27 | `<p class="text-xs uppercase tracking-[0.16em] text-brand-goldDeep">Giờ mở cửa</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 31 | `<p class="text-xs uppercase tracking-[0.16em] text-brand-goldDeep">Sức chứa</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 37 | `<p class="text-xs uppercase tracking-[0.16em] text-brand-goldDeep">Diện tích</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 44 | `<p class="text-xs uppercase tracking-[0.16em] text-brand-goldDeep">Tầng</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 51 | `<a href="tel:@Model.Hotline.Replace(" ", string.Empty)" class="font-semibold text-brand-dark transit` | A. Brand accent leak | brand-primary-deep | Low |
| 52 | `<a href="@Model.Url" class="font-semibold text-brand-goldDeep transition group-hover:text-brand-blac` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Shared\Components\FoodCard\Default.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 19 | `<span class="rounded-button bg-brand-goldSoft px-3 py-1 text-xs font-bold uppercase tracking-wide te` | A. Brand accent leak | brand-primary-soft | Low |
| 23 | `<span class="rounded-button bg-brand-gold px-3 py-1 text-xs font-bold uppercase tracking-wide text-b` | A. Brand accent leak | brand-primary | Low |
| 31 | `<span class="rounded-button border border-brand-gold/30 bg-brand-goldSoft/55 px-3 py-1 text-xs font-` | A. Brand accent leak | brand-primary-deep | Low |
| 41 | `<p class="text-xs font-semibold uppercase tracking-[0.16em] text-brand-goldDeep">@Model.CategoryName` | A. Brand accent leak | brand-primary-deep | Low |
| 43 | `<h3 class="mt-2 text-xl font-black text-brand-dark transition group-hover:text-brand-goldDeep">@Mode` | A. Brand accent leak | brand-primary-deep | Low |
| 50 | `<p class="text-lg font-black text-brand-goldDeep">@Model.Price.ToString("N0")đ</p>` | A. Brand accent leak | brand-primary-deep | Low |
| 64 | `<span class="size-2 rounded-full @(i <= Model.SpicyLevel ? "bg-brand-goldDeep" : "bg-brand-border")"` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Shared\Components\PostCard\Default.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 11 | `<div class="absolute left-4 top-4 z-30 rounded-button bg-brand-white/92 px-3 py-1 text-xs font-bold ` | A. Brand accent leak | brand-primary-deep | Low |
| 24 | `<h3 class="mt-3 text-xl font-black text-brand-dark transition group-hover:text-brand-goldDeep">@Mode` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Shared\Components\SectionHeading\Default.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 10 | `<p class="text-kicker font-black uppercase text-brand-goldDeep">@Model.Eyebrow</p>` | A. Brand accent leak | brand-primary-deep | Low |

### \Views\Shared\Partials\_Header.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 27 | `class="inline-flex items-center rounded-button border border-brand-gold/30 bg-brand-gold px-5 py-2.5` | A. Brand accent leak | brand-primary-soft | Low |
| 33 | `class="ml-auto inline-flex size-11 items-center justify-center rounded-md border border-white/20 bg-` | A. Brand accent leak | brand-primary-soft | Low |

### \Views\Shared\Partials\_MobileMenu.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 16 | `class="grid size-10 place-items-center rounded-md border border-white/20 bg-white/5 text-white trans` | A. Brand accent leak | brand-primary-soft | Low |
| 33 | `class="rounded-md px-3 py-3 text-sm font-semibold uppercase tracking-[0.1em] transition @(item.IsAct` | A. Brand accent leak | brand-primary | Low |
| 41 | `<div class="mt-8 rounded-md border border-brand-gold/20 bg-white/5 p-4">` | A. Brand accent leak | brand-primary | Low |
| 42 | `<p class="text-xs font-semibold uppercase tracking-[0.18em] text-brand-goldSoft">Hotline</p>` | A. Brand accent leak | brand-primary-soft | Low |
| 43 | `<a href="tel:@Model.Hotline.Replace(" ", string.Empty)" class="mt-2 block text-2xl font-black text-w` | A. Brand accent leak | brand-primary-soft | Low |
| 50 | `<a href="@Model.ReservationUrl" class="block w-full rounded-button bg-brand-gold px-6 py-3 text-cent` | A. Brand accent leak | brand-primary-soft | Low |

### \Views\Shared\_Layout.cshtml
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 81 | `class="fixed bottom-24 left-5 z-40 hidden size-11 place-items-center rounded-md border border-brand-` | A. Brand accent leak | brand-primary-deep | Low |

### \wwwroot\css\input.css
| Line | Snippet | Classification | Proposed Replacement | Risk |
|---|---|---|---|---|
| 24 | `--color-brand-gold: #B89A62;` | F. Compatibility alias / Token | Temporarily remap to var(--color-brand-primary) etc. or remove if unreferenced. | High |
| 25 | `--color-brand-gold-deep: #9E7F43;` | F. Compatibility alias / Token | Temporarily remap to var(--color-brand-primary) etc. or remove if unreferenced. | High |
| 26 | `--color-brand-gold-soft: #E7D9BD;` | F. Compatibility alias / Token | Temporarily remap to var(--color-brand-primary) etc. or remove if unreferenced. | High |

## Build & Verification Plan
- **Commands to run**: 
  - `npm run build:css` (or the equivalent tailwind build command found in `package.json`)
- **Files expected to change**: 
  - Source: `wwwroot/css/input.css`, various `.cshtml` files.
  - Generated: `wwwroot/css/site.css`
- **Visual QA checklist**:
  - [ ] Home Page hero and cards
  - [ ] Mobile drawer navigation
  - [ ] Reservation form validation and focus states
  - [ ] Hover states on primary buttons
  - [ ] Admin dashboard and shared layouts
  - [ ] Ensure dark text contrast is legible on soft red backgrounds

## Questions / Risks
- **Tailwind Build Command**: Please confirm the exact npm script used to rebuild `site.css`.
- **Semantic Warning Colors**: Some admin badges use `yellow-` or `amber-`. Should these remain as standard semantic UI warning colors, or do you want them mapped to a specific brand-neutral color?
