namespace SUMMS.Api.Services.Interfaces;

public sealed record MobilityProviderRequest(
    double Latitude,
    double Longitude,
    int RadiusMeters = 5000);
