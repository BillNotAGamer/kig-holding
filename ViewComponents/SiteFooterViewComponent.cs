using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.ViewComponents;

public class SiteFooterViewComponent : ViewComponent
{
    private readonly ISiteSettingService _siteSettingService;
    private readonly IBranchService _branchService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SiteFooterViewComponent> _logger;

    public SiteFooterViewComponent(
        ISiteSettingService siteSettingService,
        IBranchService branchService,
        IConfiguration configuration,
        ILogger<SiteFooterViewComponent> logger)
    {
        _siteSettingService = siteSettingService;
        _branchService = branchService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        SiteSetting? setting = null;
        IReadOnlyList<Branch>? branches = null;

        if (LayoutFallbacks.HasConfiguredDatabase(_configuration))
        {
            try
            {
                setting = await _siteSettingService.GetSettingsAsync();
                branches = await _branchService.GetActiveBranchesAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Unable to load footer data. Falling back to defaults.");
            }
        }

        return View(LayoutFallbacks.CreateFooter(setting, branches));
    }
}
