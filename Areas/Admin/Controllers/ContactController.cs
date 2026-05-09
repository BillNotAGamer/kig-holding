using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Areas.Admin.Controllers;

public class ContactController : AdminBaseController
{
    private readonly AppDbContext _dbContext;

    public ContactController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(ContactIndexViewModel filter)
    {
        var query = _dbContext.ContactMessages.AsNoTracking();

        if (filter.StatusFilter.HasValue)
        {
            query = query.Where(x => x.Status == filter.StatusFilter.Value);
        }

        filter.Messages = await query
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ContactListItemViewModel
            {
                Id = x.Id,
                FullName = x.FullName,
                PhoneNumber = x.PhoneNumber,
                Subject = x.Subject,
                Status = x.Status,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        filter.StatusOptions = BuildStatusOptions(includeAllOption: true);

        return View(filter);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var model = await _dbContext.ContactMessages
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ContactDetailViewModel
            {
                Id = x.Id,
                FullName = x.FullName,
                PhoneNumber = x.PhoneNumber,
                Email = x.Email,
                Subject = x.Subject,
                Message = x.Message,
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
    public async Task<IActionResult> UpdateStatus(ContactDetailViewModel model)
    {
        var message = await _dbContext.ContactMessages.FirstOrDefaultAsync(x => x.Id == model.Id);
        if (message is null)
        {
            return NotFound();
        }

        message.Status = model.Status;
        message.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã cập nhật trạng thái liên hệ.";
        return RedirectToAction(nameof(Details), new { id = model.Id });
    }

    private static IReadOnlyList<SelectListItem> BuildStatusOptions(bool includeAllOption)
    {
        var options = new List<SelectListItem>();

        if (includeAllOption)
        {
            options.Add(new SelectListItem { Value = string.Empty, Text = "Tất cả trạng thái" });
        }

        foreach (var status in Enum.GetValues<ContactMessageStatus>())
        {
            options.Add(new SelectListItem
            {
                Value = status.ToString(),
                Text = GetStatusLabel(status)
            });
        }

        return options;
    }

    private static string GetStatusLabel(ContactMessageStatus status)
    {
        return status switch
        {
            ContactMessageStatus.New => "Mới",
            ContactMessageStatus.Read => "Đã đọc",
            ContactMessageStatus.Handled => "Đã xử lý",
            ContactMessageStatus.Archived => "Đã lưu trữ",
            _ => status.ToString()
        };
    }
}
