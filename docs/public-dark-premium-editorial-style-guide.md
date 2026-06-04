# Public Dark Premium Editorial Style Guide

Tài liệu này hệ thống hóa ngôn ngữ thiết kế **Dark Premium Editorial (Tối giản - Cao cấp - Đậm chất Biên tập)** được đúc kết từ trang Liên hệ nhượng quyền thương hiệu `/lien-he-nhuong-quyen` (`Views/Franchise/Index.cshtml`). Hướng dẫn này đóng vai trò làm cẩm nang phát triển giao diện (Style Guide) cho các trang thông tin công cộng tương lai thuộc dự án **KIGHolding / Truyền Thuyết Champong**, giúp duy trì tính đồng bộ về thẩm mỹ, tính sang trọng và trải nghiệm thị giác cao cấp.

---

## 1. Mục đích
* **Đồng bộ hóa thị giác**: Cung cấp bộ quy tắc thiết kế nhất quán để bất kỳ nhà phát triển hoặc AI Agent nào khi xây dựng các trang thông tin công cộng (public pages) mới đều có thể tái sử dụng ngay lập tức mà không làm loãng nhận diện thương hiệu.
* **Tối ưu quy trình code**: Giảm thiểu việc viết CSS tùy biến (ad-hoc CSS) bằng cách định hình sẵn cấu trúc markup mẫu và tận dụng các biến CSS, tiện ích Tailwind có sẵn.
* **Bảo toàn chất lượng**: Lưu trữ lại lý do đằng sau các quyết định thiết kế (như việc tinh giản viền thẻ card, hạn chế ảnh nền) để ngăn chặn các sửa đổi làm giảm đi vẻ cao cấp vốn có.

---

## 2. Tinh thần thiết kế
* **Tối giản (Minimal)**: Loại bỏ các chi tiết thừa. Từng khoảng trắng, đường kẻ mỏng hay tiêu đề lớn đều được tính toán để phục vụ trực tiếp cho nội dung.
* **Ưu tiên chữ (Typography-led / Text-first)**: Nội dung chính được truyền tải thông qua nghệ thuật sắp xếp chữ. Kích thước chữ động lớn, khoảng cách dòng rộng thoáng giúp trang web giống như một trang tạp chí cao cấp.
* **Không lạm dụng thẻ khung (No boxed cards)**: Tránh việc đóng gói thông tin vào các khung hộp dày đặc hay hiệu ứng kính mờ (glassmorphism) quá đà. Hãy để nội dung "thở" tự do trực tiếp trên nền tối.
* **Điểm nhấn tinh tế (Selective Accent)**: Màu đỏ thương hiệu KIG Red (`#E50914`) được sử dụng cực kỳ chọn lọc làm điểm nhấn dẫn dắt hành vi (nút bấm chính, tiêu đề phụ, mỏ neo thị giác) thay vì phủ đỏ tràn lan.

---

## 3. Khi nào nên dùng style này
* **NÊN DÙNG CHO**:
  * Các trang thông tin tĩnh, giới thiệu thương hiệu (ví dụ: Trang Giới thiệu, Câu chuyện thương hiệu, Đối tác hợp tác).
  * Các trang liên hệ, nhượng quyền, tuyển dụng hoặc hướng dẫn dịch vụ.
  * Các trang hướng đến việc đọc thông tin dài (text-heavy), cần tạo cảm giác chuyên nghiệp, uy tín và nghệ thuật.
* **KHÔNG DÙNG CHO**:
  * **Trang quản trị (Admin Dashboard)**: Phân khu admin bắt buộc duy trì giao diện **Sáng (Light Theme)** (`bg-brand-light` / `#FAF8F3`) để phục vụ hiệu quả cho thao tác điền form, kiểm duyệt và quản lý dữ liệu số lượng lớn.
  * **Trang thực đơn trực quan (Menu Pages)**: Nơi yêu cầu hình ảnh món ăn rực rỡ, lưới sản phẩm nhiều hình ảnh trực quan.
  * **Trang bảng biểu, biểu đồ dữ liệu** hoặc các form khai báo nhiều trường dày đặc.

