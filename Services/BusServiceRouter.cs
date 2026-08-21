using MyBusApp.Models.Domain;

namespace MyBusApp.Services;

public sealed class BusServiceRouter
{
    private readonly IReadOnlyList<IBusService> _services;

    public BusServiceRouter(IEnumerable<IBusService> services)
    {
        _services = services
            .OrderByDescending(s => s.Provider == BusProvider.Transitland)
            .ThenByDescending(s => s.Provider == BusProvider.Metropolitana)
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
            var line = await service.GetLineAsync(lineNumber);
            if (line != null)
            {
                return (line, service);
            }
        }

        return (null, null);
    }
}
