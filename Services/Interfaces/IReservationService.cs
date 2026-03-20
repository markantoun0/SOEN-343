using SUMMS.Api.Domain.Models;

namespace SUMMS.Api.Services.Interfaces;

public interface IReservationService
{
    Task<IEnumerable<Reservation>> GetAllAsync(string? type = null, string? city = null);

    Task<Reservation?> GetByIdAsync(int id);

    Task<IEnumerable<Reservation>> GetByLocationIdAsync(int mobilityLocationId);

    Task<IEnumerable<Reservation>> GetByUserIdAsync(int userId);

    Task<Reservation> InsertAsync(
        int      mobilityLocationId,
        DateTime reservationTime,
        string   city,
        DateTime startDate,
        DateTime endDate,
        string   type,
        int?     userId = null);

    Task<Reservation> ReserveFromLocationAsync(
        string   placeId,
        string   name,
        string   type,
        string   city,
        double   latitude,
        double   longitude,
        int      capacity,
        int      availableSpots,
        DateTime reservationTime,
        DateTime startDate,
        DateTime endDate,
        int?     userId = null);

    Task<bool> DeleteAsync(int id, string? deleteReason = null);

    Task<int> CleanupExpiredReservationsAsync();
}
