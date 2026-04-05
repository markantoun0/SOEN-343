using SUMMS.Api.DTOs;

namespace SUMMS.Api.Services.Interfaces;

public interface ICarbonFootprintService
{
    /// <summary>
    /// Get carbon savings footprint for a user
    /// </summary>
    Task<CarbonFootprintDto?> GetUserCarbonFootprintAsync(int userId);

    /// <summary>
    /// Calculate and persist estimated emissions saved for a BIXI reservation
    /// </summary>
    Task<TripCarbonFootprintDto> RecordBixiSavingsForReservationAsync(int reservationId);

    /// <summary>
    /// Get top users by carbon savings (leaderboard)
    /// </summary>
    Task<List<UserLeaderboardEntryDto>> GetLeaderboardAsync(int topN = 10);

    /// <summary>
    /// Get user's rank in leaderboard
    /// </summary>
    Task<int?> GetUserRankAsync(int userId);
}