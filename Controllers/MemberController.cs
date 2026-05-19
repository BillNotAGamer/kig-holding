using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

[Route("thanh-vien")]
public class MemberController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Thành viên KIG Holding";
        ViewData["MetaDescription"] = "Trang thành viên KIG Holding đang được hoàn thiện, với các thông tin ưu đãi, tích điểm và cập nhật chương trình mới.";

        return View();
    }
}
