using System.Security.Claims;
using KIGHolding.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Services;

public sealed class AdminCookieAuthenticationEvents : CookieAuthenticationEvents
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<AdminCookieAuthenticationEvents> _logger;

    public AdminCookieAuthenticationEvents(
        AppDbContext dbContext,
        ILogger<AdminCookieAuthenticationEvents> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        var userIdValue = principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var cookieStamp = principal?.FindFirstValue(AdminClaimTypes.SecurityStamp);

        if (!Guid.TryParse(userIdValue, out var userId) || string.IsNullOrWhiteSpace(cookieStamp))
        {
            await RejectPrincipalAsync(context);
            return;
        }

        try
        {
            var user = await _dbContext.AdminUsers
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .Select(x => new
                {
                    x.IsActive,
                    x.SecurityStamp
                })
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

            if (user is null ||
                !user.IsActive ||
                string.IsNullOrWhiteSpace(user.SecurityStamp) ||
                !string.Equals(user.SecurityStamp, cookieStamp, StringComparison.Ordinal))
            {
                await RejectPrincipalAsync(context);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Admin cookie validation failed for user {AdminUserId}.", userId);
            await RejectPrincipalAsync(context);
        }
    }

    private static async Task RejectPrincipalAsync(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
