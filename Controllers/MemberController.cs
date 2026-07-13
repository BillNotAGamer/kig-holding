using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

[Route("thanh-vien")]
public class MemberController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Chương trình thành viên Truyền Thuyết Champong | Tích điểm & Đặc quyền";
        ViewData["MetaDescription"] = "Khám phá chương trình thành viên Truyền Thuyết Champong với 4 hạng Đồng, Bạc, Vàng, Kim Cương, quyền lợi tích điểm, ưu đãi sinh nhật và quy tắc nâng hạng.";
        ViewData["CanonicalUrl"] = "/thanh-vien";

        return View();
    }
}
