using KIGHolding.Models.Entities;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Controllers;

[Route("dat-ban")]
public class ReservationController : Controller
{
    private readonly IReservationService _reservationService;
    private readonly IBranchService _branchService;
    private readonly ISiteSettingService _siteSettingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ReservationController> _logger;

    public ReservationController(
        IReservationService reservationService,
        IBranchService branchService,
        ISiteSettingService siteSettingService,
        IConfiguration configuration,
        ILogger<ReservationController> logger)
    {
        _reservationService = reservationService;
        _branchService = branchService;
        _siteSettingService = siteSettingService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        [FromQuery] string? branch,
        [FromQuery] Guid? branchId,
        [FromQuery] string? customerName,
        [FromQuery] string? phoneNumber,
        [FromQuery] string? email,
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
            Email = email,
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
        if (model.ReservationDate.HasValue && model.ReservationDate.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            ModelState.AddModelError(nameof(model.ReservationDate), "Ngày đến không được sớm hơn hôm nay.");
        }

        if (model.BranchId == Guid.Empty)
        {
            ModelState.AddModelError(nameof(model.BranchId), "Vui lòng chọn chi nhánh.");
        }

        if (!HasConfiguredDatabase())
        {
            ModelState.AddModelError(string.Empty, "Hệ thống đặt bàn chưa kết nối cơ sở dữ liệu. Vui lòng gọi hotline để được hỗ trợ.");
        }

        if (ModelState.IsValid)
        {
            ReservationCreateResult result;
            try
            {
                result = await _reservationService.CreateReservationAsync(new ReservationCreateRequest
                {
                    CustomerName = model.CustomerName,
                    PhoneNumber = model.PhoneNumber,
                    Email = model.Email,
                    BranchId = model.BranchId!.Value,
                    GuestCount = model.GuestCount,
                    ReservationDate = model.ReservationDate!.Value,
                    ReservationTime = model.ReservationTime!.Value,
                    Note = model.Note
                }, cancellationToken);
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
                return RedirectToAction(nameof(Success), new { id = result.ReservationId!.Value });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.FieldName, error.Message);
            }
        }

        await PopulateFormMetadataAsync(model, cancellationToken);
        return View(model);
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

    private async Task PopulateFormMetadataAsync(ReservationCreateViewModel model, CancellationToken cancellationToken)
    {
        var branches = await LoadBranchesAsync(cancellationToken);
        model.Branches = MapBranches(branches);
        model.Hotline = await LoadHotlineAsync(cancellationToken);
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
            return "0909 888 777";
        }

        try
        {
            var settings = await _siteSettingService.GetSettingsAsync(cancellationToken);
            return string.IsNullOrWhiteSpace(settings?.Hotline) ? "0909 888 777" : settings.Hotline;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to load site hotline for reservation form.");
            return "0909 888 777";
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
            OpeningHours = $"{branch.OpeningTime:HH\\:mm} - {branch.ClosingTime:HH\\:mm}"
        }).ToList();
    }
}
