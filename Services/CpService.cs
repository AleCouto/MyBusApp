using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using MyBusApp.Configuration;
using MyBusApp.Models.Domain;
using MyBusApp.Models.StaticData;
using MyBusApp.Utils;

namespace MyBusApp.Services;

public sealed class CpService : ITrainService
{
    private const int StaticDataVersion = 2;
    private const char DirectionIdSeparator = '\u001f';
    private readonly HttpClient _http;
    private readonly AppLogger _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly Dictionary<string, GtfsStaticLine> _lineCache = new(StringComparer.OrdinalIgnoreCase);
    private GtfsStaticManifest? _manifest;
    private bool _manifestLoaded;

    public CpService(ApiSettings settings, HttpClient httpClient, AppLogger? logger = null)
    {
        _http = httpClient;
        _logger = logger ?? new AppLogger();
        var configuredBaseUrl = string.IsNullOrWhiteSpace(settings.Cp.StaticDataBaseUrl)
            ? "data/cp/"
            : settings.Cp.StaticDataBaseUrl;
        _http.BaseAddress = CreateBaseUri(_http.BaseAddress, configuredBaseUrl);
        _logger.Info(nameof(CpService), $"Initialized with static data URL {_http.BaseAddress}");
    }

    public string OperatorName => "CP";

