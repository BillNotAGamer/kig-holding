using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Services;

public class ReservationService : IReservationService
{
    private readonly AppDbContext _dbContext;

    public ReservationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ReservationCreateResult> CreateReservationAsync(ReservationCreateRequest request, CancellationToken cancellationToken = default)
    {
        var errors = new List<ReservationServiceError>();
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (request.ReservationDate < today)
        {
            errors.Add(new ReservationServiceError
            {
                FieldName = nameof(request.ReservationDate),
                Message = "Ngày đến không được sớm hơn hôm nay."
            });
        }

        if (request.GuestCount is < 1 or > 100)
        {
            errors.Add(new ReservationServiceError
            {
                FieldName = nameof(request.GuestCount),
                Message = "Số khách phải từ 1 đến 100."
            });
        }

        var branch = await _dbContext.Branches
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.BranchId && x.IsActive, cancellationToken);

        if (branch is null)
        {
            errors.Add(new ReservationServiceError
            {
                FieldName = nameof(request.BranchId),
                Message = "Chi nhánh đã chọn không tồn tại hoặc đang tạm ngưng nhận đặt bàn."
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
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            GuestCount = request.GuestCount,
            ReservationDate = request.ReservationDate,
            ReservationTime = request.ReservationTime,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            Status = ReservationStatus.Pending,
            Source = ReservationSource.Website,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Reservations.Add(reservation);
        await _dbContext.SaveChangesAsync(cancellationToken);

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
}
