using System.Text.Json.Serialization;

namespace MyBusApp.Models.StaticData;

public sealed record CarrisLisboaStaticManifest(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("lines")] List<CarrisLisboaStaticManifestLine> Lines,
    [property: JsonPropertyName("generatedAtUtc")] string GeneratedAtUtc = "",
    [property: JsonPropertyName("feedHash")] string FeedHash = "");

public sealed record CarrisLisboaStaticManifestLine(
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("file")] string File,
    [property: JsonPropertyName("name")] string Name);

public sealed record CarrisLisboaStaticLine(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("number")] string Number,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("directions")] List<CarrisLisboaStaticDirection> Directions,
    [property: JsonPropertyName("stops")] List<CarrisLisboaStaticStop> Stops,
    [property: JsonPropertyName("trips")] List<CarrisLisboaStaticTrip> Trips,
    [property: JsonPropertyName("stopTimes")] List<CarrisLisboaStaticStopTime> StopTimes,
    [property: JsonPropertyName("calendar")] List<CarrisLisboaStaticCalendar> Calendar,
    [property: JsonPropertyName("calendarDates")] List<CarrisLisboaStaticCalendarDate> CalendarDates);

public sealed record CarrisLisboaStaticDirection(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("routeId")] string RouteId,
    [property: JsonPropertyName("name")] string Name);

public sealed record CarrisLisboaStaticStop(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("locality")] string Locality);

public sealed record CarrisLisboaStaticTrip(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("routeId")] string RouteId,
    [property: JsonPropertyName("serviceId")] string ServiceId,
    [property: JsonPropertyName("directionId")] string DirectionId,
    [property: JsonPropertyName("headsign")] string Headsign);

public sealed record CarrisLisboaStaticStopTime(
    [property: JsonPropertyName("tripId")] string TripId,
    [property: JsonPropertyName("stopId")] string StopId,
    [property: JsonPropertyName("timeSeconds")] int TimeSeconds,
    [property: JsonPropertyName("sequence")] int Sequence);

public sealed record CarrisLisboaStaticCalendar(
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

public sealed record CarrisLisboaStaticCalendarDate(
    [property: JsonPropertyName("serviceId")] string ServiceId,
    [property: JsonPropertyName("date")] string Date,
    [property: JsonPropertyName("exceptionType")] int ExceptionType);