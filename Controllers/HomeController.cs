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
    private readonly IBranchService _branchService;
    private readonly INewsService _newsService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        ISiteSettingService siteSettingService,
        IBranchService branchService,
        INewsService newsService,
        IConfiguration configuration,
        ILogger<HomeController> logger)
    {
        _siteSettingService = siteSettingService;
        _branchService = branchService;
        _newsService = newsService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        SiteSetting? siteSetting = null;
        IReadOnlyList<Branch> branches = [];
        IReadOnlyList<Review> reviews = [];
        IReadOnlyList<Post> latestPosts = [];

        if (HasConfiguredDatabase())
        {
            siteSetting = await TryLoadAsync(
                () => _siteSettingService.GetSettingsAsync(cancellationToken),
                "site settings");

            branches = await TryLoadAsync(
                () => _branchService.GetActiveBranchesAsync(cancellationToken),
                "active branches") ?? [];

            reviews = await TryLoadAsync(
                () => _branchService.GetVisibleReviewsAsync(6, cancellationToken),
                "visible reviews") ?? [];

            latestPosts = await TryLoadAsync(
                () => _newsService.GetPublishedPostsAsync(3, cancellationToken),
                "latest posts") ?? [];
        }

        var model = new HomeIndexViewModel
        {
            SiteSetting = siteSetting,
            Branches = branches.Select(BranchCardViewModel.FromBranch).ToList(),
            Reviews = reviews.Select(HomeReviewViewModel.FromReview).ToList(),
            LatestPosts = latestPosts.Select(PostCardViewModel.FromPost).ToList(),
            ReservationForm = CreateReservationForm(branches)
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

    private BookingMiniFormViewModel CreateReservationForm(IReadOnlyList<Branch> branches)
    {
        var branchOptions = branches.Count > 0
            ? branches.Select(branch => new BookingBranchOptionViewModel
            {
                Id = branch.Id,
                Name = branch.Name
            }).ToList()
            : new List<BookingBranchOptionViewModel>
            {
                new() { Id = Guid.Empty, Name = "Chọn chi nhánh" }
            };

        return new BookingMiniFormViewModel
        {
            ActionUrl = "/dat-ban",
            Branches = branchOptions
        };
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
