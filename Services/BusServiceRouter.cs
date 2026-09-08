using MyBusApp.Models.Domain;

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

public sealed class AppLogger
{
    public void Info(string source, string message) => Write("INFO", source, message);

    public void Warning(string source, string message) => Write("WARN", source, message);

    public void Error(string source, string message, Exception? exception = null)
    {
        var details = exception is null ? message : $"{message}. {exception.GetType().Name}: {exception.Message}";
        Write("ERROR", source, details);
    }

    public void ApiRequest(string source, Uri? baseAddress, string endpoint)
        => Info(source, $"API request: {RedactApiKey(new Uri(baseAddress ?? new Uri("http://localhost/"), endpoint))}");

    public void ApiResponse(string source, string endpoint, System.Net.HttpStatusCode statusCode, int payloadLength)
        => Info(source, $"API response: {endpoint} -> {(int)statusCode} {statusCode}, payload={payloadLength} bytes");

    private static void Write(string level, string source, string message)
        => Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{source}] {message}");

    private static string RedactApiKey(Uri uri)
    {
        var query = string.Join('&', uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.StartsWith("apikey=", StringComparison.OrdinalIgnoreCase) ? "apikey=***" : part));
        return new UriBuilder(uri) { Query = query }.Uri.ToString();
    }
}
