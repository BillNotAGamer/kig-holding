using KIGHolding.Models;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace KIGHolding.Controllers;

public class HomeController : Controller
{
    private readonly ISiteSettingService _siteSettingService;
    private readonly INewsService _newsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ISiteSettingService siteSettingService,
        INewsService newsService,
        IConfiguration configuration,
        ILogger<HomeController> logger)
    {
        _siteSettingService = siteSettingService;
        _newsService = newsService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        SiteSetting? siteSetting = null;
        IReadOnlyList<PostCardViewModel> latestPosts = [];

        if (HasConfiguredDatabase())
        {
            siteSetting = await TryLoadAsync(
                () => _siteSettingService.GetSettingsAsync(cancellationToken),
                "site settings");

            latestPosts = await TryLoadAsync(
                () => _newsService.GetPublishedPostCardsAsync(3, cancellationToken),
                "latest posts") ?? [];
        }

        var model = new HomeIndexViewModel
        {
            SiteSetting = siteSetting,
            LatestPosts = latestPosts
        };

        return View(model);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private async Task<T?> TryLoadAsync<T>(Func<Task<T>> loader, string dataName)
    {
        try
        {
            return await loader();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load {DataName} for homepage.", dataName);
            return default;
        }
    }

    private bool HasConfiguredDatabase()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        return !string.IsNullOrWhiteSpace(connectionString)
            && !connectionString.Contains("your-neon-host", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("your_username", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("your_password", StringComparison.OrdinalIgnoreCase);
    }
}
