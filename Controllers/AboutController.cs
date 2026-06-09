using KIGHolding.Models.Entities;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

[Route("gioi-thieu")]
public class AboutController : Controller
{
    private readonly ISiteSettingService _siteSettingService;
    private readonly IBranchService _branchService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AboutController> _logger;

    public AboutController(
        ISiteSettingService siteSettingService,
        IBranchService branchService,
        IConfiguration configuration,
        ILogger<AboutController> logger)
    {
        _siteSettingService = siteSettingService;
        _branchService = branchService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        SiteSetting? siteSetting = null;
        IReadOnlyList<Branch> branches = [];

        if (HasConfiguredDatabase())
        {
            siteSetting = await TryLoadAsync(
                () => _siteSettingService.GetSettingsAsync(cancellationToken),
                "site settings");

            branches = await TryLoadAsync(
                () => _branchService.GetActiveBranchesAsync(cancellationToken),
                "featured branches") ?? [];
        }

        var model = new AboutIndexViewModel
        {
            SiteSetting = siteSetting,
            FeaturedBranches = branches.Take(3).Select(BranchCardViewModel.FromBranch).ToList(),
            SeoTitle = "Giới thiệu KIG Holding VN",
            SeoDescription = "Khám phá KIG Holding VN, hệ thống nhà hàng ẩm thực Hàn Quốc tại Việt Nam với Truyền Thuyết Champong, Gogi Maru, KBB COOK và hành trình phát triển từ năm 2019."
        };

        return View(model);
    }

    private async Task<T?> TryLoadAsync<T>(Func<Task<T>> loader, string dataName)
    {
        try
        {
            return await loader();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load {DataName} for about page.", dataName);
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
