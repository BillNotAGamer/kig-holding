using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.ViewComponents;

public class SectionHeadingViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(SectionHeadingViewModel model)
    {
        return View(model);
    }
}
