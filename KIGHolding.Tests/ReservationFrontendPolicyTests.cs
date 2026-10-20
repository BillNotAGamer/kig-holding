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
    public void EffectivePolicyClient_ReactsToAllInputsAndRejectsStaleResponses()
    {
        var script = ReadRepoFile("wwwroot", "js", "reservation-policy-client.js");

        Assert.Contains("branchField.addEventListener('change', handleInputChange)", script, StringComparison.Ordinal);
        Assert.Contains("dateField.addEventListener('change', handleInputChange)", script, StringComparison.Ordinal);
        Assert.Contains("guestCountField.addEventListener('input', handleInputChange)", script, StringComparison.Ordinal);
        Assert.Contains("new AbortController()", script, StringComparison.Ordinal);
        Assert.Contains("requestSequence", script, StringComparison.Ordinal);
        Assert.Contains("!isCurrentTuple(tupleKey)", script, StringComparison.Ordinal);
        Assert.Contains("activeController.abort()", script, StringComparison.Ordinal);
        Assert.Contains("requestUrl.searchParams.set('branchId'", script, StringComparison.Ordinal);
        Assert.Contains("requestUrl.searchParams.set('reservationDate'", script, StringComparison.Ordinal);
        Assert.Contains("requestUrl.searchParams.set('guestCount'", script, StringComparison.Ordinal);
        Assert.Contains("cache: 'no-store'", script, StringComparison.Ordinal);
        Assert.Contains("if (!response.ok)", script, StringComparison.Ordinal);
        Assert.Contains("'VerificationFailed'", script, StringComparison.Ordinal);
        Assert.True(
            script.IndexOf("if (!response.ok)", StringComparison.Ordinal)
            < script.IndexOf("payload.status === 'Allowed'", StringComparison.Ordinal));
    }

    [Fact]
    public void FullAndQuickForms_RecheckEffectivePolicyImmediatelyBeforeAuthoritativePost()
    {
        var layout = ReadRepoFile("Views", "Shared", "_Layout.cshtml");
        var fullView = ReadRepoFile("Views", "Reservation", "Index.cshtml");
        var modal = ReadRepoFile("Views", "Shared", "Partials", "_BookingModal.cshtml");
        var modalScript = ReadRepoFile("wwwroot", "js", "booking-modal.js");

        Assert.Contains("data-reservation-policy-endpoint=\"@Url.Action(\"Policy\", \"Reservation\")\"", fullView, StringComparison.Ordinal);
        Assert.Contains("policyValidator.validateForSubmit()", fullView, StringComparison.Ordinal);
        Assert.Contains("reservationForm.requestSubmit()", fullView, StringComparison.Ordinal);
        Assert.Contains("data-reservation-policy-error-for=\"BranchId\"", fullView, StringComparison.Ordinal);
        Assert.Contains("data-reservation-policy-error-for=\"GuestCount\"", fullView, StringComparison.Ordinal);
        Assert.Contains("data-reservation-policy-error-for=\"ReservationDate\"", fullView, StringComparison.Ordinal);

        Assert.Contains("data-reservation-policy-endpoint=\"/dat-ban/policy\"", modal, StringComparison.Ordinal);
        Assert.Contains("policyValidator.validateForSubmit()", modalScript, StringComparison.Ordinal);
        Assert.True(
            modalScript.IndexOf("policyValidator.validateForSubmit()", StringComparison.Ordinal)
            < modalScript.IndexOf("fetch(action", StringComparison.Ordinal));

        Assert.True(
            layout.IndexOf("reservation-policy-client.js", StringComparison.Ordinal)
            < layout.IndexOf("booking-modal.js", StringComparison.Ordinal));
        Assert.Contains("data-booking-modal-action=\"/dat-ban/quick\"", modal, StringComparison.Ordinal);
        Assert.Contains("asp-action=\"Index\"", fullView, StringComparison.Ordinal);
    }

    [Fact]
    public void PolicyFeedback_IsOwnedSeparatelyFromUnrelatedValidationMessages()
    {
        var fullView = ReadRepoFile("Views", "Reservation", "Index.cshtml");
        var modalScript = ReadRepoFile("wwwroot", "js", "booking-modal.js");

        Assert.Contains("data-policy-error-active", fullView, StringComparison.Ordinal);
        Assert.Contains("[data-reservation-policy-error-for][data-policy-error-active=\"true\"]", fullView, StringComparison.Ordinal);
        Assert.Contains("data-booking-modal-policy-error=\"true\"", modalScript, StringComparison.Ordinal);
        Assert.DoesNotContain("ChangeTracker", fullView, StringComparison.Ordinal);
        Assert.DoesNotContain("revision", modalScript, StringComparison.OrdinalIgnoreCase);
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
