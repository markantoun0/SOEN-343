using SUMMS.Api.Domain.Models;

namespace SUMMS.Api.Services.Interfaces;

public interface IMobilityService
{
    /// <summary>
    /// Fetches mobility-related locations (bike stations and parking) near a given coordinate.
    /// </summary>
    Task<IEnumerable<MobilityLocation>> GetNearbyMobilityLocationsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 5000);
}