---

## 4. Nguyên tắc bố cục
* **Vùng chứa chuẩn**: Luôn bọc nội dung trong thẻ `.container-page` (`max-width: 72rem` trên desktop, mở rộng tới `76rem` trên màn hình cực lớn) để đảm bảo căn lề đồng bộ.
* **Nhịp điệu khoảng cách lớn (Vertical Rhythm)**: Khoảng trống dọc giữa các section nội dung phải rộng và thoáng đạt (`padding-block: clamp(2.25rem, 4.5vw, 3.75rem)`).
* **Phân chia bằng Divider mảnh**: Ngăn cách các phần nội dung bằng viền mỏng phía trên (`border-top: 1px solid rgba(255, 255, 255, 0.08)`) thay vì tạo các khối panel nền khác màu.
* **Khống chế độ rộng tối đa**: Giới hạn bề ngang văn bản mô tả ở mức `max-width: 44rem` hoặc `46rem` giúp người dùng không bị mỏi mắt khi đọc từ trái sang phải.

---

## 5. Hero section
* **Bố cục mở (Open Hero)**: Đặt trực tiếp nội dung văn bản lên canvas nền tối, tuyệt đối không bao bọc phần hero trong các khung thẻ hay hộp bo góc (`.hero-frame`, `.hero-card`).
* **Tiêu đề phụ màu đỏ (Eyebrow/Kicker)**: Sử dụng font chữ nhỏ (`0.72rem`), viết hoa toàn bộ, giãn chữ rộng (`letter-spacing: 0.24em`), kết hợp một đường gạch ngang chuyển sắc đỏ mảnh ở phía bên trái để thu hút thị giác ban đầu.
* **Tiêu đề cực đại (Large Title)**: Dùng cỡ chữ động lớn (`clamp(2.35rem, 4.8vw, 4.6rem)`), viết sát dòng (`line-height: 0.96`), độ rộng khống chế ở khoảng `17ch` đến `18ch` để giữ chữ không bị rớt dòng đơn lẻ.
* **Đoạn mô tả ngắn (Lead)**: Tối đa 2-3 câu ngắn gọn, màu chữ trắng mờ dịu (`rgba(255, 255, 255, 0.76)`).
* **Nhóm nút chuyển đổi (Actions)**: Đặt song song một nút Đỏ Gradient chính (Primary) và một nút Viền Mờ tối giản phụ (Secondary).

---

## 6. Section nội dung
* **Lưới bài viết mở**: Trình bày danh sách ưu thế hay thông tin theo cấu trúc lưới 2 hoặc 3 cột mở, không bọc viền xung quanh mỗi thẻ con.
* **Đánh số thứ tự mỏ neo**: Mỗi phần tử trong danh sách sử dụng một số thứ tự lớn viết mờ màu đỏ nhạt (`01`, `02`, `03`) để làm điểm tựa định vị dòng đọc của mắt.
* **Đường chia nhỏ (Item borders)**: Sử dụng duy nhất một đường kẻ trên mảnh (`border-top: 1px solid rgba(255, 255, 255, 0.08)`) trước tiêu đề của mỗi bài viết nhỏ thay vì vẽ nguyên một cái khung bao bọc.
* **Nội dung súc tích**: Mỗi khối nội dung nhỏ chỉ nên dài từ 2 đến 3 dòng chữ để duy trì mật độ thông tin thoáng đãng.

---

## 7. Typography
* **Font chữ hệ thống**: Sử dụng font chữ rounded **Quicksand** (`font-sans`) được cấu hình mặc định trong dự án để tạo nét mềm mại, hiện đại nhưng không kém phần trang trọng.
* **Thang cỡ chữ quy chuẩn**:
  * Tiêu đề trang (Hero Title): `clamp(2.35rem, 4.8vw, 4.6rem)` với `font-weight: 900`.
  * Tiêu đề phần (Section Title): `clamp(1.8rem, 3vw, 2.7rem)` với `font-weight: 900`.
  * Tiêu đề bài viết con (Item Title): `1rem` đến `1.15rem` với `font-weight: 800`.
  * Văn bản mô tả (Copy/Paragraph): `0.92rem` đến `0.98rem` với `line-height: 1.65` đến `1.8`, font-weight thường.
