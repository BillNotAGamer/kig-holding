using System.Text.RegularExpressions;

namespace KIGHolding.Tests;

public sealed class ReservationFrontendPolicyTests
{
    [Fact]
    public void SharedCalendarPolicyPartialOwnsFrontendDatePolicyPayload()
    {
        var partial = ReadRepoFile("Views", "Shared", "Partials", "_ReservationCalendarPolicy.cshtml");

        Assert.Contains("id=\"reservation-calendar-policy\"", partial, StringComparison.Ordinal);
        Assert.Contains("window.kigReservationCalendarPolicy", partial, StringComparison.Ordinal);
        Assert.Contains("window.kigValidateReservationDate", partial, StringComparison.Ordinal);
        Assert.Contains("restrictedDates", partial, StringComparison.Ordinal);
        Assert.Contains("MaximumOpenReservationDate", partial, StringComparison.Ordinal);
        Assert.Contains("bookingCalendarClosed", partial, StringComparison.Ordinal);
        Assert.Contains("Date.UTC(year, month - 1, day)", partial, StringComparison.Ordinal);
        Assert.Contains("date.getUTCDay()", partial, StringComparison.Ordinal);
        Assert.Contains("parsed.dayOfWeek === 0 || parsed.dayOfWeek === 6", partial, StringComparison.Ordinal);
        Assert.DoesNotContain("unsupportedAfterYear", partial, StringComparison.Ordinal);
        Assert.DoesNotContain("unsupportedHolidayYear", partial, StringComparison.Ordinal);
    }

    [Fact]
    public void FullReservationForm_ConsumesSharedCalendarPolicyWithoutDuplicatedHolidayArray()
    {
        var view = ReadRepoFile("Views", "Reservation", "Index.cshtml");

        Assert.Contains("window.kigValidateReservationDate", view, StringComparison.Ordinal);
        Assert.Contains("data-calendar-policy-error-for=\"ReservationDate\"", view, StringComparison.Ordinal);
        Assert.Contains("MaximumOpenReservationDate", view, StringComparison.Ordinal);
        Assert.DoesNotContain("const holidays = new Set", view, StringComparison.Ordinal);
        Assert.DoesNotContain("2026-02-16", view, StringComparison.Ordinal);
        Assert.DoesNotContain("2028-12-31", view, StringComparison.Ordinal);
    }

    [Fact]
    public void QuickModal_ConsumesSharedCalendarPolicyBeforeFetchAndKeepsServerErrorHandling()
    {
        var modal = ReadRepoFile("Views", "Shared", "Partials", "_BookingModal.cshtml");
        var script = ReadRepoFile("wwwroot", "js", "booking-modal.js");

        Assert.Contains("_ReservationCalendarPolicy.cshtml", modal, StringComparison.Ordinal);
        Assert.Contains("MaximumOpenReservationDate", modal, StringComparison.Ordinal);
        Assert.Contains("validateReservationDateField", script, StringComparison.Ordinal);
        Assert.Contains("window.kigValidateReservationDate", script, StringComparison.Ordinal);
        Assert.True(
            script.IndexOf("if (!validateReservationDateField())", StringComparison.Ordinal)
            < script.IndexOf("fetch(action", StringComparison.Ordinal));
        Assert.Contains("response.status === 400 && payload?.ok === false", script, StringComparison.Ordinal);
        Assert.DoesNotContain("2026-02-16", script, StringComparison.Ordinal);
        Assert.DoesNotContain("2028-12-31", modal, StringComparison.Ordinal);
        Assert.DoesNotContain("2028-12-31", script, StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendPolicy_DoesNotLeaveMultipleHardCodedHolidayArrays()
    {
        var frontendSources = string.Join(
            Environment.NewLine,
            ReadRepoFile("Views", "Reservation", "Index.cshtml"),
            ReadRepoFile("Views", "Shared", "Partials", "_BookingModal.cshtml"),
            ReadRepoFile("wwwroot", "js", "booking-modal.js"),
            ReadRepoFile("Views", "Shared", "Partials", "_ReservationCalendarPolicy.cshtml"));

        Assert.Empty(Regex.Matches(frontendSources, @"const\s+holidays\s*=\s*new\s+Set"));
        Assert.Single(Regex.Matches(frontendSources, "id=\"reservation-calendar-policy\""));
        Assert.Empty(Regex.Matches(frontendSources, "2028-12-31"));
    }

    private static string ReadRepoFile(params string[] relativeSegments)
    {
        var path = Path.Combine(GetRepositoryRoot(), Path.Combine(relativeSegments));
        return File.ReadAllText(path);
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }
}
