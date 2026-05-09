using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.ViewComponents;

public class FloatingContactButtonsViewComponent : ViewComponent
{
    private readonly ISiteSettingService _siteSettingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FloatingContactButtonsViewComponent> _logger;

    public FloatingContactButtonsViewComponent(
        ISiteSettingService siteSettingService,
        IConfiguration configuration,
        ILogger<FloatingContactButtonsViewComponent> logger)
    {
        _siteSettingService = siteSettingService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        SiteSetting? setting = null;

        if (LayoutFallbacks.HasConfiguredDatabase(_configuration))
        {
            try
            {
                setting = await _siteSettingService.GetSettingsAsync();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Unable to load floating contact data. Falling back to defaults.");
            }
        }

        return View(LayoutFallbacks.CreateFloatingButtons(setting));
    }
}
