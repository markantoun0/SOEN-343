using SUMMS.Api.Domain.Models;

namespace SUMMS.Api.Services.Interfaces;

public interface IMobilityProviderAdapter
{
    MobilityProvider Provider { get; }

    Task<IEnumerable<MobilityLocation>> GetLocationsAsync(
        MobilityProviderRequest request,
        CancellationToken cancellationToken = default);
}
