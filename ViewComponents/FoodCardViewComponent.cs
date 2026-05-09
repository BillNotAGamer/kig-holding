using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.ViewComponents;

public class FoodCardViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(FoodCardViewModel model)
    {
        return View(model);
    }
}
