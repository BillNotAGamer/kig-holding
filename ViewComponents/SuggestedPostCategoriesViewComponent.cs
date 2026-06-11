using KIGHolding.Services;
using KIGHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.ViewComponents;

public class SuggestedPostCategoriesViewComponent : ViewComponent
{
    private readonly INewsService _newsService;
    private readonly ILogger<SuggestedPostCategoriesViewComponent> _logger;

    public SuggestedPostCategoriesViewComponent(
        INewsService newsService,
        ILogger<SuggestedPostCategoriesViewComponent> logger)
    {
        _newsService = newsService;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        string mode = "Home",
        int? limit = null,
        int? poolSize = null,
        bool? rotateWithinRecentPool = null)
    {
        var resolvedMode = string.Equals(mode, "News", StringComparison.OrdinalIgnoreCase)
            ? "News"
            : "Home";
        var isNewsMode = string.Equals(resolvedMode, "News", StringComparison.OrdinalIgnoreCase);
        var resolvedLimit = limit ?? (isNewsMode ? 6 : 4);
        var resolvedPoolSize = poolSize ?? (isNewsMode ? 10 : 8);
        var resolvedRotation = rotateWithinRecentPool ?? !isNewsMode;

        try
        {
            var categories = await _newsService.GetSuggestedCategoriesAsync(
                resolvedLimit,
                resolvedPoolSize,
                resolvedRotation,
                HttpContext.RequestAborted);

            if (categories.Count == 0)
            {
                return Content(string.Empty);
            }

            return View(new SuggestedPostCategoriesViewModel
            {
                Mode = resolvedMode,
                Items = categories
            });
        }
        catch (OperationCanceledException)
        {
            return Content(string.Empty);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load suggested news categories.");
            return Content(string.Empty);
        }
    }
}
