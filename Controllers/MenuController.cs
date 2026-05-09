using KIGHolding.Models.Entities;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

[Route("thuc-don")]
public class MenuController : Controller
{
    private readonly IMenuService _menuService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MenuController> _logger;

    public MenuController(
        IMenuService menuService,
        IConfiguration configuration,
        ILogger<MenuController> logger)
    {
        _menuService = menuService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] string? category, CancellationToken cancellationToken)
    {
        var selectedCategorySlug = NormalizeSlug(category);
        IReadOnlyList<MenuCategory> categories = [];
        IReadOnlyList<MenuItem> menuItems = [];
        IReadOnlyList<MenuItem> featuredItems = [];

        if (HasConfiguredDatabase())
        {
            categories = await TryLoadAsync(
                () => _menuService.GetActiveCategoriesAsync(cancellationToken),
                "menu categories") ?? [];

            menuItems = await TryLoadAsync(
                () => _menuService.GetActiveMenuItemsAsync(selectedCategorySlug, cancellationToken),
                "menu items") ?? [];

            featuredItems = await TryLoadAsync(
                () => _menuService.GetFeaturedMenuItemsAsync(1, cancellationToken),
                "featured menu item") ?? [];
        }

        var categoryFilters = categories.Count > 0
            ? categories.Select(x => new MenuCategoryFilterViewModel
            {
                Name = x.Name,
                Slug = x.Slug,
                IsActive = string.Equals(x.Slug, selectedCategorySlug, StringComparison.OrdinalIgnoreCase)
            }).ToList()
            : CreateDefaultCategories(selectedCategorySlug);

        var selectedCategory = categoryFilters.FirstOrDefault(x => x.IsActive);
        var pageTitle = selectedCategory is null
            ? "Thực đơn Truyền Thuyết Champong"
            : selectedCategory.Name;

        var featuredItem = menuItems.FirstOrDefault(x => x.IsSignature || x.IsBestSeller)
            ?? featuredItems.FirstOrDefault();

        var model = new MenuIndexViewModel
        {
            Categories = categoryFilters,
            SelectedCategorySlug = selectedCategorySlug,
            MenuItems = menuItems.Select(FoodCardViewModel.FromMenuItem).ToList(),
            FeaturedItem = featuredItem is null ? null : FoodCardViewModel.FromMenuItem(featuredItem),
            PageTitle = pageTitle,
            SeoTitle = selectedCategory is null ? "Thực đơn" : $"{selectedCategory.Name} - Thực đơn",
            SeoDescription = selectedCategory is null
                ? "Khám phá mì Champong hải sản cay nồng, Korean BBQ, lẩu, combo, panchan và đồ uống tại Truyền Thuyết Champong."
                : $"Khám phá các món {selectedCategory.Name.ToLowerInvariant()} tại Truyền Thuyết Champong."
        };

        return View(model);
    }

    [HttpGet("{slug}")]
    public async Task<IActionResult> Detail(string slug, CancellationToken cancellationToken)
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

        var menuItem = await TryLoadAsync(
            () => _menuService.GetMenuItemBySlugAsync(normalizedSlug, cancellationToken),
            "menu item detail");

        if (menuItem is null)
        {
            return NotFound();
        }

        var relatedItems = await TryLoadAsync(
            () => _menuService.GetRelatedMenuItemsAsync(menuItem.CategoryId, menuItem.Id, 4, cancellationToken),
            "related menu items") ?? [];

        var model = new MenuDetailViewModel
        {
            MenuItem = menuItem,
            Category = menuItem.Category,
            GalleryImages = CreateGallery(menuItem),
            RelatedItems = relatedItems.Select(FoodCardViewModel.FromMenuItem).ToList(),
            SeoTitle = string.IsNullOrWhiteSpace(menuItem.SeoTitle) ? menuItem.Name : menuItem.SeoTitle,
            SeoDescription = string.IsNullOrWhiteSpace(menuItem.SeoDescription) ? menuItem.ShortDescription : menuItem.SeoDescription
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

    private static IReadOnlyList<MenuCategoryFilterViewModel> CreateDefaultCategories(string? selectedCategorySlug)
    {
        return
        [
            CreateDefaultCategory("Mì Champong", "mi-champong", selectedCategorySlug),
            CreateDefaultCategory("Mì tương đen", "mi-tuong-den", selectedCategorySlug),
            CreateDefaultCategory("BBQ", "bbq", selectedCategorySlug),
            CreateDefaultCategory("Lẩu", "lau", selectedCategorySlug),
            CreateDefaultCategory("Cơm & món Hàn", "com-mon-han", selectedCategorySlug),
            CreateDefaultCategory("Combo", "combo", selectedCategorySlug),
            CreateDefaultCategory("Panchan", "panchan", selectedCategorySlug),
            CreateDefaultCategory("Đồ uống", "do-uong", selectedCategorySlug)
        ];
    }

    private static MenuCategoryFilterViewModel CreateDefaultCategory(string name, string slug, string? selectedCategorySlug)
    {
        return new MenuCategoryFilterViewModel
        {
            Name = name,
            Slug = slug,
            IsActive = string.Equals(slug, selectedCategorySlug, StringComparison.OrdinalIgnoreCase)
        };
    }

    private static IReadOnlyList<MenuGalleryImageViewModel> CreateGallery(MenuItem menuItem)
    {
        var galleryImages = menuItem.Images
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new MenuGalleryImageViewModel
            {
                ImageUrl = x.ImageUrl,
                AltText = string.IsNullOrWhiteSpace(x.AltText) ? menuItem.Name : x.AltText
            })
            .ToList();

        if (galleryImages.Count == 0 && !string.IsNullOrWhiteSpace(menuItem.ThumbnailUrl))
        {
            galleryImages.Add(new MenuGalleryImageViewModel
            {
                ImageUrl = menuItem.ThumbnailUrl,
                AltText = menuItem.Name
            });
        }

        return galleryImages;
    }
}
