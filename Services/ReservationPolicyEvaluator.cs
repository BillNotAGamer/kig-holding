using KIGHolding.Data;
using KIGHolding.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Services;

public sealed class ReservationPolicyEvaluator : IReservationPolicyEvaluator
{
    private readonly AppDbContext _dbContext;
    private readonly IReservationBlockedDateService _blockedDateService;
    private readonly IBranchReservationDatePolicyService _branchPolicyService;

    public ReservationPolicyEvaluator(
        AppDbContext dbContext,
        IReservationBlockedDateService blockedDateService,
        IBranchReservationDatePolicyService branchPolicyService)
    {
        _dbContext = dbContext;
        _blockedDateService = blockedDateService;
        _branchPolicyService = branchPolicyService;
    }

    public async Task<ReservationPolicyEvaluationResult> EvaluateAsync(
        Guid branchId,
        DateOnly reservationDate,
        int guestCount,
        DateOnly vietnamToday,
        CancellationToken cancellationToken = default)
    {
        var globalResult = await _blockedDateService.EvaluateReservationDateAsync(
            reservationDate,
            vietnamToday,
            cancellationToken);

        if (globalResult.Status == ReservationDatePolicyStatus.PastDate)
        {
            return new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.PastDate);
        }

        if (globalResult.Status == ReservationDatePolicyStatus.BlockedDate)
        {
            return new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.GlobalBlockedDate);
        }

        var branch = branchId == Guid.Empty
            ? null
            : await _dbContext.Branches
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    b => b.Id == branchId && b.IsActive && b.AllowsReservations,
                    cancellationToken);

        if (branch is null)
        {
            return new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.InvalidBranch);
        }

        if (guestCount is < 1 or > 100)
        {
            return new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.InvalidGuestCount);
        }

        // Tier 2: Specific Date Policy Override (Short-Circuiting)
        var datePolicy = await _branchPolicyService.GetPolicyAsync(
            branchId,
            reservationDate,
            cancellationToken);

        if (datePolicy is not null)
        {
            if (datePolicy.RuleType == BranchReservationRuleType.Closed)
            {
                return new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.BranchClosedDate);
            }

            if (datePolicy.RuleType == BranchReservationRuleType.MinimumGuests && datePolicy.MinGuests.HasValue)
            {
                if (guestCount < datePolicy.MinGuests.Value)
                {
                    return new ReservationPolicyEvaluationResult(
                        ReservationPolicyEvaluationStatus.MinimumGuestsNotMet,
                        datePolicy.MinGuests.Value);
                }

                return new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.Allowed);
            }

            return new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.Allowed);
        }

        // Tier 3: Branch Weekly Default Policy (Only evaluated when NO specific date policy row exists in Tier 2)
        var isWeekend = reservationDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var defaultMinGuests = isWeekend ? branch.WeekendMinGuests : branch.WeekdayMinGuests;

        if (defaultMinGuests.HasValue && guestCount < defaultMinGuests.Value)
        {
            return new ReservationPolicyEvaluationResult(
                ReservationPolicyEvaluationStatus.MinimumGuestsNotMet,
                defaultMinGuests.Value);
        }

        // Tier 4: Allowed
        return new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.Allowed);
    }
}
