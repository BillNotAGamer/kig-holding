using KIGHolding.Models.Entities;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

[Route("chi-nhanh")]
public class BranchController : Controller
{
    private readonly IBranchService _branchService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BranchController> _logger;

    public BranchController(
        IBranchService branchService,
        IConfiguration configuration,
        ILogger<BranchController> logger)
    {
        _branchService = branchService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] string? city, CancellationToken cancellationToken)
    {
        IReadOnlyList<Branch> activeBranches = [];

        if (HasConfiguredDatabase())
        {
            activeBranches = await TryLoadAsync(
                () => _branchService.GetActiveBranchesAsync(cancellationToken),
                "active branches") ?? [];
        }

        var cities = activeBranches
            .Select(x => x.City)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        var selectedCity = ResolveSelectedCity(cities, city);
        var filteredBranches = string.IsNullOrWhiteSpace(selectedCity)
            ? activeBranches
            : activeBranches
                .Where(x => string.Equals(x.City, selectedCity, StringComparison.OrdinalIgnoreCase))
                .ToList();

        var mapBranch = filteredBranches.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.GoogleMapUrl))
            ?? activeBranches.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.GoogleMapUrl));

        var model = new BranchIndexViewModel
        {
            Branches = filteredBranches.Select(BranchCardViewModel.FromBranch).ToList(),
            Cities = cities,
            SelectedCity = selectedCity,
            MainMapUrl = mapBranch?.GoogleMapUrl,
            SeoTitle = string.IsNullOrWhiteSpace(selectedCity) ? "Hệ thống chi nhánh" : $"Chi nhánh {selectedCity}",
            SeoDescription = string.IsNullOrWhiteSpace(selectedCity)
                ? "Tìm chi nhánh Truyền Thuyết Champong gần bạn nhất và đặt bàn trước cho bữa ăn Hàn Quốc cay nóng."
                : $"Tìm chi nhánh Truyền Thuyết Champong tại {selectedCity} và đặt bàn trước."
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
            _logger.LogWarning(exception, "Unable to load {DataName} for branch page.", dataName);
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

    private static string? ResolveSelectedCity(IReadOnlyList<string> cities, string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
        {
            return null;
        }

        var normalizedCity = city.Trim();
        return cities.FirstOrDefault(x => string.Equals(x, normalizedCity, StringComparison.OrdinalIgnoreCase));
    }
}
