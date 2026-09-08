using System.Globalization;
using System.IO.Compression;
using System.Text;
using MyBusApp.Configuration;
using MyBusApp.Models.Domain;

namespace MyBusApp.Services;

public sealed class CarrisLisboaService : IBusService
{
    private readonly HttpClient _http;
    private readonly MyBusApp.Services.AppLogger _logger;
    private GtfsData? _data;

    public BusProvider Provider => BusProvider.CarrisLisboa;

    public CarrisLisboaService(ApiSettings settings, MyBusApp.Services.AppLogger? logger = null)
    {
        _http = new HttpClient { BaseAddress = new Uri(settings.Carris.BaseUrl.TrimEnd('/') + "/") };
        _logger = logger ?? new MyBusApp.Services.AppLogger();
        _logger.Info(nameof(CarrisLisboaService), $"Initialized with GTFS URL {_http.BaseAddress}");
    }

    public async Task<BusLine?> GetLineAsync(string lineNumber)
    {
        var data = await LoadAsync();
        var route = data.Routes.FirstOrDefault(r => r.ShortName.Equals(lineNumber.Trim(), StringComparison.OrdinalIgnoreCase));
        return route is null ? null : new BusLine(route.Id, route.ShortName, route.LongName, "#dc3545", Provider);
    }

    public async Task<List<BusDirection>> GetDirectionsAsync(string lineId)
    {
        var data = await LoadAsync();
        var route = data.Routes.FirstOrDefault(r => r.Id == lineId || r.ShortName == lineId);
        if (route is null) return [];

        var routeIds = data.Routes.Where(r => r.ShortName == route.ShortName).Select(r => r.Id).ToHashSet();
        return data.Trips.Where(t => routeIds.Contains(t.RouteId))
            .GroupBy(t => new { t.RouteId, Direction = t.DirectionId ?? t.HeadSign ?? "unknown" })
            .Select(g => new BusDirection($"{g.Key.RouteId}|{g.Key.Direction}", g.First().HeadSign ?? route.LongName))
            .ToList();
    }

    public async Task<List<BusStop>> GetStopsAsync(string directionId, string lineId)
    {
        var data = await LoadAsync();
        var route = data.Routes.FirstOrDefault(r => r.Id == lineId || r.ShortName == lineId);
        if (route is null) return [];
        var direction = directionId.Split('|', 2);
        var directionRouteId = direction.Length == 2 ? direction[0] : route.Id;
        var directionName = direction.Length == 2 ? direction[1] : directionId;
        var tripIds = data.Trips.Where(t => t.RouteId == directionRouteId && (directionName == (t.DirectionId ?? t.HeadSign ?? "unknown"))).Select(t => t.Id).ToHashSet();
        var trip = data.StopTimes.Where(s => tripIds.Contains(s.TripId)).GroupBy(s => s.TripId).FirstOrDefault();
        return trip?.OrderBy(s => s.Sequence).Select(s => data.Stops.FirstOrDefault(stop => stop.Id == s.StopId)).Where(s => s is not null).Select(s => new BusStop(s!.Id, s.Name, s.Locality)).ToList() ?? [];
    }

    public async Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId)
    {
        var data = await LoadAsync();
        var route = data.Routes.FirstOrDefault(r => r.Id == lineId || r.ShortName == lineId);
        if (route is null) return [];
        var now = DateTime.Now.TimeOfDay;
        return data.StopTimes.Where(s => s.StopId == stopId && s.Time >= now && data.Trips.Any(t => t.Id == s.TripId && t.RouteId == route.Id)).OrderBy(s => s.Time).Take(5).Select(s => { var trip = data.Trips.First(t => t.Id == s.TripId); return new BusArrival(route.ShortName, trip.HeadSign ?? route.LongName, s.Time.ToString(@"hh\:mm"), false); }).ToList();
    }

    private async Task<GtfsData> LoadAsync()
    {
        if (_data is not null) return _data;
        try
        {
            _logger.ApiRequest(nameof(CarrisLisboaService), _http.BaseAddress, string.Empty);
            await using var stream = await _http.GetStreamAsync(string.Empty);
            _logger.Info(nameof(CarrisLisboaService), "GTFS download completed. Reading archive files.");
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var data = new GtfsData(ReadRoutes(archive), ReadTrips(archive), ReadStops(archive), ReadStopTimes(archive));
            _logger.Info(nameof(CarrisLisboaService), $"GTFS loaded: routes={data.Routes.Count}, trips={data.Trips.Count}, stops={data.Stops.Count}, stop_times={data.StopTimes.Count}.");
            return _data = data;
        }
        catch (Exception exception)
        {
            _logger.Error(nameof(CarrisLisboaService), "Failed to load GTFS archive", exception);
            return _data = new GtfsData([], [], [], []);
        }
    }

    private static List<GtfsRoute> ReadRoutes(ZipArchive a) => ReadCsv(a, "routes.txt").Skip(1).Select(Csv).Where(c => c.Count > 3).Select(c => new GtfsRoute(c[0], c[2], c[3])).ToList();
    private static List<GtfsTrip> ReadTrips(ZipArchive a) => ReadCsv(a, "trips.txt").Skip(1).Select(Csv).Where(c => c.Count > 6).Select(c => new GtfsTrip(c[2], c[0], c[5], c[3])).ToList();
    private static List<GtfsStop> ReadStops(ZipArchive a) => ReadCsv(a, "stops.txt").Skip(1).Select(Csv).Where(c => c.Count > 2).Select(c => new GtfsStop(c[0], c[2], c.Count > 7 ? c[7] : string.Empty)).ToList();
    private static List<GtfsStopTime> ReadStopTimes(ZipArchive a) => ReadCsv(a, "stop_times.txt").Skip(1).Select(Csv).Where(c => c.Count > 5 && TimeSpan.TryParse(c[1], CultureInfo.InvariantCulture, out _)).Select(c => new GtfsStopTime(c[0], c[3], TimeSpan.Parse(c[1], CultureInfo.InvariantCulture), int.TryParse(c[4], out var n) ? n : 0)).ToList();

    private static IEnumerable<string> ReadCsv(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name);
        if (entry is null) return [];
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        var lines = new List<string>();
        while (reader.ReadLine() is { } line) lines.Add(line);
        return lines;
    }

    private static List<string> Csv(string line)
    {
        var values = new List<string>(); var value = new StringBuilder(); var quoted = false;
        foreach (var character in line)
        {
            if (character == '"') quoted = !quoted;
            else if (character == ',' && !quoted) { values.Add(value.ToString()); value.Clear(); }
            else value.Append(character);
        }
        values.Add(value.ToString()); return values;
    }

    private sealed record GtfsData(List<GtfsRoute> Routes, List<GtfsTrip> Trips, List<GtfsStop> Stops, List<GtfsStopTime> StopTimes);
    private sealed record GtfsRoute(string Id, string ShortName, string LongName);
    private sealed record GtfsTrip(string Id, string RouteId, string? DirectionId, string? HeadSign);
    private sealed record GtfsStop(string Id, string Name, string Locality);
    private sealed record GtfsStopTime(string TripId, string StopId, TimeSpan Time, int Sequence);
}
