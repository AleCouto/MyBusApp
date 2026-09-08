using System.Text.Json;
using MyBusApp.Configuration;
using MyBusApp.Models.Domain;
using MyBusApp.Models.DTOs.CarrisMetropolitana;

namespace MyBusApp.Services;

public class CarrisMetropolitanaService : IBusService
{
    private readonly HttpClient _http;
    private readonly MyBusApp.Services.AppLogger _logger;
    private readonly JsonSerializerOptions  _options = new() { PropertyNameCaseInsensitive = true };
    
    // Cache em memória para evitar pedidos repetidos durante a mesma sessão
    private List<Line>? _cachedLines;
    private List<Route>? _cachedRoutes;
    private List<Pattern>? _cachedPatterns;
    private List<Stop>? _cachedStops;
    
    public BusProvider Provider => BusProvider.Metropolitana;

    public CarrisMetropolitanaService(ApiSettings settings, MyBusApp.Services.AppLogger? logger = null)
    {
        _http = new HttpClient { BaseAddress = new Uri(settings.CarrisMetropolitana.BaseUrl) };
        _logger = logger ?? new MyBusApp.Services.AppLogger();
        _logger.Info(nameof(CarrisMetropolitanaService), $"Initialized with base URL {_http.BaseAddress}");
    }

    
    // --- IMPLEMENTAÇÃO IBusService (Otimizada usando os seus métodos existentes) ---

    public async Task<BusLine?> GetLineAsync(string lineNumber)
    {
        var line = await GetLineByNumberAsync(lineNumber);
        if (line == null) return null;
        return new BusLine(line.Id, line.ShortName, line.LongName, line.Color, Provider);
    }

    public async Task<List<BusDirection>> GetDirectionsAsync(string lineId)
    {
        var patterns = await GetPatternsByLineIdAsync(lineId);
        return patterns.Select(p => new BusDirection(p.Id, p.Headsign)).ToList();
    }

    public async Task<List<BusStop>> GetStopsAsync(string directionId, string lineId)
    {
        // Aproveita o seu cache de patterns
        var patterns = await GetPatternsByLineIdAsync(lineId);
        var pattern = patterns.FirstOrDefault(p => p.Id == directionId);
        if (pattern == null) return [];

        _cachedStops ??= await GetStopsFromApiAsync();
        var allStops = _cachedStops;
        
        var stopsById = allStops.ToDictionary(s => s.Id);

        return pattern.Path
            .OrderBy(p => p.StopSequence)
            .Where(p => stopsById.ContainsKey(p.StopId))
            .Select(p => stopsById[p.StopId])
            .Select(s => new BusStop(s.Id, s.LongName, s.LocalityId))
            .ToList();
    }

