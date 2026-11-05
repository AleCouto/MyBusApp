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
    string? LineNumber = null,
    string? DestinationId = null)
{
    public bool HasSameIdentityAs(TransitFavorite other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return Mode == other.Mode &&
               Provider == other.Provider &&
               string.Equals(StopId, other.StopId, StringComparison.Ordinal) &&
               (Mode == FavoriteTransportMode.Bus
                   ? string.Equals(LineNumber, other.LineNumber, StringComparison.Ordinal)
                   : string.Equals(DestinationId, other.DestinationId, StringComparison.Ordinal));
    }
}
