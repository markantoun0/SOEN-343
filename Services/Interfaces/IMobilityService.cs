using SUMMS.Api.Domain.Models;

namespace SUMMS.Api.Services.Interfaces;

public interface IMobilityService
{
    Task<IEnumerable<MobilityLocation>> GetNearbyMobilityLocationsAsync(
        double latitude,
        double longitude,
        int radiusMeters = 5000);
}