* **Tính dễ đọc (Readability)**: Luôn giới hạn độ rộng chữ mô tả bằng class `max-width` để tối ưu hóa trải nghiệm đọc.

---

## 8. Màu sắc và accent
* **Nền tối Canvas chủ đạo**: 
  * Sử dụng màu nền cơ sở đen sâu (`#080808` đến `#090909`).
  * Phối hợp các dải màu chuyển nhẹ nhàng bằng Radial Gradient: Góc trái trên là ánh đỏ mờ (`rgba(229, 9, 20, 0.18)`), góc phải trên là màu kem dịu nhẹ (`rgba(231, 217, 189, 0.06)`).
* **Màu chữ**:
  * Tiêu đề chính/phần: Màu kem ấm trắng (`#fff7ed` hoặc `#fffaf2`).
  * Nội dung mô tả: Màu trắng mờ nhẹ (`rgba(255, 255, 255, 0.7)` hoặc `rgba(255, 255, 255, 0.76)`).
* **Điểm nhấn Đỏ (KIG Red Accent)**:
  * Sử dụng màu đỏ thuần `#E50914` hoặc đỏ đậm `#B91C1C` cho các điểm cần nhấn mạnh thị giác mạnh mẽ.
  * Giữ nguyên tắc tiết chế màu đỏ, không lạm dụng làm màu nền cho các mảng lớn để tránh gây mỏi mắt người dùng.

---

## 9. Border, blur và background
* **Hạn chế tối đa viền khung**: Không đóng hộp nội dung.
* **Đường phân tách mảnh**: Chỉ sử dụng các đường biên mỏng `border-top` hoặc `border-left` với màu mờ `rgba(255, 255, 255, 0.08)` để ngăn cách các khu vực chức năng.
* **Không dùng hiệu ứng kính mờ (Backdrop Blur)** trên diện rộng ở các phần nội dung tĩnh để tránh làm giảm hiệu năng kết xuất trên thiết bị di động cũ và gây rối mắt khi đọc văn bản.
* **Không dùng nền xám/sáng**: Tránh tuyệt đối chèn các khối panel màu sáng xám chen ngang bố cục tối cao cấp của trang.

---

## 10. Button / CTA
* **Nút bấm chính (Primary Button)**:
  * Bo góc tròn hoàn toàn (`rounded-full`), viết hoa toàn bộ, giãn chữ rộng (`letter-spacing: 0.16em`).
  * Nền gradient đỏ sậm (`linear-gradient(135deg, #e50914, #b91c1c)`) đi kèm bóng đổ đỏ mờ (`box-shadow: 0 12px 28px rgba(229, 9, 20, 0.18)`).
  * Khi hover: Sáng hơn nhẹ, tăng bóng đổ và dịch chuyển nhẹ lên trên (`-translate-y-px`).
* **Nút bấm phụ (Secondary Button)**:
  * Viền mờ (`border: 1px solid rgba(255, 255, 255, 0.1)`), nền trong suốt mờ nhẹ (`rgba(255, 255, 255, 0.03)`), chữ trắng.
  * Khi hover: Đổi màu viền sang đỏ mờ (`border-color: rgba(229, 9, 20, 0.42)`).

---

## 11. Animation
* **Quy chuẩn hiệu ứng cuộn**:
  * Chỉ sử dụng hệ thống kích hoạt bằng thuộc tính (attribute-driven) scroll-reveal mới thông qua JavaScript `uw-reveal.js` (sử dụng Intersection Observer & Web Animations API):
    ```html
    data-uw-reveal="fade-up"
    data-uw-once="true"
    data-uw-delay="80"
    ```
* **Các lớp CSS cũ BỊ CẤM RE-INTRODUCE**:
  * Tuyệt đối không khai báo hoặc thêm các class hiệu ứng cũ như: `scroll-reveal`, `reveal-up`, `reveal-left`, `reveal-right`, `delay-100`, `is-visible`. Các class này gây dư thừa tài nguyên hoạt hoạt và làm chậm tốc độ cuộn trang.

