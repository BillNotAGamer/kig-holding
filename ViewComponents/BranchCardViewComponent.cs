using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.ViewComponents;

public class BranchCardViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(BranchCardViewModel model)
    {
        return View(model);
    }
}
