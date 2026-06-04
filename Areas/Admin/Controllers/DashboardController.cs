using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public DashboardController(AppDbContext dbContext, IConfiguration configuration)
    {
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new AdminDashboardViewModel
        {
            AdminUsername = User.Identity?.Name ?? "superadmin"
        };

        if (!HasConfiguredDatabase())
        {
            return View(model);
        }

        try
        {
            var cancellationToken = HttpContext.RequestAborted;

            model.TotalBranches = await _dbContext.Branches.CountAsync(cancellationToken);
            model.ActiveBranches = await _dbContext.Branches.CountAsync(x => x.IsActive, cancellationToken);
            model.TotalPosts = await _dbContext.Posts.CountAsync(cancellationToken);
            model.PendingReservations = await _dbContext.Reservations
                .CountAsync(x => x.Status == ReservationStatus.Pending, cancellationToken);
            model.TotalMessages = await _dbContext.ContactMessages.CountAsync(cancellationToken);
            model.DatabaseConnected = true;
        }
        catch
        {
            model.DatabaseConnected = false;
        }

        return View(model);
    }

    private bool HasConfiguredDatabase()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        return !string.IsNullOrWhiteSpace(connectionString)
            && !connectionString.Contains("your-neon-host", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("your_username", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("your_password", StringComparison.OrdinalIgnoreCase);
    }
}
