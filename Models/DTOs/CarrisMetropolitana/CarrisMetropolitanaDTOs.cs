using System.Text.Json.Serialization;

namespace MyBusApp.Models.DTOs.CarrisMetropolitana;

public record Line(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("short_name")] string ShortName,
    [property: JsonPropertyName("long_name")] string LongName,
    [property: JsonPropertyName("color")] string Color = "#000000",
    [property: JsonPropertyName("text_color")] string TextColor = "#FFFFFF",
    [property: JsonPropertyName("facilities")] List<string>? Facilities = null,
    [property: JsonPropertyName("municipality_ids")] List<string>? MunicipalityIds = null,
    [property: JsonPropertyName("route_ids")] List<string>? RouteIds = null
);

public record Route(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("line_id")] string LineId,
    [property: JsonPropertyName("short_name")] string ShortName,
    [property: JsonPropertyName("long_name")] string LongName,
    [property: JsonPropertyName("color")] string Color = "#000000",
    [property: JsonPropertyName("text_color")] string TextColor = "#FFFFFF",
    [property: JsonPropertyName("district_ids")] List<string>? DistrictIds = null,
    [property: JsonPropertyName("municipality_ids")] List<string>? MunicipalityIds = null,
    [property: JsonPropertyName("locality_ids")] List<string>? LocalityIds = null,
    [property: JsonPropertyName("pattern_ids")] List<string>? PatternIds = null,
    [property: JsonPropertyName("facilities")] List<string>? Facilities = null,
    [property: JsonPropertyName("stop_ids")] List<string>? StopIds = null
);

public record Pattern(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("line_id")] string LineId,
    [property: JsonPropertyName("route_id")] string RouteId,
    [property: JsonPropertyName("short_name")] string ShortName,
    [property: JsonPropertyName("long_name")] string LongName,
    [property: JsonPropertyName("headsign")] string Headsign,
    [property: JsonPropertyName("direction_id")] int DirectionId,
    [property: JsonPropertyName("color")] string Color = "#000000",
    [property: JsonPropertyName("text_color")] string TextColor = "#FFFFFF",
    [property: JsonPropertyName("path")] List<PatternPath> Path = null!,
    [property: JsonPropertyName("trips")] List<PatternTrip>? Trips = null
)
{
    public List<string> StopIds => Path?.OrderBy(p => p.StopSequence).Select(p => p.StopId).ToList() ?? new();
}

public record PatternPath(
    [property: JsonPropertyName("stop_id")] string StopId,
    [property: JsonPropertyName("stop_sequence")] int StopSequence,
    [property: JsonPropertyName("allow_pickup")] bool AllowPickup,
    [property: JsonPropertyName("allow_drop_off")] bool AllowDropOff,
    [property: JsonPropertyName("distance")] double Distance
);

public record PatternTrip(
    [property: JsonPropertyName("trip_ids")] List<string> TripIds,
    [property: JsonPropertyName("service_ids")] List<string> ServiceIds
);

public record Stop(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("long_name")] string LongName,
    [property: JsonPropertyName("short_name")] string ShortName,
    [property: JsonPropertyName("lat")] double Latitude,
    [property: JsonPropertyName("lon")] double Longitude,
    [property: JsonPropertyName("locality_id")] string LocalityId,
    [property: JsonPropertyName("municipality_id")] string MunicipalityId,
    [property: JsonPropertyName("operational_status")] string OperationalStatus,
    [property: JsonPropertyName("line_ids")] List<string>? LineIds = null,
    [property: JsonPropertyName("route_ids")] List<string>? RouteIds = null,
    [property: JsonPropertyName("facilities")] List<string>? Facilities = null
);

public record Arrival(
    [property: JsonPropertyName("line_id")] string LineId,
    [property: JsonPropertyName("stop_id")] string StopId,
    [property: JsonPropertyName("route_id")] string RouteId,
    [property: JsonPropertyName("scheduled_arrival")] string ScheduledArrival,
    [property: JsonPropertyName("estimated_arrival")] string EstimatedArrival,
    [property: JsonPropertyName("headsign")] string Headsign,
    [property: JsonPropertyName("pattern_id")] string PatternId,
    [property: JsonPropertyName("stop_sequence")] int StopSequence
)
{
    public bool IsRealTime => !string.IsNullOrEmpty(EstimatedArrival);

    public string DisplayTime 
    { 
        get 
        {
            var timeToProcess = IsRealTime ? EstimatedArrival : ScheduledArrival;
            if (string.IsNullOrEmpty(timeToProcess)) return string.Empty;

            var parts = timeToProcess.Split('T');
            var timePart = parts.Length > 1 ? parts[1] : parts[0];
            return timePart.Length >= 5 ? timePart.Substring(0, 5) : timePart;
        }
    }
}