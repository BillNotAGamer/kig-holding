using KIGHolding.Services;
using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.ViewComponents;

public class BookingMiniFormViewComponent : ViewComponent
{
    private readonly IBranchService _branchService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BookingMiniFormViewComponent> _logger;

    public BookingMiniFormViewComponent(
        IBranchService branchService,
        IConfiguration configuration,
        ILogger<BookingMiniFormViewComponent> logger)
    {
        _branchService = branchService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync(string actionUrl = "/dat-ban")
    {
        var model = new BookingMiniFormViewModel
        {
            ActionUrl = actionUrl,
            Branches = LayoutFallbacks.CreateFallbackBranchOptions()
        };

        if (LayoutFallbacks.HasConfiguredDatabase(_configuration))
        {
            try
            {
                var branches = await _branchService.GetActiveBranchesAsync();
                model.Branches = branches
                    .Select(branch => new BookingBranchOptionViewModel
                    {
                        Id = branch.Id,
                        Name = branch.Name
                    })
                    .ToList();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Unable to load branch options for booking mini form. Falling back to defaults.");
            }
        }

        return View(model);
    }
}