    public async Task<List<TrainStation>> GetStationsAsync()
        => (await GetAllLineDataAsync())
            .SelectMany(line => line.Stops)
            .Where(stop => !string.IsNullOrWhiteSpace(stop.Id))
            .GroupBy(stop => stop.Id, StringComparer.Ordinal)
            .Select(group => new TrainStation(
                group.Key,
                group.Select(stop => stop.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .OrderBy(name => name, StringComparer.Ordinal)
                    .FirstOrDefault() ?? group.Key))
            .OrderBy(station => station.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(station => station.Id, StringComparer.Ordinal)
            .ToList();

    public async Task<List<TrainDirection>> GetDestinationsAsync(string stationId)
    {
        if (string.IsNullOrWhiteSpace(stationId)) return [];

        var destinations = new Dictionary<string, TrainDirection>(StringComparer.Ordinal);
        foreach (var line in await GetAllLineDataAsync())
        {
            var tripIdsAtStation = line.StopTimes
                .Where(stopTime => string.Equals(stopTime.StopId, stationId, StringComparison.Ordinal))
                .Select(stopTime => stopTime.TripId)
                .ToHashSet(StringComparer.Ordinal);
            if (tripIdsAtStation.Count == 0) continue;

            var routeIds = line.Trips
                .Where(trip => tripIdsAtStation.Contains(trip.Id))
                .Select(trip => trip.RouteId)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(routeId => routeId, StringComparer.Ordinal);
            foreach (var routeId in routeIds)
            {
                var directionId = CreateDirectionId(line.Number, routeId);
                destinations.TryAdd(directionId, new TrainDirection(directionId, GetDirectionName(line, routeId)));
            }
        }

        return destinations.Values
            .OrderBy(direction => direction.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(direction => direction.Id, StringComparer.Ordinal)
            .ToList();
    }

    public async Task<List<TrainDeparture>> GetDeparturesAsync(string stationId, string directionId)
    {
        if (string.IsNullOrWhiteSpace(stationId) ||
            !TryParseDirectionId(directionId, out var lineNumber, out var routeId))
            return [];

        var lisbonZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, lisbonZone);
        var candidates = new List<(DateTime ScheduledAt, string TripId, TrainDeparture Departure)>();

        foreach (var line in await GetAllLineDataAsync())
        {
            if (!string.Equals(line.Number, lineNumber, StringComparison.Ordinal)) continue;

            var trainLine = new TrainLine(line.Number, line.Name);
            var directionName = GetDirectionName(line, routeId);
            var tripsById = line.Trips
                .Where(trip => string.Equals(trip.RouteId, routeId, StringComparison.Ordinal))
                .ToDictionary(trip => trip.Id, StringComparer.Ordinal);

            foreach (var stopTime in line.StopTimes.Where(stopTime =>
                         string.Equals(stopTime.StopId, stationId, StringComparison.Ordinal)))
            {
                if (!tripsById.TryGetValue(stopTime.TripId, out var trip)) continue;

                for (var offset = -1; offset <= 1; offset++)
                {
                    var serviceDate = now.Date.AddDays(offset);
                    if (!IsServiceActive(line, trip.ServiceId, serviceDate)) continue;

                    var timeSeconds = GetScheduledTimeSeconds(stopTime);
                    var scheduledAt = serviceDate.AddSeconds(timeSeconds);
                    if (scheduledAt <= now.DateTime) continue;

                    var serviceName = string.IsNullOrWhiteSpace(trainLine.Name)
                        ? trainLine.Id
                        : trainLine.Name;
                    var destination = string.IsNullOrWhiteSpace(trip.Headsign)
                        ? directionName
                        : trip.Headsign;
                    candidates.Add((scheduledAt, trip.Id, new TrainDeparture(
                        serviceName,
                        destination,
                        FormatGtfsTime(timeSeconds))));
                }
            }
        }

        return candidates
            .OrderBy(candidate => candidate.ScheduledAt)
            .ThenBy(candidate => candidate.TripId, StringComparer.Ordinal)
            .Take(5)
            .Select(candidate => candidate.Departure)
            .ToList();
    }

    public static bool IsServiceActive(GtfsStaticLine line, string serviceId, DateTime date)
    {
        var calendar = line.Calendar.FirstOrDefault(item => item.ServiceId == serviceId);
        var dateKey = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        var exception = line.CalendarDates.FirstOrDefault(item => item.ServiceId == serviceId && item.Date == dateKey);
        if (exception is not null)
            return exception.ExceptionType == 1;

        if (calendar is null || !DateTime.TryParseExact(calendar.StartDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var startDate) ||
            !DateTime.TryParseExact(calendar.EndDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var endDate) ||
            date.Date < startDate.Date || date.Date > endDate.Date)
            return false;

        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => calendar.Monday,
            DayOfWeek.Tuesday => calendar.Tuesday,
            DayOfWeek.Wednesday => calendar.Wednesday,
            DayOfWeek.Thursday => calendar.Thursday,
            DayOfWeek.Friday => calendar.Friday,
            DayOfWeek.Saturday => calendar.Saturday,
            DayOfWeek.Sunday => calendar.Sunday,
            _ => false
        };
    }

    private async Task<List<GtfsStaticLine>> GetAllLineDataAsync()
    {
        var manifest = await LoadManifestAsync();
        if (manifest is null) return [];

        var lines = new List<GtfsStaticLine>(manifest.Lines.Count);
        foreach (var manifestLine in manifest.Lines)
        {
            var line = await GetLineDataAsync(manifestLine.Number);
            if (line is not null)
                lines.Add(line);
        }

        return lines;
    }

    private async Task<GtfsStaticLine?> GetLineDataAsync(string lineNumber)
    {
        if (string.IsNullOrWhiteSpace(lineNumber)) return null;
        var normalizedLineNumber = lineNumber.Trim();
        if (_lineCache.TryGetValue(normalizedLineNumber, out var cachedLine)) return cachedLine;

        var manifest = await LoadManifestAsync();
        var manifestLine = manifest?.Lines.FirstOrDefault(line =>
            string.Equals(line.Number, normalizedLineNumber, StringComparison.OrdinalIgnoreCase));
        if (manifestLine is null) return null;

        try
        {
            using var response = await _http.GetAsync(manifestLine.File);
            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning(nameof(CpService), $"Static line data failed with status {response.StatusCode}: {manifestLine.File}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            if (!HasCurrentStopTimeSchema(json))
            {
                _logger.Warning(nameof(CpService), $"Static line data has an unsupported stop time schema: {manifestLine.File}");
                return null;
            }

            var line = JsonSerializer.Deserialize<GtfsStaticLine>(json, _jsonOptions);
            if (line is null || line.Version != StaticDataVersion)
            {
                _logger.Warning(nameof(CpService), $"Static line data has an unsupported format: {manifestLine.File}");
                return null;
            }

            _lineCache[normalizedLineNumber] = line;
            return line;
        }
        catch (Exception exception)
        {
            _logger.Error(nameof(CpService), $"Failed to load static line data: {manifestLine.File}", exception);
            return null;
        }
    }

    private async Task<GtfsStaticManifest?> LoadManifestAsync()
    {
        if (_manifestLoaded) return _manifest;
        _manifestLoaded = true;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "manifest.json");
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning(nameof(CpService), $"Static data manifest failed with status {response.StatusCode}.");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var manifest = JsonSerializer.Deserialize<GtfsStaticManifest>(json, _jsonOptions);
            if (manifest is null || manifest.Version != StaticDataVersion)
            {
                _logger.Warning(nameof(CpService), "Static data manifest has an unsupported format.");
                return null;
            }

            return _manifest = manifest;
        }
        catch (Exception exception)
        {
            _logger.Error(nameof(CpService), "Failed to load static data manifest.", exception);
            return null;
        }
    }

