using MyBusApp.Configuration;
using MyBusApp.Utils;

namespace MyBusApp.Services;

/// <summary>Fertagus train service over the shared static GTFS engine.</summary>
public sealed class FertagusService : GtfsTrainService
{
    public FertagusService(ApiSettings settings, HttpClient httpClient, AppLogger? logger = null)
        : base(
            httpClient,
            "Fertagus",
            string.IsNullOrWhiteSpace(settings.Fertagus.StaticDataBaseUrl) ? "data/fertagus/" : settings.Fertagus.StaticDataBaseUrl,
            logger)
    {
    }
}
