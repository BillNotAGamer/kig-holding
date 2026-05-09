using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Areas.Admin.Controllers;

public class SettingController : AdminBaseController
{
    private readonly AppDbContext _dbContext;
    private readonly ISiteSettingService _siteSettingService;

    public SettingController(AppDbContext dbContext, ISiteSettingService siteSettingService)
    {
        _dbContext = dbContext;
        _siteSettingService = siteSettingService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var entity = await EnsureSettingsAsync();

        var model = new SettingViewModel
        {
            Id = entity.Id,
            BrandName = entity.BrandName,
            Slogan = entity.Slogan,
            Hotline = entity.Hotline,
            Email = entity.Email,
            Address = entity.Address,
            GoogleMapUrl = entity.GoogleMapUrl,
            FacebookUrl = entity.FacebookUrl,
            ZaloUrl = entity.ZaloUrl,
            TiktokUrl = entity.TiktokUrl
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(SettingViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(nameof(Index), model);
        }

        var entity = await EnsureSettingsAsync();
        entity.SiteName = string.IsNullOrWhiteSpace(entity.SiteName)
            ? model.BrandName.Trim()
            : entity.SiteName;
        entity.BrandName = model.BrandName.Trim();
        entity.Slogan = model.Slogan.Trim();
        entity.Hotline = model.Hotline.Trim();
        entity.Email = model.Email.Trim();
        entity.Address = model.Address.Trim();
        entity.GoogleMapUrl = model.GoogleMapUrl.Trim();
        entity.FacebookUrl = model.FacebookUrl.Trim();
        entity.ZaloUrl = model.ZaloUrl.Trim();
        entity.TiktokUrl = model.TiktokUrl.Trim();
        entity.UpdatedAt = DateTimeOffset.UtcNow;

        await _dbContext.SaveChangesAsync();
        _siteSettingService.InvalidateCache();

        TempData["SuccessMessage"] = "Đã cập nhật cấu hình website.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<SiteSetting> EnsureSettingsAsync()
    {
        var entity = await _dbContext.SiteSettings.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync();
        if (entity is not null)
        {
            return entity;
        }

        entity = new SiteSetting
        {
            SiteName = "Truyền Thuyết Champong",
            BrandName = string.Empty,
            Slogan = string.Empty,
            Hotline = string.Empty,
            Email = string.Empty,
            Address = string.Empty,
            GoogleMapUrl = string.Empty,
            FacebookUrl = string.Empty,
            ZaloUrl = string.Empty,
            TiktokUrl = string.Empty,
            LogoUrl = string.Empty,
            FaviconUrl = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.SiteSettings.Add(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
    }
}
