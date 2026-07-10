using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Areas.Admin.Controllers;

public class BranchController : AdminBaseController
{
    private const int PageSize = 10;
    private static readonly TimeOnly DefaultOpeningTime = new(10, 0);
    private static readonly TimeOnly DefaultClosingTime = new(22, 0);

    private readonly AppDbContext _dbContext;
    private readonly IBranchService _branchService;
    private readonly IImageStorageService _imageStorageService;
    private readonly ILogger<BranchController> _logger;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };

    private const int MaxFileSize = 2 * 1024 * 1024;

    public BranchController(
        AppDbContext dbContext,
        IBranchService branchService,
        IImageStorageService imageStorageService,
        ILogger<BranchController> logger)
    {
        _dbContext = dbContext;
        _branchService = branchService;
        _imageStorageService = imageStorageService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        var requestedPage = Math.Max(page, 1);

        var query = _dbContext.Branches.AsNoTracking();
        var totalItems = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)PageSize));
        var currentPage = Math.Min(requestedPage, totalPages);

        var branches = await query
            .OrderBy(x => x.DisplayOrder)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Skip((currentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();

        return View(new BranchIndexViewModel
        {
            Branches = branches,
            Page = currentPage,
            PageSize = PageSize,
            TotalItems = totalItems,
            TotalPages = totalPages
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new BranchCreateViewModel
        {
            AllowsReservations = true,
            OpeningTimeText = FormatTime(DefaultOpeningTime),
            ClosingTimeText = FormatTime(DefaultClosingTime)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BranchCreateViewModel model)
    {
        var parsedTimes = await ValidateBranchModelAsync(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var thumbnailUrl = string.Empty;
        if (model.ThumbnailFile is not null && model.ThumbnailFile.Length > 0)
        {
            try
            {
                thumbnailUrl = await _imageStorageService.UploadAsync(
                    model.ThumbnailFile,
                    ImageCategory.Branches,
                    HttpContext.RequestAborted);
            }
            catch
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Không thể tải ảnh lên. Vui lòng thử lại.");
                return View(model);
            }
        }

        var branch = new Branch
        {
            Name = model.Name.Trim(),
            Slug = await BuildUniqueSlugAsync(model.Slug, model.Name),
            Address = model.Address.Trim(),
            District = model.District.Trim(),
            City = model.City.Trim(),
            Hotline = model.Hotline.Trim(),
            Email = model.Email.Trim(),
            OpeningTime = parsedTimes.OpeningTime!.Value,
            ClosingTime = parsedTimes.ClosingTime!.Value,
            LunchBreakStart = parsedTimes.LunchBreakStart,
            LunchBreakEnd = parsedTimes.LunchBreakEnd,
            Capacity = model.Capacity,
            AreaSquareMeters = model.AreaSquareMeters,
            NumberOfFloors = model.NumberOfFloors,
            Description = model.Description.Trim(),
            GoogleMapUrl = model.GoogleMapUrl.Trim(),
            ThumbnailUrl = thumbnailUrl,
            IsActive = model.IsActive,
            AllowsReservations = model.AllowsReservations,
            DisplayOrder = model.DisplayOrder,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Branches.Add(branch);
        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            await _imageStorageService.DeleteAsync(thumbnailUrl, ImageCategory.Branches, HttpContext.RequestAborted);
            throw;
        }

        _branchService.InvalidateActiveBranchesCache();

        return RedirectToAdminIndex();
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var branch = await _dbContext.Branches.FindAsync(id);
        if (branch is null)
        {
            return NotFound();
        }

        var model = new BranchEditViewModel
        {
            Id = branch.Id,
            Name = branch.Name,
            Slug = branch.Slug,
            Address = branch.Address,
            District = branch.District,
            City = branch.City,
            Hotline = branch.Hotline,
            Email = branch.Email,
            OpeningTimeText = FormatTime(branch.OpeningTime),
            ClosingTimeText = FormatTime(branch.ClosingTime),
            LunchBreakStartText = branch.LunchBreakStart.HasValue ? FormatTime(branch.LunchBreakStart.Value) : null,
            LunchBreakEndText = branch.LunchBreakEnd.HasValue ? FormatTime(branch.LunchBreakEnd.Value) : null,
            Capacity = branch.Capacity,
            AreaSquareMeters = branch.AreaSquareMeters,
            NumberOfFloors = branch.NumberOfFloors,
            Description = branch.Description,
            GoogleMapUrl = branch.GoogleMapUrl,
            ExistingThumbnailUrl = branch.ThumbnailUrl,
            IsActive = branch.IsActive,
            AllowsReservations = branch.AllowsReservations,
            DisplayOrder = branch.DisplayOrder
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, BranchEditViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var branch = await _dbContext.Branches.FindAsync(id);
        if (branch is null)
        {
            return NotFound();
        }

        var parsedTimes = await ValidateBranchModelAsync(model, branch.Id);

        if (!ModelState.IsValid)
        {
            model.ExistingThumbnailUrl = branch.ThumbnailUrl;
            return View(model);
        }

        var previousThumbnailUrl = branch.ThumbnailUrl;
        string? uploadedThumbnailUrl = null;
        if (model.ThumbnailFile is not null && model.ThumbnailFile.Length > 0)
        {
            try
            {
                uploadedThumbnailUrl = await _imageStorageService.UploadAsync(
                    model.ThumbnailFile,
                    ImageCategory.Branches,
                    HttpContext.RequestAborted);
            }
            catch
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Không thể tải ảnh lên. Vui lòng thử lại.");
                model.ExistingThumbnailUrl = branch.ThumbnailUrl;
                return View(model);
            }
        }

        branch.Name = model.Name.Trim();
        branch.Slug = await BuildUniqueSlugAsync(model.Slug, model.Name, branch.Id);
        branch.Address = model.Address.Trim();
        branch.District = model.District.Trim();
        branch.City = model.City.Trim();
        branch.Hotline = model.Hotline.Trim();
        branch.Email = model.Email.Trim();
        branch.OpeningTime = parsedTimes.OpeningTime!.Value;
        branch.ClosingTime = parsedTimes.ClosingTime!.Value;
        branch.LunchBreakStart = parsedTimes.LunchBreakStart;
        branch.LunchBreakEnd = parsedTimes.LunchBreakEnd;
        branch.Capacity = model.Capacity;
        branch.AreaSquareMeters = model.AreaSquareMeters;
        branch.NumberOfFloors = model.NumberOfFloors;
        branch.Description = model.Description.Trim();
        branch.GoogleMapUrl = model.GoogleMapUrl.Trim();
        if (!string.IsNullOrWhiteSpace(uploadedThumbnailUrl))
        {
            branch.ThumbnailUrl = uploadedThumbnailUrl;
        }

        branch.IsActive = model.IsActive;
        branch.AllowsReservations = model.AllowsReservations;
        branch.DisplayOrder = model.DisplayOrder;
        branch.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            await _imageStorageService.DeleteAsync(uploadedThumbnailUrl, ImageCategory.Branches, HttpContext.RequestAborted);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(uploadedThumbnailUrl))
        {
            await _imageStorageService.DeleteAsync(previousThumbnailUrl, ImageCategory.Branches, HttpContext.RequestAborted);
        }

        _branchService.InvalidateActiveBranchesCache();

        return RedirectToAdminIndex();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, int page = 1)
    {
        var branch = await _dbContext.Branches.FindAsync(id);
        if (branch is null)
        {
            return NotFound();
        }

        branch.IsActive = false;
        branch.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        _branchService.InvalidateActiveBranchesCache();
        SetSuccessMessage("Chi nhánh đã được ẩn khỏi website công khai.");

        return RedirectToAdminIndex(page);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PermanentDelete(Guid id, int page = 1, CancellationToken cancellationToken = default)
    {
        var branch = await _dbContext.Branches.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (branch is null)
        {
            return NotFound();
        }

        var hasReservations = await _dbContext.Reservations
            .AsNoTracking()
            .AnyAsync(x => x.BranchId == id, cancellationToken);

        if (hasReservations)
        {
            SetErrorMessage("Không thể xóa vĩnh viễn chi nhánh vì đang có dữ liệu đặt bàn liên quan. Hãy sử dụng Xóa mềm để bảo toàn lịch sử vận hành.");
            return RedirectToAdminIndex(page);
        }

        var branchName = branch.Name;
        var thumbnailUrl = branch.ThumbnailUrl;

        _dbContext.Branches.Remove(branch);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Permanent delete failed for branch {BranchId}.", id);
            SetErrorMessage("Không thể xóa vĩnh viễn chi nhánh vì vẫn còn dữ liệu liên quan. Hãy sử dụng Xóa mềm.");
            return RedirectToAdminIndex(page);
        }

        _branchService.InvalidateActiveBranchesCache();

        if (!string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            await _imageStorageService.DeleteAsync(thumbnailUrl, ImageCategory.Branches, CancellationToken.None);
        }

        SetSuccessMessage($"Chi nhánh '{branchName}' đã được xóa vĩnh viễn.");

        return RedirectToAdminIndex(page);
    }

    private async Task<BranchTimeParseResult> ValidateBranchModelAsync(BranchCreateViewModel model, Guid? currentBranchId = null)
    {
        var parsedTimes = ParseAndValidateTimeInputs(model);

        if (model.ThumbnailFile is not null && model.ThumbnailFile.Length > 0)
        {
            var extension = Path.GetExtension(model.ThumbnailFile.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Chỉ chấp nhận file .jpg, .jpeg, .png hoặc .webp.");
            }

            if (!string.IsNullOrWhiteSpace(model.ThumbnailFile.ContentType) &&
                !AllowedContentTypes.Contains(model.ThumbnailFile.ContentType))
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Định dạng ảnh tải lên không hợp lệ.");
            }

            if (model.ThumbnailFile.Length > MaxFileSize)
            {
                ModelState.AddModelError(nameof(model.ThumbnailFile), "Ảnh tải lên không được vượt quá 2MB.");
            }
        }

        var isManualSlug = !string.IsNullOrWhiteSpace(model.Slug);
        var normalizedSlug = NormalizeSlugInput(isManualSlug ? model.Slug : model.Name);
        if (string.IsNullOrWhiteSpace(normalizedSlug))
        {
            ModelState.AddModelError(nameof(model.Slug), "Không thể tạo slug hợp lệ. Vui lòng nhập lại tên hoặc slug.");
            return parsedTimes;
        }

        if (isManualSlug)
        {
            var slugExists = await _dbContext.Branches
                .AsNoTracking()
                .AnyAsync(x => x.Slug == normalizedSlug && (!currentBranchId.HasValue || x.Id != currentBranchId.Value));

            if (slugExists)
            {
                ModelState.AddModelError(nameof(model.Slug), "Slug này đã tồn tại. Vui lòng chọn slug khác.");
            }
        }

        return parsedTimes;
    }

    private async Task<string> BuildUniqueSlugAsync(string? requestedSlug, string fallbackName, Guid? currentBranchId = null)
    {
        var baseSlug = NormalizeSlugInput(string.IsNullOrWhiteSpace(requestedSlug) ? fallbackName : requestedSlug);
        if (string.IsNullOrWhiteSpace(baseSlug))
        {
            baseSlug = Guid.NewGuid().ToString("N")[..8];
        }

        var slug = baseSlug;
        var suffix = 2;

        while (await _dbContext.Branches
                   .AsNoTracking()
                   .AnyAsync(x => x.Slug == slug && (!currentBranchId.HasValue || x.Id != currentBranchId.Value)))
        {
            slug = $"{baseSlug}-{suffix}";
            suffix++;
        }

        return slug;
    }

    private RedirectToActionResult RedirectToAdminIndex(int page = 1)
    {
        return RedirectToAction(
            nameof(Index),
            "Branch",
            new { area = "Admin", page = Math.Max(page, 1) })!;
    }

    private static string NormalizeSlugInput(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        normalized = builder.ToString().Normalize(NormalizationForm.FormC);
        normalized = normalized.Replace('đ', 'd');
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", string.Empty);
        normalized = Regex.Replace(normalized, @"[\s-]+", "-").Trim('-');

        return normalized;
    }

    private BranchTimeParseResult ParseAndValidateTimeInputs(BranchCreateViewModel model)
    {
        var openingTime = ParseRequiredTime(
            model.OpeningTimeText,
            nameof(model.OpeningTimeText),
            "Giờ mở cửa là bắt buộc.",
            "Giờ mở cửa không hợp lệ. Vui lòng nhập theo định dạng HH:mm.");

        var closingTime = ParseRequiredTime(
            model.ClosingTimeText,
            nameof(model.ClosingTimeText),
            "Giờ đóng cửa là bắt buộc.",
            "Giờ đóng cửa không hợp lệ. Vui lòng nhập theo định dạng HH:mm.");

        if (openingTime.HasValue && closingTime.HasValue && openingTime.Value >= closingTime.Value)
        {
            ModelState.AddModelError(nameof(model.ClosingTimeText), "Giờ đóng cửa phải sau giờ mở cửa.");
        }

        var hasLunchBreakStart = !string.IsNullOrWhiteSpace(model.LunchBreakStartText);
        var hasLunchBreakEnd = !string.IsNullOrWhiteSpace(model.LunchBreakEndText);

        if (hasLunchBreakStart != hasLunchBreakEnd)
        {
            ModelState.AddModelError(nameof(model.LunchBreakStartText), "Vui lòng nhập cả giờ bắt đầu và giờ kết thúc nghỉ trưa.");
        }

        var lunchBreakStart = ParseOptionalTime(
            model.LunchBreakStartText,
            nameof(model.LunchBreakStartText),
            "Giờ bắt đầu nghỉ trưa không hợp lệ. Vui lòng nhập theo định dạng HH:mm.");

        var lunchBreakEnd = ParseOptionalTime(
            model.LunchBreakEndText,
            nameof(model.LunchBreakEndText),
            "Giờ kết thúc nghỉ trưa không hợp lệ. Vui lòng nhập theo định dạng HH:mm.");

        if (lunchBreakStart.HasValue && lunchBreakEnd.HasValue)
        {
            if (lunchBreakStart.Value >= lunchBreakEnd.Value)
            {
                ModelState.AddModelError(nameof(model.LunchBreakStartText), "Giờ bắt đầu nghỉ trưa phải trước giờ kết thúc nghỉ trưa.");
            }
            else if (openingTime.HasValue &&
                     closingTime.HasValue &&
                     (lunchBreakStart.Value < openingTime.Value || lunchBreakEnd.Value > closingTime.Value))
            {
                ModelState.AddModelError(nameof(model.LunchBreakStartText), "Giờ nghỉ trưa phải nằm trong khung giờ mở cửa của chi nhánh.");
            }
        }

        return new BranchTimeParseResult(openingTime, closingTime, lunchBreakStart, lunchBreakEnd);
    }

    private TimeOnly? ParseRequiredTime(string? value, string fieldName, string requiredMessage, string invalidMessage)
    {
        var normalizedValue = NormalizeTimeInput(value);
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            ModelState.AddModelError(fieldName, requiredMessage);
            return null;
        }

        if (TryParseTimeInput(normalizedValue, out var parsedTime))
        {
            return parsedTime;
        }

        ModelState.AddModelError(fieldName, invalidMessage);
        return null;
    }

    private TimeOnly? ParseOptionalTime(string? value, string fieldName, string invalidMessage)
    {
        var normalizedValue = NormalizeTimeInput(value);
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            return null;
        }

        if (TryParseTimeInput(normalizedValue, out var parsedTime))
        {
            return parsedTime;
        }

        ModelState.AddModelError(fieldName, invalidMessage);
        return null;
    }

    private static string? NormalizeTimeInput(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static bool TryParseTimeInput(string value, out TimeOnly parsedTime)
    {
        return TimeOnly.TryParseExact(
            value,
            "HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out parsedTime);
    }

    private static string FormatTime(TimeOnly value)
    {
        return value.ToString("HH:mm", CultureInfo.InvariantCulture);
    }

    private readonly record struct BranchTimeParseResult(
        TimeOnly? OpeningTime,
        TimeOnly? ClosingTime,
        TimeOnly? LunchBreakStart,
        TimeOnly? LunchBreakEnd);
}
