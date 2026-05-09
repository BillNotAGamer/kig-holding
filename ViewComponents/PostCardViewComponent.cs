using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.ViewComponents;

public class PostCardViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(PostCardViewModel model)
    {
        return View(model);
    }
}
