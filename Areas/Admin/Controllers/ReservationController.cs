using System.Text.Json;
using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Enums;
using KIGHolding.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Areas.Admin.Controllers;

public class ReservationController : AdminBaseController
{
    private const int PageSize = 10;
    private const string BranchPolicyConflictMessage =
        "Quy định đặt bàn đã được quản trị viên khác thay đổi. Vui lòng tải lại dữ liệu trước khi tiếp tục.";
    private const string BranchPolicyPersistenceMessage =
        "Không thể cập nhật quy định đặt bàn lúc này. Vui lòng tải lại trang và thử lại.";

    private readonly AppDbContext _dbContext;
    private readonly IReservationBlockedDateService _blockedDateService;
    private readonly IBranchReservationDatePolicyService _branchPolicyService;
    private readonly IDataProtector _impactConfirmationProtector;
    private readonly ILogger<ReservationController> _logger;
    private readonly TimeProvider _timeProvider;

    public ReservationController(
        AppDbContext dbContext,
        IReservationBlockedDateService blockedDateService,
        IBranchReservationDatePolicyService branchPolicyService,
        IDataProtectionProvider dataProtectionProvider,
        ILogger<ReservationController> logger,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _blockedDateService = blockedDateService;
        _branchPolicyService = branchPolicyService;
        _impactConfirmationProtector = dataProtectionProvider.CreateProtector(
            "KIGHolding.Admin.BranchReservationPolicy.ImpactConfirmation.v1");
        _logger = logger;
        _timeProvider = timeProvider;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ReservationIndexViewModel filter)
    {
        var requestedPage = Math.Max(filter.Page, 1);
        var query = _dbContext.Reservations.AsNoTracking();
        var search = string.IsNullOrWhiteSpace(filter.SearchQuery) ? null : filter.SearchQuery.Trim();

        if (filter.StatusFilter.HasValue)
        {
            query = query.Where(x => x.Status == filter.StatusFilter.Value);
        }

        if (filter.BranchFilter.HasValue)
        {
            query = query.Where(x => x.BranchId == filter.BranchFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedPhoneSearch = search.Replace(" ", string.Empty, StringComparison.Ordinal);

            query = query.Where(x =>
                EF.Functions.ILike(x.CustomerName, $"%{search}%") ||
                EF.Functions.ILike(x.PhoneNumber, $"%{search}%") ||
                EF.Functions.ILike(x.PhoneNumber.Replace(" ", string.Empty), $"%{normalizedPhoneSearch}%"));
        }

        var totalItems = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));
        var currentPage = Math.Min(requestedPage, totalPages);

        filter.Reservations = await query
            .OrderByDescending(x => x.ReservationDate)
            .ThenByDescending(x => x.ReservationTime)
            .ThenByDescending(x => x.Id)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .Select(x => new ReservationListItemViewModel
            {
                Id = x.Id,
                CustomerName = x.CustomerName,
                PhoneNumber = x.PhoneNumber,
                BranchName = x.Branch.Name,
                ReservationDate = x.ReservationDate,
                ReservationTime = x.ReservationTime,
                GuestCount = x.GuestCount,
                DiningOccasionCodes = x.DiningOccasionCodes,
                Status = x.Status,
                StatusLabel = GetStatusLabel(x.Status)
            })
            .ToListAsync();

        foreach (var reservation in filter.Reservations)
        {
            reservation.DiningOccasionDisplay =
                ReservationOptionCatalog.FormatDiningOccasionCodesForDisplay(reservation.DiningOccasionCodes);
        }

        filter.Page = currentPage;
        filter.PageSize = PageSize;
        filter.TotalItems = totalItems;
        filter.TotalPages = totalPages;
        filter.SearchQuery = search;
        filter.StatusOptions = BuildFilterStatusOptions();
        filter.BranchOptions = await BuildBranchOptionsAsync();

