using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Areas.Admin.Controllers;

public class ReservationController : AdminBaseController
{
    private readonly AppDbContext _dbContext;

    public ReservationController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ReservationIndexViewModel filter)
    {
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

        filter.Reservations = await query
            .OrderByDescending(x => x.ReservationDate)
            .ThenByDescending(x => x.ReservationTime)
            .Select(x => new ReservationListItemViewModel
            {
                Id = x.Id,
                CustomerName = x.CustomerName,
                PhoneNumber = x.PhoneNumber,
                BranchName = x.Branch.Name,
                ReservationDate = x.ReservationDate,
                ReservationTime = x.ReservationTime,
                GuestCount = x.GuestCount,
                Status = x.Status,
                StatusLabel = GetStatusLabel(x.Status)
            })
            .ToListAsync();

        filter.SearchQuery = search;
        filter.StatusOptions = BuildFilterStatusOptions();
        filter.BranchOptions = await BuildBranchOptionsAsync();

        return View(filter);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var model = await BuildReservationDetailViewModelAsync(id);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(ReservationDetailViewModel model)
    {
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

            return View("Details", detailModel);
        }

        if (model.Status.HasValue && model.Status.Value != reservation.Status)
        {
            reservation.Status = model.Status.Value;
        }

        reservation.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();
        reservation.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã cập nhật trạng thái đặt bàn.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
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
}
