using System;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MyBusApp.Configuration;
using MyBusApp.Models.Domain;
using MyBusApp.Models.DTOs.Transitland;
using MyBusApp.Utils;

namespace MyBusApp.Services;

public class TransitlandService : IBusService
{
    private const string CarrisAgencyOnestopId = "o-eyckr-carris";
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _country;
    private readonly AppLogger _logger;
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    public BusProvider Provider => BusProvider.Transitland;
    private readonly Dictionary<string, List<TransitlandRouteStopPattern>> _patternCache = new();

    public TransitlandService(ApiSettings settings, AppLogger? logger = null)
    {
        var baseUrl = settings.Transitland.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("A BaseUrl do Transitland não está configurada.");
        }

        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute)
        };

        _apiKey = settings.Transitland.ApiKey;
        _country = settings.Transitland.Country;
        _logger = logger ?? new AppLogger();
    }

    public TransitlandService(ApiSettings settings, HttpClient httpClient, AppLogger? logger = null)
    {
        var baseUrl = settings.Transitland.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("A BaseUrl do Transitland não está configurada.");
        }

        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        _http = httpClient;
        _apiKey = settings.Transitland.ApiKey;
        _country = settings.Transitland.Country;
        _logger = logger ?? new AppLogger();
    }

    public async Task<BusLine?> GetLineAsync(string lineNumber)
    {
        try
        {
            var routes = await SearchRoutesAsync(lineNumber);
            var route = routes.FirstOrDefault(r => MatchesLineNumber(r, lineNumber));

            if (route == null) return null;

            var displayName = !string.IsNullOrWhiteSpace(route.RouteShortName)
                ? route.RouteShortName
                : route.RouteNumber ?? route.Id;
            var routeId = route.OnestopId ?? route.Id ?? lineNumber;
            var shortName = displayName ?? lineNumber;
            var longName = route.Name ?? route.LongName ?? shortName;

            return new BusLine(
                routeId,
                shortName,
                longName,
                "#007bff",
                Provider);
        }
        catch (Exception exception)
        {
            _logger.Error(nameof(TransitlandService), $"Failed to resolve line '{lineNumber}'.", exception);
            return null;
        }
    }

    public async Task<List<BusDirection>> GetDirectionsAsync(string lineId)
    {
        var patterns = await GetPatternsByLineIdAsync(lineId);
        return patterns.Select(p => {
                    var name = p.Name;
                    if (string.IsNullOrEmpty(name))
                        name = p.StopIds.Count > 0 ? $"Percurso ({p.StopIds.Count} paragens)" : p.Id;
                    return new BusDirection(p.OnestopId ?? p.Id, name);
        }).ToList();
    }

    public async Task<List<BusStop>> GetStopsAsync(string directionId, string lineId)
    {
        var patterns = await GetPatternsByLineIdAsync(lineId);
        var pattern = patterns.FirstOrDefault(p =>
            p.OnestopId == directionId || p.Id == directionId);

        if (pattern == null) return new List<BusStop>();

        // Busca apenas as paragens deste pattern, não todas
        var stops = await GetTransitlandListAsync<TransitlandStop>(
            $"stops?route_stop_pattern_onestop_id={Uri.EscapeDataString(directionId)}&per_page=500",
            "stops");

        // Respeitamos a ordem do pattern se existir
        if (pattern.StopIds.Count > 0)
        {
            var stopMap = stops.ToDictionary(s => s.OnestopId ?? s.Id);
            return pattern.StopIds
                .Select(sid => stopMap.TryGetValue(sid, out var s) ? s : null)
                .Where(s => s != null)
                .Select(s => new BusStop(s!.OnestopId ?? s.Id, s.Name ?? s.Id, s.Locality ?? string.Empty))
                .ToList();
        }

        return stops
            .Select(s => new BusStop(s.OnestopId ?? s.Id, s.Name ?? s.Id, s.Locality ?? string.Empty))
            .ToList();
    }

    public async Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId)
    {
        try
        {
            // Hora local de Lisboa — não UTC
            var lisboaZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");
            var nowLocal = TimeZoneInfo.ConvertTime(DateTime.UtcNow, lisboaZone);
            var today = nowLocal.ToString("yyyy-MM-dd");
            var nowTime = nowLocal.TimeOfDay;
    
            // Garantir que usamos um route_onestop_id (onestop id) — se o caller passou um short name, resolver
            var routeOnestopId = lineId;
            if (string.IsNullOrWhiteSpace(routeOnestopId) || !routeOnestopId.StartsWith("r-", StringComparison.OrdinalIgnoreCase))
            {
                var resolved = await GetRouteByNumberAsync(lineId);
                routeOnestopId = resolved?.OnestopId ?? resolved?.Id ?? lineId;
            }

            var schedules = await GetTransitlandListAsync<TransitlandSchedule>(
                $"schedules?stop_onestop_id={Uri.EscapeDataString(stopId)}" +
                $"&route_onestop_id={Uri.EscapeDataString(routeOnestopId)}" +
                $"&date={today}&per_page=200",
                "schedules");
    
            if (!schedules.Any()) return new List<BusArrival>();
    
            // Obter o shortName uma vez, fora do loop
            var route = await GetRouteByNumberAsync(lineId);
            var shortName = route?.RouteNumber ?? route?.Name ?? lineId;
    
            return schedules
                .Select(s => {
                    var timeStr = s.ArrivalTime ?? s.DepartureTime;
                    if (string.IsNullOrEmpty(timeStr)) return null;
                    // Horários GTFS podem ter >24h (ex: "25:10:00" = 1:10 do dia seguinte)
                    if (!TryParseGtfsTime(timeStr, out var t)) return null;
                    if (t < nowTime) return null;
                    return new BusArrival(shortName, s.TripHeadsign ?? "—", t.ToString(@"hh\:mm"), false);
                })
                .Where(a => a != null)
                .OrderBy(a => a!.DisplayTime)
                .Take(5)
                .ToList()!;
        }
        catch { return new List<BusArrival>(); }
    }
    
    // GTFS permite horas > 24 para viagens noturnas que passam a meia-noite
    private static bool TryParseGtfsTime(string timeStr, out TimeSpan result)
    {
        result = TimeSpan.Zero;
        var parts = timeStr.Split(':');
        if (parts.Length < 2) return false;
        if (!int.TryParse(parts[0], out var h)) return false;
        if (!int.TryParse(parts[1], out var m)) return false;
        var s = parts.Length > 2 && int.TryParse(parts[2], out var sec) ? sec : 0;
        result = new TimeSpan(h, m, s); // pode ser >24h, TimeSpan suporta
        return true;
    }
    
    // --- Internal cached fetchers and helpers ---

    private async Task<List<TransitlandRouteStopPattern>> GetPatternsByLineIdAsync(string lineId)
    {
        if (string.IsNullOrWhiteSpace(lineId)) return new List<TransitlandRouteStopPattern>();
        if (_patternCache.TryGetValue(lineId, out var cachedPatterns)) return cachedPatterns;

        var patterns = await GetTransitlandListAsync<TransitlandRouteStopPattern>(
            $"route_stop_patterns?route_onestop_id={Uri.EscapeDataString(lineId)}&country={Uri.EscapeDataString(_country)}&per_page=500",
            "route_stop_patterns");

        if (!patterns.Any()) return new List<TransitlandRouteStopPattern>();

        _patternCache[lineId] = patterns;
        return patterns;
    }

    private async Task<TransitlandRoute?> GetRouteByNumberAsync(string lineNumber)
    {
        if (string.IsNullOrWhiteSpace(lineNumber)) return null;

        var routes = await SearchRoutesAsync(lineNumber);
        return routes.FirstOrDefault(r => MatchesLineNumber(r, lineNumber));
    }

    private async Task<List<TransitlandRoute>> SearchRoutesAsync(string lineNumber)
    {
        if (string.IsNullOrWhiteSpace(lineNumber)) return new List<TransitlandRoute>();

        var carrisRoutes = await GetRoutesByAgencyAsync(lineNumber);
        if (carrisRoutes.Any()) return carrisRoutes;

        var candidateUrls = new[]
        {
            $"routes?route_short_name={Uri.EscapeDataString(lineNumber)}&country={Uri.EscapeDataString(_country)}&per_page=20",
            $"routes?route_number={Uri.EscapeDataString(lineNumber)}&country={Uri.EscapeDataString(_country)}&per_page=20",
            $"routes?search={Uri.EscapeDataString(lineNumber)}&country={Uri.EscapeDataString(_country)}&per_page=20"
        };

        var allRoutes = new List<TransitlandRoute>();
        var seenRouteKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var url in candidateUrls)
        {
            var routes = await GetTransitlandListAsync<TransitlandRoute>(url, "routes");
            var validCount = routes.Count(r => IsValidTransitlandRoute(r));
            _logger.Info(nameof(TransitlandService), $"Candidate '{url}' returned {routes.Count} routes, validRoutes={validCount}.");
            if (validCount == 0)
            {
                continue;
            }

            foreach (var route in routes)
            {
                var routeKey = route.OnestopId ?? route.Id;
                if (string.IsNullOrWhiteSpace(routeKey)) continue;

                if (seenRouteKeys.Add(routeKey))
                {
                    allRoutes.Add(route);
                }
            }
        }

        return allRoutes
            .Where(r => MatchesLineNumber(r, lineNumber))
            .ToList();
    }

    private async Task<List<TransitlandRoute>> GetRoutesByAgencyAsync(string lineNumber)
    {
        var routes = new List<TransitlandRoute>();
        string? after = null;

        do
        {
            var query = $"routes?operator_onestop_id={Uri.EscapeDataString(CarrisAgencyOnestopId)}&per_page=500";
            if (!string.IsNullOrWhiteSpace(after))
            {
                query += $"&after={Uri.EscapeDataString(after)}";
            }

            var page = await GetTransitlandPageAsync<TransitlandRoute>(query, "routes");
            routes.AddRange(page.Items.Where(r => MatchesLineNumber(r, lineNumber)));
            after = page.After;
        }
        while (!string.IsNullOrWhiteSpace(after) && !routes.Any());

        return routes;
    }

    private static bool IsValidTransitlandRoute(TransitlandRoute route)
    {
        if (route == null) return false;
        if (string.IsNullOrWhiteSpace(route.Id) && string.IsNullOrWhiteSpace(route.OnestopId)) return false;

        return !string.IsNullOrWhiteSpace(route.RouteShortName)
            || !string.IsNullOrWhiteSpace(route.RouteNumber)
            || !string.IsNullOrWhiteSpace(route.Name)
            || !string.IsNullOrWhiteSpace(route.LongName)
            || !string.IsNullOrWhiteSpace(route.OnestopId);
    }

    public static bool MatchesLineNumber(TransitlandRoute route, string lineNumber)
    {
        if (route == null || string.IsNullOrWhiteSpace(lineNumber)) return false;

        var normalizedQuery = lineNumber.Trim();
        var normalizedQueryNoSpaces = normalizedQuery.Replace(" ", string.Empty);

        return
            string.Equals(route.RouteNumber, normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(route.RouteShortName, normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(route.RouteNumber?.Replace(" ", string.Empty), normalizedQueryNoSpaces, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(route.RouteShortName?.Replace(" ", string.Empty), normalizedQueryNoSpaces, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(route.OnestopId, normalizedQuery, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(route.Id, normalizedQuery, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<List<T>> GetTransitlandListAsync<T>(string url, params string[] arrayPropertyNames)
    {
        var page = await GetTransitlandPageAsync<T>(url, arrayPropertyNames);
        return page.Items;
    }

    private async Task<(List<T> Items, string? After)> GetTransitlandPageAsync<T>(string url, params string[] arrayPropertyNames)
    {
        // Adiciona a API key a todos os pedidos se estiver configurada
        var urlWithKey = string.IsNullOrEmpty(_apiKey)
            ? url
            : $"{url}&apikey={Uri.EscapeDataString(_apiKey)}";

        var response = await _http.GetAsync(urlWithKey);
        var status = response.StatusCode;
        _logger.ApiRequest(nameof(TransitlandService), _http.BaseAddress, urlWithKey);
        if (!response.IsSuccessStatusCode)
        {
            _logger.Warning(nameof(TransitlandService), $"API request failed with status {status}: {url}");
            return (new List<T>(), null);
        }

        var json = await response.Content.ReadAsStringAsync();
        _logger.ApiResponse(nameof(TransitlandService), url, status, json.Length);
        if (string.IsNullOrWhiteSpace(json)) return (new List<T>(), null);

        var trimmedBody = json.TrimStart();
        if (trimmedBody.StartsWith("<", StringComparison.Ordinal))
        {
            _logger.Warning(nameof(TransitlandService), $"API returned HTML instead of JSON. Status: {status}. Endpoint: {url}. Snippet: {GetSnippet(trimmedBody)}");
            return (new List<T>(), null);
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                return (JsonSerializer.Deserialize<List<T>>(root.GetRawText(), _options) ?? new List<T>(), null);
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var propertyName in arrayPropertyNames)
                {
                    if (TryGetPropertyIgnoreCase(root, propertyName, out var property) && property.ValueKind == JsonValueKind.Array)
                    {
                        return (JsonSerializer.Deserialize<List<T>>(property.GetRawText(), _options) ?? new List<T>(), GetAfter(root));
                    }
                }

                foreach (var property in root.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        return (JsonSerializer.Deserialize<List<T>>(property.Value.GetRawText(), _options) ?? new List<T>(), GetAfter(root));
                    }
                }

                // Se chegámos até aqui, a resposta foi 200 mas não continha arrays — registar chaves e payload para diagnóstico
                try
                {
                    var keys = root.EnumerateObject().Select(p => p.Name).ToArray();
                    _logger.Warning(nameof(TransitlandService), $"API returned JSON without arrays. Keys: {string.Join(", ", keys)}. Status: {status}. Endpoint: {url}. Payload: {GetSnippet(json)}");
                }
                catch
                {
                    _logger.Warning(nameof(TransitlandService), $"API returned JSON without arrays and keys could not be enumerated. Status: {status}. Endpoint: {url}. Payload: {GetSnippet(json)}");
                }
            }

            return (new List<T>(), null);
        }
        catch (JsonException ex)
        {
            _logger.Error(nameof(TransitlandService), $"API returned invalid JSON. Status: {status}. Endpoint: {url}. Snippet: {GetSnippet(json)}", ex);
            return (new List<T>(), null);
        }
    }

    private static string? GetAfter(JsonElement root)
    {
        return root.TryGetProperty("meta", out var meta) && meta.TryGetProperty("after", out var after)
            ? after.ToString()
            : null;
    }

    private static string GetSnippet(string value)
    {
        const int maxLength = 300;
        var snippet = value.Length <= maxLength ? value : value.Substring(0, maxLength);
        return snippet.Replace("\r", " ").Replace("\n", " ");
    }

    private static bool TryGetPropertyIgnoreCase(JsonElement root, string propertyName, out JsonElement property)
    {
        property = default;
        foreach (var candidate in root.EnumerateObject())
        {
            if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                property = candidate.Value;
                return true;
            }
        }

        return false;
    }
}
