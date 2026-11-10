using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using MyBusApp.Configuration;
using MyBusApp.Models.Domain;
using MyBusApp.Models.StaticData;
using MyBusApp.Utils;

namespace MyBusApp.Services;

public class GtfsTrainService : ITrainService
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
    private readonly string _operatorName;
    private GtfsStaticManifest? _manifest;
    private bool _manifestLoaded;

    public GtfsTrainService(HttpClient httpClient, string operatorName, string staticDataBaseUrl, AppLogger? logger = null)
    {
        _http = httpClient;
        _operatorName = operatorName;
        _logger = logger ?? new AppLogger();
        _http.BaseAddress = CreateBaseUri(_http.BaseAddress, staticDataBaseUrl);
        _logger.Info(nameof(GtfsTrainService), $"Initialized {operatorName} with static data URL {_http.BaseAddress}");
    }

    public string OperatorName => _operatorName;

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

            foreach (var direction in line.Directions)
            {
                var directionKey = GetDirectionKey(direction);
                var servesStation = line.Trips.Any(trip =>
                    string.Equals(trip.RouteId, direction.RouteId, StringComparison.Ordinal) &&
                    string.Equals(GetTripDirectionKey(trip), directionKey, StringComparison.Ordinal) &&
                    tripIdsAtStation.Contains(trip.Id));
                if (!servesStation) continue;

                var directionId = CreateDirectionId(line, direction);
                destinations.TryAdd(directionId, new TrainDirection(directionId, GetDirectionName(line, direction)));
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
            !TryParseDirectionId(directionId, out var lineNumber, out var routeId, out var directionKey))
            return [];

        var lisbonZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");
        var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, lisbonZone);
        var candidates = new List<(DateTime ScheduledAt, string TripId, TrainDeparture Departure)>();

        foreach (var line in await GetAllLineDataAsync())
        {
            if (!string.Equals(line.Number, lineNumber, StringComparison.Ordinal)) continue;

            var trainLine = new TrainLine(line.Number, line.Name);
            var direction = line.Directions.FirstOrDefault(item =>
                string.Equals(item.RouteId, routeId, StringComparison.Ordinal) &&
                (directionKey is null || string.Equals(GetDirectionKey(item), directionKey, StringComparison.Ordinal)));
            var directionName = direction is null ? routeId : GetDirectionName(line, direction);
            var tripsById = line.Trips
                .Where(trip => string.Equals(trip.RouteId, routeId, StringComparison.Ordinal) &&
                    (directionKey is null || string.Equals(GetTripDirectionKey(trip), directionKey, StringComparison.Ordinal)))
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

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(manifestLine.File);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or IOException)
        {
            _logger.Error(nameof(GtfsTrainService), $"Failed to load static line data: {manifestLine.File}", exception);
            throw new TransitSourceUnavailableException(
                $"CP static data is unavailable for line '{normalizedLineNumber}'.",
                exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning(nameof(GtfsTrainService), $"Static line data failed with status {response.StatusCode}: {manifestLine.File}");
                throw new TransitSourceUnavailableException(
                    $"CP static data returned HTTP {(int)response.StatusCode} for line '{normalizedLineNumber}'.");
            }
            string json;
            try
            {
                json = await response.Content.ReadAsStringAsync();
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or IOException)
            {
                _logger.Error(nameof(GtfsTrainService), $"Failed to read static line data: {manifestLine.File}", exception);
                throw new TransitSourceUnavailableException(
                    $"CP static data is unavailable for line '{normalizedLineNumber}'.",
                    exception);
            }
            if (!HasCurrentStopTimeSchema(json))
            {
                _logger.Warning(nameof(GtfsTrainService), $"Static line data has an unsupported stop time schema: {manifestLine.File}");
                throw new InvalidDataException($"CP static line data has an unsupported stop time schema: {manifestLine.File}");
            }

            var line = JsonSerializer.Deserialize<GtfsStaticLine>(json, _jsonOptions);
            if (line is null || line.Version != StaticDataVersion)
            {
                _logger.Warning(nameof(GtfsTrainService), $"Static line data has an unsupported format: {manifestLine.File}");
                throw new InvalidDataException($"CP static line data has an unsupported format: {manifestLine.File}");
            }

            _lineCache[normalizedLineNumber] = line;
            return line;
        }
    }

    private async Task<GtfsStaticManifest?> LoadManifestAsync()
    {
        if (_manifestLoaded) return _manifest;

        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "manifest.json");
            request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
            response = await _http.SendAsync(request);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or IOException)
        {
            _logger.Error(nameof(GtfsTrainService), "Failed to load static data manifest.", exception);
            throw new TransitSourceUnavailableException("CP static data manifest is unavailable.", exception);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                _logger.Warning(nameof(GtfsTrainService), $"Static data manifest failed with status {response.StatusCode}.");
                throw new TransitSourceUnavailableException(
                    $"CP static data manifest returned HTTP {(int)response.StatusCode}.");
            }

            string json;
            try
            {
                json = await response.Content.ReadAsStringAsync();
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or IOException)
            {
                _logger.Error(nameof(GtfsTrainService), "Failed to read static data manifest.", exception);
                throw new TransitSourceUnavailableException(
                    "CP static data manifest is unavailable.",
                    exception);
            }
            var manifest = JsonSerializer.Deserialize<GtfsStaticManifest>(json, _jsonOptions);
            if (manifest is null || manifest.Version != StaticDataVersion || manifest.Lines is null)
            {
                _logger.Warning(nameof(GtfsTrainService), "Static data manifest has an unsupported format.");
                throw new InvalidDataException("CP static data manifest has an unsupported format.");
            }

            _manifestLoaded = true;
            return _manifest = manifest;
        }
    }

    private static string GetDirectionName(GtfsStaticLine line, GtfsStaticDirection direction)
    {
        var stopsById = line.Stops.ToDictionary(stop => stop.Id, StringComparer.Ordinal);
        var directionKey = GetDirectionKey(direction);
        var tripIds = line.Trips
            .Where(trip => string.Equals(trip.RouteId, direction.RouteId, StringComparison.Ordinal) &&
                string.Equals(GetTripDirectionKey(trip), directionKey, StringComparison.Ordinal))
            .Select(trip => trip.Id)
            .ToHashSet(StringComparer.Ordinal);
        var endpoints = GetEndpoints(line, tripIds, stopsById);
        if (endpoints is not null)
            return $"{endpoints.Value.First} → {endpoints.Value.Last}";

        if (!string.IsNullOrWhiteSpace(direction.Name)) return direction.Name;
        return string.IsNullOrWhiteSpace(line.Name) ? direction.RouteId : line.Name;
    }

    private static string GetTripDirectionKey(GtfsStaticTrip trip)
        => !string.IsNullOrWhiteSpace(trip.DirectionId)
            ? trip.DirectionId
            : !string.IsNullOrWhiteSpace(trip.Headsign) ? trip.Headsign : "unknown";

    private static string GetDirectionKey(GtfsStaticDirection direction)
    {
        var separatorIndex = direction.Id.LastIndexOf('|');
        return separatorIndex >= 0 && separatorIndex < direction.Id.Length - 1
            ? direction.Id[(separatorIndex + 1)..]
            : direction.Id;
    }

    private static string CreateDirectionId(GtfsStaticLine line, GtfsStaticDirection direction)
    {
        var routeHasMultipleDirections = line.Directions
            .Count(item => string.Equals(item.RouteId, direction.RouteId, StringComparison.Ordinal)) > 1;
        return routeHasMultipleDirections
            ? string.Join(DirectionIdSeparator, line.Number, direction.Id)
            : string.Join(DirectionIdSeparator, line.Number, direction.RouteId);
    }

    private static bool TryParseDirectionId(
        string directionId,
        out string lineNumber,
        out string routeId,
        out string? directionKey)
    {
        lineNumber = string.Empty;
        routeId = string.Empty;
        directionKey = null;
        if (string.IsNullOrWhiteSpace(directionId)) return false;

        var separatorIndex = directionId.IndexOf(DirectionIdSeparator);
        if (separatorIndex <= 0 || separatorIndex == directionId.Length - 1)
            return false;

        lineNumber = directionId[..separatorIndex];
        var remainder = directionId[(separatorIndex + 1)..];
        var directionSeparatorIndex = remainder.LastIndexOf('|');
        if (directionSeparatorIndex > 0 && directionSeparatorIndex < remainder.Length - 1)
        {
            routeId = remainder[..directionSeparatorIndex];
            directionKey = remainder[(directionSeparatorIndex + 1)..];
        }
        else
        {
            routeId = remainder;
        }

        return !string.IsNullOrWhiteSpace(routeId);
    }

    private static (string First, string Last)? GetEndpoints(
        GtfsStaticLine line,
        IReadOnlySet<string> tripIds,
        IReadOnlyDictionary<string, GtfsStaticStop> stopsById)
    {
        var sequence = line.StopTimes
            .Where(stopTime => tripIds.Contains(stopTime.TripId))
            .GroupBy(stopTime => stopTime.TripId, StringComparer.Ordinal)
            .Select(group => group.OrderBy(stopTime => stopTime.Sequence).ToList())
            .OrderByDescending(group => group.Count)
            .ThenBy(group => string.Join("\u001f", group.Select(stopTime => stopTime.StopId)), StringComparer.Ordinal)
            .FirstOrDefault();
        if (sequence is null || sequence.Count < 2) return null;

        var first = stopsById.GetValueOrDefault(sequence[0].StopId)?.Name;
        var last = stopsById.GetValueOrDefault(sequence[^1].StopId)?.Name;
        return string.IsNullOrWhiteSpace(first) ||
               string.IsNullOrWhiteSpace(last) ||
               string.Equals(first, last, StringComparison.Ordinal)
            ? null
            : (first, last);
    }

    private static Uri CreateBaseUri(Uri? currentBaseUri, string configuredBaseUrl)
    {
        if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var absoluteUri))
            return new Uri(absoluteUri.ToString().TrimEnd('/') + "/", UriKind.Absolute);

        var baseUri = currentBaseUri ?? throw new InvalidOperationException(
            "GtfsTrainService requires an HttpClient BaseAddress when the static data URL is relative.");
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
