using SUMMS.Api.Domain.Models;

namespace SUMMS.Api.Services.Interfaces;

public interface IReservationService
{
    Task<IEnumerable<Reservation>> GetAllAsync(string? type = null, string? city = null);

    Task<Reservation?> GetByIdAsync(int id);

    Task<IEnumerable<Reservation>> GetByLocationIdAsync(int mobilityLocationId);

    Task<Reservation> InsertAsync(
        int      mobilityLocationId,
        DateTime reservationTime,
        string   city,
        string   type);

    Task<Reservation> ReserveFromLocationAsync(
        string   placeId,
        string   name,
        string   type,
        string   city,
        double   latitude,
        double   longitude,
        int      capacity,
        int      availableSpots,
        DateTime reservationTime);

    Task<bool> DeleteAsync(int id);
}
