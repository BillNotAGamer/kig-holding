namespace KIGHolding.Tests.Admin;

public sealed class BranchReservationPolicyViewTests
{
    [Fact]
    public void BranchCalendar_ProvidesDistinctEffectiveStatesAndPreservesUnderlyingGlobalOverlayRule()
    {
        var view = ReadRepoFile("Areas", "Admin", "Views", "Reservation", "BranchPolicy.cshtml");

        Assert.Contains("Cho phép đặt bàn", view, StringComparison.Ordinal);
        Assert.Contains("Không nhận đặt bàn", view, StringComparison.Ordinal);
        Assert.Contains("Tối thiểu {branchPolicy.MinGuests} khách", view, StringComparison.Ordinal);
        Assert.Contains("Khóa toàn hệ thống", view, StringComparison.Ordinal);
        Assert.Contains("data-underlying-label", view, StringComparison.Ordinal);
        Assert.Contains("Quy định chi nhánh không thể mở lại ngày này", view, StringComparison.Ordinal);
    }

    [Fact]
    public void BranchCalendar_ProvidesAccessibleEditingAndImpactConfirmationControls()
    {
        var view = ReadRepoFile("Areas", "Admin", "Views", "Reservation", "BranchPolicy.cshtml");

        Assert.Contains("@Html.AntiForgeryToken()", view, StringComparison.Ordinal);
        Assert.Contains("data-branch-policy-branch-select", view, StringComparison.Ordinal);
        Assert.Contains("data-minimum-guests-input", view, StringComparison.Ordinal);
        Assert.Contains("min=\"1\" max=\"100\"", view, StringComparison.Ordinal);
        Assert.Contains("data-impact-confirmation", view, StringComparison.Ordinal);
        Assert.Contains("data-impact-checkbox", view, StringComparison.Ordinal);
        Assert.Contains("data-editor-submit", view, StringComparison.Ordinal);
        Assert.Contains("focus(", view, StringComparison.Ordinal);
        Assert.Contains("aria-label", view, StringComparison.Ordinal);
        Assert.Contains("@@media (max-width: 640px)", view, StringComparison.Ordinal);
    }

    [Fact]
    public void GlobalAndBranchPolicyViews_LinkToSeparateMutationAreas()
    {
        var globalView = ReadRepoFile("Areas", "Admin", "Views", "Reservation", "Policy.cshtml");
        var branchView = ReadRepoFile("Areas", "Admin", "Views", "Reservation", "BranchPolicy.cshtml");

        Assert.Contains("asp-action=\"BranchPolicy\"", globalView, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"Policy\"", branchView, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"BranchPolicy\"", branchView, StringComparison.Ordinal);
        Assert.DoesNotContain("BlockedDatesInput", branchView, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(params string[] relativeSegments)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine(root, Path.Combine(relativeSegments)));
    }
}
