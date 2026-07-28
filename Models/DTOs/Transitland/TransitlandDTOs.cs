using System.Text.Json.Serialization;

namespace MyBusApp.Models.DTOs.Transitland;

public record TransitlandRoute(
    [property: JsonPropertyName("onestop_id")] string? OnestopId,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("route_number")] string? RouteNumber,
    [property: JsonPropertyName("name")] string? Name
);

public record TransitlandRouteStopPattern(
    [property: JsonPropertyName("onestop_id")] string? OnestopId,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("stop_onestop_ids")] List<string>? StopOnestopIds = null,
    [property: JsonPropertyName("pattern_stop_onestop_ids")] List<TransitlandPatternStop>? PatternStopOnestopIds = null
)
{
    [JsonIgnore]
    public List<string> StopIds => StopOnestopIds?.ToList() ?? PatternStopOnestopIds?.Select(ps => ps.StopOnestopId).Where(id => !string.IsNullOrEmpty(id)).ToList() ?? new();
}

public record TransitlandPatternStop(
    [property: JsonPropertyName("stop_onestop_id")] string StopOnestopId
);

public record TransitlandStop(
    [property: JsonPropertyName("onestop_id")] string? OnestopId,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("locality")] string? Locality
);

public record TransitlandSchedule(
    [property: JsonPropertyName("arrival_time")] string? ArrivalTime,
    [property: JsonPropertyName("departure_time")] string? DepartureTime,
    [property: JsonPropertyName("trip_headsign")] string? TripHeadsign
);
