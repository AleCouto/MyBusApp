using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyBusApp.Models.StaticData;

namespace MyBusApp.Tools.CarrisLisboaDataGenerator;

public sealed class CarrisLisboaStaticDataGenerator
{
    private const int DataVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public void Generate(string gtfsZipPath, string outputDirectory)
    {
        if (!File.Exists(gtfsZipPath))
            throw new FileNotFoundException("GTFS ZIP not found.", gtfsZipPath);

        using var archive = ZipFile.OpenRead(gtfsZipPath);
        var routes = ReadRoutes(archive);
        var trips = ReadTrips(archive);
        var stops = ReadStops(archive);
        var calendars = ReadCalendars(archive);
        var calendarDates = ReadCalendarDates(archive);
        var linesDirectory = Path.Combine(outputDirectory, "lines");
        Directory.CreateDirectory(linesDirectory);

        var builders = routes
            .Where(route => !string.IsNullOrWhiteSpace(route.ShortName))
            .GroupBy(route => route.ShortName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => new LineBuilder(group.Key, group.ToList()),
                StringComparer.OrdinalIgnoreCase);
        var buildersByRouteId = builders.Values
            .SelectMany(builder => builder.Routes.Select(route => new { route.Id, builder }))
            .ToDictionary(item => item.Id, item => item.builder, StringComparer.Ordinal);
        var tripsById = trips.ToDictionary(trip => trip.Id, StringComparer.Ordinal);
        foreach (var trip in trips)
        {
            if (buildersByRouteId.TryGetValue(trip.RouteId, out var builder))
                builder.Trips.Add(trip);
        }

        ReadStopTimesStreaming(archive, tripsById, buildersByRouteId);

        var manifestLines = new List<CarrisLisboaStaticManifestLine>();
        var feedHash = ComputeFeedHash(gtfsZipPath);
        foreach (var builder in builders.Values.OrderBy(item => item.Number, StringComparer.OrdinalIgnoreCase))
        {
            var stopIds = builder.StopTimes.Select(stopTime => stopTime.StopId).ToHashSet(StringComparer.Ordinal);
            var lineStops = stops.Where(stop => stopIds.Contains(stop.Id)).ToList();
            var serviceIds = builder.Trips.Select(trip => trip.ServiceId).ToHashSet(StringComparer.Ordinal);
            var lineCalendars = calendars.Where(calendar => serviceIds.Contains(calendar.ServiceId)).ToList();
            var lineCalendarDates = calendarDates.Where(date => serviceIds.Contains(date.ServiceId)).ToList();
            var routesById = builder.Routes.ToDictionary(route => route.Id, StringComparer.Ordinal);

            var directions = builder.Trips
                .GroupBy(trip => new
                {
                    trip.RouteId,
                    Direction = FirstNonEmpty(trip.DirectionId, trip.Headsign, "unknown")
                })
                .Select(group => new CarrisLisboaStaticDirection(
                    $"{group.Key.RouteId}|{group.Key.Direction}",
                    group.Key.RouteId,
                    FirstNonEmpty(routesById[group.Key.RouteId].LongName, group.First().Headsign, group.Key.Direction)))
                .OrderBy(direction => direction.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var line = new CarrisLisboaStaticLine(
                DataVersion,
                builder.Number,
                FirstNonEmpty(builder.Routes[0].LongName, builder.Number),
                directions,
                lineStops.Select(stop => new CarrisLisboaStaticStop(stop.Id, stop.Name, stop.Locality)).ToList(),
                builder.Trips.Select(trip => new CarrisLisboaStaticTrip(
                    trip.Id,
                    trip.RouteId,
                    trip.ServiceId,
                    FirstNonEmpty(trip.DirectionId, trip.Headsign, "unknown"),
                    FirstNonEmpty(trip.Headsign, routesById[trip.RouteId].LongName, builder.Number))).ToList(),
                builder.StopTimes.Select(stopTime => new CarrisLisboaStaticStopTime(
                    stopTime.TripId,
                    stopTime.StopId,
                    stopTime.TimeSeconds,
                    stopTime.Sequence)).ToList(),
                lineCalendars.Select(calendar => new CarrisLisboaStaticCalendar(
                    calendar.ServiceId,
                    calendar.StartDate,
                    calendar.EndDate,
                    calendar.Monday,
                    calendar.Tuesday,
                    calendar.Wednesday,
                    calendar.Thursday,
                    calendar.Friday,
                    calendar.Saturday,
                    calendar.Sunday)).ToList(),
                lineCalendarDates.Select(date => new CarrisLisboaStaticCalendarDate(
                    date.ServiceId,
                    date.Date,
                    date.ExceptionType)).ToList());

            var fileName = $"{SanitizeFileName(builder.Number)}.{feedHash}.json";
            var relativeFile = $"lines/{fileName}";
            File.WriteAllText(Path.Combine(linesDirectory, fileName), JsonSerializer.Serialize(line, JsonOptions));
            manifestLines.Add(new CarrisLisboaStaticManifestLine(builder.Number, relativeFile, line.Name));
        }

        var manifest = new CarrisLisboaStaticManifest(
            DataVersion,
            manifestLines,
            DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            feedHash);
        File.WriteAllText(Path.Combine(outputDirectory, "manifest.json"), JsonSerializer.Serialize(manifest, JsonOptions));
        Console.WriteLine($"Generated {manifestLines.Count} line files in {Path.GetFullPath(outputDirectory)}.");
    }

    private static List<GtfsRoute> ReadRoutes(ZipArchive archive)
    {
        var rows = ReadTable(archive, "routes.txt");
        return rows.Select(row => new GtfsRoute(
                Required(row, "route_id", "routes.txt"),
                Required(row, "route_short_name", "routes.txt"),
                Get(row, "route_long_name") ?? Get(row, "route_short_name") ?? string.Empty))
            .ToList();
    }

    private static List<GtfsTrip> ReadTrips(ZipArchive archive)
    {
        var rows = ReadTable(archive, "trips.txt");
        return rows.Select(row => new GtfsTrip(
                Required(row, "trip_id", "trips.txt"),
                Required(row, "route_id", "trips.txt"),
                Required(row, "service_id", "trips.txt"),
                Get(row, "direction_id") ?? string.Empty,
                Get(row, "trip_headsign") ?? string.Empty))
            .ToList();
    }

    private static List<GtfsStop> ReadStops(ZipArchive archive)
    {
        var rows = ReadTable(archive, "stops.txt");
        return rows.Select(row => new GtfsStop(
                Required(row, "stop_id", "stops.txt"),
                Get(row, "stop_name") ?? string.Empty,
                Get(row, "stop_desc") ?? string.Empty))
            .ToList();
    }

    private static void ReadStopTimesStreaming(
        ZipArchive archive,
        IReadOnlyDictionary<string, GtfsTrip> tripsById,
        IReadOnlyDictionary<string, LineBuilder> buildersByRouteId)
    {
        var entry = archive.GetEntry("stop_times.txt")
            ?? throw new InvalidDataException("Required GTFS file 'stop_times.txt' is missing.");
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
            throw new InvalidDataException("GTFS file 'stop_times.txt' has no header.");

        var headers = ParseCsv(headerLine);
        var tripIndex = RequiredIndex(headers, "trip_id", "stop_times.txt");
        var stopIndex = RequiredIndex(headers, "stop_id", "stop_times.txt");
        var arrivalIndex = RequiredIndex(headers, "arrival_time", "stop_times.txt");
        var sequenceIndex = RequiredIndex(headers, "stop_sequence", "stop_times.txt");

        while (reader.ReadLine() is { } line)
        {
            var values = ParseCsv(line);
            if (tripIndex >= values.Count || !tripsById.TryGetValue(values[tripIndex], out var trip) ||
                !buildersByRouteId.TryGetValue(trip.RouteId, out var builder))
                continue;

            if (stopIndex >= values.Count || arrivalIndex >= values.Count || sequenceIndex >= values.Count)
                throw new InvalidDataException("GTFS file 'stop_times.txt' has an incomplete row.");

            builder.StopTimes.Add(new GtfsStopTime(
                trip.Id,
                values[stopIndex],
                ParseGtfsTime(values[arrivalIndex]),
                ParseInteger(values[sequenceIndex], "stop_sequence")));
        }
    }

    private static List<GtfsCalendar> ReadCalendars(ZipArchive archive)
    {
        var rows = ReadOptionalTable(archive, "calendar.txt");
        return rows.Select(row => new GtfsCalendar(
                Required(row, "service_id", "calendar.txt"),
                Required(row, "start_date", "calendar.txt"),
                Required(row, "end_date", "calendar.txt"),
                IsActive(row, "monday"), IsActive(row, "tuesday"), IsActive(row, "wednesday"),
                IsActive(row, "thursday"), IsActive(row, "friday"), IsActive(row, "saturday"), IsActive(row, "sunday")))
            .ToList();
    }

    private static List<GtfsCalendarDate> ReadCalendarDates(ZipArchive archive)
    {
        var rows = ReadOptionalTable(archive, "calendar_dates.txt");
        return rows.Select(row => new GtfsCalendarDate(
                Required(row, "service_id", "calendar_dates.txt"),
                Required(row, "date", "calendar_dates.txt"),
                ParseInteger(Required(row, "exception_type", "calendar_dates.txt"), "exception_type")))
            .ToList();
    }

    private static List<Dictionary<string, string>> ReadTable(ZipArchive archive, string name)
    {
        var rows = ReadOptionalTable(archive, name);
        if (rows.Count == 0 && archive.GetEntry(name) is null)
            throw new InvalidDataException($"Required GTFS file '{name}' is missing.");
        return rows;
    }

    private static List<Dictionary<string, string>> ReadOptionalTable(ZipArchive archive, string name)
    {
        var entry = archive.GetEntry(name);
        if (entry is null) return [];

        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine)) return [];
        var headers = ParseCsv(headerLine);
        var rows = new List<Dictionary<string, string>>();
        while (reader.ReadLine() is { } line)
        {
            var values = ParseCsv(line);
            var row = headers
                .Select((header, index) => new { header, value = index < values.Count ? values[index] : string.Empty })
                .ToDictionary(item => item.header, item => item.value, StringComparer.OrdinalIgnoreCase);
            rows.Add(row);
        }

