using System.Globalization;
using KIGHolding.Models.Entities;
using KIGHolding.Options;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace KIGHolding.Controllers;

[Route("dat-ban")]
public class ReservationController : Controller
{
    private const string DefaultBusinessRecipientEmail = "truyenthuyetchamponghcm@gmail.com";

    private readonly IReservationService _reservationService;
    private readonly IBranchService _branchService;
    private readonly ISiteSettingService _siteSettingService;
    private readonly IEmailService _emailService;
    private readonly ResendSettings _resendSettings;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReservationController> _logger;

    public ReservationController(
        IReservationService reservationService,
        IBranchService branchService,
        ISiteSettingService siteSettingService,
        IEmailService emailService,
        IOptions<ResendSettings> resendSettings,
        IConfiguration configuration,
        ILogger<ReservationController> logger)
    {
        _reservationService = reservationService;
        _branchService = branchService;
        _siteSettingService = siteSettingService;
        _emailService = emailService;
        _resendSettings = resendSettings.Value;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] string? branch,
        [FromQuery] Guid? branchId,
        [FromQuery] string? customerName,
        [FromQuery] string? phoneNumber,
        [FromQuery] int? guests,
        [FromQuery] int? guestCount,
        [FromQuery] DateOnly? date,
        [FromQuery] DateOnly? reservationDate,
        [FromQuery] TimeOnly? time,
        [FromQuery] TimeOnly? reservationTime,
        CancellationToken cancellationToken)
    {
        var branches = await LoadBranchesAsync(cancellationToken);
        var selectedBranch = ResolveSelectedBranch(branches, branch, branchId);

        var model = new ReservationCreateViewModel
        {
            CustomerName = customerName ?? string.Empty,
            PhoneNumber = phoneNumber ?? string.Empty,
            BranchId = selectedBranch?.Id,
            GuestCount = guestCount ?? guests ?? 2,
            ReservationDate = reservationDate ?? date ?? DateOnly.FromDateTime(DateTime.Today),
            ReservationTime = reservationTime ?? time ?? new TimeOnly(18, 0),
            SelectedBranchSlug = branch,
            Branches = MapBranches(branches),
            Hotline = await LoadHotlineAsync(cancellationToken)
        };

        return View(model);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ReservationCreateViewModel model, CancellationToken cancellationToken)
    {
        ApplyControllerValidation(model);

        if (ModelState.IsValid)
        {
            ReservationCreateResult result;
            try
            {
                result = await CreateReservationAsync(model, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Unable to save reservation.");
                ModelState.AddModelError(string.Empty, "Không thể lưu thông tin đặt bàn lúc này. Vui lòng thử lại hoặc gọi hotline để được hỗ trợ.");
                await PopulateFormMetadataAsync(model, cancellationToken);
                return View(model);
            }

            if (result.Succeeded)
            {
                await TrySendReservationNotificationAsync(result.ReservationId!.Value, model);
                return RedirectToAction(nameof(Success), new { id = result.ReservationId!.Value });
            }

            AddServiceErrorsToModelState(result);
        }

        await PopulateFormMetadataAsync(model, cancellationToken);
        return View(model);
    }

    [HttpPost("quick")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Quick(ReservationCreateViewModel model, CancellationToken cancellationToken)
    {
        ApplyControllerValidation(model);

        if (!ModelState.IsValid)
        {
            return BadRequest(CreateValidationErrorPayload());
        }

        ReservationCreateResult result;
        try
        {
            result = await CreateReservationAsync(model, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to save quick reservation.");
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                ok = false,
                message = "Không thể gửi yêu cầu đặt bàn lúc này. Vui lòng thử lại sau hoặc gọi hotline để được hỗ trợ."
            });
        }

        if (!result.Succeeded)
        {
            AddServiceErrorsToModelState(result);
            return BadRequest(CreateValidationErrorPayload());
        }

        var reservation = await TryLoadReservationAsync(result.ReservationId!.Value, cancellationToken);
        await TrySendReservationNotificationAsync(result.ReservationId!.Value, model, reservation);

        return Ok(new
        {
            ok = true,
            message = "Cảm ơn bạn. Yêu cầu đặt bàn đã được ghi nhận.",
            reservationId = result.ReservationId,
            summary = new
            {
                customerName = reservation?.CustomerName ?? model.CustomerName.Trim(),
                phoneNumber = reservation?.PhoneNumber ?? model.PhoneNumber.Trim(),
                guestCount = reservation?.GuestCount ?? model.GuestCount,
                reservationDate = (reservation?.ReservationDate ?? model.ReservationDate!.Value).ToString("dd/MM/yyyy"),
                reservationTime = (reservation?.ReservationTime ?? model.ReservationTime!.Value).ToString("HH:mm"),
                branchName = reservation?.Branch?.Name,
                note = string.IsNullOrWhiteSpace(reservation?.Note) ? model.Note?.Trim() : reservation.Note
            }
        });
    }

    [HttpGet("thanh-cong")]
    public async Task<IActionResult> Success([FromQuery] Guid id, CancellationToken cancellationToken)
    {
        if (id == Guid.Empty || !HasConfiguredDatabase())
        {
            return NotFound();
        }

        Reservation? reservation;
        try
        {
            reservation = await _reservationService.GetReservationByIdAsync(id, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load reservation success summary for {ReservationId}.", id);
            return NotFound();
        }

        if (reservation is null)
        {
            return NotFound();
        }

        var hotline = await LoadHotlineAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(hotline))
        {
            hotline = reservation.Branch.Hotline;
        }

        var model = new ReservationSuccessViewModel
        {
            ReservationId = reservation.Id,
            CustomerName = reservation.CustomerName,
            BranchName = reservation.Branch.Name,
            ReservationDate = reservation.ReservationDate,
            ReservationTime = reservation.ReservationTime,
            GuestCount = reservation.GuestCount,
            Hotline = hotline
        };

        return View(model);
    }

    private async Task TrySendReservationNotificationAsync(
        Guid reservationId,
        ReservationCreateViewModel model,
        Reservation? reservation = null)
    {
        try
        {
            reservation ??= await TryLoadReservationAsync(reservationId, CancellationToken.None);

            var recipientEmail = await ResolveBusinessRecipientEmailAsync();
            var notification = BuildReservationNotificationModel(reservationId, model, reservation);

            await _emailService.SendReservationNotificationAsync(
                recipientEmail,
                ReservationNotificationEmailBuilder.BuildSubject(notification),
                ReservationNotificationEmailBuilder.BuildHtmlBody(notification),
                ReservationNotificationEmailBuilder.BuildTextBody(notification),
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to complete reservation notification workflow for {ReservationId}.", reservationId);
        }
    }

    private async Task<string> ResolveBusinessRecipientEmailAsync()
    {
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
            _logger.LogWarning(exception, "Unable to load business email from site settings for reservation notifications.");
        }

        if (!string.IsNullOrWhiteSpace(_resendSettings.BusinessRecipientEmail))
        {
            return _resendSettings.BusinessRecipientEmail.Trim();
        }

        return DefaultBusinessRecipientEmail;
    }

    private ReservationNotificationEmailModel BuildReservationNotificationModel(
        Guid reservationId,
        ReservationCreateViewModel model,
        Reservation? reservation)
    {
        var branch = reservation?.Branch;
        var branchAddress = branch is null
            ? null
            : string.Join(", ", new[] { branch.Address, branch.District, branch.City }.Where(value => !string.IsNullOrWhiteSpace(value)));

        return new ReservationNotificationEmailModel
        {
            ReservationId = reservationId,
            CustomerName = reservation?.CustomerName ?? model.CustomerName.Trim(),
            PhoneNumber = reservation?.PhoneNumber ?? model.PhoneNumber.Trim(),
            GuestCount = reservation?.GuestCount ?? model.GuestCount,
            ReservationDate = reservation?.ReservationDate ?? model.ReservationDate!.Value,
            ReservationTime = reservation?.ReservationTime ?? model.ReservationTime!.Value,
            BranchName = branch?.Name,
            BranchAddress = branchAddress,
            DiningOccasionDisplay = reservation is not null
                ? ReservationOptionCatalog.FormatDiningOccasionCodesForDisplay(reservation.DiningOccasionCodes)
                : ReservationOptionCatalog.FormatDiningOccasionCode(model.DiningOccasionCode),
            DiningOccasionOtherNote = reservation?.DiningOccasionOtherNote
                ?? ResolveConditionalOtherNote(model.DiningOccasionCode, model.DiningOccasionOtherNote),
            Note = string.IsNullOrWhiteSpace(reservation?.Note) ? model.Note?.Trim() : reservation.Note,
            SubmittedAt = reservation?.CreatedAt
        };
    }

    private void ApplyControllerValidation(ReservationCreateViewModel model)
    {
        if (model.ReservationDate.HasValue && model.ReservationDate.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(nameof(model.ReservationDate), "Ngày đến không được sớm hơn hôm nay.");
        }

        if (model.BranchId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.BranchId), "Vui lòng chọn chi nhánh.");
        }

        ValidateSingleSelectPost(nameof(model.DiningOccasionCode), "Lựa chọn hình thức dùng bữa không hợp lệ.");

        if (!HasConfiguredDatabase())
        {
            ModelState.AddModelError(string.Empty, "Hệ thống đặt bàn chưa kết nối cơ sở dữ liệu. Vui lòng gọi hotline để được hỗ trợ.");
        }
    }

    private async Task<ReservationCreateResult> CreateReservationAsync(ReservationCreateViewModel model, CancellationToken cancellationToken)
    {
        return await _reservationService.CreateReservationAsync(new ReservationCreateRequest
        {
            CustomerName = model.CustomerName,
            PhoneNumber = model.PhoneNumber,
            BranchId = model.BranchId!.Value,
            GuestCount = model.GuestCount,
            ReservationDate = model.ReservationDate!.Value,
            ReservationTime = model.ReservationTime!.Value,
            DiningOccasionCode = model.DiningOccasionCode,
            DiningOccasionOtherNote = model.DiningOccasionOtherNote,
            Note = model.Note
        }, cancellationToken);
    }

    private void AddServiceErrorsToModelState(ReservationCreateResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.IsNullOrWhiteSpace(error.FieldName) ? string.Empty : error.FieldName, error.Message);
        }
    }

    private object CreateValidationErrorPayload()
    {
        return new
        {
            ok = false,
            message = "Vui lòng kiểm tra lại thông tin đặt bàn.",
            errors = BuildModelStateErrors(ModelState)
        };
    }

    private static Dictionary<string, string[]> BuildModelStateErrors(ModelStateDictionary modelState)
    {
        return modelState
            .Where(entry => entry.Value is { Errors.Count: > 0 })
            .ToDictionary(
                entry => string.IsNullOrWhiteSpace(entry.Key) ? "_summary" : entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Dữ liệu không hợp lệ." : error.ErrorMessage)
                    .Distinct()
                    .ToArray());
    }

    private async Task PopulateFormMetadataAsync(ReservationCreateViewModel model, CancellationToken cancellationToken)
    {
        var branches = await LoadBranchesAsync(cancellationToken);
        model.Branches = MapBranches(branches);
        model.Hotline = await LoadHotlineAsync(cancellationToken);
    }

    private async Task<Reservation?> TryLoadReservationAsync(Guid reservationId, CancellationToken cancellationToken)
    {
        try
        {
            return await _reservationService.GetReservationByIdAsync(reservationId, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load reservation details for {ReservationId}.", reservationId);
            return null;
        }
    }

    private async Task<IReadOnlyList<Branch>> LoadBranchesAsync(CancellationToken cancellationToken)
    {
        if (!HasConfiguredDatabase())
        {
            return [];
        }

        try
        {
            return await _branchService.GetActiveBranchesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load active branches for reservation form.");
            return [];
        }
    }

    private async Task<string> LoadHotlineAsync(CancellationToken cancellationToken)
    {
        if (!HasConfiguredDatabase())
        {
            return "0922 055 755";
        }

        try
        {
            var settings = await _siteSettingService.GetSettingsAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(settings?.Hotline) ? "0922 055 755" : settings.Hotline;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load site hotline for reservation form.");
            return "0922 055 755";
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

    private static Branch? ResolveSelectedBranch(IReadOnlyList<Branch> branches, string? branchSlug, Guid? branchId)
    {
        if (!string.IsNullOrWhiteSpace(branchSlug))
        {
            var normalizedSlug = branchSlug.Trim().ToLowerInvariant();
            var branchBySlug = branches.FirstOrDefault(x => string.Equals(x.Slug, normalizedSlug, StringComparison.OrdinalIgnoreCase));
            if (branchBySlug is not null)
            {
                return branchBySlug;
            }
        }

        return branchId.HasValue
            ? branches.FirstOrDefault(x => x.Id == branchId.Value)
            : null;
    }

    private static IReadOnlyList<ReservationBranchOptionViewModel> MapBranches(IReadOnlyList<Branch> branches)
    {
        return branches.Select(branch => new ReservationBranchOptionViewModel
        {
            Id = branch.Id,
            Name = branch.Name,
            Slug = branch.Slug,
            Address = $"{branch.Address}, {branch.District}, {branch.City}",
            OpeningHours = FormatTimeRange(branch.OpeningTime, branch.ClosingTime),
            LunchBreakHours = branch.LunchBreakStart.HasValue && branch.LunchBreakEnd.HasValue
                ? FormatTimeRange(branch.LunchBreakStart.Value, branch.LunchBreakEnd.Value)
                : null
        }).ToList();
    }

    private void ValidateSingleSelectPost(string fieldName, string errorMessage)
    {
        if (!Request.HasFormContentType)
        {
            return;
        }

        if (Request.Form[fieldName].Count > 1)
        {
            ModelState.AddModelError(fieldName, errorMessage);
        }
    }

    private static string? ResolveConditionalOtherNote(string? code, string? note)
    {
        if (!string.Equals(
                ReservationOptionCatalog.NormalizeSingleCode(code),
                ReservationOptionCatalog.OtherCode,
                StringComparison.Ordinal))
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    private static string FormatTimeRange(TimeOnly start, TimeOnly end)
    {
        return $"{start.ToString("HH:mm", CultureInfo.InvariantCulture)} - {end.ToString("HH:mm", CultureInfo.InvariantCulture)}";
    }
}
