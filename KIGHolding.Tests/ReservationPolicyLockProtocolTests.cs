namespace KIGHolding.Tests;

public sealed class ReservationPolicyLockProtocolTests
{
    [Fact]
    public void Booking_AcquiresDateLockAndFinalPolicyCheckBeforePhoneLock()
    {
        var source = ReadRepoFile("Services", "ReservationService.cs");

        var beginTransaction = source.IndexOf("BeginTransactionAsync", StringComparison.Ordinal);
        var dateLock = source.IndexOf("AcquireDateLockAsync(request.ReservationDate", StringComparison.Ordinal);
        var finalCheck = source.IndexOf("_policyEvaluator.EvaluateAsync", dateLock, StringComparison.Ordinal);
        var phoneLock = source.IndexOf("pg_advisory_xact_lock(hashtext", StringComparison.Ordinal);
        var save = source.IndexOf("SaveChangesAsync", phoneLock, StringComparison.Ordinal);

        Assert.True(beginTransaction >= 0);
        Assert.True(beginTransaction < dateLock);
        Assert.True(dateLock < finalCheck);
        Assert.True(finalCheck < phoneLock);
        Assert.True(phoneLock < save);
    }

    [Fact]
    public void PolicyDateLock_UsesStableTwoIntegerNamespaceAndDateDayNumber()
    {
        var source = ReadRepoFile("Services", "ReservationPolicyLockService.cs");

        Assert.Contains("PolicyDateLockNamespace", source, StringComparison.Ordinal);
        Assert.Contains("date.DayNumber", source, StringComparison.Ordinal);
        Assert.Contains("pg_advisory_xact_lock({0}, {1})", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetHashCode", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CoordinatedTransactionSources_ExplicitlyRequestReadCommitted()
    {
        var reservationSource = ReadRepoFile("Services", "ReservationService.cs");
        var globalPolicySource = ReadRepoFile("Services", "ReservationBlockedDateService.cs");
        var branchPolicySource = ReadRepoFile("Services", "BranchReservationDatePolicyService.cs");

        Assert.Equal(1, CountOccurrences(reservationSource, "IsolationLevel.ReadCommitted"));
        Assert.Equal(1, CountOccurrences(globalPolicySource, "IsolationLevel.ReadCommitted"));
        Assert.Equal(2, CountOccurrences(branchPolicySource, "IsolationLevel.ReadCommitted"));
        Assert.Equal(1, CountOccurrences(reservationSource, "BeginTransactionAsync"));
        Assert.Equal(1, CountOccurrences(globalPolicySource, "BeginTransactionAsync"));
        Assert.Equal(2, CountOccurrences(branchPolicySource, "BeginTransactionAsync"));
    }

    private static int CountOccurrences(string source, string value)
    {
        return source.Split(value, StringSplitOptions.None).Length - 1;
    }

    private static string ReadRepoFile(params string[] relativeSegments)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        return File.ReadAllText(Path.Combine(root, Path.Combine(relativeSegments)));
    }
}