        return rows;
    }

    private static string Required(IReadOnlyDictionary<string, string> row, string name, string file)
        => Get(row, name) is { Length: > 0 } value
            ? value
            : throw new InvalidDataException($"GTFS file '{file}' has a row without '{name}'.");

    private static string? Get(IReadOnlyDictionary<string, string> row, string name)
        => row.TryGetValue(name, out var value) ? value.Trim() : null;

    private static bool IsActive(IReadOnlyDictionary<string, string> row, string name)
        => Get(row, name) == "1";

    private static int ParseInteger(string value, string field)
        => int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new InvalidDataException($"Invalid GTFS {field} value '{value}'.");

    private static int RequiredIndex(IReadOnlyList<string> headers, string name, string file)
    {
        for (var index = 0; index < headers.Count; index++)
        {
            if (string.Equals(headers[index], name, StringComparison.OrdinalIgnoreCase))
                return index;
        }

        throw new InvalidDataException($"GTFS file '{file}' is missing '{name}'.");
    }

    private static int ParseGtfsTime(string value)
    {
        var parts = value.Split(':');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var hours) ||
            !int.TryParse(parts[1], out var minutes) || !int.TryParse(parts[2], out var seconds) ||
            minutes is < 0 or > 59 || seconds is < 0 or > 59)
        {
            throw new InvalidDataException($"Invalid GTFS time value '{value}'.");
        }

        return checked(hours * 3600 + minutes * 60 + seconds);
    }

    private static string FirstNonEmpty(params string[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

    private static string SanitizeFileName(string value)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "line" : sanitized;
    }

    private static string ComputeFeedHash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant()[..16];
    }

    private static List<string> ParseCsv(string line)
    {
        var values = new List<string>();
        var value = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < line.Length; index++)
        {
            var character = line[index];
            if (character == '"' && quoted && index + 1 < line.Length && line[index + 1] == '"')
            {
                value.Append('"');
                index++;
            }
            else if (character == '"')
            {
                quoted = !quoted;
            }
            else if (character == ',' && !quoted)
            {
                values.Add(value.ToString().Trim());
                value.Clear();
            }
            else
            {
                value.Append(character);
            }
        }

        values.Add(value.ToString().Trim());
        return values;
    }

    private sealed record GtfsRoute(string Id, string ShortName, string LongName);
    private sealed record GtfsTrip(string Id, string RouteId, string ServiceId, string DirectionId, string Headsign);
    private sealed record GtfsStop(string Id, string Name, string Locality);
    private readonly record struct GtfsStopTime(string TripId, string StopId, int TimeSeconds, int Sequence);
    private sealed record GtfsCalendar(string ServiceId, string StartDate, string EndDate, bool Monday, bool Tuesday, bool Wednesday, bool Thursday, bool Friday, bool Saturday, bool Sunday);
    private sealed record GtfsCalendarDate(string ServiceId, string Date, int ExceptionType);

    private sealed class LineBuilder(string number, List<GtfsRoute> routes)
    {
        public string Number { get; } = number;
        public List<GtfsRoute> Routes { get; } = routes;
        public List<GtfsTrip> Trips { get; } = [];
        public List<GtfsStopTime> StopTimes { get; } = [];
    }
}