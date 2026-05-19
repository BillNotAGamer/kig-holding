using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace KIGHolding.Services;

public class ReservationPolicyService : IReservationPolicyService
{
    private readonly ReservationPolicyOptions _options;
    private readonly ISpecialDateProvider _specialDateProvider;
    private readonly ILogger<ReservationPolicyService> _logger;
    private readonly TimeOnly _bookingStartTime;
    private readonly TimeOnly _bookingEndTime;
    private readonly TimeOnly _branchBlackoutStartTime;
    private readonly TimeOnly _branchBlackoutEndTime;
    private readonly HashSet<string> _normalizedBlackoutDistricts;
    private readonly TimeZoneInfo? _vietnamTimeZone;

    public ReservationPolicyService(
        IOptions<ReservationPolicyOptions> options,
        ISpecialDateProvider specialDateProvider,
        ILogger<ReservationPolicyService> logger)
    {
        _options = options.Value;
        _specialDateProvider = specialDateProvider;
        _logger = logger;
        _bookingStartTime = ParseTime(_options.BookingStartTime, new TimeOnly(10, 40), nameof(_options.BookingStartTime));
        _bookingEndTime = ParseTime(_options.BookingEndTime, new TimeOnly(21, 30), nameof(_options.BookingEndTime));
        _branchBlackoutStartTime = ParseTime(_options.BranchBlackoutStartTime, new TimeOnly(14, 0), nameof(_options.BranchBlackoutStartTime));
        _branchBlackoutEndTime = ParseTime(_options.BranchBlackoutEndTime, new TimeOnly(16, 0), nameof(_options.BranchBlackoutEndTime));
        _normalizedBlackoutDistricts = _options.BlackoutDistricts
            .Select(NormalizeText)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);
        _vietnamTimeZone = ResolveVietnamTimeZone();
    }

    public DateOnly GetVietnamToday()
    {
        return DateOnly.FromDateTime(GetVietnamNow().DateTime);
    }

    public Task<ReservationPolicyResult> ValidateAsync(ReservationPolicyRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var errors = new List<ReservationPolicyError>();
        var branch = request.Branch;
        var today = GetVietnamToday();
        var currentTime = TimeOnly.FromDateTime(GetVietnamNow().DateTime);

        if (branch is null || branch.IsActive == false)
        {
            errors.Add(CreateError(nameof(request.BranchId), "Chi nhánh không khả dụng.", "branch_inactive_or_missing"));
        }

        if (request.GuestCount < _options.AbsoluteMinimumGuestCount || request.GuestCount > _options.MaximumGuestCount)
        {
            errors.Add(CreateError(nameof(request.GuestCount), $"Số khách phải từ {_options.AbsoluteMinimumGuestCount} đến {_options.MaximumGuestCount}.", "guest_count_out_of_range"));
        }

        if (request.ReservationDate < today)
        {
            errors.Add(CreateError(nameof(request.ReservationDate), "Không thể đặt bàn trong quá khứ.", "reservation_date_in_past"));
        }
        else if (request.ReservationDate == today && request.ReservationTime <= currentTime)
        {
            errors.Add(CreateError(nameof(request.ReservationTime), "Không thể đặt bàn trong quá khứ.", "reservation_time_in_past"));
        }

        if (request.ReservationTime < _bookingStartTime || request.ReservationTime > _bookingEndTime)
        {
            errors.Add(CreateError(nameof(request.ReservationTime), "Nhà hàng chỉ nhận đặt bàn từ 10:40 đến 21:30.", "reservation_time_out_of_global_hours"));
        }

        if (branch is not null && branch.IsActive && IsBlackoutDistrict(branch.District) && request.ReservationTime >= _branchBlackoutStartTime && request.ReservationTime < _branchBlackoutEndTime)
        {
            errors.Add(CreateError(nameof(request.ReservationTime), "Chi nhánh này không nhận đặt bàn trong khung 14:00 - 16:00. Vui lòng chọn giờ khác.", "branch_blackout_window"));
        }

        if (!AllowsSingleGuest(request.ReservationDate) && request.GuestCount < _options.WeekdayMinimumGuestCount)
        {
            errors.Add(CreateError(nameof(request.GuestCount), "Ngày thường nhà hàng chỉ nhận đặt bàn từ 2 khách trở lên.", "weekday_minimum_guest_count"));
        }

        return Task.FromResult(errors.Count == 0 ? ReservationPolicyResult.Success() : ReservationPolicyResult.Failed(errors));
    }

    private bool AllowsSingleGuest(DateOnly date)
    {
        return date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || _specialDateProvider.IsSpecialDate(date);
    }

    private bool IsBlackoutDistrict(string? district)
    {
        var normalizedDistrict = NormalizeText(district);
        return !string.IsNullOrWhiteSpace(normalizedDistrict) && _normalizedBlackoutDistricts.Contains(normalizedDistrict);
    }

    private DateTimeOffset GetVietnamNow()
    {
        if (_vietnamTimeZone is not null)
        {
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, _vietnamTimeZone);
        }

        _logger.LogWarning("Vietnam time zone could not be resolved. Falling back to UTC+7 offset.");
        return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7));
    }

    private static TimeZoneInfo? ResolveVietnamTimeZone()
    {
        if (TryFindTimeZone("Asia/Ho_Chi_Minh", out var timeZone))
        {
            return timeZone;
        }

        if (TryFindTimeZone("SE Asia Standard Time", out timeZone))
        {
            return timeZone;
        }

        return null;
    }

    private static bool TryFindTimeZone(string timeZoneId, out TimeZoneInfo? timeZone)
    {
        try
        {
            timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            timeZone = null;
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            timeZone = null;
            return false;
        }
    }

    private static TimeOnly ParseTime(string value, TimeOnly fallback, string settingName)
    {
        if (TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ||
            TimeOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static ReservationPolicyError CreateError(string fieldName, string message, string code)
    {
        return new ReservationPolicyError
        {
            FieldName = fieldName,
            Message = message,
            Code = code
        };
    }

    private static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        normalized = builder.ToString().Normalize(NormalizationForm.FormC);
        normalized = normalized.Replace('đ', 'd');
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s]", " ");
        normalized = Regex.Replace(normalized, @"\s+", " ").Trim();

        return normalized;
    }
}
