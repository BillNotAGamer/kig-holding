using KIGHolding.Options;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace KIGHolding.Controllers;

[Route("lien-he-nhuong-quyen")]
public class FranchiseController : Controller
{
    private const string FranchiseContactSuccessKey = "FranchiseContactSuccess";
    private const string DefaultBusinessRecipientEmail = "truyenthuyetchamponghcm@gmail.com";

    private readonly IContactService _contactService;
    private readonly ISiteSettingService _siteSettingService;
    private readonly IEmailService _emailService;
    private readonly ResendSettings _resendSettings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FranchiseController> _logger;

    public FranchiseController(
        IContactService contactService,
        ISiteSettingService siteSettingService,
        IEmailService emailService,
        IOptions<ResendSettings> resendSettings,
        IConfiguration configuration,
        ILogger<FranchiseController> logger)
    {
        _contactService = contactService;
        _siteSettingService = siteSettingService;
        _emailService = emailService;
        _resendSettings = resendSettings.Value;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    public IActionResult Index()
    {
        SetPageMetadata();

        var model = new ContactCreateViewModel
        {
            Subject = ContactSubjectOptions.Partnership,
            IsSuccess = TryConsumeFranchiseContactSuccessFlag()
        };

        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ContactCreateViewModel model, CancellationToken cancellationToken)
    {
        SetPageMetadata();

        model.Subject = ContactSubjectOptions.Partnership;
        ModelState.Remove(nameof(ContactCreateViewModel.Subject));

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
                Subject = ContactSubjectOptions.Partnership,
                Message = model.Message
            }, cancellationToken);

            if (result.Succeeded)
            {
                await TrySendContactNotificationAsync(result.ContactMessageId!.Value, model);
                TempData[FranchiseContactSuccessKey] = true;
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.FieldName, error.Message);
            }
        }

        model.Subject = ContactSubjectOptions.Partnership;
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
                Subject = ContactSubjectOptions.Partnership,
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
            _logger.LogWarning(exception, "Unable to complete franchise contact notification workflow for {ContactMessageId}.", contactMessageId);
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
            _logger.LogWarning(exception, "Unable to load business email from site settings for franchise contact notifications.");
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

    private bool TryConsumeFranchiseContactSuccessFlag()
    {
        if (!TempData.TryGetValue(FranchiseContactSuccessKey, out var rawValue) || rawValue is null)
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

    private void SetPageMetadata()
    {
        ViewData["Title"] = "Liên hệ nhượng quyền";
        ViewData["MetaDescription"] = "Trao đổi cơ hội hợp tác phát triển cùng KIG Holding VN: chủ mặt bằng, nhà đầu tư và doanh nghiệp đồng hành cùng hệ thống nhà hàng Hàn Quốc được vận hành bài bản.";
    }
}
