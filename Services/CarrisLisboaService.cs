using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using MyBusApp.Configuration;
using MyBusApp.Models.Domain;
using MyBusApp.Models.StaticData;
using MyBusApp.Utils;

namespace MyBusApp.Services;

public sealed class CarrisLisboaService : IBusService
{
    private const int StaticDataVersion = 2;
    private readonly HttpClient _http;
    private readonly AppLogger _logger;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };
    private readonly Dictionary<string, GtfsStaticLine> _lineCache = new(StringComparer.OrdinalIgnoreCase);
    private GtfsStaticManifest? _manifest;
    private bool _manifestLoaded;

    public BusProvider Provider => BusProvider.CarrisLisboa;

    public CarrisLisboaService(ApiSettings settings, AppLogger? logger = null)
        : this(settings, new HttpClient { BaseAddress = new Uri("http://localhost/", UriKind.Absolute) }, logger)
    {
    }

    public CarrisLisboaService(ApiSettings settings, HttpClient httpClient, AppLogger? logger = null)
    {
        _http = httpClient;
        _logger = logger ?? new AppLogger();
        var configuredBaseUrl = string.IsNullOrWhiteSpace(settings.Carris.StaticDataBaseUrl)
            ? "data/carris-lisboa/"
            : settings.Carris.StaticDataBaseUrl;
        _http.BaseAddress = CreateBaseUri(_http.BaseAddress, configuredBaseUrl);
        _logger.Info(nameof(CarrisLisboaService), $"Initialized with static data URL {_http.BaseAddress}");
    }

    public async Task<BusLine?> GetLineAsync(string lineNumber)
    {
        var line = await GetLineDataAsync(lineNumber);
        return line is null ? null : new BusLine(line.Number, line.Number, line.Name, "#dc3545", Provider);
    }

    public async Task<List<BusDirection>> GetDirectionsAsync(string lineId)
    {
        var line = await GetLineDataAsync(lineId);
        return line?.Directions
            .Select(direction => new BusDirection(
                direction.Id,
                string.IsNullOrWhiteSpace(direction.Name) ? line.Name : direction.Name))
            .ToList() ?? [];
    }

    public async Task<List<BusStop>> GetStopsAsync(string directionId, string lineId)
    {
        var line = await GetLineDataAsync(lineId);
        var direction = line?.Directions.FirstOrDefault(item => item.Id == directionId);
        if (line is null || direction is null) return [];

        var directionKey = direction.Id[(direction.Id.IndexOf('|') + 1)..];
        var tripIds = line.Trips
            .Where(trip => trip.RouteId == direction.RouteId && trip.DirectionId == directionKey)
            .Select(trip => trip.Id)
            .ToHashSet(StringComparer.Ordinal);
        var stopsById = line.Stops.ToDictionary(stop => stop.Id, StringComparer.Ordinal);
        var stopTimes = line.StopTimes
            .Where(stopTime => tripIds.Contains(stopTime.TripId))
            .GroupBy(stopTime => stopTime.TripId)
            .SelectMany(group => group.OrderBy(stopTime => stopTime.Sequence))
            .GroupBy(stopTime => stopTime.StopId)
            .Select(group => group.OrderBy(stopTime => stopTime.Sequence).First())
            .OrderBy(stopTime => stopTime.Sequence);

        return stopTimes
            .Where(stopTime => stopsById.ContainsKey(stopTime.StopId))
            .Select(stopTime => stopsById[stopTime.StopId])
            .Select(stop => new BusStop(stop.Id, stop.Name, stop.Locality))
            .ToList();
    }

    public async Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId)
    {
        var line = await GetLineDataAsync(lineId);
        if (line is null) return [];

        try
        {
            var lisbonZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");
            var now = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, lisbonZone);
            return GetUpcomingArrivals(line, stopId, now);
        }
        catch (Exception exception)
        {
            _logger.Error(nameof(CarrisLisboaService), $"Failed to filter static arrivals for stop '{stopId}'.", exception);
            return [];
        }
    }

    public static List<BusArrival> GetUpcomingArrivals(
        GtfsStaticLine line,
        string stopId,
        DateTimeOffset now)
    {
        var tripsById = line.Trips.ToDictionary(trip => trip.Id, StringComparer.Ordinal);
        var candidates = new List<(DateTime ScheduledAt, GtfsStaticTrip Trip, GtfsStaticStopTime StopTime, int TimeSeconds)>();

        for (var offset = -1; offset <= 1; offset++)
        {
            var serviceDate = now.Date.AddDays(offset);
            foreach (var stopTime in line.StopTimes.Where(item => item.StopId == stopId))
            {
                if (!tripsById.TryGetValue(stopTime.TripId, out var trip) ||
                    !IsServiceActive(line, trip.ServiceId, serviceDate))
                    continue;

                var timeSeconds = GetScheduledTimeSeconds(stopTime);
                var scheduledAt = serviceDate.AddSeconds(timeSeconds);
                if (scheduledAt > now.DateTime)
                    candidates.Add((scheduledAt, trip, stopTime, timeSeconds));
            }
        }

        return candidates
            .OrderBy(candidate => candidate.ScheduledAt)
            .Take(5)
            .Select(candidate => new BusArrival(
                line.Number,
                string.IsNullOrWhiteSpace(candidate.Trip.Headsign) ? line.Name : candidate.Trip.Headsign,
                FormatGtfsTime(candidate.TimeSeconds),
                false))
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
                _logger.Warning(nameof(CarrisLisboaService), $"Static line data failed with status {response.StatusCode}: {manifestLine.File}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            if (!HasCurrentStopTimeSchema(json))
            {
                _logger.Warning(nameof(CarrisLisboaService), $"Static line data has an unsupported stop time schema: {manifestLine.File}");
                return null;
            }

            var line = JsonSerializer.Deserialize<GtfsStaticLine>(json, _jsonOptions);
            if (line is null || line.Version != StaticDataVersion)
            {
                _logger.Warning(nameof(CarrisLisboaService), $"Static line data has an unsupported format: {manifestLine.File}");
                return null;
            }

            _lineCache[normalizedLineNumber] = line;
            return line;
        }
        catch (Exception exception)
        {
            _logger.Error(nameof(CarrisLisboaService), $"Failed to load static line data: {manifestLine.File}", exception);
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
                _logger.Warning(nameof(CarrisLisboaService), $"Static data manifest failed with status {response.StatusCode}.");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var manifest = JsonSerializer.Deserialize<GtfsStaticManifest>(json, _jsonOptions);
            if (manifest is null || manifest.Version != StaticDataVersion)
            {
                _logger.Warning(nameof(CarrisLisboaService), "Static data manifest has an unsupported format.");
                return null;
            }

            return _manifest = manifest;
        }
        catch (Exception exception)
        {
            _logger.Error(nameof(CarrisLisboaService), "Failed to load static data manifest.", exception);
            return null;
        }
    }

    private static Uri CreateBaseUri(Uri? currentBaseUri, string configuredBaseUrl)
    {
        if (Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var absoluteUri))
            return new Uri(absoluteUri.ToString().TrimEnd('/') + "/", UriKind.Absolute);

        var baseUri = currentBaseUri ?? new Uri("http://localhost/", UriKind.Absolute);
        return new Uri(baseUri, configuredBaseUrl.TrimStart('/').TrimEnd('/') + "/");
    }

    private static string FormatGtfsTime(int seconds)
    {
        var timeOfDaySeconds = seconds % (24 * 60 * 60);
        return TimeSpan.FromSeconds(timeOfDaySeconds).ToString(@"hh\:mm", CultureInfo.InvariantCulture);
    }

    private static bool HasCurrentStopTimeSchema(string json)
    {
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("stopTimes", out var stopTimes) ||
            stopTimes.ValueKind != JsonValueKind.Array)
            return false;

        return stopTimes.EnumerateArray().All(stopTime =>
            stopTime.TryGetProperty("arrivalTimeSeconds", out var arrivalTime) &&
            arrivalTime.ValueKind == JsonValueKind.Number &&
            stopTime.TryGetProperty("departureTimeSeconds", out var departureTime) &&
            departureTime.ValueKind == JsonValueKind.Number);
    }

    private static int GetScheduledTimeSeconds(GtfsStaticStopTime stopTime)
        => stopTime.DepartureTimeSeconds > 0 ? stopTime.DepartureTimeSeconds : stopTime.ArrivalTimeSeconds;
}