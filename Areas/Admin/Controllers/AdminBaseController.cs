using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace KIGHolding.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public abstract class AdminBaseController : Controller
{
    protected void SetSuccessMessage(string message)
    {
        TempData["SuccessMessage"] = message;
    }

    protected void SetErrorMessage(string message)
    {
        TempData["ErrorMessage"] = message;
    }

    public override void OnActionExecuted(ActionExecutedContext context)
    {
        base.OnActionExecuted(context);

        if (!HttpMethods.IsPost(Request.Method))
        {
            return;
        }

        if (TempData.ContainsKey("SuccessMessage") || TempData.ContainsKey("ErrorMessage"))
        {
            return;
        }

        if (context.Result is RedirectResult or RedirectToActionResult or LocalRedirectResult or RedirectToRouteResult)
        {
            TempData["SuccessMessage"] = "Thao tác đã được thực hiện thành công.";
            return;
        }

        if (context.Result is ViewResult)
        {
            TempData["ErrorMessage"] = "Vui lòng kiểm tra lại dữ liệu đã nhập.";
        }
    }
}
