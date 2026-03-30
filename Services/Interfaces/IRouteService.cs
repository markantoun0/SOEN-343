namespace SUMMS.Api.Services.Interfaces;

public interface IRouteService
{
    Task<RouteResult> ComputeRouteAsync(
        string origin,
        string destination,
        string travelMode,
        CancellationToken cancellationToken = default);
}

public sealed record RouteResult(int DistanceMeters, string Duration, string EncodedPolyline);
