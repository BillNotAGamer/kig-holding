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
        Assert.Contains("IReservationBlockedDateService", partial, StringComparison.Ordinal);
        Assert.Contains("GetActiveBlockedDatesAsync", partial, StringComparison.Ordinal);
        Assert.Contains("blockedDates", partial, StringComparison.Ordinal);
        Assert.Contains("Date.UTC(year, month - 1, day)", partial, StringComparison.Ordinal);
        Assert.DoesNotContain("date.getUTCDay()", partial, StringComparison.Ordinal);
        Assert.DoesNotContain("parsed.dayOfWeek === 0 || parsed.dayOfWeek === 6", partial, StringComparison.Ordinal);
        Assert.DoesNotContain("MaximumOpenReservationDate", partial, StringComparison.Ordinal);
        Assert.DoesNotContain("bookingCalendarClosed", partial, StringComparison.Ordinal);
    }

    [Fact]
    public void FullReservationForm_ConsumesSharedCalendarPolicyWithoutMaximumDateOrWeekendBlocking()
    {
        var view = ReadRepoFile("Views", "Reservation", "Index.cshtml");

        Assert.Contains("window.kigValidateReservationDate", view, StringComparison.Ordinal);
        Assert.Contains("data-calendar-policy-error-for=\"ReservationDate\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("max=\"@reservationCalendarMaxDate\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("MaximumOpenReservationDate", view, StringComparison.Ordinal);
        Assert.DoesNotContain("const holidays = new Set", view, StringComparison.Ordinal);
        Assert.DoesNotContain("2028-12-31", view, StringComparison.Ordinal);
    }

    [Fact]
    public void QuickModal_ConsumesSharedCalendarPolicyBeforeFetchAndKeepsServerErrorHandling()
    {
        var modal = ReadRepoFile("Views", "Shared", "Partials", "_BookingModal.cshtml");
        var script = ReadRepoFile("wwwroot", "js", "booking-modal.js");

        Assert.Contains("_ReservationCalendarPolicy.cshtml", modal, StringComparison.Ordinal);
        Assert.DoesNotContain("MaximumOpenReservationDate", modal, StringComparison.Ordinal);
        Assert.DoesNotContain("max=\"@maximumOpenDate\"", modal, StringComparison.Ordinal);
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
    public void FrontendPolicy_DoesNotLeaveDuplicateHardCodedPolicySources()
    {
        var frontendSources = string.Join(
            Environment.NewLine,
            ReadRepoFile("Views", "Reservation", "Index.cshtml"),
            ReadRepoFile("Views", "Shared", "Partials", "_BookingModal.cshtml"),
            ReadRepoFile("wwwroot", "js", "booking-modal.js"),
            ReadRepoFile("Views", "Shared", "Partials", "_ReservationCalendarPolicy.cshtml"));

        Assert.Empty(Regex.Matches(frontendSources, @"const\s+holidays\s*=\s*new\s+Set"));
        Assert.Empty(Regex.Matches(frontendSources, "MaximumOpenReservationDate"));
        Assert.Empty(Regex.Matches(frontendSources, "weekendRestricted"));
        Assert.Empty(Regex.Matches(frontendSources, "2028-12-31"));
        Assert.Single(Regex.Matches(frontendSources, "id=\"reservation-calendar-policy\""));
    }

    [Fact]
    public void AdminPolicyCalendar_UsesSevenColumnsAndDatabaseBackedBlockedState()
    {
        var policyView = ReadRepoFile("Areas", "Admin", "Views", "Reservation", "Policy.cshtml");

        Assert.Equal(2, Regex.Matches(policyView, "grid-cols-7").Count);
        Assert.Contains("Model.BlockedDates.Contains(date)", policyView, StringComparison.Ordinal);
        Assert.Contains("disabled=\"@(isPast ? \"disabled\" : null)\"", policyView, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"@(isBlocked ? \"true\" : \"false\")\"", policyView, StringComparison.Ordinal);
        Assert.Contains("Không nhận đặt bàn", policyView, StringComparison.Ordinal);
        Assert.Contains("Cho phép đặt bàn", policyView, StringComparison.Ordinal);
        Assert.DoesNotContain("2026-09-02", policyView, StringComparison.Ordinal);
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
