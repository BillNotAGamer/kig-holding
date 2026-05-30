using KIGHolding.Models.Entities;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

[Route("thuc-don")]
public class MenuController : Controller
{
    private readonly IMenuGroupService _menuGroupService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MenuController> _logger;

    public MenuController(
        IMenuGroupService menuGroupService,
        IWebHostEnvironment webHostEnvironment,
        IConfiguration configuration,
        ILogger<MenuController> logger)
    {
        _menuGroupService = menuGroupService;
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        IReadOnlyList<MenuGroup> groups = [];

        if (HasConfiguredDatabase())
        {
            groups = await TryLoadAsync(
                () => _menuGroupService.GetPublishedGroupsAsync(cancellationToken),
                "menu groups") ?? [];
        }

        var model = new MenuGroupLandingViewModel
        {
            Groups = groups.Select(CreateMenuGroupCard).ToList()
        };

        return View(model);
    }

    [HttpGet("nhom/{slug}")]
    public async Task<IActionResult> Group(string slug, CancellationToken cancellationToken)
    {
        if (!HasConfiguredDatabase())
        {
            return NotFound();
        }

        var normalizedSlug = NormalizeSlug(slug);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            return NotFound();
        }

        var group = await TryLoadAsync(
            () => _menuGroupService.GetPublishedGroupBySlugAsync(normalizedSlug, cancellationToken),
            "menu group detail");

        if (group is null)
        {
            return NotFound();
        }

        var images = await TryLoadAsync(
            () => _menuGroupService.GetPublishedImagesByGroupSlugAsync(normalizedSlug, cancellationToken),
            "menu group images") ?? [];

        var imageViewModels = images
            .Select((image, index) => new MenuPageImageViewModel
            {
                ImageUrl = image.ImageUrl,
                AltText = string.IsNullOrWhiteSpace(image.AltText)
                    ? $"{group.Name} - Trang thực đơn {(index + 1):00}"
                    : image.AltText,
                DisplayOrder = image.DisplayOrder,
                PageNumber = index + 1
            })
            .ToList();

        var firstImageUrl = imageViewModels.FirstOrDefault()?.ImageUrl;
        var viewerCoverImageUrl = string.IsNullOrWhiteSpace(group.CoverImageUrl)
            ? firstImageUrl
            : group.CoverImageUrl;

        var model = new MenuGroupDetailViewModel
        {
            Name = group.Name,
            Slug = group.Slug,
            ShortDescription = string.IsNullOrWhiteSpace(group.ShortDescription)
                ? GetFallbackGroupDescription(group.Slug)
                : group.ShortDescription,
            Description = group.Description,
            CoverImageUrl = viewerCoverImageUrl,
            FirstImageUrl = firstImageUrl,
            BackUrl = "/thuc-don",
            GroupUrl = $"/thuc-don/nhom/{group.Slug}",
            TotalPages = imageViewModels.Count,
            HasImages = imageViewModels.Count > 0,
            Images = imageViewModels
        };

        return View(model);
    }

    [HttpGet("{slug}")]
    public IActionResult Detail(string slug)
    {
        return RedirectToAction(nameof(Index));
    }

    private async Task<T?> TryLoadAsync<T>(Func<Task<T>> loader, string dataName)
    {
        try
        {
            return await loader();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load {DataName} for menu page.", dataName);
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

    private static string? NormalizeSlug(string? slug)
    {
        return string.IsNullOrWhiteSpace(slug)
            ? null
            : slug.Trim().ToLowerInvariant();
    }

    private MenuGroupCardViewModel CreateMenuGroupCard(MenuGroup group)
    {
        return new MenuGroupCardViewModel
        {
            Name = group.Name,
            Slug = group.Slug,
            ShortDescription = string.IsNullOrWhiteSpace(group.ShortDescription)
                ? GetFallbackGroupDescription(group.Slug)
                : group.ShortDescription,
            Description = group.Description,
            CoverImageUrl = ResolveMenuGroupCoverImage(group.CoverImageUrl, group.Slug),
            DisplayOrder = group.DisplayOrder,
            Url = $"/thuc-don/nhom/{group.Slug}",
            PageImageCount = group.PageImages.Count(x => x.IsPublished)
        };
    }

    private string? ResolveMenuGroupCoverImage(string? coverImageUrl, string slug)
    {
        if (!string.IsNullOrWhiteSpace(coverImageUrl))
        {
            return coverImageUrl;
        }

        var fallbackUrl = slug switch
        {
            "truyen-thuyet-champong" => "/images/home/images/truyen-thuyet-champong.webp",
            "gogimaru" => "/images/home/images/gogimaru-img.webp",
            "kbb-cook" => "/images/home/images/kbb-cook-img.png",
            _ => null
        };

        return StaticWebFileExists(fallbackUrl) ? fallbackUrl : null;
    }

    private bool StaticWebFileExists(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath))
        {
            return false;
        }

        var normalizedUrl = url.Split('?', '#')[0]
            .TrimStart('/', '~')
            .Replace('/', Path.DirectorySeparatorChar);
        var webRootPath = Path.GetFullPath(_webHostEnvironment.WebRootPath);
        var filePath = Path.GetFullPath(Path.Combine(webRootPath, normalizedUrl));

        return filePath.StartsWith(webRootPath, StringComparison.OrdinalIgnoreCase)
            && System.IO.File.Exists(filePath);
    }

    private static string GetFallbackGroupDescription(string slug)
    {
        return slug switch
        {
            "truyen-thuyet-champong" => "Các món Hàn đậm vị, nổi bật với tinh thần Champong hiện đại và trải nghiệm dùng bữa chỉn chu.",
            "gogimaru" => "Thực đơn nướng Hàn Quốc với các lựa chọn thịt, lẩu và món ăn kèm phù hợp cho nhóm.",
            "kbb-cook" => "Không gian BBQ hiện đại với nguyên liệu chọn lọc và thực đơn phù hợp cho những buổi gặp gỡ.",
            _ => "Chọn nhóm thực đơn để xem các trang menu được cập nhật từ Truyền Thuyết Champong."
        };
    }
}
