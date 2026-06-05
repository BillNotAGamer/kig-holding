# Content Structure Plan

This plan maps out the content architecture for `/thanh-vien` using Vietnamese copywriting. Since the membership program is under active development, the content is kept honest ("Sắp ra mắt", "Đang hoàn thiện") without inventing functional database operations or account logins.

---

## 1. Hero Section

- **Purpose**: Welcome visitors and establish the premium editorial tone.
- **Suggested heading**: "Đặc quyền thành viên KIG Holding"
- **Suggested copy direction**: "Chào mừng bạn đến với chương trình chăm sóc khách hàng thân thiết của KIG Holding. Chúng tôi đang xây dựng một không gian trải nghiệm cao cấp, nơi các thương hiệu ẩm thực gắn kết và mang lại giá trị thiết thực hơn cho bạn."
- **Suggested CTA**:
  * Primary: "Đăng ký nhận thông tin" (links to [Contact](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Contact/Index.cshtml) at `/lien-he`)
  * Secondary: "Khám phá thực đơn" (links to [Menu](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Menu/Index.cshtml) at `/thuc-don`)
- **Recommended layout**: Open Hero layout directly on the dark canvas.
- **Recommended style guide pattern**: `.public-editorial-page__hero`
- **Responsive notes**: Full-width button stack on mobile, clamping typography.

---

## 2. Membership Promise

- **Purpose**: Explain the philosophy and rationale of the program.
- **Suggested heading**: "Đặc quyền tinh gọn và tinh tế"
- **Suggested copy direction**: "Chương trình thành viên KIG Holding được xây dựng trên triết lý tối giản và tôn trọng trải nghiệm cá nhân. Không phức tạp hóa quy trình tích điểm, chúng tôi tập trung vào việc ghi nhận sự đồng hành của bạn tại mọi điểm chạm trong hệ sinh thái nhà hàng."
- **Suggested CTA**: None.
- **Recommended layout**: Two-column layout (Left: Description text; Right: Custom red bullet-mark highlights).
- **Recommended style guide pattern**: `.public-editorial-page__columns`
- **Responsive notes**: Stacks vertically on mobile; left-hand border on desktop becomes top border on mobile.

---

## 3. Benefits Grid

- **Purpose**: Introduce the planned membership benefits in a non-boxed grid.
- **Suggested heading**: "Đặc quyền dự kiến dành cho hội viên"
- **Suggested copy direction**:
  * **01 / Ưu đãi ẩm thực**: Tích lũy điểm tiêu dùng và quy đổi ưu đãi trực tiếp tại tất cả các thương hiệu thành viên thuộc hệ sinh thái.
  * **02 / Quà tặng cá nhân**: Các món quà bất ngờ được thiết kế riêng cho bạn vào ngày sinh nhật hoặc các dịp kỷ niệm đặc biệt.
  * **03 / Trải nghiệm sớm**: Cơ hội đăng ký thưởng thức các món ăn mới trong thực đơn thử nghiệm hoặc trải nghiệm trước không gian chi nhánh mới.
  * **04 / Cập nhật đặc quyền**: Nhận thông tin sớm nhất về các chương trình ưu đãi độc quyền dành riêng cho nhóm khách hàng thân thiết.
- **Suggested CTA**: None.
- **Recommended layout**: 4-column open list grid.
- **Recommended style guide pattern**: `.public-editorial-page__grid` with `.public-editorial-page__item` and `.public-editorial-page__item-index`.
- **Responsive notes**: Stacks to 2 columns on tablet and 1 column on mobile.

---

## 4. How It Works / Roadmap

- **Purpose**: Provide clear details on the program's rollout stages.
- **Suggested heading**: "Lộ trình hoàn thiện chương trình"
- **Suggested copy direction**:
  * **01 / Hoàn thiện kỹ thuật**: Thiết lập hệ thống bảo mật và đồng bộ hóa dữ liệu khách hàng giữa các thương hiệu.
  * **02 / Thử nghiệm nội bộ**: Chạy thử nghiệm chương trình tại một số chi nhánh chọn lọc để tối ưu hóa quy trình phục vụ.
  * **03 / Phát hành chính thức**: Mở cổng đăng ký tài khoản trực tuyến và kích hoạt các quyền lợi hội viên đầu tiên.
- **Suggested CTA**: None.
- **Recommended layout**: Vertical steps layout.
- **Recommended style guide pattern**: `.public-editorial-page__steps` with `.public-editorial-page__step` and `.public-editorial-page__step-index`.
- **Responsive notes**: Spacing collapses cleanly on mobile.

---

## 5. Brand Ecosystem Tie-In

- **Purpose**: Connect the membership program to the actual restaurant group brands.
- **Suggested heading**: "Đồng hành cùng mọi thương hiệu"
- **Suggested copy direction**: "Quyền lợi thành viên của bạn sẽ được áp dụng thống nhất và đồng thời tại tất cả các thương hiệu thuộc hệ sinh thái KIG Holding, mang lại trải nghiệm ẩm thực liền mạch dù bạn dùng bữa tại bất kỳ đâu."
- **Suggested CTA**: None.
- **Recommended layout**: 3-column layout highlighting the core concepts:
  * **01 / Truyền Thuyết Champong**: Hương vị mì Hàn Quốc cay nồng trứ danh.
  * **02 / Gogimaru**: Thực đơn nướng cao cấp chuẩn vị.
  * **03 / KBB Cook**: Buffet BBQ hiện đại cho các buổi sum họp.
- **Recommended style guide pattern**: `.public-editorial-page__grid`
- **Responsive notes**: Stacks vertically on mobile.

---

## 6. Call To Action (CTA)

- **Purpose**: Keep users engaged and prompt them to stay in touch while the program is finalized.
- **Suggested heading**: "Đăng ký nhận thông báo sớm nhất"
- **Suggested copy direction**: "Để lại thông tin liên hệ của bạn để trở thành những người đầu tiên nhận thông báo khi cổng đăng ký thành viên chính thức được kích hoạt."
- **Suggested CTA**:
  * Primary: "Đăng ký nhận tin" (links to [Contact](file:///f:/Coding/Web%20development/KIG%20Holding/KIGHolding/Views/Contact/Index.cshtml) at `/lien-he`)
  * Secondary: "Quay lại trang chủ" (links to `/`)
- **Recommended layout**: Side-by-side CTA box.
- **Recommended style guide pattern**: `.public-editorial-page__cta`
- **Responsive notes**: Stacks into a centered block layout on mobile.
