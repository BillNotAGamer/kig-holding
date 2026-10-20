using KIGHolding.Data;
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

        var branchResult = await _branchPolicyService.EvaluateAsync(
            branchId,
            reservationDate,
            guestCount,
            cancellationToken);

        if (branchResult.Status != BranchReservationPolicyEvaluationStatus.Allowed)
        {
            return branchResult.Status switch
            {
                BranchReservationPolicyEvaluationStatus.InvalidBranch =>
                    new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.InvalidBranch),
                BranchReservationPolicyEvaluationStatus.InvalidGuestCount =>
                    new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.InvalidGuestCount),
                BranchReservationPolicyEvaluationStatus.BranchClosedDate =>
                    new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.BranchClosedDate),
                BranchReservationPolicyEvaluationStatus.MinimumGuestsNotMet =>
                    new ReservationPolicyEvaluationResult(
                        ReservationPolicyEvaluationStatus.MinimumGuestsNotMet,
                        branchResult.MinGuests),
                _ => throw new InvalidOperationException("Unsupported branch reservation policy evaluation result.")
            };
        }

        // Tier 3: City Day-of-Week Policy (HCMC branches only)
        var isHcmc = !string.IsNullOrWhiteSpace(branch.City) && 
                     (branch.City.Contains("Hồ Chí Minh", StringComparison.OrdinalIgnoreCase) || 
                      branch.City.Contains("HCM", StringComparison.OrdinalIgnoreCase));

        if (isHcmc)
        {
            var isWeekend = reservationDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var requiredMinGuests = isWeekend ? 10 : 6;

            if (guestCount < requiredMinGuests)
            {
                return new ReservationPolicyEvaluationResult(
                    ReservationPolicyEvaluationStatus.MinimumGuestsNotMet,
                    requiredMinGuests);
            }
        }

        return new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.Allowed);
    }
}
