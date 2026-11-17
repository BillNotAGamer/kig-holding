using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

[Route("tuyen-dung")]
public class CareerController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Tuyển dụng nhà hàng Hàn Quốc KIG Holding | Champong, Gogi Maru, KBB Cook";
        ViewData["MetaDescription"] = "Tuyển dụng phục vụ, bếp, thu ngân, quản lý ca tại chuỗi nhà hàng Hàn Quốc KIG Holding: Truyền Thuyết Champong, Gogi Maru, KBB Cook, Seoul Gukbap. Ứng tuyển nhanh qua Zalo hoặc email.";
        ViewData["CanonicalUrl"] = "/tuyen-dung";

        return View();
    }
}
