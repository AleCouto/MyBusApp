using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using MyBusApp.Configuration;
using MyBusApp.Models.Domain;
using MyBusApp.Models.DTOs.Transitland;

namespace MyBusApp.Services;

public class TransitlandService : IBusService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    public BusProvider Provider => BusProvider.Transitland;
    private readonly Dictionary<string, List<TransitlandRouteStopPattern>> _patternCache = new();

    public TransitlandService(ApiSettings settings)
    {
        _http = new HttpClient { BaseAddress = new Uri(settings.Transitland.BaseUrl) };
    }

    public async Task<BusLine?> GetLineAsync(string lineNumber)
    {
        try
        {
            var routes = await GetTransitlandListAsync<TransitlandRoute>(
                $"routes?route_number={Uri.EscapeDataString(lineNumber)}&per_page=10", "routes");

            var route = routes.FirstOrDefault(r =>
                string.Equals(r.RouteNumber, lineNumber, StringComparison.OrdinalIgnoreCase));

            if (route == null) return null;

            return new BusLine(
                route.OnestopId ?? route.Id,
                route.RouteNumber ?? route.Id,
                route.Name ?? route.RouteNumber ?? route.Id,
                "#007bff",
                Provider);
        }
        catch { return null; }
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
    
            var schedules = await GetTransitlandListAsync<TransitlandSchedule>(
                $"schedules?stop_onestop_id={Uri.EscapeDataString(stopId)}" +
                $"&route_onestop_id={Uri.EscapeDataString(lineId)}" +
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
            $"route_stop_patterns?route_onestop_id={Uri.EscapeDataString(lineId)}&per_page=500",
            "route_stop_patterns");

        if (!patterns.Any()) return new List<TransitlandRouteStopPattern>();

        _patternCache[lineId] = patterns;
        return patterns;
    }

    private async Task<TransitlandRoute?> GetRouteByNumberAsync(string lineNumber)
    {
        if (string.IsNullOrWhiteSpace(lineNumber)) return null;

        var routes = await GetTransitlandListAsync<TransitlandRoute>(
            $"routes?onestop_id={Uri.EscapeDataString(lineNumber)}&per_page=10",
            "routes");

        if (!routes.Any())
        {
            routes = await GetTransitlandListAsync<TransitlandRoute>(
                $"routes?route_number={Uri.EscapeDataString(lineNumber)}&per_page=10",
                "routes");
        }

        return routes.FirstOrDefault(r =>
            string.Equals(r.RouteNumber, lineNumber, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r.OnestopId, lineNumber, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r.Id, lineNumber, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<T>> GetTransitlandListAsync<T>(string url, params string[] arrayPropertyNames)
    {
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new List<T>();

        var json = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(json)) return new List<T>();

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            return JsonSerializer.Deserialize<List<T>>(root.GetRawText(), _options) ?? new List<T>();
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var propertyName in arrayPropertyNames)
            {
                if (root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<T>>(property.GetRawText(), _options) ?? new List<T>();
                }
            }

            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Array)
                {
                    return JsonSerializer.Deserialize<List<T>>(property.Value.GetRawText(), _options) ?? new List<T>();
                }
            }
        }

        return new List<T>();
    }
}
