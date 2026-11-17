using KIGHolding.Models.Content;

namespace KIGHolding.Services.AutoNews;

public sealed record EvergreenArticle(
    string Slug,
    string Title,
    string FocusKeyword,
    string Description,
    string ImagePath,
    BrandProfile Brand,
    string BodyHtml)
{
    public string Category => NewsCategories.MenuMonMoi;

    public string BuildHtml()
    {
        return BodyHtml +
               $"<h2>Thưởng thức tại {Brand.Name}</h2>" +
               $"<p>{Brand.Name} thuộc hệ thống KIG Holding, phục vụ {Brand.Dishes} theo phong cách Hàn Quốc. " +
               "Xem <a href=\"" + SeoArticleComposer.SiteBaseUrl + "/thuc-don\">thực đơn</a>, <a href=\"" + SeoArticleComposer.SiteBaseUrl + "/chi-nhanh\">địa chỉ chi nhánh</a> " +
               "hoặc <a href=\"" + SeoArticleComposer.SiteBaseUrl + "/dat-ban\">đặt bàn online</a> để giữ chỗ. " +
               SeoArticleComposer.BookingConditions + "</p>";
    }
}

/// <summary>
/// Evergreen articles about Korean food and culture, published one per day
/// on days when the Fanpage has no new post.
/// </summary>
public static class KoreanFoodArticleLibrary
{
    private const string Img = "/images/home/images/";

