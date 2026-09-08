using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyBusApp.Models.DTOs.Transitland;

public record TransitlandRoute(
    [property: JsonPropertyName("onestop_id")] string? OnestopId,
    [property: JsonPropertyName("id"), JsonConverter(typeof(StringOrNumberConverter))] string? Id,
    [property: JsonPropertyName("route_number")] string? RouteNumber,
    [property: JsonPropertyName("route_short_name")] string? RouteShortName,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("route_long_name")] string? LongName,
    [property: JsonPropertyName("agency")] TransitlandAgency? Agency = null
);

public record TransitlandAgency(
    [property: JsonPropertyName("onestop_id")] string? OnestopId,
    [property: JsonPropertyName("agency_name")] string? Name
);

public sealed class StringOrNumberConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.GetInt64().ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonTokenType.Null => null,
            _ => throw new JsonException($"Token inesperado para string: {reader.TokenType}")
        };
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

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