        return View(filter);
    }

    [HttpGet]
    public async Task<IActionResult> Policy(
        int? year = null,
        int? month = null,
        CancellationToken cancellationToken = default)
    {
        var vietnamToday = VietnamClock.GetVietnamToday(_timeProvider);
        await _blockedDateService.CleanupPastDatesAsync(vietnamToday, cancellationToken);

        var model = await BuildPolicyViewModelAsync(vietnamToday, year, month, cancellationToken);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Policy(ReservationPolicyViewModel model, CancellationToken cancellationToken)
    {
        var vietnamToday = VietnamClock.GetVietnamToday(_timeProvider);

        if (!model.PolicyPayloadPresent)
        {
            ModelState.Remove(nameof(model.PolicyPayloadPresent));
            ModelState.AddModelError(nameof(model.PolicyPayloadPresent), "Du lieu chinh sach khong day du. Vui long tai lai trang va thu lai.");
            model = await BuildPolicyViewModelAsync(
                vietnamToday,
                model.CalendarYear,
                model.CalendarMonth,
                cancellationToken);
            return View(model);
        }

        var submittedDates = ParseSubmittedBlockedDates(model.BlockedDatesInput, model, vietnamToday);

        if (!ModelState.IsValid)
        {
            model = await BuildPolicyViewModelAsync(
                vietnamToday,
                model.CalendarYear,
                model.CalendarMonth,
                cancellationToken,
                submittedDates);
            return View(model);
        }

        if (submittedDates.Count == 0 && !model.ConfirmClearAllBlockedDates)
        {
            var activeBlockedDates = await _blockedDateService.GetActiveBlockedDatesAsync(vietnamToday, cancellationToken);
            if (activeBlockedDates.Count > 0)
            {
                ModelState.AddModelError(
                    nameof(model.ConfirmClearAllBlockedDates),
                    "Vui long xac nhan mo lai tat ca ngay dang bi khoa truoc khi cap nhat.");
                model = await BuildPolicyViewModelAsync(
                    vietnamToday,
                    model.CalendarYear,
                    model.CalendarMonth,
                    cancellationToken,
                    submittedDates);
                return View(model);
            }
        }

        await _blockedDateService.ReplaceActiveBlockedDatesAsync(submittedDates, vietnamToday, cancellationToken);

        TempData["SuccessMessage"] = "Da cap nhat chinh sach dat ban.";
        return RedirectToAction(nameof(Policy), new { year = model.CalendarYear, month = model.CalendarMonth });
    }

    [HttpGet]
    public async Task<IActionResult> BranchPolicy(
        Guid? branchId = null,
        int? year = null,
        int? month = null,
        DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var model = await BuildBranchPolicyViewModelAsync(
                branchId,
                year,
                month,
                date,
                cancellationToken);
            return View(model);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Unable to load the Admin branch reservation policy calendar.");
            var vietnamToday = VietnamClock.GetVietnamToday(_timeProvider);
            return View(new BranchReservationPolicyViewModel
            {
                VietnamToday = vietnamToday,
                CalendarYear = vietnamToday.Year,
                CalendarMonth = vietnamToday.Month,
                PageMessage = BranchPolicyPersistenceMessage
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BranchPolicy(
        [Bind(Prefix = nameof(BranchReservationPolicyViewModel.Editor))]
        BranchReservationPolicyEditViewModel editor,
        CancellationToken cancellationToken)
    {
        var vietnamToday = VietnamClock.GetVietnamToday(_timeProvider);

        try
        {
            await ValidateBranchPolicyEditorAsync(editor, vietnamToday, cancellationToken);
            if (!ModelState.IsValid)
            {
                return View(await BuildBranchPolicyViewModelAsync(
                    editor.BranchId == Guid.Empty ? null : editor.BranchId,
                    editor.CalendarYear,
                    editor.CalendarMonth,
                    editor.Date == default ? null : editor.Date,
                    cancellationToken,
                    editor));
            }

            IReadOnlyList<ReservationStatusCount> impactCounts = [];
            if (editor.RuleType is BranchPolicyAdminRuleType.Closed or BranchPolicyAdminRuleType.MinimumGuests)
            {
                impactCounts = await _branchPolicyService.GetExistingReservationCountsAsync(
                    editor.BranchId,
                    editor.Date,
                    cancellationToken);
            }

            if (impactCounts.Sum(item => item.Count) > 0 &&
                (!editor.ConfirmExistingReservations ||
                 !IsValidImpactConfirmation(editor, impactCounts)))
            {
                var attemptedConfirmation = editor.ConfirmExistingReservations ||
                    !string.IsNullOrWhiteSpace(editor.ImpactConfirmationToken);
                var confirmationKey =
                    $"{nameof(BranchReservationPolicyViewModel.Editor)}.{nameof(editor.ConfirmExistingReservations)}";
                ModelState.Remove(confirmationKey);
                ModelState.Remove(
                    $"{nameof(BranchReservationPolicyViewModel.Editor)}.{nameof(editor.ImpactConfirmationToken)}");
                ModelState.AddModelError(
                    confirmationKey,
                    attemptedConfirmation
                        ? "Số lượt đặt bàn hiện tại đã thay đổi hoặc xác nhận không còn hợp lệ. Vui lòng kiểm tra và xác nhận lại."
                        : "Vui lòng xác nhận sau khi kiểm tra các lượt đặt bàn hiện có.");

                editor.ConfirmExistingReservations = false;
                editor.ImpactConfirmationToken = CreateImpactConfirmation(editor, impactCounts);
                var confirmationModel = await BuildBranchPolicyViewModelAsync(
                    editor.BranchId,
                    editor.CalendarYear,
                    editor.CalendarMonth,
                    editor.Date,
                    cancellationToken,
                    editor);
                confirmationModel.RequiresImpactConfirmation = true;
                confirmationModel.ExistingReservationCounts = BuildReservationStatusCounts(impactCounts);
                return View(confirmationModel);
            }

            var mutationResult = await ApplyBranchPolicyMutationAsync(editor, cancellationToken);
            if (mutationResult.Succeeded)
            {
                TempData["SuccessMessage"] = "Đã cập nhật quy định đặt bàn theo chi nhánh.";
                return RedirectToAction(nameof(BranchPolicy), new
                {
                    branchId = editor.BranchId,
                    year = editor.CalendarYear,
                    month = editor.CalendarMonth,
                    date = editor.Date.ToString("yyyy-MM-dd")
                });
            }

            if (mutationResult.Status is BranchReservationPolicyMutationStatus.Conflict or
                BranchReservationPolicyMutationStatus.PolicyNotFound)
            {
                ModelState.Clear();
                ModelState.AddModelError(string.Empty, BranchPolicyConflictMessage);
                var conflictModel = await BuildBranchPolicyViewModelAsync(
                    editor.BranchId,
                    editor.CalendarYear,
                    editor.CalendarMonth,
                    editor.Date,
                    cancellationToken);
                conflictModel.HasConcurrencyConflict = true;
                return View(conflictModel);
            }

            var resultMessage = mutationResult.Status switch
            {
                BranchReservationPolicyMutationStatus.BranchNotFound => "Không tìm thấy chi nhánh.",
                BranchReservationPolicyMutationStatus.BranchNotReservable =>
                    "Chi nhánh không hoạt động hoặc hiện không nhận đặt bàn.",
                BranchReservationPolicyMutationStatus.ValidationFailed =>
                    "Quy định đặt bàn không hợp lệ. Vui lòng kiểm tra lại.",
                _ => BranchPolicyPersistenceMessage
            };
            ModelState.AddModelError(string.Empty, resultMessage);
            return View(await BuildBranchPolicyViewModelAsync(
                editor.BranchId,
                editor.CalendarYear,
                editor.CalendarMonth,
                editor.Date,
                cancellationToken,
                editor));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unexpected Admin branch reservation policy failure for branch {BranchId} and date {PolicyDate}.",
                editor.BranchId,
                editor.Date);
            ModelState.AddModelError(string.Empty, BranchPolicyPersistenceMessage);
            return View(await BuildBranchPolicyFailureViewModelAsync(editor, cancellationToken));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        int page = 1,
        string? searchQuery = null,
        ReservationStatus? statusFilter = null,
        Guid? branchFilter = null)
    {
        var (normalizedPage, normalizedSearchQuery) = NormalizeReservationListState(page, searchQuery);
        SetReservationListState(normalizedPage, normalizedSearchQuery, statusFilter, branchFilter);

        var model = await BuildReservationDetailViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(
        ReservationDetailViewModel model,
        int page = 1,
        string? searchQuery = null,
        ReservationStatus? statusFilter = null,
        Guid? branchFilter = null)
    {
        var (normalizedPage, normalizedSearchQuery) = NormalizeReservationListState(page, searchQuery);
        var reservation = await _dbContext.Reservations.FirstOrDefaultAsync(x => x.Id == model.Id);
        if (reservation is null)
        {
            return NotFound();
        }

        if (!model.Status.HasValue)
        {
            ModelState.AddModelError(nameof(model.Status), "Vui lòng chọn tình trạng xử lý.");
        }
        else if (model.Status.Value == ReservationStatus.Pending)
        {
            ModelState.AddModelError(nameof(model.Status), "Không thể chuyển yêu cầu đã xử lý về trạng thái đang chờ duyệt.");
        }
        else if (model.Status.Value != reservation.Status && !CanTransition(reservation.Status, model.Status.Value))
        {
            var message = IsBackwardTransition(reservation.Status, model.Status.Value)
                ? "Không thể chuyển về tình trạng xử lý trước đó."
                : "Không thể cập nhật tình trạng này cho yêu cầu đặt bàn.";

            ModelState.AddModelError(nameof(model.Status), message);
        }

        if (!ModelState.IsValid)
        {
            var detailModel = await BuildReservationDetailViewModelAsync(reservation.Id, model.Status, model.Note);
            if (detailModel is null)
            {
                return NotFound();
            }

            SetReservationListState(normalizedPage, normalizedSearchQuery, statusFilter, branchFilter);
            return View("Details", detailModel);
        }

        if (model.Status.HasValue && model.Status.Value != reservation.Status)
        {
            reservation.Status = model.Status.Value;
        }

        reservation.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();
        reservation.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Cập nhật trạng thái đặt bàn thành công.";
        return RedirectToAction(
            nameof(Index),
            "Reservation",
            new
            {
                area = "Admin",
                page = normalizedPage,
                SearchQuery = normalizedSearchQuery,
                StatusFilter = statusFilter,
                BranchFilter = branchFilter
            });
    }

    private async Task<ReservationDetailViewModel?> BuildReservationDetailViewModelAsync(
        Guid id,
        ReservationStatus? submittedStatus = null,
        string? noteOverride = null)
    {
        var model = await _dbContext.Reservations
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ReservationDetailViewModel
            {
                Id = x.Id,
                CustomerName = x.CustomerName,
                PhoneNumber = x.PhoneNumber,
                BranchName = x.Branch.Name,
                BranchAddress = x.Branch.Address,
                ReservationDate = x.ReservationDate,
                ReservationTime = x.ReservationTime,
                GuestCount = x.GuestCount,
                DiningOccasionCodes = x.DiningOccasionCodes,
                DiningOccasionOtherNote = x.DiningOccasionOtherNote,
                Note = x.Note,
                CurrentStatus = x.Status,
                StatusLabel = GetStatusLabel(x.Status),
                Status = GetDefaultSelectedStatus(x.Status),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (model is null)
        {
            return null;
        }

        if (noteOverride is not null)
        {
            model.Note = noteOverride;
        }

        if (submittedStatus.HasValue && IsSelectableStatus(model.CurrentStatus, submittedStatus.Value))
        {
            model.Status = submittedStatus.Value;
        }

        model.DiningOccasionDisplay = ReservationOptionCatalog.FormatDiningOccasionCodesForDisplay(model.DiningOccasionCodes);
        model.StatusOptions = BuildAllowedStatusOptions(model.CurrentStatus);
        return model;
    }

    private async Task<IReadOnlyList<SelectListItem>> BuildBranchOptionsAsync()
    {
        var branchItems = await _dbContext.Branches
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = $"{x.Name} - {x.City}"
            })
            .ToListAsync();

        var options = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = "Tất cả chi nhánh" }
        };

        options.AddRange(branchItems);
        return options;
    }

    private async Task<BranchReservationPolicyViewModel> BuildBranchPolicyViewModelAsync(
        Guid? requestedBranchId,
        int? requestedYear,
        int? requestedMonth,
        DateOnly? requestedDate,
        CancellationToken cancellationToken,
        BranchReservationPolicyEditViewModel? editorOverride = null)
    {
        var vietnamToday = VietnamClock.GetVietnamToday(_timeProvider);
        var (year, month) = NormalizeCalendarMonth(requestedYear, requestedMonth, vietnamToday);
        var firstOfMonth = new DateOnly(year, month, 1);
        var lastOfMonth = firstOfMonth.AddMonths(1).AddDays(-1);
        var branches = await _dbContext.Branches
            .AsNoTracking()
            .Where(branch => branch.IsActive && branch.AllowsReservations)
            .OrderBy(branch => branch.DisplayOrder)
            .ThenBy(branch => branch.Name)
            .Select(branch => new { branch.Id, branch.Name, branch.City })
            .ToListAsync(cancellationToken);

        var selectedBranch = requestedBranchId.HasValue
            ? branches.FirstOrDefault(branch => branch.Id == requestedBranchId.Value)
            : branches.FirstOrDefault();
        if (requestedBranchId.HasValue && selectedBranch is null)
        {
            ModelState.AddModelError(
                $"{nameof(BranchReservationPolicyViewModel.Editor)}.{nameof(BranchReservationPolicyEditViewModel.BranchId)}",
                "Chi nhánh không hợp lệ, không hoạt động hoặc hiện không nhận đặt bàn.");
        }

        IReadOnlyList<BranchReservationPolicyCalendarItemViewModel> policies = [];
        if (selectedBranch is not null)
        {
            policies = (await _branchPolicyService.GetPoliciesAsync(
                    selectedBranch.Id,
                    firstOfMonth,
                    lastOfMonth,
                    cancellationToken))
                .Select(policy => new BranchReservationPolicyCalendarItemViewModel
                {
                    Date = policy.Date,
                    RuleType = policy.RuleType,
                    MinGuests = policy.MinGuests,
                    Version = policy.Version
                })
                .ToArray();
        }

        var globalBlockedDates = (await _blockedDateService.GetActiveBlockedDatesAsync(
                vietnamToday,
                cancellationToken))
            .Where(date => date >= firstOfMonth && date <= lastOfMonth)
            .ToHashSet();

        DateOnly? selectedDate = requestedDate;
        if (!selectedDate.HasValue ||
            selectedDate.Value < firstOfMonth ||
            selectedDate.Value > lastOfMonth ||
            selectedDate.Value < vietnamToday)
        {
            selectedDate = lastOfMonth < vietnamToday
                ? null
                : firstOfMonth < vietnamToday
                    ? vietnamToday
                    : firstOfMonth;
        }

        var currentPolicy = selectedDate.HasValue
            ? policies.FirstOrDefault(policy => policy.Date == selectedDate.Value)
            : null;
        var editor = editorOverride ?? new BranchReservationPolicyEditViewModel
        {
            BranchId = selectedBranch?.Id ?? Guid.Empty,
            Date = selectedDate ?? default,
            RuleType = currentPolicy?.RuleType switch
            {
                BranchReservationRuleType.Closed => BranchPolicyAdminRuleType.Closed,
                BranchReservationRuleType.MinimumGuests => BranchPolicyAdminRuleType.MinimumGuests,
                _ => BranchPolicyAdminRuleType.Allowed
            },
            MinGuests = currentPolicy?.MinGuests,
            ExpectedVersion = currentPolicy?.Version,
            CalendarYear = year,
            CalendarMonth = month
        };
        editor.CalendarYear = year;
        editor.CalendarMonth = month;

        return new BranchReservationPolicyViewModel
        {
            VietnamToday = vietnamToday,
            CalendarYear = year,
            CalendarMonth = month,
            SelectedBranchId = selectedBranch?.Id,
            SelectedBranchName = selectedBranch?.Name,
            BranchOptions = branches.Select(branch => new SelectListItem
            {
                Value = branch.Id.ToString(),
                Text = string.IsNullOrWhiteSpace(branch.City) ? branch.Name : $"{branch.Name} - {branch.City}",
                Selected = selectedBranch?.Id == branch.Id
            }).ToArray(),
            Policies = policies,
            GlobalBlockedDates = globalBlockedDates,
            Editor = editor
        };
    }

    private async Task<BranchReservationPolicyViewModel> BuildBranchPolicyFailureViewModelAsync(
        BranchReservationPolicyEditViewModel editor,
        CancellationToken cancellationToken)
    {
        try
        {
            return await BuildBranchPolicyViewModelAsync(
                editor.BranchId == Guid.Empty ? null : editor.BranchId,
                editor.CalendarYear,
                editor.CalendarMonth,
                editor.Date == default ? null : editor.Date,
                cancellationToken,
                editor);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(
                exception,
                "Unable to reload the Admin branch reservation policy calendar after a failed mutation.");
            var vietnamToday = VietnamClock.GetVietnamToday(_timeProvider);
            var (year, month) = NormalizeCalendarMonth(
                editor.CalendarYear,
                editor.CalendarMonth,
                vietnamToday);
            editor.CalendarYear = year;
            editor.CalendarMonth = month;
            return new BranchReservationPolicyViewModel
            {
                VietnamToday = vietnamToday,
                CalendarYear = year,
                CalendarMonth = month,
                Editor = editor,
                PageMessage = BranchPolicyPersistenceMessage
            };
        }
    }

    private async Task ValidateBranchPolicyEditorAsync(
        BranchReservationPolicyEditViewModel editor,
        DateOnly vietnamToday,
        CancellationToken cancellationToken)
    {
        if (editor.BranchId == Guid.Empty)
        {
            ModelState.AddModelError(
                $"{nameof(BranchReservationPolicyViewModel.Editor)}.{nameof(editor.BranchId)}",
                "Vui lòng chọn chi nhánh.");
        }
        else if (!await _dbContext.Branches.AsNoTracking().AnyAsync(
                     branch => branch.Id == editor.BranchId && branch.IsActive && branch.AllowsReservations,
                     cancellationToken))
        {
            ModelState.AddModelError(
                $"{nameof(BranchReservationPolicyViewModel.Editor)}.{nameof(editor.BranchId)}",
                "Chi nhánh không hợp lệ, không hoạt động hoặc hiện không nhận đặt bàn.");
        }

        if (editor.Date == default)
        {
            ModelState.AddModelError(
                $"{nameof(BranchReservationPolicyViewModel.Editor)}.{nameof(editor.Date)}",
                "Ngày áp dụng không hợp lệ.");
        }
        else if (editor.Date < vietnamToday)
        {
            ModelState.AddModelError(
                $"{nameof(BranchReservationPolicyViewModel.Editor)}.{nameof(editor.Date)}",
                "Không thể thay đổi quy định của ngày đã qua.");
        }

        if (!Enum.IsDefined(editor.RuleType))
        {
            ModelState.AddModelError(
                $"{nameof(BranchReservationPolicyViewModel.Editor)}.{nameof(editor.RuleType)}",
                "Loại quy định không hợp lệ.");
        }
        else if (editor.RuleType == BranchPolicyAdminRuleType.MinimumGuests &&
                 editor.MinGuests is null or < 1 or > 100)
        {
            ModelState.AddModelError(
                $"{nameof(BranchReservationPolicyViewModel.Editor)}.{nameof(editor.MinGuests)}",
                "Số khách tối thiểu phải từ 1 đến 100.");
        }
        else if (editor.RuleType != BranchPolicyAdminRuleType.MinimumGuests && editor.MinGuests is not null)
        {
            ModelState.AddModelError(
                $"{nameof(BranchReservationPolicyViewModel.Editor)}.{nameof(editor.MinGuests)}",
                "Chỉ quy định số khách tối thiểu mới được nhập giá trị này.");
        }

        if (editor.ExpectedVersion == Guid.Empty)
        {
            ModelState.AddModelError(
                $"{nameof(BranchReservationPolicyViewModel.Editor)}.{nameof(editor.ExpectedVersion)}",
                "Phiên bản quy định không hợp lệ.");
        }

        if (editor.CalendarMonth is < 1 or > 12 || editor.CalendarYear is < 1 or > 9999)
        {
            ModelState.AddModelError(string.Empty, "Tháng hiển thị không hợp lệ.");
        }
    }

    private async Task<BranchReservationPolicyMutationResult> ApplyBranchPolicyMutationAsync(
        BranchReservationPolicyEditViewModel editor,
        CancellationToken cancellationToken)
    {
        if (editor.RuleType == BranchPolicyAdminRuleType.Allowed)
        {
            if (!editor.ExpectedVersion.HasValue)
            {
                var current = await _branchPolicyService.GetPolicyAsync(
                    editor.BranchId,
                    editor.Date,
                    cancellationToken);
                return current is null
                    ? new BranchReservationPolicyMutationResult(BranchReservationPolicyMutationStatus.Succeeded)
                    : new BranchReservationPolicyMutationResult(BranchReservationPolicyMutationStatus.Conflict);
            }

            return await _branchPolicyService.RemovePolicyAsync(
                editor.BranchId,
                editor.Date,
                editor.ExpectedVersion.Value,
                cancellationToken);
        }

        var ruleType = editor.RuleType == BranchPolicyAdminRuleType.Closed
            ? BranchReservationRuleType.Closed
            : BranchReservationRuleType.MinimumGuests;
        return await _branchPolicyService.SetPolicyAsync(
            new BranchReservationPolicyMutation(
                editor.BranchId,
                editor.Date,
                ruleType,
                editor.MinGuests,
                editor.ExpectedVersion),
            cancellationToken);
    }

    private string CreateImpactConfirmation(
        BranchReservationPolicyEditViewModel editor,
        IReadOnlyList<ReservationStatusCount> counts)
    {
        var payload = new BranchPolicyImpactConfirmationPayload(
            editor.BranchId,
            editor.Date,
            editor.RuleType,
            editor.MinGuests,
            editor.ExpectedVersion,
            BuildImpactFingerprint(counts));
        return _impactConfirmationProtector.Protect(JsonSerializer.Serialize(payload));
    }

    private bool IsValidImpactConfirmation(
        BranchReservationPolicyEditViewModel editor,
        IReadOnlyList<ReservationStatusCount> counts)
    {
        if (string.IsNullOrWhiteSpace(editor.ImpactConfirmationToken))
        {
            return false;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<BranchPolicyImpactConfirmationPayload>(
                _impactConfirmationProtector.Unprotect(editor.ImpactConfirmationToken));
            return payload is not null &&
                   payload.BranchId == editor.BranchId &&
                   payload.Date == editor.Date &&
                   payload.RuleType == editor.RuleType &&
                   payload.MinGuests == editor.MinGuests &&
                   payload.ExpectedVersion == editor.ExpectedVersion &&
                   string.Equals(payload.ImpactFingerprint, BuildImpactFingerprint(counts), StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static string BuildImpactFingerprint(IReadOnlyList<ReservationStatusCount> counts)
    {
        return string.Join(
            ";",
            counts.OrderBy(item => item.Status).Select(item => $"{(int)item.Status}:{item.Count}"));
    }

    private static IReadOnlyList<ReservationStatusCountViewModel> BuildReservationStatusCounts(
        IReadOnlyList<ReservationStatusCount> counts)
    {
        return counts
            .OrderBy(item => item.Status)
            .Select(item => new ReservationStatusCountViewModel
            {
                Status = item.Status,
                Label = GetStatusLabel(item.Status),
                Count = item.Count
            })
            .ToArray();
    }

    private static (int Year, int Month) NormalizeCalendarMonth(
        int? requestedYear,
        int? requestedMonth,
        DateOnly vietnamToday)
    {
        var year = requestedYear.GetValueOrDefault(vietnamToday.Year);
        var month = requestedMonth.GetValueOrDefault(vietnamToday.Month);
        return year is >= 1 and <= 9999 && month is >= 1 and <= 12
            ? (year, month)
            : (vietnamToday.Year, vietnamToday.Month);
    }

    private sealed record BranchPolicyImpactConfirmationPayload(
        Guid BranchId,
        DateOnly Date,
        BranchPolicyAdminRuleType RuleType,
        int? MinGuests,
        Guid? ExpectedVersion,
        string ImpactFingerprint);

    private async Task<ReservationPolicyViewModel> BuildPolicyViewModelAsync(
        DateOnly vietnamToday,
        int? requestedYear,
        int? requestedMonth,
        CancellationToken cancellationToken,
        IReadOnlyList<DateOnly>? submittedDates = null)
    {
        var year = requestedYear.GetValueOrDefault(vietnamToday.Year);
        var month = requestedMonth.GetValueOrDefault(vietnamToday.Month);

        if (month is < 1 or > 12)
        {
            year = vietnamToday.Year;
            month = vietnamToday.Month;
        }

        var blockedDates = submittedDates
            ?? await _blockedDateService.GetActiveBlockedDatesAsync(vietnamToday, cancellationToken);

        return new ReservationPolicyViewModel
        {
            VietnamToday = vietnamToday,
            CalendarYear = year,
            CalendarMonth = month,
            BlockedDates = blockedDates.OrderBy(date => date).ToArray(),
            BlockedDatesInput = string.Join(",", blockedDates
                .OrderBy(date => date)
                .Select(date => date.ToString("yyyy-MM-dd"))),
            PolicyPayloadPresent = true
        };
    }

    private List<DateOnly> ParseSubmittedBlockedDates(
        string? blockedDatesInput,
        ReservationPolicyViewModel model,
        DateOnly vietnamToday)
    {
        var dates = new List<DateOnly>();

        if (string.IsNullOrWhiteSpace(blockedDatesInput))
        {
            return dates;
        }

        foreach (var rawValue in blockedDatesInput.Split(',', StringSplitOptions.None))
        {
            var normalizedValue = rawValue.Trim();
            if (!DateOnly.TryParseExact(normalizedValue, "yyyy-MM-dd", out var date))
            {
                ModelState.AddModelError(nameof(model.BlockedDatesInput), "Danh sach ngay khoa khong hop le.");
                continue;
            }

            if (date >= vietnamToday && !dates.Contains(date))
            {
                dates.Add(date);
            }
        }

        return dates;
    }

    private static IReadOnlyList<SelectListItem> BuildFilterStatusOptions()
    {
        var options = new List<SelectListItem>
        {
            new() { Value = string.Empty, Text = "Tất cả trạng thái" }
        };

        foreach (var status in Enum.GetValues<ReservationStatus>())
        {
            options.Add(CreateStatusOption(status));
        }

        return options;
    }

    private static IReadOnlyList<SelectListItem> BuildAllowedStatusOptions(ReservationStatus currentStatus)
    {
        return currentStatus switch
        {
            ReservationStatus.Pending =>
            [
                new SelectListItem { Value = string.Empty, Text = "Tình trạng" },
                CreateStatusOption(ReservationStatus.Confirmed),
                CreateStatusOption(ReservationStatus.Arrived),
                CreateStatusOption(ReservationStatus.NoShow)
            ],
            ReservationStatus.Confirmed =>
            [
                CreateStatusOption(ReservationStatus.Confirmed),
                CreateStatusOption(ReservationStatus.Arrived),
                CreateStatusOption(ReservationStatus.NoShow)
            ],
            ReservationStatus.Arrived =>
            [
                CreateStatusOption(ReservationStatus.Arrived),
                CreateStatusOption(ReservationStatus.NoShow)
            ],
            ReservationStatus.NoShow =>
            [
                CreateStatusOption(ReservationStatus.NoShow)
            ],
            ReservationStatus.Cancelled =>
            [
                CreateStatusOption(ReservationStatus.Cancelled)
            ],
            _ => []
        };
    }

    private static SelectListItem CreateStatusOption(ReservationStatus status)
    {
        return new SelectListItem
        {
            Value = status.ToString(),
            Text = GetStatusLabel(status)
        };
    }

    private static ReservationStatus? GetDefaultSelectedStatus(ReservationStatus currentStatus)
    {
        return currentStatus == ReservationStatus.Pending ? null : currentStatus;
    }

    private static bool IsSelectableStatus(ReservationStatus currentStatus, ReservationStatus nextStatus)
    {
        return BuildAllowedStatusOptions(currentStatus)
            .Any(option => string.Equals(option.Value, nextStatus.ToString(), StringComparison.Ordinal));
    }

    private static bool CanTransition(ReservationStatus current, ReservationStatus next)
    {
        if (next == ReservationStatus.Pending)
        {
            return false;
        }

        return current switch
        {
            ReservationStatus.Pending => next == ReservationStatus.Confirmed
                || next == ReservationStatus.Arrived
                || next == ReservationStatus.NoShow,
            ReservationStatus.Confirmed => next == ReservationStatus.Arrived
                || next == ReservationStatus.NoShow,
            ReservationStatus.Arrived => next == ReservationStatus.NoShow,
            _ => false
        };
    }

    private static bool IsBackwardTransition(ReservationStatus current, ReservationStatus next)
    {
        return GetWorkflowOrder(next) < GetWorkflowOrder(current);
    }

    private static int GetWorkflowOrder(ReservationStatus status)
    {
        return status switch
        {
            ReservationStatus.Pending => 0,
            ReservationStatus.Confirmed => 1,
            ReservationStatus.Arrived => 2,
            ReservationStatus.NoShow => 3,
            ReservationStatus.Cancelled => 4,
            _ => int.MaxValue
        };
    }

    private static string GetStatusLabel(ReservationStatus status)
    {
        return status switch
        {
            ReservationStatus.Pending => "Đang chờ duyệt",
            ReservationStatus.Confirmed => "Đã xem",
            ReservationStatus.Arrived => "Đã liên hệ",
            ReservationStatus.NoShow => "Đã thông báo",
            ReservationStatus.Cancelled => "Đã hủy",
            _ => status.ToString()
        };
    }

    private void SetReservationListState(
        int page,
        string? searchQuery,
        ReservationStatus? statusFilter,
        Guid? branchFilter)
    {
        ViewData["ReservationListPage"] = page;
        ViewData["ReservationListSearchQuery"] = searchQuery;
        ViewData["ReservationListStatusFilter"] = statusFilter;
        ViewData["ReservationListBranchFilter"] = branchFilter;
    }

    private static (int Page, string? SearchQuery) NormalizeReservationListState(int page, string? searchQuery)
    {
        return (
            Math.Max(page, 1),
            string.IsNullOrWhiteSpace(searchQuery) ? null : searchQuery.Trim());
    }
}
