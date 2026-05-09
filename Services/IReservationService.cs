using KIGHolding.Models.Entities;

namespace KIGHolding.Services;

public interface IReservationService
{
    Task<ReservationCreateResult> CreateReservationAsync(ReservationCreateRequest request, CancellationToken cancellationToken = default);
    Task<Reservation?> GetReservationByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