---

## 12. Responsive
* **Stack cấu trúc**: Trên màn hình nhỏ hơn `1024px` (dưới kích thước desktop), tất cả bố cục dạng cột (2 cột, 3 cột) phải tự động co về 1 cột dọc (`grid-cols-1` hoặc `flex-col`).
* **Chuyển đổi viền ngăn cách**: Các đường viền ngăn cột dọc (`border-left`) trên desktop phải được chuyển đổi thành viền ngang (`border-top`) hoặc ẩn đi hoàn toàn trên mobile để tránh phá vỡ luồng đọc dọc.
* **Nút bấm Full-width**: Trên thiết bị di động, nhóm nút CTA sẽ được kéo dài 100% chiều rộng màn hình và xếp dọc nhau để tối ưu hóa diện tích nhấn của ngón tay.
* **Spacing gọn gàng**: Giảm đệm dọc tổng thể của shell wrapper trên màn hình di động xuống mức tối thiểu (`padding-block: 2.35rem`) để tránh khoảng trống thừa quá lớn trên màn hình nhỏ.

---

## 13. Những điều nên tránh
* **KHÔNG** sử dụng các lớp bọc thẻ cũ như `.hero-frame`, `.hero-card` để đóng khung tiêu đề lớn.
* **KHÔNG** sử dụng các panel nền xám nhạt hoặc trắng sáng kiểu `.surface-panel` hay `.surface-muted`.
* **KHÔNG** tự ý chèn ảnh minh họa stock không đúng chuẩn nhận diện thương hiệu. Hãy ưu tiên bố cục text-only sang trọng.
* **KHÔNG** dùng hiệu ứng làm mờ nền (blur) cho từng thẻ card nội dung nhỏ.
* **KHÔNG** dùng lại các biến hoặc tông màu vàng đồng (Gold) cũ trong các trang thiết kế Dark Premium mới.
* **KHÔNG** để văn bản chạy tràn màn hình (luôn khống chế `max-width` cho dòng mô tả).
* **KHÔNG** đặt tiêu đề chính dài quá `20ch`.

---

## 14. Checklist áp dụng cho page mới
Trước khi phát hành một trang thông tin mới theo phong cách này, hãy kiểm tra danh sách sau:

- [ ] Trang sử dụng nền tối sâu (`#080808` / `#090909`) kết hợp hiệu ứng radial gradient đỏ-kem tinh tế.
- [ ] Phần Hero hoàn toàn mở, đặt chữ trực tiếp lên nền tối, không bị bọc khung.
- [ ] Tiêu đề chính (Hero Title) sử dụng cỡ chữ động lớn thông qua hàm `clamp()` và giới hạn độ rộng ký tự (`max-width: 17ch` hoặc `18ch`).
- [ ] Đoạn mô tả/dẫn nhập (Lead Paragraph) súc tích, ngắn gọn và có `max-width` rõ ràng để dễ đọc.
- [ ] Điểm nhấn đỏ (KIG Red Accent) được sử dụng có kiểm duyệt (chỉ dùng cho kicker, bullet mark, primary button).
- [ ] Các phần nội dung lớn được phân tách bằng đường kẻ mỏng trên (`border-top: 1px solid rgba(255, 255, 255, 0.08)`).
- [ ] Không xuất hiện các block panel màu sáng, xám hoặc card kính mờ dày đặc.
- [ ] Không lạm dụng ảnh nền hay hình ảnh minh họa không cần thiết.
- [ ] Giao diện hoạt động trơn tru trên thiết bị di động (stack dọc các cột, kéo rộng nút bấm 100%).
- [ ] Tất cả hiệu ứng chuyển động cuộn đều sử dụng thuộc tính `data-uw-reveal`, không sử dụng các lớp CSS cũ.
- [ ] Không thay đổi bất kỳ logic backend nào hoặc tệp CSS đã biên dịch (`site.css`). Mọi thay đổi CSS (nếu có bổ sung) chỉ được thực hiện tại `input.css`.

---

