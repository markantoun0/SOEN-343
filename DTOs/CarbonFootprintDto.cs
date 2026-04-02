namespace SUMMS.Api.DTOs;

public class CarbonFootprintDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public double TotalCarbonKg { get; set; }
    public int TripsCompleted { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class UserLeaderboardEntryDto
{
    public int Rank { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public double TotalCarbonKg { get; set; }
    public int TripsCompleted { get; set; }
}

public class TripCarbonFootprintDto
{
    public int ReservationId { get; set; }
    public string MobilityType { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
    public double DurationMinutes { get; set; }
    public double CarbonKg { get; set; }
}

