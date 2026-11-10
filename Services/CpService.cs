using MyBusApp.Configuration;
using MyBusApp.Utils;

namespace MyBusApp.Services;

/// <summary>CP train service over the shared static GTFS engine.</summary>
public sealed class CpService : GtfsTrainService
{
    public CpService(ApiSettings settings, HttpClient httpClient, AppLogger? logger = null)
        : base(
            httpClient,
            "CP",
            string.IsNullOrWhiteSpace(settings.Cp.StaticDataBaseUrl) ? "data/cp/" : settings.Cp.StaticDataBaseUrl,
            logger)
    {
    }
}
