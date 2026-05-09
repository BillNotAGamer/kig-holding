using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
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
            model.TotalBranches = await _dbContext.Branches.CountAsync();
            model.ActiveBranches = await _dbContext.Branches.CountAsync(x => x.IsActive);
            model.TotalPosts = await _dbContext.Posts.CountAsync();
            model.PendingReservations = await _dbContext.Reservations.CountAsync();
            model.TotalMessages = await _dbContext.ContactMessages.CountAsync();
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
