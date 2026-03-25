using SUMMS.Api.Domain.Models;

namespace SUMMS.Api.Services.Interfaces;

public interface IMobilityLocationService
{
    Task<IEnumerable<MobilityLocation>> GetAllAsync(string? type = null);

    Task<MobilityLocation?> GetByIdAsync(int id);

    Task<MobilityLocation> InsertAsync(
        string placeId,
        string name,
        string type,
        string city,
        double latitude,
        double longitude,
        int capacity,
        int availableSpots);

    Task<MobilityLocation?> UpdateAvailableSpotsAsync(int id, int availableSpots);

    Task<bool> DeleteAsync(int id);
    
    Task<object> GetCityAnalyticsAsync();
}
