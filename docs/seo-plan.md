# Kế hoạch SEO kigholding.vn

Mục tiêu: lên trang 1 Google cho nhóm từ khóa thương hiệu và từ khóa món Hàn Quốc theo khu vực.

## 1. Bản đồ từ khóa → trang đích

Mỗi trang chỉ nhắm 1 nhóm từ khóa chính, để các trang không tranh nhau.

| Trang | Từ khóa chính | Từ khóa phụ |
|---|---|---|
| `/` | nhà hàng Hàn Quốc, Truyền Thuyết Champong, KIG Holding | mì cay hải sản, buffet nướng Hàn Quốc, thịt nướng Hàn Quốc |
| `/thuc-don/nhom/truyen-thuyet-champong` | mì cay hải sản champong, mì tương đen | mì Hàn Quốc, jjamppong, jjajangmyeon, lẩu Hàn Quốc, bánh gạo |
| `/thuc-don/nhom/gogi-maru` | Gogi Maru, thịt nướng Hàn Quốc | thịt nướng dry-aged, thịt heo lên men, bò nướng than hồng |
| `/thuc-don/nhom/kbb-cook` | KBB Cook, buffet nướng Hàn Quốc | buffet nướng không giới hạn, buffet 278K, buffet lẩu nướng |
| `/chi-nhanh` | nhà hàng Hàn Quốc gần đây, nhà hàng Hàn Quốc + [quận] | Champong Quận 10, Champong Tân Bình, Champong Thủ Đức |
| `/dat-ban` | đặt bàn nhà hàng Hàn Quốc | đặt bàn online, đặt tiệc sinh nhật |
| `/lien-he-nhuong-quyen` | nhượng quyền nhà hàng Hàn Quốc | nhượng quyền mì cay, hợp tác mở nhà hàng |
| `/tin-tuc/...` | từ khóa dài (long-tail), xem mục 3 | |

## 2. Những gì đã sửa trong code (on-page)

- Title + meta description có từ khóa cho trang chủ, thực đơn, từng brand, chi nhánh, đặt bàn, liên hệ, tin tức, giới thiệu, nhượng quyền.
- Trang chủ có thẻ `<h1>` (trước đây không có).
- Alt ảnh có tên món và tên brand.
- Thêm phần FAQ hiển thị trên trang chủ, kèm schema `FAQPage`.
- Schema `Organization` + `Brand` + `WebSite` cho KIG Holding.
- Schema `Restaurant` cho từng chi nhánh (địa chỉ, giờ mở cửa, hotline, bản đồ), lấy dữ liệu từ DB.
- `og:locale`, canonical cho trang thực đơn brand và trang đặt bàn.
- `robots.txt` chặn `/admin` và `/dat-ban/thanh-cong`; trang đặt bàn thành công gắn `noindex`.

## 3. Nội dung blog (làm đều mỗi tuần, cách hiệu quả nhất để có nhiều từ khóa)

Mỗi bài 800–1.500 chữ, có ảnh thật, link nội bộ về trang thực đơn hoặc trang đặt bàn. Đăng trong admin "Tin tức" và điền SEO title/description cho từng bài.

1. Champong là gì? Cách ăn mì cay hải sản Hàn Quốc đúng vị
2. Mì tương đen (jjajangmyeon) và ngày Black Day ở Hàn Quốc
3. Top món nên gọi khi đi nhà hàng Hàn Quốc lần đầu
4. Thịt dry-aged là gì? Vì sao thịt nướng Gogi Maru mềm hơn
5. Kinh nghiệm ăn buffet nướng Hàn Quốc không bị ngán
6. Gợi ý nhà hàng Hàn Quốc cho tiệc sinh nhật, tiệc công ty
7. Nhà hàng Hàn Quốc ngon tại Quận 10 / Tân Bình / Thủ Đức (mỗi quận 1 bài, có địa chỉ chi nhánh)
8. Seoul Gukbap: cơm canh Hàn Quốc cho bữa trưa văn phòng

## 4. SEO local (Google Maps) – làm ngay, hiệu quả nhanh

- Tạo/xác minh **Google Business Profile** cho từng chi nhánh: tên, địa chỉ, hotline, giờ mở cửa trùng khớp 100% với website.
- Mỗi profile để link về `https://kigholding.vn/chi-nhanh` hoặc trang thực đơn brand.
- Up ảnh món, ảnh không gian hằng tuần. Trả lời mọi review.
- Xin review thật từ khách (QR tại bàn). Không mua review ảo vì Google sẽ xóa và có thể khóa profile.

## 5. Backlink – nhiều nhưng phải "sạch"

Mua backlink hàng loạt, PBN, hay spam comment/forum có thể khiến Google phạt và cả domain bị tụt hạng. Nên làm backlink thật, từ trang có người đọc:

| Nguồn | Cách làm | Ưu tiên |
|---|---|---|
| Fanpage, TikTok, Instagram, YouTube của từng brand | Link website ở bio/mô tả, trong bài đăng | Cao |
| Google Business Profile, Foody/ShopeeFood, GrabFood, Bến Thành… | Link về website | Cao |
| Danh bạ doanh nghiệp VN (Trang Vàng, Hotfrog VN, Cốc Cốc Map…) | Đăng thông tin giống hệt website | Trung bình |
| Báo/blog ẩm thực (Kenh14, Foody blog, Vietcetera, blog food reviewer) | PR khai trương, sự kiện, ưu đãi | Cao |
| KOL/KOC review | Nhờ để link website trong bài/video | Cao |
| Trang nhượng quyền, báo kinh doanh (CafeF, CafeBiz, Doanh Nhân) | Bài PR về KIG Holding, mô hình nhượng quyền | Trung bình |
| Đối tác, nhà cung cấp, tổ chức sự kiện | Link "đối tác" qua lại | Thấp |

Anchor text nên đa dạng: tên brand ("Truyền Thuyết Champong"), URL trần, và từ khóa ("buffet nướng Hàn Quốc") chỉ khoảng 10–20%.

## 6. Sau khi deploy

1. Vào **Google Search Console**, thêm domain `kigholding.vn`, gửi `https://kigholding.vn/sitemap.xml`.
2. Kiểm tra schema bằng **Rich Results Test** (trang chủ và `/chi-nhanh`).
3. Theo dõi từ khóa trong Search Console (Hiệu suất → Truy vấn) mỗi 2 tuần và tối ưu thêm trang có thứ hạng 8–20.
4. Kiểm tra tốc độ bằng PageSpeed Insights (ảnh banner nên dưới 200KB, ưu tiên webp).

Thời gian thường thấy: từ khóa thương hiệu 2–6 tuần, từ khóa chung như "buffet nướng Hàn Quốc" 3–6 tháng, tùy cạnh tranh và tốc độ làm nội dung/backlink.