    public async Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId)
    {
        var rawArrivals = await GetArrivalsByStopIdAsync(stopId);
        _logger.Info(nameof(CarrisMetropolitanaService), $"Arrivals loaded for stop '{stopId}': {rawArrivals.Count} records; filtering for line '{lineId}'.");
          var currentTime = DateTime.Now.TimeOfDay;

        var arrivals = rawArrivals
            .Where(a => (a.LineId == lineId || a.RouteId == lineId || a.PatternId.StartsWith(lineId + "_")) &&
                TryGetArrivalTime(a, out var arrivalTime) && arrivalTime >= currentTime)
            .OrderBy(a => GetArrivalSortTime(a))
            .Take(5)
            .Select(a => new BusArrival(a.LineId, a.Headsign, a.DisplayTime, a.IsRealTime))
            .ToList();

        _logger.Info(nameof(CarrisMetropolitanaService), $"Arrivals returned for stop '{stopId}' and line '{lineId}': {arrivals.Count}.");
        return arrivals;
    }

    private static TimeSpan GetArrivalSortTime(Arrival arrival)
        => TryGetArrivalTime(arrival, out var time) ? time : TimeSpan.MaxValue;

    private static bool TryGetArrivalTime(Arrival arrival, out TimeSpan time)
    {
        var value = !string.IsNullOrWhiteSpace(arrival.EstimatedArrival)
            ? arrival.EstimatedArrival
            : arrival.ScheduledArrival;

        time = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var timePart = value.Contains('T') ? value.Split('T')[1] : value;
        if (timePart.Contains('+')) timePart = timePart.Split('+')[0];
        if (timePart.Contains('Z')) timePart = timePart.TrimEnd('Z');
        return TimeSpan.TryParse(timePart, out time);
    }

    // --- SEUS MÉTODOS ORIGINAIS (MANTIDOS IGUAIS) ---

    public async Task<Line?> GetLineByNumberAsync(string lineNumber)
    {
        _cachedLines ??= await GetLinesFromApiAsync();
        return _cachedLines.FirstOrDefault(l => l.ShortName == lineNumber || l.Id == lineNumber);
    }

    public async Task<List<Route>> GetRoutesByLineIdAsync(string lineId)
    {
        if (lineId == null) return [];
        _cachedRoutes ??= await GetRoutesFromApiAsync();
        return _cachedRoutes.Where(r => r.LineId == lineId || r.Id.StartsWith(lineId + "_")).ToList();
    }

    public async Task<List<Stop>> GetStopsByLineIdAsync(string lineId)
    {
        if (lineId == null) return [];
        _cachedStops ??= await GetStopsFromApiAsync();
        return _cachedStops.Where(s => s.LineIds != null && s.LineIds.Contains(lineId)).ToList();
    }

    public async Task<List<Stop>> GetStopsByRouteAsync(string routeId)
    {
        if (routeId == null) return [];
        _cachedStops ??= await GetStopsFromApiAsync();
        return _cachedStops.Where(s => s.RouteIds != null && s.RouteIds.Contains(routeId)).ToList();
    }

    public async Task<List<Pattern>> GetPatternsByLineIdAsync(string lineId)
    {
        if (lineId == null) return [];
    
        // 1. Inicializa o cache se estiver nulo
        _cachedPatterns ??= new List<Pattern>();
    
        // 2. Obtemos os IDs de patterns da linha (através das rotas)
        var lineRoutes = await GetRoutesByLineIdAsync(lineId);

        if (lineRoutes == null) return [];

        var patternIds = lineRoutes
            .SelectMany(r => r.PatternIds ?? Enumerable.Empty<string>())
            .Distinct()
            .ToList();
    
        // 3. Verificação de Cache: O que já temos? O que falta buscar?
        var finalPatterns = _cachedPatterns.Where(p => patternIds.Contains(p.Id)).ToList();
        var idsToFetch = patternIds.Where(id => !_cachedPatterns.Any(p => p.Id == id)).ToList();
    
        // 4. Só vamos à API buscar o que NÃO está no cache
        if (idsToFetch.Any())
        {
            var tasks = idsToFetch.Select(id => GetPatternFromApiAsync(id));
            var newResults = await Task.WhenAll(tasks);

            foreach (var p in newResults.Where(p => p != null))
            {
                _cachedPatterns.Add(p!); // Guarda no cache global para a próxima vez
                finalPatterns.Add(p!);
            }
        }

        return finalPatterns.OrderBy(p => p.Headsign).ToList();
    }

    public async Task<List<Arrival>> GetArrivalsByStopIdAsync(string stopId)
    {
        if (stopId == null) return [];
        var allArrivals = await GetArrivalsFromApiAsync(stopId);
        return allArrivals.Where(a => (!string.IsNullOrEmpty(a.EstimatedArrival) || !string.IsNullOrEmpty(a.ScheduledArrival)) && !string.IsNullOrEmpty(a.DisplayTime)).ToList();
    }



    //////////////////////// Private Methods from API //////////////////////// 
    
    private async Task<List<Line>> GetLinesFromApiAsync()
    {
        const string endpoint = "lines";
        try
        {
            _logger.ApiRequest(nameof(CarrisMetropolitanaService), _http.BaseAddress, endpoint);
            var response = await _http.GetAsync(endpoint);
            var json = await response.Content.ReadAsStringAsync();
            _logger.ApiResponse(nameof(CarrisMetropolitanaService), endpoint, response.StatusCode, json.Length);
            if (string.IsNullOrWhiteSpace(json) || json == "[]") return [];
            return JsonSerializer.Deserialize<List<Line>>(json, _options) ?? [];
        } 
        catch (Exception exception) { _logger.Error(nameof(CarrisMetropolitanaService), $"Failed API request: {endpoint}", exception); return []; }
    }

    private async Task<List<Route>> GetRoutesFromApiAsync()
    {
        const string endpoint = "routes";
        try
        {
            _logger.ApiRequest(nameof(CarrisMetropolitanaService), _http.BaseAddress, endpoint);
            var response = await _http.GetAsync(endpoint);
            var json = await response.Content.ReadAsStringAsync();
            _logger.ApiResponse(nameof(CarrisMetropolitanaService), endpoint, response.StatusCode, json.Length);
            if (string.IsNullOrWhiteSpace(json) || json == "[]") return [];       
            return JsonSerializer.Deserialize<List<Route>>(json, _options) ?? [];
        } 
        catch (Exception exception) { _logger.Error(nameof(CarrisMetropolitanaService), $"Failed API request: {endpoint}", exception); return []; }
    }

    private async Task<List<Stop>> GetStopsFromApiAsync()
    {
        const string endpoint = "stops";
        try
        {
            _logger.ApiRequest(nameof(CarrisMetropolitanaService), _http.BaseAddress, endpoint);
            var response = await _http.GetAsync(endpoint);
            var json = await response.Content.ReadAsStringAsync();
            _logger.ApiResponse(nameof(CarrisMetropolitanaService), endpoint, response.StatusCode, json.Length);
            if (string.IsNullOrWhiteSpace(json) || json == "[]") return [];           
            return JsonSerializer.Deserialize<List<Stop>>(json, _options) ?? [];
        } 
        catch (Exception exception) { _logger.Error(nameof(CarrisMetropolitanaService), $"Failed API request: {endpoint}", exception); return []; }
    }

    private async Task<Pattern?> GetPatternFromApiAsync(string patternId)
    {
        var endpoint = $"patterns/{patternId}";
        try
        {
            _logger.ApiRequest(nameof(CarrisMetropolitanaService), _http.BaseAddress, endpoint);
            var response = await _http.GetAsync(endpoint);
            var json = await response.Content.ReadAsStringAsync();
            _logger.ApiResponse(nameof(CarrisMetropolitanaService), endpoint, response.StatusCode, json.Length);
            if (string.IsNullOrWhiteSpace(json) || json == "[]" || json == "null") return null;
            // Lemos como lista porque a API manda []
            var list = JsonSerializer.Deserialize<List<Pattern>>(json, _options);
            // Tiramos o primeiro objeto da lista e devolvemos só ele
            return list?.FirstOrDefault(p => p.Id == patternId);
        } 
        catch (Exception exception) { _logger.Error(nameof(CarrisMetropolitanaService), $"Failed API request: {endpoint}", exception); return null; }
    }

    private async Task<List<Arrival>> GetArrivalsFromApiAsync(string stopId)
    {
        var endpoint = $"arrivals/by_stop/{stopId}";
        try
        {
            _logger.ApiRequest(nameof(CarrisMetropolitanaService), _http.BaseAddress, endpoint);
            var response = await _http.GetAsync(endpoint);
            var json = await response.Content.ReadAsStringAsync();
            _logger.ApiResponse(nameof(CarrisMetropolitanaService), endpoint, response.StatusCode, json.Length);
            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning(nameof(CarrisMetropolitanaService), $"No arrivals available for stop '{stopId}'. HTTP {(int)response.StatusCode}.");
                return [];
            }
            if (string.IsNullOrWhiteSpace(json) || json == "[]") return [];          
            return JsonSerializer.Deserialize<List<Arrival>>(json, _options) ?? [];
        } 
        catch (Exception exception) { _logger.Error(nameof(CarrisMetropolitanaService), $"Failed API request: {endpoint}", exception); return []; }
    }
}