using KIGHolding.Models.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Text.RegularExpressions;

namespace KIGHolding.Tests;

public sealed class HistoricalReservationDeletionControllerSecurityTests
{
    [Fact]
    public void DeleteHistorical_IsPostOnlySuperAdminOnlyAndAntiforgeryProtected()
    {
        var controller = ReadRepoFile("Areas", "Admin", "Controllers", "ReservationController.cs");

        Assert.Contains("[HttpPost]", controller, StringComparison.Ordinal);
        Assert.Contains("[Authorize(Roles = \"SuperAdmin\")]", controller, StringComparison.Ordinal);
        Assert.Contains("[ValidateAntiForgeryToken]", controller, StringComparison.Ordinal);
        Assert.Contains("Task<IActionResult> DeleteHistorical(", controller, StringComparison.Ordinal);
        Assert.Contains("TryGetCurrentAdminUserId", controller, StringComparison.Ordinal);
        Assert.Contains("new HistoricalReservationDeletionRequest(model.Id, model.ExpectedVersion, actorAdminUserId)", controller, StringComparison.Ordinal);
    }

    [Fact]
    public void DetailsView_UsesAntiforgeryExpectedRevisionAndSeparateDestructiveConfirmation()
    {
        var view = ReadRepoFile("Areas", "Admin", "Views", "Reservation", "Details.cshtml");

        Assert.Contains("asp-action=\"DeleteHistorical\"", view, StringComparison.Ordinal);
        Assert.Contains("name=\"ExpectedVersion\"", view, StringComparison.Ordinal);
        Assert.Contains("@Html.AntiForgeryToken()", view, StringComparison.Ordinal);
        Assert.Contains("Xóa lịch sử đặt bàn", view, StringComparison.Ordinal);
        Assert.Contains("không bị xóa vật lý", view, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HistoricalDeleteRedirect_GeneratesAdminIndexUrlWithListState_NotPublicBookingUrl()
    {
        await using var app = await CreateRoutingApplicationAsync();
        var branchId = Guid.NewGuid();
        var linkGenerator = app.Services.GetRequiredService<LinkGenerator>();
        var path = linkGenerator.GetPathByAction(
            action: "Index",
            controller: "Reservation",
            values: new
            {
                area = "Admin",
                page = 5,
                SearchQuery = "historic",
                StatusFilter = ReservationStatus.Confirmed,
                BranchFilter = branchId
            });

        Assert.NotNull(path);
        Assert.StartsWith("/admin/Reservation", path, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/dat-ban", path, StringComparison.OrdinalIgnoreCase);

        var query = QueryHelpers.ParseQuery(new Uri("https://localhost" + path).Query);
        Assert.Equal("5", query["page"]);
        Assert.Equal("historic", query["SearchQuery"]);
        Assert.Equal(nameof(ReservationStatus.Confirmed), query["StatusFilter"]);
        Assert.Equal(branchId.ToString(), query["BranchFilter"]);
    }

    [Fact]
    public void DeleteHistorical_AllRedirectOutcomesExplicitlyTargetAdminReservationIndex()
    {
        var controller = ReadRepoFile("Areas", "Admin", "Controllers", "ReservationController.cs");
        var deleteAction = controller[controller.IndexOf("Task<IActionResult> DeleteHistorical(", StringComparison.Ordinal)..
            controller.IndexOf("private async Task<ReservationDetailViewModel?> BuildReservationDetailViewModelAsync", StringComparison.Ordinal)];

        Assert.Equal(3, Regex.Matches(
            deleteAction,
            "RedirectToAction\\(\\s*nameof\\(Index\\),\\s*\\\"Reservation\\\",",
            RegexOptions.CultureInvariant).Count);
        Assert.Equal(3, Regex.Matches(deleteAction, "area = \\\"Admin\\\"", RegexOptions.CultureInvariant).Count);
        Assert.DoesNotContain("RedirectToAction(nameof(Index), new", deleteAction, StringComparison.Ordinal);
        Assert.Contains("TempData[\"SuccessMessage\"] = \"Đã xóa lịch sử đặt bàn khỏi danh sách quản lý.\"", deleteAction, StringComparison.Ordinal);
        Assert.Contains("HistoricalReservationDeletionStatus.Conflict", deleteAction, StringComparison.Ordinal);
        Assert.Contains("HistoricalReservationDeletionStatus.NotEligible", deleteAction, StringComparison.Ordinal);
        Assert.Contains("HistoricalReservationDeletionStatus.AlreadyDeleted", deleteAction, StringComparison.Ordinal);
    }

    private static async Task<WebApplication> CreateRoutingApplicationAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Development
        });
        builder.WebHost.UseTestServer();
        builder.Services
            .AddControllersWithViews()
            .AddApplicationPart(typeof(KIGHolding.Controllers.ReservationController).Assembly);

        var app = builder.Build();
        app.MapControllerRoute(
            name: "admin-root",
            pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}",
            defaults: new { area = "Admin" });
        app.MapControllerRoute(
            name: "areas",
            pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        await app.StartAsync();
        return app;
    }

    private static string ReadRepoFile(params string[] relativeSegments)
    {
        var path = Path.Combine(GetRepositoryRoot(), Path.Combine(relativeSegments));
        return File.ReadAllText(path);
    }

    private static string GetRepositoryRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
