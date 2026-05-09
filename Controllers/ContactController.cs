using KIGHolding.Models.Entities;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

[Route("lien-he")]
public class ContactController : Controller
{
    private readonly IContactService _contactService;
    private readonly ISiteSettingService _siteSettingService;
    private readonly IBranchService _branchService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContactController> _logger;

    public ContactController(
        IContactService contactService,
        ISiteSettingService siteSettingService,
        IBranchService branchService,
        IConfiguration configuration,
        ILogger<ContactController> logger)
    {
        _contactService = contactService;
        _siteSettingService = siteSettingService;
        _branchService = branchService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index([FromQuery] bool success = false, CancellationToken cancellationToken = default)
    {
        var model = new ContactCreateViewModel
        {
            IsSuccess = success
        };

        await PopulateContactMetadataAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContactCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!HasConfiguredDatabase())
        {
            ModelState.AddModelError(string.Empty, "Hệ thống liên hệ chưa kết nối cơ sở dữ liệu. Vui lòng gọi hotline để được hỗ trợ.");
        }

        if (ModelState.IsValid)
        {
            var result = await _contactService.CreateContactMessageAsync(new ContactCreateRequest
            {
                FullName = model.FullName,
                PhoneNumber = model.PhoneNumber,
                Email = model.Email,
                Subject = model.Subject,
                Message = model.Message
            }, cancellationToken);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(Index), new { success = true });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.FieldName, error.Message);
            }
        }

        await PopulateContactMetadataAsync(model, cancellationToken);
        model.IsSuccess = false;
        return View(model);
    }

    private async Task PopulateContactMetadataAsync(ContactCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!HasConfiguredDatabase())
        {
            model.SiteSetting = null;
            model.Branches = [];
            return;
        }

        model.SiteSetting = await TryLoadAsync(
            () => _siteSettingService.GetSettingsAsync(cancellationToken),
            "site settings");

        var branches = await TryLoadAsync(
            () => _branchService.GetActiveBranchesAsync(cancellationToken),
            "active branches") ?? [];

        model.Branches = branches.Select(MapBranch).ToList();
    }

    private async Task<T?> TryLoadAsync<T>(Func<Task<T>> loader, string dataName)
    {
        try
        {
            return await loader();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load {DataName} for contact page.", dataName);
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

    private static ContactBranchDisplayViewModel MapBranch(Branch branch)
    {
        return new ContactBranchDisplayViewModel
        {
            Name = branch.Name,
            Address = $"{branch.Address}, {branch.District}, {branch.City}",
            Hotline = branch.Hotline,
            OpeningHours = $"{branch.OpeningTime:HH\\:mm} - {branch.ClosingTime:HH\\:mm}"
        };
    }
}
