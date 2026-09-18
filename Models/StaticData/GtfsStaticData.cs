using System.Text.Json.Serialization;

namespace MyBusApp.Models.StaticData;

public sealed record GtfsStaticManifest(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("lines")] List<GtfsStaticManifestLine> Lines,
    [property: JsonPropertyName("generatedAtUtc")] string GeneratedAtUtc = "",
    [property: JsonPropertyName("feedHash")] string FeedHash = "");

public sealed record GtfsStaticManifestLine(
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("name")] string Name);

public sealed record GtfsStaticLine(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("directions")] List<GtfsStaticDirection> Directions,
    [property: JsonPropertyName("stops")] List<GtfsStaticStop> Stops,
    [property: JsonPropertyName("trips")] List<GtfsStaticTrip> Trips,
    [property: JsonPropertyName("stopTimes")] List<GtfsStaticStopTime> StopTimes,
    [property: JsonPropertyName("calendar")] List<GtfsStaticCalendar> Calendar,
    [property: JsonPropertyName("calendarDates")] List<GtfsStaticCalendarDate> CalendarDates);

public sealed record GtfsStaticDirection(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("routeId")] string RouteId,
    [property: JsonPropertyName("name")] string Name);

public sealed record GtfsStaticStop(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("locality")] string Locality);

public sealed record GtfsStaticTrip(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("routeId")] string RouteId,
    [property: JsonPropertyName("serviceId")] string ServiceId,
    [property: JsonPropertyName("directionId")] string DirectionId,
    [property: JsonPropertyName("headsign")] string Headsign);

public sealed record GtfsStaticStopTime(
    [property: JsonPropertyName("tripId")] string TripId,
    [property: JsonPropertyName("stopId")] string StopId,
    [property: JsonPropertyName("arrivalTimeSeconds")] int ArrivalTimeSeconds,
    [property: JsonPropertyName("departureTimeSeconds")] int DepartureTimeSeconds,
    [property: JsonPropertyName("sequence")] int Sequence);

public sealed record GtfsStaticCalendar(
    [property: JsonPropertyName("serviceId")] string ServiceId,
    [property: JsonPropertyName("startDate")] string StartDate,
    [property: JsonPropertyName("endDate")] string EndDate,
    [property: JsonPropertyName("monday")] bool Monday,
    [property: JsonPropertyName("tuesday")] bool Tuesday,
    [property: JsonPropertyName("wednesday")] bool Wednesday,
    [property: JsonPropertyName("thursday")] bool Thursday,
    [property: JsonPropertyName("friday")] bool Friday,
    [property: JsonPropertyName("saturday")] bool Saturday,
    [property: JsonPropertyName("sunday")] bool Sunday);

public sealed record GtfsStaticCalendarDate(
    [property: JsonPropertyName("serviceId")] string ServiceId,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("exceptionType")] int ExceptionType);