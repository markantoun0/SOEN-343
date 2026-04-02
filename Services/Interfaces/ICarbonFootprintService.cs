using SUMMS.Api.DTOs;

namespace SUMMS.Api.Services.Interfaces;

public interface ICarbonFootprintService
{
    /// <summary>
    /// Get carbon footprint for a user
    /// </summary>
    Task<CarbonFootprintDto?> GetUserCarbonFootprintAsync(int userId);

    /// <summary>
    /// Calculate and update carbon footprint for a trip
    /// </summary>
    Task<TripCarbonFootprintDto> CalculateTripCarbonFootprintAsync(int reservationId, double distanceKm);

    /// <summary>
    /// Get top users by carbon savings (leaderboard)
    /// </summary>
    Task<List<UserLeaderboardEntryDto>> GetLeaderboardAsync(int topN = 10);

    /// <summary>
    /// Get user's rank in leaderboard
    /// </summary>
    Task<int?> GetUserRankAsync(int userId);
}

