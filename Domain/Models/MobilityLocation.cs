namespace SUMMS.Api.Domain.Models;

public class MobilityLocation
{
    public string PlaceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "bixi" | "parking"
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Vicinity { get; set; }
    /// <summary>Simulated available spots (1-28). Only set for parking locations.</summary>
    public int? AvailableSpots { get; set; }
}