    private static string GetDirectionName(GtfsStaticLine line, string routeId)
    {
        var stopsById = line.Stops.ToDictionary(stop => stop.Id, StringComparer.Ordinal);
        var endpoints = GetRouteEndpoints(line, routeId, stopsById);
        if (endpoints is not null)
            return $"{endpoints.Value.First} → {endpoints.Value.Last}";

        var direction = line.Directions.FirstOrDefault(item => item.RouteId == routeId);
        return string.IsNullOrWhiteSpace(direction?.Name) ? routeId : direction.Name;
    }

    private static string CreateDirectionId(string lineNumber, string routeId)
        => string.Join(DirectionIdSeparator, lineNumber, routeId);

    private static bool TryParseDirectionId(string directionId, out string lineNumber, out string routeId)
    {
        lineNumber = string.Empty;
        routeId = string.Empty;
        if (string.IsNullOrWhiteSpace(directionId)) return false;

        var separatorIndex = directionId.IndexOf(DirectionIdSeparator);
        if (separatorIndex <= 0 || separatorIndex == directionId.Length - 1)
            return false;

        lineNumber = directionId[..separatorIndex];
        routeId = directionId[(separatorIndex + 1)..];
        return true;
    }

    private static (string First, string Last)? GetRouteEndpoints(
        GtfsStaticLine line,
        string routeId,
        IReadOnlyDictionary<string, GtfsStaticStop> stopsById)
    {
        var tripIds = line.Trips
            .Where(trip => trip.RouteId == routeId)
            .Select(trip => trip.Id)
            .ToHashSet(StringComparer.Ordinal);
        var sequence = line.StopTimes
            .Where(stopTime => tripIds.Contains(stopTime.TripId))
            .GroupBy(stopTime => stopTime.TripId, StringComparer.Ordinal)
            .Select(group => group.OrderBy(stopTime => stopTime.Sequence).ToList())
            .OrderByDescending(group => group.Count)
            .ThenBy(group => string.Join("\u001f", group.Select(stopTime => stopTime.StopId)), StringComparer.Ordinal)
            .FirstOrDefault();
        if (sequence is null || sequence.Count == 0) return null;

        var first = stopsById.GetValueOrDefault(sequence[0].StopId)?.Name;
        var last = stopsById.GetValueOrDefault(sequence[^1].StopId)?.Name;
        return string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last)
            ? null
            : (first, last);
    }

    private static Uri CreateBaseUri(Uri? currentBaseUri, string configuredBaseUrl)
    {
        if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var absoluteUri))
            return new Uri(absoluteUri.ToString().TrimEnd('/') + "/", UriKind.Absolute);

        var baseUri = currentBaseUri ?? throw new InvalidOperationException(
            "CpService requires an HttpClient BaseAddress when Cp.StaticDataBaseUrl is relative.");
        return new Uri(baseUri, configuredBaseUrl.TrimStart('/').TrimEnd('/') + "/");
    }

    private static int GetScheduledTimeSeconds(GtfsStaticStopTime stopTime)
        => stopTime.DepartureTimeSeconds > 0 ? stopTime.DepartureTimeSeconds : stopTime.ArrivalTimeSeconds;

    private static string FormatGtfsTime(int seconds)
        => TimeSpan.FromSeconds(seconds % (24 * 60 * 60)).ToString(@"hh\:mm", CultureInfo.InvariantCulture);

    private static bool HasCurrentStopTimeSchema(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("stopTimes", out var stopTimes) ||
            stopTimes.ValueKind != JsonValueKind.Array)
            return false;

        return stopTimes.EnumerateArray().All(stopTime =>
            stopTime.ValueKind == JsonValueKind.Object &&
            stopTime.TryGetProperty("arrivalTimeSeconds", out var arrivalTime) &&
            arrivalTime.ValueKind == JsonValueKind.Number &&
            arrivalTime.TryGetInt32(out _) &&
            stopTime.TryGetProperty("departureTimeSeconds", out var departureTime) &&
            departureTime.ValueKind == JsonValueKind.Number &&
            departureTime.TryGetInt32(out _));
    }
}
