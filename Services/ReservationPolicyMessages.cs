namespace KIGHolding.Services;

public static class ReservationPolicyMessages
{
    public const string InvalidBranch = "Chi nhánh này hiện không nhận đặt bàn. Vui lòng chọn chi nhánh khác.";
    public const string InvalidGuestCount = "Số khách phải từ 1 đến 100.";
    public const string BranchClosedDate = "Chi nhánh này không nhận đặt bàn vào ngày đã chọn.";

    public static string GetMessage(ReservationPolicyEvaluationResult result)
    {
        return result.Status switch
        {
            ReservationPolicyEvaluationStatus.PastDate => ReservationBlockedDateService.PastDateMessage,
            ReservationPolicyEvaluationStatus.GlobalBlockedDate => ReservationBlockedDateService.BlockedDateMessage,
            ReservationPolicyEvaluationStatus.InvalidBranch => InvalidBranch,
            ReservationPolicyEvaluationStatus.InvalidGuestCount => InvalidGuestCount,
            ReservationPolicyEvaluationStatus.BranchClosedDate => BranchClosedDate,
            ReservationPolicyEvaluationStatus.MinimumGuestsNotMet when result.MinGuests.HasValue =>
                $"Chi nhánh chỉ nhận đặt bàn từ {result.MinGuests.Value} khách trở lên vào ngày này.",
            _ => throw new InvalidOperationException("An allowed or malformed reservation policy result does not have an error message.")
        };
    }
}
