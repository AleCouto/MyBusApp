using MyBusApp.Models.Domain;
using MyBusApp.Utils;

namespace MyBusApp.Services;

public sealed class BusServiceRouter
{
    private readonly IReadOnlyList<IBusService> _services;
    private readonly AppLogger _logger;

    public BusServiceRouter(IEnumerable<IBusService> services, AppLogger? logger = null)
    {
        _logger = logger ?? new AppLogger();
        _services = services
            .OrderByDescending(s => s.Provider == BusProvider.Metropolitana)
            .ThenByDescending(s => s.Provider == BusProvider.CarrisLisboa)
            .ThenByDescending(s => s.Provider == BusProvider.Transitland)
            .ToList();
    }

    public async Task<(BusLine? Line, IBusService? Service)> ResolveLineAsync(string lineNumber)
    {
        if (string.IsNullOrWhiteSpace(lineNumber))
        {
            return (null, null);
        }

        foreach (var service in _services)
        {
            _logger.Info(nameof(BusServiceRouter), $"Trying provider {service.Provider} for line '{lineNumber}'.");
            var line = await service.GetLineAsync(lineNumber);
            if (line != null)
            {
                _logger.Info(nameof(BusServiceRouter), $"Provider {service.Provider} resolved line '{lineNumber}' as '{line.Id}'.");
                return (line, service);
            }

            _logger.Warning(nameof(BusServiceRouter), $"Provider {service.Provider} did not find line '{lineNumber}'. Trying next provider.");
        }

        _logger.Warning(nameof(BusServiceRouter), $"No provider found line '{lineNumber}'.");
        return (null, null);
    }
}
