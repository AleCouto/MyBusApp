using System.Text.Json.Serialization;

namespace MyBusApp.Models.DTOs.CarrisLisboa;

// Representa uma Carreira (Linha)
public record CarrisRouteDto(
    [property: JsonPropertyName("routeNumber")] string RouteNumber,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("color")] string Color,
    [property: JsonPropertyName("isNight")] bool IsNight);

// Representa uma Variante (Sentido/Direção)
public record CarrisVariantDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("variantName")] string VariantName,
    [property: JsonPropertyName("isOfficial")] bool IsOfficial);

// Representa uma Paragem
public record CarrisStopDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("locality")] string Locality);

// Representa uma Previsão de Chegada
public record CarrisEstimationDto(
    [property: JsonPropertyName("line")] string Line,
    [property: JsonPropertyName("destination")] string Destination,
    [property: JsonPropertyName("time")] string Time,
    [property: JsonPropertyName("isRealTime")] bool IsRealTime);