    public static readonly IReadOnlyList<EvergreenArticle> Articles =
    [
        new(
            "mi-tuong-den-jajangmyeon-la-gi",
            "Mì tương đen Jajangmyeon là gì? Nguồn gốc và cách ăn chuẩn vị Hàn",
            "mì tương đen",
            "Mì tương đen (Jajangmyeon) là món mì sợi dai trộn sốt tương đậu đen chunjang, thịt và rau củ. Tìm hiểu nguồn gốc và cách ăn mì tương đen chuẩn vị Hàn.",
            Img + "truyen-thuyet-champong.webp",
            SeoArticleComposer.Champong,
            """
            <p><strong>Mì tương đen (짜장면 – Jajangmyeon)</strong> là một trong những món mì phổ biến nhất Hàn Quốc: sợi mì dai, phủ lớp sốt đen bóng làm từ tương đậu đen chunjang xào cùng thịt heo, hành tây và rau củ.</p>
            <h2>Nguồn gốc mì tương đen</h2>
            <p>Món ăn bắt nguồn từ cộng đồng người Hoa sinh sống tại thành phố cảng Incheon đầu thế kỷ 20. Qua thời gian, người Hàn điều chỉnh công thức với tương chunjang ngọt hơn, sốt sánh hơn, tạo nên phiên bản Jajangmyeon mang bản sắc riêng.</p>
            <h2>Hương vị đặc trưng</h2>
            <ul>
            <li>Sốt tương đen đậm đà, vị mặn ngọt hài hòa, thơm mùi hành tây xào.</li>
            <li>Sợi mì kéo tay hoặc mì tươi dai, giữ sốt tốt.</li>
            <li>Thường ăn kèm củ cải vàng muối (danmuji) và hành tây chấm tương.</li>
            </ul>
            <h2>Cách ăn mì tương đen chuẩn vị Hàn</h2>
            <p>Khi mì được mang ra, hãy dùng đũa trộn đều sốt với sợi mì cho đến khi từng sợi phủ màu đen bóng. Ăn kèm củ cải vàng để cân bằng vị. Mì tương đen thường được gọi cùng thịt heo chiên xù sốt chua ngọt (tangsuyuk) cho bữa ăn nhóm.</p>
            """),
        new(
            "mi-champong-jjamppong-la-gi",
            "Mì Champong (Jjamppong) – mì hải sản cay nóng kiểu Hàn Quốc",
            "mì champong",
            "Mì Champong (Jjamppong) là món mì nước hải sản cay đỏ đặc trưng của Hàn Quốc. Khám phá nguyên liệu, hương vị và cách thưởng thức mì champong.",
            Img + "spicy-noodle-img.webp",
            SeoArticleComposer.Champong,
            """
            <p><strong>Mì Champong (짬뽕 – Jjamppong)</strong> là món mì nước có màu đỏ đặc trưng nhờ ớt bột, nấu cùng hải sản như mực, tôm, nghêu và rau củ xào lửa lớn.</p>
            <h2>Champong khác gì mì tương đen?</h2>
            <p>Nếu mì tương đen là món mì trộn đậm vị tương, thì champong là món mì nước cay nóng. Tại Hàn Quốc, câu hỏi "ăn jjajang hay jjamppong?" quen thuộc đến mức nhiều quán có cả bát chia đôi để thực khách thử cả hai.</p>
            <h2>Nguyên liệu chính của mì champong</h2>
            <ul>
            <li>Nước dùng từ xương và hải sản, xào cùng ớt bột để có màu đỏ và mùi thơm khói.</li>
            <li>Hải sản: mực, tôm, nghêu, vẹm tùy công thức.</li>
            <li>Rau củ: bắp cải, hành tây, cà rốt, nấm.</li>
            </ul>
            <h2>Thưởng thức thế nào cho ngon?</h2>
            <p>Nên ăn khi còn nóng để cảm nhận vị cay ấm và độ dai của sợi mì. Người chưa quen ăn cay có thể gọi kèm cơm trắng hoặc mì tương đen để cân bằng khẩu vị.</p>
            """),
        new(
            "thit-heo-chien-xu-sot-chua-ngot-tangsuyuk",
            "Tangsuyuk – thịt heo chiên xù sốt chua ngọt trong quán mì Hàn",
            "thịt heo chiên xù",
            "Tangsuyuk là món thịt heo chiên xù giòn rụm rưới sốt chua ngọt, thường gọi cùng mì tương đen và mì champong. Tìm hiểu món ăn và tranh luận 'chấm hay rưới'.",
            Img + "korean-food-no-bg.webp",
            SeoArticleComposer.Champong,
            """
            <p><strong>Tangsuyuk (탕수육)</strong> là thịt heo cắt miếng, áo bột chiên giòn và ăn cùng sốt chua ngọt có trái cây, rau củ. Đây là món gọi kèm quen thuộc trong bộ ba của quán mì Hàn: mì tương đen – mì champong – tangsuyuk.</p>
            <h2>Điểm làm nên một đĩa tangsuyuk ngon</h2>
            <ul>
            <li>Lớp vỏ giòn, không ngấm dầu, giữ độ giòn lâu.</li>
            <li>Thịt bên trong mềm, mọng nước.</li>
            <li>Sốt chua ngọt cân bằng, không quá gắt.</li>
            </ul>
            <h2>"Bumeok" hay "jjikmeok"?</h2>
            <p>Người Hàn có cuộc tranh luận vui: rưới sốt lên thịt (bumeok – 부먹) hay chấm từng miếng (jjikmeok – 찍먹). Rưới sốt giúp thịt thấm vị, còn chấm giữ được độ giòn. Khi đi nhóm, nên hỏi ý mọi người trước khi rưới sốt.</p>
            """),
        new(
            "kimchi-mon-an-kem-quoc-dan-han-quoc",
            "Kimchi – món ăn kèm quốc dân trong bữa cơm Hàn Quốc",
            "kimchi hàn quốc",
            "Kimchi là món rau củ lên men không thể thiếu trong bữa ăn Hàn Quốc. Tìm hiểu các loại kimchi phổ biến và văn hóa Kimjang được UNESCO ghi danh.",
            Img + "kimchi-img.webp",
            SeoArticleComposer.Champong,
            """
            <p><strong>Kimchi (김치)</strong> là rau củ muối và lên men với ớt bột, tỏi, gừng, mắm. Món ăn này xuất hiện trong hầu hết bữa ăn của người Hàn, từ cơm gia đình đến quán mì, quán nướng.</p>
            <h2>Các loại kimchi phổ biến</h2>
            <ul>
            <li><strong>Baechu kimchi</strong>: kimchi cải thảo – loại quen thuộc nhất.</li>
            <li><strong>Kkakdugi</strong>: kimchi củ cải cắt hạt lựu, giòn và hơi ngọt.</li>
            <li><strong>Oi sobagi</strong>: kimchi dưa leo nhồi rau, hợp mùa hè.</li>
            </ul>
            <h2>Văn hóa Kimjang</h2>
            <p>Kimjang là truyền thống cả gia đình, hàng xóm cùng muối kimchi vào cuối thu để dùng suốt mùa đông. Năm 2013, Kimjang được UNESCO ghi danh là Di sản văn hóa phi vật thể đại diện của nhân loại.</p>
            <h2>Kimchi ăn kèm món gì?</h2>
            <p>Vị chua cay của kimchi giúp cân bằng các món đậm vị như mì tương đen, thịt nướng hay canh gukbap.</p>
            """),
        new(
            "banchan-mon-an-kem-han-quoc",
            "Banchan là gì? Văn hóa món ăn kèm trong bữa ăn Hàn Quốc",
            "món ăn hàn quốc",
            "Banchan là các đĩa món ăn kèm nhỏ trong bữa ăn Hàn Quốc như kimchi, rau trộn, trứng cuộn. Tìm hiểu ý nghĩa và các loại banchan phổ biến.",
            "/images/about/images/about-panchan-detail.webp",
            SeoArticleComposer.GogiMaru,
            """
            <p><strong>Banchan (반찬)</strong> là tên gọi chung các đĩa món ăn kèm nhỏ được dọn cùng cơm và món chính trong bữa ăn Hàn Quốc.</p>
            <h2>Các loại banchan thường gặp</h2>
            <ul>
            <li>Kimchi cải thảo, kimchi củ cải.</li>
            <li>Namul – rau trộn dầu mè như giá đỗ, rau bina.</li>
            <li>Gyeran-mari – trứng cuộn.</li>
            <li>Đậu phụ rim, chả cá xào, khoai tây rim.</li>
            </ul>
            <h2>Ý nghĩa của banchan</h2>
            <p>Banchan thể hiện sự hiếu khách và giúp bữa ăn cân bằng dinh dưỡng với nhiều vị chua, cay, mặn, ngọt. Trong các quán thịt nướng Hàn Quốc, banchan và rau cuốn là phần không thể thiếu.</p>
            <h2>Mẹo thưởng thức</h2>
            <p>Hãy xen kẽ banchan giữa các miếng thịt nướng hoặc thìa canh để vị giác luôn "mới" và bữa ăn bớt ngấy.</p>
            """),
        new(
            "black-day-14-4-an-mi-tuong-den",
            "Black Day 14/4 – ngày ăn mì tương đen của giới trẻ Hàn Quốc",
            "mì tương đen hàn quốc",
            "Black Day 14/4 là ngày những người độc thân ở Hàn Quốc mặc đồ đen và ăn mì tương đen. Tìm hiểu nguồn gốc ngày lễ vui này.",
            Img + "banner1.webp",
            SeoArticleComposer.Champong,
            """
            <p>Ở Hàn Quốc, ngày <strong>14/4 – Black Day</strong> gắn liền với hình ảnh những người trẻ độc thân mặc đồ đen và cùng nhau ăn một tô <strong>mì tương đen</strong>.</p>
            <h2>Black Day bắt nguồn từ đâu?</h2>
            <p>Người Hàn có Valentine 14/2 (con gái tặng quà) và White Day 14/3 (con trai đáp lễ). Những ai không nhận được quà trong hai ngày này sẽ "đón" Black Day 14/4 – một dịp vui để tự thưởng cho bản thân.</p>
            <h2>Vì sao lại là mì tương đen?</h2>
            <p>Màu đen của sốt chunjang trùng với "chủ đề" của ngày lễ. Dần dần, việc ăn Jajangmyeon vào 14/4 trở thành một nét văn hóa vui nhộn, nhiều quán còn có chương trình riêng cho ngày này.</p>
            <h2>Không cần đợi 14/4</h2>
            <p>Dù độc thân hay không, một tô mì tương đen nóng hổi luôn là lựa chọn dễ ăn cho bữa trưa, bữa tối hay buổi tụ tập bạn bè.</p>
            """),
        new(
            "cach-an-thit-nuong-han-quoc-dung-dieu",
            "Cách ăn thịt nướng Hàn Quốc đúng điệu: cuốn ssam như người Hàn",
            "thịt nướng hàn quốc",
            "Hướng dẫn cách ăn thịt nướng Hàn Quốc đúng điệu: cuốn rau ssam, chấm ssamjang, ăn kèm banchan. Bí quyết để bữa BBQ Hàn ngon và trọn vị.",
            "/images/about/images/about-bbq-premium-meat.webp",
            SeoArticleComposer.GogiMaru,
            """
            <p>Thịt nướng Hàn Quốc ngon không chỉ nhờ thịt mà còn ở <strong>cách ăn</strong>. Người Hàn thường cuốn thịt với rau – gọi là <strong>ssam (쌈)</strong>.</p>
            <h2>Các bước cuốn ssam</h2>
            <ul>
            <li>Đặt một lá xà lách hoặc lá tía tô Hàn lên lòng bàn tay.</li>
            <li>Thêm miếng thịt nướng vừa chín, chấm chút muối mè hoặc ssamjang.</li>
            <li>Thêm tỏi nướng, kimchi hoặc hành trộn tùy thích.</li>
            <li>Gói lại vừa một miếng và ăn trọn.</li>
            </ul>
            <h2>Các phần thịt phổ biến</h2>
            <p>Samgyeopsal (ba chỉ heo), moksal (nạc vai), galbi (sườn ướp) là những lựa chọn quen thuộc. Mỗi phần có độ béo và độ dai khác nhau, nên gọi kết hợp để thử nhiều vị.</p>
            <h2>Mẹo nướng thịt ngon</h2>
            <p>Không lật thịt quá nhiều lần, cắt miếng vừa ăn sau khi mặt ngoài đã xém vàng và ăn ngay khi còn nóng.</p>
            """),
        new(
            "thit-u-len-men-la-gi",
            "Thịt ủ lên men là gì? Vì sao thịt nướng ủ lâu mềm và đậm vị hơn",
            "thịt nướng hàn quốc",
            "Thịt ủ lên men (aging) là quá trình ủ thịt trong điều kiện kiểm soát để thịt mềm và đậm vị hơn. Tìm hiểu nguyên lý và lý do được dùng trong BBQ Hàn Quốc.",
            Img + "gogimaru-img.webp",
            SeoArticleComposer.GogiMaru,
            """
            <p><strong>Ủ thịt (aging)</strong> là phương pháp bảo quản thịt trong điều kiện nhiệt độ và độ ẩm được kiểm soát trong một khoảng thời gian, để enzyme tự nhiên làm mềm thớ thịt.</p>
            <h2>Hai cách ủ phổ biến</h2>
            <ul>
            <li><strong>Ủ khô (dry aging)</strong>: thịt được ủ trong tủ chuyên dụng, mất bớt nước nên vị đậm hơn.</li>
            <li><strong>Ủ ướt (wet aging)</strong>: thịt được hút chân không và ủ lạnh, giữ độ ẩm tốt.</li>
            </ul>
            <h2>Lợi ích khi nướng</h2>
            <p>Thịt sau khi ủ thường mềm hơn, thớ thịt ít co rút khi gặp nhiệt và giữ được vị ngọt tự nhiên. Vì vậy nhiều quán BBQ Hàn Quốc chọn thịt ủ để khách thưởng thức trọn vị nguyên bản, hạn chế tẩm ướp.</p>
            <h2>Cách thưởng thức</h2>
            <p>Nướng vừa chín tới, chấm chút muối hoặc dầu mè để cảm nhận rõ nhất độ mềm và vị ngọt của thịt.</p>
            """),
        new(
            "kinh-nghiem-di-buffet-nuong-han-quoc",
            "Kinh nghiệm đi buffet nướng Hàn Quốc theo nhóm: ăn ngon, không lãng phí",
            "buffet nướng hàn quốc",
            "Kinh nghiệm đi buffet nướng Hàn Quốc theo nhóm: chọn gói phù hợp, thứ tự nướng, đặt bàn trước. Mẹo để buổi buffet vui và trọn vị.",
            Img + "kbb-cook-img.webp",
            SeoArticleComposer.KbbCook,
            """
            <p><strong>Buffet nướng Hàn Quốc</strong> là lựa chọn quen thuộc cho các buổi họp lớp, sinh nhật hay liên hoan công ty. Một vài kinh nghiệm nhỏ sẽ giúp cả nhóm ăn ngon và thoải mái hơn.</p>
            <h2>Trước khi đi</h2>
            <ul>
            <li>Đặt bàn trước, đặc biệt vào cuối tuần và ngày lễ.</li>
            <li>Xem trước các gói buffet và thời gian sử dụng để chọn gói phù hợp ngân sách.</li>
            </ul>
            <h2>Thứ tự nướng hợp lý</h2>
            <p>Nên bắt đầu với các phần thịt ít ướp như ba chỉ, nạc vai để cảm nhận vị nguyên bản, sau đó mới đến thịt ướp sốt và hải sản. Cách này giúp vỉ nướng sạch hơn và không lẫn vị.</p>
            <h2>Ăn ngon, không lãng phí</h2>
            <p>Gọi từng lượt vừa đủ, ăn hết rồi gọi tiếp. Kết hợp rau cuốn, kimchi và canh để bữa ăn cân bằng, đỡ ngấy.</p>
            """),
        new(
            "dwaeji-gukbap-canh-com-xuong-heo-busan",
            "Dwaeji Gukbap – canh cơm xương heo đặc sản thành phố Busan",
            "gukbap",
            "Dwaeji Gukbap là món canh cơm xương heo trứ danh của Busan, nước dùng ninh lâu, ăn kèm cơm và kimchi. Tìm hiểu nguồn gốc và cách ăn gukbap.",
            Img + "seoul-gukbap.webp",
            SeoArticleComposer.SeoulGukbap,
            """
            <p><strong>Dwaeji Gukbap (돼지국밥)</strong> là món canh cơm với nước dùng xương heo ninh nhiều giờ, thịt heo thái lát và cơm trắng – đặc sản nổi tiếng của thành phố cảng Busan.</p>
            <h2>Nguồn gốc</h2>
            <p>Món ăn gắn với giai đoạn sau chiến tranh, khi người dân tận dụng xương heo để nấu nồi canh đủ no cho nhiều người. Từ đó, gukbap trở thành món ăn bình dân, ấm bụng được yêu thích khắp Hàn Quốc.</p>
            <h2>Cách ăn gukbap chuẩn vị</h2>
            <ul>
            <li>Nêm thêm mắm tôm Hàn (saeujeot) hoặc muối theo khẩu vị.</li>
            <li>Thêm hẹ trộn, ớt bột nếu thích cay.</li>
            <li>Cho cơm vào canh hoặc ăn riêng từng thìa, kèm kimchi củ cải.</li>
            </ul>
            <h2>Khi nào nên ăn gukbap?</h2>
            <p>Gukbap hợp cho bữa sáng, bữa trưa nhanh gọn hay những ngày trời mưa cần một món nóng hổi.</p>
            """),
        new(
            "soju-van-hoa-ban-an-han-quoc",
            "Soju và văn hóa bàn ăn Hàn Quốc: những phép lịch sự nên biết",
            "văn hóa hàn quốc",
            "Tìm hiểu soju và các phép lịch sự trên bàn ăn Hàn Quốc: rót bằng hai tay, quay mặt khi uống trước người lớn. Uống có trách nhiệm, chỉ dành cho người trên 18 tuổi.",
            Img + "soyu-bottle-img.webp",
            SeoArticleComposer.GogiMaru,
            """
            <p><strong>Soju (소주)</strong> là đồ uống có cồn phổ biến tại Hàn Quốc, thường xuất hiện trong các bữa thịt nướng, lẩu hay đồ chiên.</p>
            <h2>Phép lịch sự khi rót và uống</h2>
            <ul>
            <li>Rót cho người lớn tuổi bằng hai tay hoặc một tay đỡ cổ tay.</li>
            <li>Không tự rót cho mình – người cùng bàn sẽ rót cho nhau.</li>
            <li>Khi uống trước người lớn tuổi, quay mặt sang bên để thể hiện sự tôn trọng.</li>
            </ul>
            <h2>Món ăn hợp với soju</h2>
            <p>Thịt nướng, canh nóng, gà rán hay các món cay đều là "anju" (món nhắm) quen thuộc.</p>
            <h2>Uống có trách nhiệm</h2>
            <p>Đồ uống có cồn chỉ dành cho người trên 18 tuổi. Đã uống rượu bia thì không lái xe – hãy chọn taxi hoặc xe công nghệ khi về.</p>
            """),
        new(
            "gochugaru-gochujang-vi-cay-han-quoc",
            "Gochugaru và Gochujang – bí mật làm nên vị cay của món Hàn",
            "món ăn hàn quốc",
            "Gochugaru (ớt bột) và Gochujang (tương ớt) là hai gia vị tạo nên vị cay đặc trưng của ẩm thực Hàn Quốc. Phân biệt và cách dùng trong món ăn.",
            Img + "chilli-img.webp",
            SeoArticleComposer.Champong,
            """
            <p>Vị cay của món Hàn khác với vị cay của nhiều nền ẩm thực khác nhờ hai gia vị quen thuộc: <strong>gochugaru</strong> và <strong>gochujang</strong>.</p>
            <h2>Gochugaru – ớt bột Hàn Quốc</h2>
            <p>Gochugaru là ớt đỏ phơi khô rồi xay, có màu đỏ tươi và vị cay vừa, hơi ngọt và thơm mùi khói. Đây là gia vị chính trong kimchi và các món canh, mì cay như champong.</p>
            <h2>Gochujang – tương ớt lên men</h2>
            <p>Gochujang được làm từ ớt bột, gạo nếp, đậu nành lên men và muối. Tương có vị cay, ngọt, mặn hòa quyện, dùng trong tteokbokki, cơm trộn bibimbap hay sốt ướp thịt.</p>
            <h2>Phân biệt nhanh</h2>
            <ul>
            <li>Gochugaru: dạng bột, cay thanh, tạo màu đỏ.</li>
            <li>Gochujang: dạng sệt, đậm vị, có độ ngọt tự nhiên.</li>
            </ul>
            """),
        new(
            "tteokbokki-banh-gao-cay-duong-pho-han-quoc",
            "Tteokbokki – bánh gạo cay đường phố được yêu thích nhất Hàn Quốc",
            "tteokbokki",
            "Tteokbokki là bánh gạo nấu sốt gochujang cay ngọt, món ăn vặt đường phố nổi tiếng Hàn Quốc. Tìm hiểu nguồn gốc và các biến tấu phổ biến.",
            Img + "korean-food-no-bg.webp",
            SeoArticleComposer.Champong,
            """
            <p><strong>Tteokbokki (떡볶이)</strong> là món bánh gạo dẻo nấu cùng sốt gochujang cay ngọt, chả cá và hành lá – món ăn vặt đường phố quen thuộc tại Hàn Quốc.</p>
            <h2>Từ cung đình đến vỉa hè</h2>
            <p>Phiên bản xưa là gungjung tteokbokki xào xì dầu, dành cho hoàng gia. Phiên bản cay với gochujang phổ biến từ giữa thế kỷ 20 và trở thành món ăn đường phố được nhiều thế hệ yêu thích.</p>
            <h2>Các biến tấu phổ biến</h2>
            <ul>
            <li>Rabokki: tteokbokki nấu cùng mì gói.</li>
            <li>Cheese tteokbokki: phủ phô mai kéo sợi.</li>
            <li>Rose tteokbokki: sốt kem kết hợp gochujang, vị béo dịu cay.</li>
            </ul>
            <h2>Ăn kèm món gì?</h2>
            <p>Tteokbokki hợp với trứng luộc, chả cá, đồ chiên và đặc biệt là ăn cùng các món mì Hàn trong bữa ăn nhóm.</p>
            """),
        new(
            "goi-y-thuc-don-han-quoc-cho-tiec-nhom",
            "Gợi ý thực đơn Hàn Quốc cho tiệc nhóm, liên hoan công ty",
            "nhà hàng hàn quốc",
            "Gợi ý thực đơn Hàn Quốc cho tiệc nhóm và liên hoan công ty: món chính, món chia sẻ, món ăn kèm. Mẹo đặt bàn để buổi tiệc diễn ra suôn sẻ.",
            Img + "banner2.webp",
            SeoArticleComposer.Champong,
            """
            <p>Ẩm thực Hàn Quốc rất hợp cho <strong>tiệc nhóm</strong> vì có nhiều món dùng chung, dễ gọi và hợp khẩu vị số đông.</p>
            <h2>Gợi ý thực đơn cân bằng</h2>
            <ul>
            <li><strong>Món chính</strong>: mì tương đen, mì champong – mỗi người một tô hoặc chia bát.</li>
            <li><strong>Món chia sẻ</strong>: thịt heo chiên xù sốt chua ngọt, đồ chiên, thịt nướng.</li>
            <li><strong>Món ăn kèm</strong>: kimchi, củ cải vàng, rau trộn.</li>
            <li><strong>Món nóng</strong>: canh hoặc lẩu cho những ngày mưa.</li>
            </ul>
            <h2>Mẹo cho buổi tiệc suôn sẻ</h2>
            <p>Chốt số lượng khách và đặt bàn trước để quán chuẩn bị chỗ ngồi liền nhau. Với nhóm đông, nên gọi món chia sẻ trước rồi gọi thêm theo nhu cầu để tránh lãng phí.</p>
            """),
        new(
            "phep-lich-su-ban-an-nguoi-han",
            "Những phép lịch sự trên bàn ăn của người Hàn bạn nên biết",
            "văn hóa hàn quốc",
            "Những phép lịch sự trên bàn ăn Hàn Quốc: chờ người lớn cầm đũa trước, không cắm đũa vào bát cơm, dùng thìa cho cơm và canh.",
            Img + "banner3.webp",
            SeoArticleComposer.SeoulGukbap,
            """
            <p>Bữa ăn của người Hàn đề cao sự tôn trọng người lớn tuổi. Biết một vài phép lịch sự sẽ giúp bạn tự tin hơn khi dùng bữa cùng bạn bè, đối tác Hàn Quốc.</p>
            <h2>Những điều nên làm</h2>
            <ul>
            <li>Chờ người lớn tuổi nhất cầm đũa rồi mới bắt đầu ăn.</li>
            <li>Dùng thìa cho cơm và canh, đũa cho món ăn kèm.</li>
            <li>Ăn chậm, giữ nhịp cùng mọi người trên bàn.</li>
            </ul>
            <h2>Những điều nên tránh</h2>
            <ul>
            <li>Không cắm đũa thẳng đứng vào bát cơm.</li>
            <li>Không nâng bát lên miệng như khi ăn cơm kiểu Việt.</li>
            <li>Không đứng dậy rời bàn trước khi người lớn ăn xong.</li>
            </ul>
            <p>Những món ăn chung như canh gukbap, đĩa thịt nướng hay banchan là dịp để mọi người cùng chia sẻ và trò chuyện.</p>
            """)
    ];
}