## 15. Mapping từ trang `/lien-he-nhuong-quyen`
Bảng ánh xạ các lớp CSS thực tế đang hoạt động trên trang Franchise đến vai trò thiết kế trong hệ thống:

| Tên lớp CSS thực tế | Vai trò thiết kế | Cách thể hiện trong code |
|---|---|---|
| `.franchise-page` | Nền canvas tối của trang | Gradient radial đỏ-kem trên nền đen thẳm để tạo chiều sâu không gian nhà hàng tối cao cấp. |
| `.franchise-page__shell` | Vùng đệm dọc của toàn bộ trang | Định hình khoảng thở dọc lớn thông qua `clamp(3rem, 6vw, 5.5rem)`. |
| `.franchise-page__section` | Phần nội dung độc lập | Có viền trên mờ mảnh (`rgba(255, 255, 255, 0.08)`) trừ section đầu tiên. |
| `.franchise-page__hero` | Cấu trúc khối đầu trang mở | Không có khung thẻ bọc ngoài, căn chỉnh lưới trống thoáng đạt rộng tối đa `72rem` (`76rem` trên LG). |
| `.franchise-page__eyebrow` | Tiêu đề phụ sắc đỏ | Chữ viết hoa nhỏ kèm đường kẻ mảnh chuyển sắc đỏ bên trái làm mỏ neo thị giác. |
| `.franchise-page__title` | Tiêu đề lớn thu hút thị giác | Chữ màu trắng kem ấm, cỡ chữ cực lớn, có đổ bóng mờ bảo vệ độ tương phản trên nền tối. |
| `.franchise-page__lead` | Mô tả ngắn dẫn dắt | Chữ màu trắng mờ, giới hạn chiều rộng `44rem` để tăng tối đa tính dễ đọc. |
| `.franchise-page__actions` | Cụm nút hành động đầu trang | Sắp xếp nút bấm dạng flex tự động xuống dòng và stack dọc trên màn hình di động. |
| `.franchise-page__button` | Quy chuẩn nút hành động | Bo tròn hoàn toàn, viết hoa chữ giãn rộng, hiệu ứng chuyển dịch nhẹ đi lên khi hover. |
| `.franchise-page__button--primary` | Nút kêu gọi hành động chính | Nền gradient đỏ thương hiệu kèm hiệu ứng bóng đổ đỏ mờ tinh tế. |
| `.franchise-page__button--secondary` | Nút kêu gọi hành động phụ | Viền mảnh mờ màu trắng, trong suốt, chỉ đổi màu đỏ mờ khi người dùng hover chuột. |
| `.franchise-page__grid` | Hệ thống lưới chia cột mở | Tự động chuyển đổi từ lưới 3 cột trên desktop (`grid-template-columns: repeat(3, 1fr)`) sang 1 cột dọc trên mobile. |
| `.franchise-page__item` | Phần tử con trong lưới | Phân cách bằng viền mảnh ngang phía trên và kèm chỉ mục số mờ lớn (`.franchise-page__item-index`). |
| `.franchise-page__columns` | Bố cục hai cột so sánh | Chia đôi màn hình tỉ lệ `1.06 : 0.94` trên desktop, tự stack dọc và đổi viền cột thành viền ngang trên di động. |
| `.franchise-page__bullet-mark` | Điểm nhấn dòng liệt kê | Chấm tròn chuyển sắc đỏ nhỏ có bóng viền đỏ mờ bao quanh thay thế cho dấu bullet mặc định. |
| `.franchise-page__step-index` | Vòng tròn số đếm quy trình | Vòng tròn viền kem mờ chứa số chỉ mục viết hoa nét đậm, tạo điểm dừng nhịp điệu khi dọc quy trình. |
| `.franchise-page__cta` | Khối kêu gọi hành động cuối trang | Chuyển đổi linh hoạt từ dạng lưới ngang 2 cột (Chữ bên trái - Nút bên phải) trên desktop sang dạng xếp dọc trên mobile. |

---

## 16. Gợi ý skeleton markup
Dưới đây là khung xương HTML (Skeleton Markup) tiêu chuẩn bằng Razor/HTML để tạo các trang thông tin công cộng phong cách Dark Premium Editorial mới:

