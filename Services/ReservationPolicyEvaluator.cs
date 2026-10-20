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

        var branchIsReservable = branchId != Guid.Empty && await _dbContext.Branches
            .AsNoTracking()
            .AnyAsync(
                branch => branch.Id == branchId && branch.IsActive && branch.AllowsReservations,
                cancellationToken);

        if (!branchIsReservable)
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

        return branchResult.Status switch
        {
            BranchReservationPolicyEvaluationStatus.Allowed =>
                new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.Allowed),
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
}
