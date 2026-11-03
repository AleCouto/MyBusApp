namespace MyBusApp.Models.Domain;

public enum FavoriteTransportMode
{
    Bus,
    Train
}

public enum FavoriteProvider
{
    Metropolitana,
    CarrisLisboa,
    Transitland,
    Cp
}

public sealed record TransitFavorite(
    FavoriteTransportMode Mode,
    FavoriteProvider Provider,
    string StopId,
    string StopName,
    string? LineNumber = null);
