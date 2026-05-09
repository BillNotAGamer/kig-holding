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
    private readonly IMenuService _menuService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AboutController> _logger;

    public AboutController(
        ISiteSettingService siteSettingService,
        IBranchService branchService,
        IMenuService menuService,
        IConfiguration configuration,
        ILogger<AboutController> logger)
    {
        _siteSettingService = siteSettingService;
        _branchService = branchService;
        _menuService = menuService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        SiteSetting? siteSetting = null;
        IReadOnlyList<Branch> branches = [];
        IReadOnlyList<MenuItem> signatureMenuItems = [];

        if (HasConfiguredDatabase())
        {
            siteSetting = await TryLoadAsync(
                () => _siteSettingService.GetSettingsAsync(cancellationToken),
                "site settings");

            branches = await TryLoadAsync(
                () => _branchService.GetActiveBranchesAsync(cancellationToken),
                "featured branches") ?? [];

            signatureMenuItems = await TryLoadAsync(
                () => _menuService.GetSignatureMenuItemsAsync(4, cancellationToken),
                "signature menu items") ?? [];
        }

        var model = new AboutIndexViewModel
        {
            SiteSetting = siteSetting,
            FeaturedBranches = branches.Take(3).Select(BranchCardViewModel.FromBranch).ToList(),
            SignatureMenuItems = signatureMenuItems.Select(FoodCardViewModel.FromMenuItem).ToList(),
            SeoTitle = "Giới thiệu",
            SeoDescription = "Khám phá câu chuyện Truyền Thuyết Champong: cảm hứng ẩm thực Hàn Quốc, nước dùng cay nồng, nguyên liệu tươi và không gian dùng bữa ấm cúng."
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
