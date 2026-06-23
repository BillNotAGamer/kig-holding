using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace KIGHolding.Services;

public class ReservationService : IReservationService
{
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(10);

    private readonly AppDbContext _dbContext;
    private readonly IMemoryCache _cache;

    public ReservationService(AppDbContext dbContext, IMemoryCache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task<ReservationCreateResult> CreateReservationAsync(ReservationCreateRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ReservationServiceError>();
        var today = VietnamHolidayEvaluator.GetVietnamToday();
        var normalizedDiningOccasionCode = ReservationOptionCatalog.NormalizeSingleCode(request.DiningOccasionCode);
        var diningOccasionOtherNote = NormalizeOptionalText(request.DiningOccasionOtherNote);

        if (request.ReservationDate < today)
        {
            errors.Add(new ReservationServiceError
            {
                FieldName = nameof(request.ReservationDate),
                Message = "Ngày đến không được sớm hơn hôm nay."
            });
        }

        if (VietnamHolidayEvaluator.IsRestrictedDate(request.ReservationDate))
        {
            errors.Add(new ReservationServiceError
            {
                FieldName = nameof(request.ReservationDate),
                Message = "Hệ thống không nhận đặt bàn vào Thứ Bảy, Chủ Nhật và các ngày Lễ Tết."
            });
        }

        // ── Gate 1: IMemoryCache fail-fast rate-limit ─────────────────────────────
        var normalizedPhone = IdentityNormalizer.NormalizePhone(request.PhoneNumber);
        var phoneLockKey    = IdentityNormalizer.PhoneLockKey(normalizedPhone);

        if (!string.IsNullOrEmpty(normalizedPhone) && _cache.TryGetValue(phoneLockKey, out _))
        {
            errors.Add(new ReservationServiceError
            {
                FieldName = nameof(request.PhoneNumber),
                Message   = "Bạn đã thực hiện đặt bàn trong vòng 10 phút qua. Vui lòng đợi và thử lại sau."
            });

            return ReservationCreateResult.Failed(errors);
        }

        if (request.GuestCount is < 1 or > 100)
        {
            errors.Add(new ReservationServiceError
            {
                FieldName = nameof(request.GuestCount),
                Message = "Số khách phải từ 1 đến 100."
            });
        }

        if (!string.IsNullOrWhiteSpace(normalizedDiningOccasionCode)
            && !ReservationOptionCatalog.IsAllowedDiningOccasionCode(normalizedDiningOccasionCode))
        {
            errors.Add(new ReservationServiceError
            {
                FieldName = nameof(request.DiningOccasionCode),
                Message = "Lựa chọn hình thức dùng bữa không hợp lệ."
            });
        }

        if (string.Equals(normalizedDiningOccasionCode, ReservationOptionCatalog.OtherCode, StringComparison.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(diningOccasionOtherNote))
            {
                errors.Add(new ReservationServiceError
                {
                    FieldName = nameof(request.DiningOccasionOtherNote),
                    Message = "Vui lòng nhập nội dung khác cho hình thức dùng bữa."
                });
            }
            else if (diningOccasionOtherNote.Length > 200)
            {
                errors.Add(new ReservationServiceError
                {
                    FieldName = nameof(request.DiningOccasionOtherNote),
                    Message = "Nội dung khác không được vượt quá 200 ký tự."
                });
            }
        }
        else
        {
            diningOccasionOtherNote = null;
        }

        var branch = await _dbContext.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.BranchId && x.IsActive, cancellationToken);

        if (branch is null)
        {
            errors.Add(new ReservationServiceError
            {
                FieldName = nameof(request.BranchId),
                Message = "Chi nhánh đã chọn không tồn tại hoặc đang tạm ngừng nhận đặt bàn."
            });
        }
        else if (!IsWithinOpeningHours(request.ReservationTime, branch.OpeningTime, branch.ClosingTime))
        {
            errors.Add(new ReservationServiceError
            {
                FieldName = nameof(request.ReservationTime),
                Message = $"Giờ đến phải nằm trong khung {branch.OpeningTime:HH\\:mm} - {branch.ClosingTime:HH\\:mm} của chi nhánh này."
            });
        }
        else if (IsWeekday(request.ReservationDate)
            && TryGetValidLunchBreak(branch, out var lunchBreakStart, out var lunchBreakEnd)
            && IsWithinLunchBreak(request.ReservationTime, lunchBreakStart, lunchBreakEnd))
        {
            errors.Add(new ReservationServiceError
            {
                FieldName = nameof(request.ReservationTime),
                Message = "Chi nhánh tạm nghỉ trưa trong khung giờ này từ Thứ Hai đến Thứ Sáu. Vui lòng chọn thời gian khác."
            });
        }

        if (errors.Count > 0)
        {
            return ReservationCreateResult.Failed(errors);
        }

        var now = DateTimeOffset.UtcNow;
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            BranchId = request.BranchId,
            CustomerName = request.CustomerName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Email = null,
            GuestCount = request.GuestCount,
            ReservationDate = request.ReservationDate,
            ReservationTime = request.ReservationTime,
            DiningOccasionCodes = normalizedDiningOccasionCode,
            DiningOccasionOtherNote = diningOccasionOtherNote,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Status = ReservationStatus.Pending,
            Source = ReservationSource.Website,
            CreatedAt = now,
            UpdatedAt = now
        };

        // ── Gate 2: PostgreSQL advisory transaction lock ───────────────────────────
        // pg_advisory_xact_lock serialises concurrent DB sessions for the same phone
        // hash, making AnyAsync-level races impossible within the transaction scope.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        await _dbContext.Database.ExecuteSqlRawAsync(
            "SELECT pg_advisory_xact_lock(hashtext({0}))",
            [normalizedPhone],
            cancellationToken);

        _dbContext.Reservations.Add(reservation);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // ── Stamp IMemoryCache keys after successful commit ────────────────────────
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = RateLimitWindow,
            Size = 1
        };

        if (!string.IsNullOrEmpty(normalizedPhone))
        {
            _cache.Set(phoneLockKey, true, cacheOptions);
        }

        return ReservationCreateResult.Success(reservation.Id);
    }

    public Task<Reservation?> GetReservationByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Reservations
            .AsNoTracking()
            .Include(x => x.Branch)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private static bool IsWithinOpeningHours(TimeOnly reservationTime, TimeOnly openingTime, TimeOnly closingTime)
    {
        return openingTime <= closingTime
            ? reservationTime >= openingTime && reservationTime <= closingTime
            : reservationTime >= openingTime || reservationTime <= closingTime;
    }

    private static bool IsWeekday(DateOnly reservationDate)
    {
        return reservationDate.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;
    }

    private static bool TryGetValidLunchBreak(Branch branch, out TimeOnly start, out TimeOnly end)
    {
        start = default;
        end = default;

        if (!branch.LunchBreakStart.HasValue || !branch.LunchBreakEnd.HasValue)
        {
            return false;
        }

        start = branch.LunchBreakStart.Value;
        end = branch.LunchBreakEnd.Value;

        return start < end;
    }

    private static bool IsWithinLunchBreak(TimeOnly reservationTime, TimeOnly start, TimeOnly end)
    {
        return reservationTime >= start && reservationTime < end;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
