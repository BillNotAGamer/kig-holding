using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.ViewComponents;

public class SiteHeaderViewComponent : ViewComponent
{
    private readonly ISiteSettingService _siteSettingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SiteHeaderViewComponent> _logger;

    public SiteHeaderViewComponent(
        ISiteSettingService siteSettingService,
        IConfiguration configuration,
        ILogger<SiteHeaderViewComponent> logger)
    {
        _siteSettingService = siteSettingService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var setting = await TryGetSettingsAsync();
        var model = LayoutFallbacks.CreateHeader(setting, HttpContext.Request.Path.Value);

        return View(model);
    }

    private async Task<SiteSetting?> TryGetSettingsAsync()
    {
        if (!LayoutFallbacks.HasConfiguredDatabase(_configuration))
        {
            return null;
        }

        try
        {
            return await _siteSettingService.GetSettingsAsync();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load site settings for the header. Falling back to defaults.");
            return null;
        }
    }
}
