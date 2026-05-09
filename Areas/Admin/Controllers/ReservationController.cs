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

        if (filter.StatusFilter.HasValue)
        {
            query = query.Where(x => x.Status == filter.StatusFilter.Value);
        }

        if (filter.BranchFilter.HasValue)
        {
            query = query.Where(x => x.BranchId == filter.BranchFilter.Value);
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
                Status = x.Status
            })
            .ToListAsync();

        filter.StatusOptions = BuildStatusOptions(includeAllOption: true);
        filter.BranchOptions = await BuildBranchOptionsAsync();

        return View(filter);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _dbContext.Reservations
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ReservationDetailViewModel
            {
                Id = x.Id,
                CustomerName = x.CustomerName,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                BranchName = x.Branch.Name,
                BranchAddress = x.Branch.Address,
                ReservationDate = x.ReservationDate,
                ReservationTime = x.ReservationTime,
                GuestCount = x.GuestCount,
                Note = x.Note,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();

        if (model is null)
        {
            return NotFound();
        }

        model.StatusOptions = BuildStatusOptions(includeAllOption: false);
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

        reservation.Status = model.Status;
        reservation.Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim();
        reservation.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã cập nhật trạng thái đặt bàn.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
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

    private static IReadOnlyList<SelectListItem> BuildStatusOptions(bool includeAllOption)
    {
        var options = new List<SelectListItem>();

        if (includeAllOption)
        {
            options.Add(new SelectListItem { Value = string.Empty, Text = "Tất cả trạng thái" });
        }

        foreach (var status in Enum.GetValues<ReservationStatus>())
        {
            options.Add(new SelectListItem
            {
                Value = status.ToString(),
                Text = GetStatusLabel(status)
            });
        }

        return options;
    }

    private static string GetStatusLabel(ReservationStatus status)
    {
        return status switch
        {
            ReservationStatus.Pending => "Chờ xác nhận",
            ReservationStatus.Confirmed => "Đã xác nhận",
            ReservationStatus.Arrived => "Đã đến",
            ReservationStatus.Cancelled => "Đã hủy",
            ReservationStatus.NoShow => "Không đến",
            _ => status.ToString()
        };
    }
}
