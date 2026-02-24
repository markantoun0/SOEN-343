using SUMMS.Api.Domain.Models;

namespace SUMMS.Api.Services.Interfaces;

public interface IBixiService
{
    Task<IEnumerable<MobilityLocation>> GetStationsAsync();
}

