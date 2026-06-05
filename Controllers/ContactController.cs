using KIGHolding.Models.Entities;
using KIGHolding.Options;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KIGHolding.Controllers;

[Route("lien-he")]
public class ContactController : Controller
{
    private const string ContactSuccessKey = "ContactSuccess";
    private const string DefaultBusinessRecipientEmail = "truyenthuyetchamponghcm@gmail.com";

    private readonly IContactService _contactService;
    private readonly ISiteSettingService _siteSettingService;
    private readonly IBranchService _branchService;
    private readonly IEmailService _emailService;
    private readonly ResendSettings _resendSettings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContactController> _logger;

    public ContactController(
        IContactService contactService,
        ISiteSettingService siteSettingService,
        IBranchService branchService,
        IEmailService emailService,
        IOptions<ResendSettings> resendSettings,
        IConfiguration configuration,
        ILogger<ContactController> logger)
    {
        _contactService = contactService;
        _siteSettingService = siteSettingService;
        _branchService = branchService;
        _emailService = emailService;
        _resendSettings = resendSettings.Value;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken = default)
    {
        var model = new ContactCreateViewModel
        {
            IsSuccess = TryConsumeContactSuccessFlag()
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
                await TrySendContactNotificationAsync(result.ContactMessageId!.Value, model);
                TempData[ContactSuccessKey] = true;
                return RedirectToAction(nameof(Index));
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

    private async Task TrySendContactNotificationAsync(Guid contactMessageId, ContactCreateViewModel model)
    {
        try
        {
            var recipientEmail = await ResolveBusinessRecipientEmailAsync();
            var notification = new ContactNotificationEmailModel
            {
                Id = contactMessageId,
                FullName = model.FullName.Trim(),
                PhoneNumber = model.PhoneNumber.Trim(),
                Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
                Subject = string.IsNullOrWhiteSpace(model.Subject) ? null : model.Subject.Trim(),
                Message = model.Message.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _emailService.SendEmailAsync(
                recipientEmail,
                ContactNotificationEmailBuilder.BuildSubject(),
                ContactNotificationEmailBuilder.BuildHtmlBody(notification),
                ContactNotificationEmailBuilder.BuildTextBody(notification),
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to complete contact notification workflow for {ContactMessageId}.", contactMessageId);
        }
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

    private async Task<string> ResolveBusinessRecipientEmailAsync()
    {
        if (!string.IsNullOrWhiteSpace(_resendSettings.BusinessRecipientEmail))
        {
            return _resendSettings.BusinessRecipientEmail.Trim();
        }

        try
        {
            var settings = await _siteSettingService.GetSettingsAsync(CancellationToken.None);
            if (!string.IsNullOrWhiteSpace(settings?.Email))
            {
                return settings.Email.Trim();
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load business email from site settings for contact notifications.");
        }

        return DefaultBusinessRecipientEmail;
    }

    private bool HasConfiguredDatabase()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        return !string.IsNullOrWhiteSpace(connectionString)
            && !connectionString.Contains("your-neon-host", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("your_username", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("your_password", StringComparison.OrdinalIgnoreCase);
    }

    private bool TryConsumeContactSuccessFlag()
    {
        if (!TempData.TryGetValue(ContactSuccessKey, out var rawValue) || rawValue is null)
        {
            return false;
        }

        return rawValue switch
        {
            bool boolValue => boolValue,
            string stringValue when bool.TryParse(stringValue, out var parsed) => parsed,
            _ => false
        };
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
