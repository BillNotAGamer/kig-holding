using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

[Route("lien-he-nhuong-quyen")]
public class FranchiseController : Controller
{
    [HttpGet("")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Liên hệ nhượng quyền";
        ViewData["MetaDescription"] = "Trang liên hệ nhượng quyền KIG Holding đang được hoàn thiện để tiếp nhận trao đổi hợp tác và cơ hội phát triển thương hiệu.";

        return View();
    }
}
