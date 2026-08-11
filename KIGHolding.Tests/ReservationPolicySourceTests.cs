namespace KIGHolding.Tests;

public sealed class ReservationPolicySourceTests
{
    [Fact]
    public void RepositoryDoesNotKeepSecondSpecialDatePolicySource()
    {
        var root = GetRepositoryRoot();

        Assert.False(File.Exists(Path.Combine(root, "Services", "ConfiguredSpecialDateProvider.cs")));
        Assert.False(File.Exists(Path.Combine(root, "Services", "ReservationPolicyService.cs")));
        Assert.False(File.Exists(Path.Combine(root, "Services", "ISpecialDateProvider.cs")));

        var appsettings = File.ReadAllText(Path.Combine(root, "appsettings.json"));
        Assert.DoesNotContain("SpecialDates", appsettings, StringComparison.Ordinal);
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }
}
