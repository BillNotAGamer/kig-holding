using System.Security.Claims;
using KIGHolding.Areas.Admin.ViewModels.Account;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Areas.Admin.Controllers;

public class AccountController : AdminBaseController
{
    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<AdminUser> _passwordHasher;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        AppDbContext dbContext,
        IPasswordHasher<AdminUser> passwordHasher,
        ILogger<AccountController> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Security(CancellationToken cancellationToken)
    {
        var user = await LoadCurrentAdminAsync(cancellationToken);
        if (user is null || !user.IsActive)
        {
            await SignOutCurrentBrowserAsync();
            SetErrorMessage("Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.");
            return RedirectToAction("Login", "Auth", new { area = "Admin" });
        }

        return View(BuildViewModel(user));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        var user = await LoadCurrentAdminAsync(cancellationToken);
        if (user is null || !user.IsActive)
        {
            await SignOutCurrentBrowserAsync();
            SetErrorMessage("Phiên đăng nhập không còn hợp lệ. Vui lòng đăng nhập lại.");
            return RedirectToAction("Login", "Auth", new { area = "Admin" });
        }

        PopulateViewContext(model, user);

        if (!ModelState.IsValid)
        {
            return View("Security", model);
        }

        var currentPasswordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword);
        if (currentPasswordResult == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Mật khẩu hiện tại không đúng.");
            return View("Security", model);
        }

        var newPasswordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.NewPassword);
        if (newPasswordResult != PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(model.NewPassword), "Mật khẩu mới phải khác mật khẩu hiện tại.");
            return View("Security", model);
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
        user.SecurityStamp = AdminSecurityStampGenerator.Create();
        user.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Password change failed for admin user {AdminUserId}.", user.Id);
            ModelState.AddModelError(string.Empty, "Không thể cập nhật mật khẩu. Vui lòng thử lại.");
            return View("Security", model);
        }

        try
        {
            await ReissueCookieAsync(user);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Admin cookie reissue failed after password change for user {AdminUserId}.", user.Id);
            await SignOutCurrentBrowserAsync();
            SetErrorMessage("Mật khẩu đã được cập nhật. Vui lòng đăng nhập lại để tiếp tục.");
            return RedirectToAction("Login", "Auth", new { area = "Admin" });
        }

        SetSuccessMessage("Mật khẩu quản trị đã được cập nhật.");
        return RedirectToAction(nameof(Security), "Account", new { area = "Admin" });
    }

    private async Task<AdminUser?> LoadCurrentAdminAsync(CancellationToken cancellationToken)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        return await _dbContext.AdminUsers.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    private static ChangePasswordViewModel BuildViewModel(AdminUser user)
    {
        var model = new ChangePasswordViewModel();
        PopulateViewContext(model, user);
        return model;
    }

    private static void PopulateViewContext(ChangePasswordViewModel model, AdminUser user)
    {
        model.Username = user.Username;
        model.Email = MaskEmail(user.Email);
    }

    private async Task ReissueCookieAsync(AdminUser user)
    {
        var authenticateResult = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var currentProperties = authenticateResult.Properties;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(AdminClaimTypes.SecurityStamp, user.SecurityStamp)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = currentProperties?.IsPersistent ?? false,
            ExpiresUtc = currentProperties?.ExpiresUtc
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);
    }

    private async Task SignOutCurrentBrowserAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var trimmedEmail = email.Trim();
        var atIndex = trimmedEmail.IndexOf('@');
        if (atIndex <= 1 || atIndex == trimmedEmail.Length - 1)
        {
            return "***";
        }

        var localPart = trimmedEmail[..atIndex];
        var domain = trimmedEmail[(atIndex + 1)..];
        var visiblePrefix = localPart.Length <= 2 ? localPart[..1] : localPart[..2];

        return $"{visiblePrefix}***@{domain}";
    }
}
