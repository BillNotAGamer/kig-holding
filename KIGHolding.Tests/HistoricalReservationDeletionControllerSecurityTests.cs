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

    private static string ReadRepoFile(params string[] relativeSegments)
    {
        var path = Path.Combine(GetRepositoryRoot(), Path.Combine(relativeSegments));
        return File.ReadAllText(path);
    }

    private static string GetRepositoryRoot() =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
}