```html
@{
    ViewData["Title"] = "Tiêu Đề Trang Mới";
    ViewData["MetaDescription"] = "Mô tả ngắn gọn hấp dẫn cho công cụ tìm kiếm và chia sẻ liên kết.";
}

<main class="public-editorial-page public-editorial-page--dark relative isolate overflow-hidden text-white">
    <div class="public-editorial-page__shell">
        
        <!-- SECTION 1: HERO BANNER (MỞ - KHÔNG KHUNG CARD) -->
        <section class="public-editorial-page__section public-editorial-page__section--hero">
            <div class="container-page">
                <div class="public-editorial-page__hero" data-uw-reveal="fade-up" data-uw-once="true">
                    <p class="public-editorial-page__eyebrow">TÊN CHỦ ĐỀ PHỤ</p>
                    <h1 class="public-editorial-page__title">Tiêu đề chính trang web viết ở đây</h1>
                    <p class="public-editorial-page__lead">
                        Đoạn văn ngắn dẫn dắt câu chuyện khoảng 2 đến 3 câu. Hãy giữ nó súc tích, cuốn hút và mô tả đúng giá trị cốt lõi muốn truyền tải.
                    </p>
                    <div class="public-editorial-page__actions">
                        <a href="/action-primary" class="public-editorial-page__button public-editorial-page__button--primary">Hành động chính</a>
                        <a href="/action-secondary" class="public-editorial-page__button public-editorial-page__button--secondary">Hành động phụ</a>
                    </div>
                </div>
            </div>
        </section>

        <!-- SECTION 2: DẠNG LƯỚI THÔNG TIN MỞ (3 CỘT TRÊN DESKTOP, 1 CỘT TRÊN MOBILE) -->
        <section class="public-editorial-page__section">
            <div class="container-page">
                <div class="public-editorial-page__section-head" data-uw-reveal="fade-up" data-uw-delay="80" data-uw-once="true">
                    <p class="public-editorial-page__eyebrow">TIÊU ĐỀ PHỤ PHẦN 2</p>
                    <h2 class="public-editorial-page__section-title">Tiêu đề phần nội dung dạng lưới</h2>
                </div>

                <div class="public-editorial-page__grid">
                    <!-- Phần tử thứ nhất -->
                    <article class="public-editorial-page__item" data-uw-reveal="fade-up" data-uw-delay="80" data-uw-once="true">
                        <span class="public-editorial-page__item-index">01</span>
                        <h3 class="public-editorial-page__item-title">Lợi điểm thứ nhất</h3>
                        <p class="public-editorial-page__item-copy">
                            Mô tả ngắn về lợi điểm thứ nhất. Viết ngắn gọn từ một đến hai câu để giữ giao diện thông thoáng.
                        </p>
                    </article>

                    <!-- Phần tử thứ hai -->
                    <article class="public-editorial-page__item" data-uw-reveal="fade-up" data-uw-delay="120" data-uw-once="true">
                        <span class="public-editorial-page__item-index">02</span>
                        <h3 class="public-editorial-page__item-title">Lợi điểm thứ hai</h3>
                        <p class="public-editorial-page__item-copy">
                            Mô tả ngắn về lợi điểm thứ hai. Viết ngắn gọn từ một đến hai câu để giữ giao diện thông thoáng.
                        </p>
                    </article>

                    <!-- Phần tử thứ ba -->
                    <article class="public-editorial-page__item" data-uw-reveal="fade-up" data-uw-delay="160" data-uw-once="true">
                        <span class="public-editorial-page__item-index">03</span>
                        <h3 class="public-editorial-page__item-title">Lợi điểm thứ ba</h3>
                        <p class="public-editorial-page__item-copy">
                            Mô tả ngắn về lợi điểm thứ ba. Viết ngắn gọn từ một đến hai câu để giữ giao diện thông thoáng.
                        </p>
                    </article>
                </div>
            </div>
        </section>

        <!-- SECTION 3: BỐ CỤC 2 CỘT TƯƠNG PHẢN (CHỮ VÀ QUY TRÌNH) -->
        <section class="public-editorial-page__section">
            <div class="container-page">
                <div class="public-editorial-page__columns">
                    <!-- Cột trái: Văn bản giới thiệu -->
                    <div class="public-editorial-page__column" data-uw-reveal="fade-up" data-uw-delay="80" data-uw-once="true">
                        <p class="public-editorial-page__eyebrow">CHỦ ĐỀ CỘT TRÁI</p>
                        <h2 class="public-editorial-page__section-title">Tiêu đề cột văn bản bên trái</h2>
                        <p class="public-editorial-page__section-copy">
                            Đoạn văn mô tả chi tiết thông tin giới thiệu. Cung cấp đầy đủ ngữ cảnh cần thiết và các điểm quan trọng cho đối tác hoặc độc giả.
                        </p>

                        <!-- Danh sách Bullet Đỏ đặc trưng -->
                        <div class="public-editorial-page__bullets" aria-label="Tiêu chí chi tiết">
                            <div class="public-editorial-page__bullet">
                                <span class="public-editorial-page__bullet-mark" aria-hidden="true"></span>
                                <span>Tiêu chí quan trọng thứ nhất cần lưu ý</span>
                            </div>
                            <div class="public-editorial-page__bullet">
                                <span class="public-editorial-page__bullet-mark" aria-hidden="true"></span>
                                <span>Tiêu chí quan trọng thứ hai cần lưu ý</span>
                            </div>
                        </div>
                    </div>

                    <!-- Cột phải: Các bước quy trình dọc -->
                    <div class="public-editorial-page__column public-editorial-page__column--process" data-uw-reveal="fade-up" data-uw-delay="120" data-uw-once="true">
                        <p class="public-editorial-page__eyebrow">QUY TRÌNH CHI TIẾT</p>
                        <h2 class="public-editorial-page__section-title">Các bước thực hiện dự kiến</h2>

                        <div class="public-editorial-page__steps">
                            <!-- Bước 1 -->
                            <article class="public-editorial-page__step">
                                <span class="public-editorial-page__step-index">01</span>
                                <div>
                                    <h3 class="public-editorial-page__step-title">Tiêu đề bước một</h3>
                                    <p class="public-editorial-page__step-copy">Chi tiết nội dung cần thực hiện ở bước đầu tiên.</p>
                                </div>
                            </article>

                            <!-- Bước 2 -->
                            <article class="public-editorial-page__step">
                                <span class="public-editorial-page__step-index">02</span>
                                <div>
                                    <h3 class="public-editorial-page__step-title">Tiêu đề bước hai</h3>
                                    <p class="public-editorial-page__step-copy">Chi tiết nội dung cần thực hiện ở bước tiếp theo.</p>
                                </div>
                            </article>
                        </div>
                    </div>
                </div>
            </div>
        </section>

        <!-- SECTION 4: CALL TO ACTION (CTA) CUỐI TRANG -->
        <section class="public-editorial-page__section public-editorial-page__section--cta">
            <div class="container-page">
                <div class="public-editorial-page__cta" data-uw-reveal="fade-up" data-uw-delay="160" data-uw-once="true">
                    <div class="public-editorial-page__cta-copy">
                        <p class="public-editorial-page__eyebrow">HÀNH ĐỘNG</p>
                        <h2 class="public-editorial-page__cta-title">Sẵn sàng thực hiện bước tiếp theo cùng chúng tôi?</h2>
                        <p class="public-editorial-page__cta-lead">
                            Để lại yêu cầu liên lạc hoặc gọi ngay vào số máy hỗ trợ để bắt đầu trao đổi trực tiếp.
                        </p>
                    </div>

                    <div class="public-editorial-page__cta-actions">
                        <a href="/lien-he" class="public-editorial-page__button public-editorial-page__button--primary">Đăng ký ngay</a>
                        <a href="tel:0909888777" class="public-editorial-page__button public-editorial-page__button--secondary">Gọi trực tiếp</a>
                    </div>
                </div>
            </div>
        </section>
        
    </div>
</main>
```
