using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using MyBusApp.Models.Domain;

namespace MyBusApp.Services;

public sealed class FavoriteStore(IJSRuntime jsRuntime)
{
    private const string StorageKey = "mybusapp:favorites:v1";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<List<TransitFavorite>> LoadAsync()
    {
        var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (json is null)
            return [];

        var favorites = JsonSerializer.Deserialize<List<TransitFavorite>>(json, JsonOptions)
            ?? throw new InvalidDataException("The saved favorites document is empty.");
        if (favorites.Any(favorite => !IsValid(favorite)))
            throw new InvalidDataException("The saved favorites document contains an invalid favorite.");

        return favorites;
    }

    public async Task SaveAsync(IReadOnlyCollection<TransitFavorite> favorites)
    {
        ArgumentNullException.ThrowIfNull(favorites);
        if (favorites.Any(favorite => !IsValid(favorite)))
            throw new ArgumentException("The collection contains an invalid favorite.", nameof(favorites));

        var json = JsonSerializer.Serialize(favorites, JsonOptions);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
    }

    public async Task<bool> AddAsync(TransitFavorite favorite)
    {
        ArgumentNullException.ThrowIfNull(favorite);

        var favorites = await LoadAsync();
        if (favorites.Any(existing => IsSameFavorite(existing, favorite)))
            return false;

        await SaveAsync([.. favorites, favorite]);
        return true;
    }

    public async Task<bool> IsFavoriteAsync(TransitFavorite favorite)
    {
        ArgumentNullException.ThrowIfNull(favorite);
        var favorites = await LoadAsync();
        return favorites.Any(existing => IsSameFavorite(existing, favorite));
    }

    public async Task<bool> SetFavoriteAsync(TransitFavorite favorite, bool isFavorite)
    {
        ArgumentNullException.ThrowIfNull(favorite);
        if (!IsValid(favorite))
            throw new ArgumentException("The favorite is invalid.", nameof(favorite));

        var favorites = await LoadAsync();
        var index = favorites.FindIndex(existing => IsSameFavorite(existing, favorite));
        if (isFavorite)
        {
            if (index >= 0)
                return true;

            favorites.Add(favorite);
        }
        else
        {
            if (index < 0)
                return false;

            favorites.RemoveAt(index);
        }

        await SaveAsync(favorites);
        return isFavorite;
    }

    private static bool IsValid(TransitFavorite favorite)
    {
        if (favorite is null ||
            !Enum.IsDefined(favorite.Mode) ||
            !Enum.IsDefined(favorite.Provider) ||
            string.IsNullOrWhiteSpace(favorite.StopId) ||
            string.IsNullOrWhiteSpace(favorite.StopName))
            return false;

        return favorite.Mode switch
        {
            FavoriteTransportMode.Bus =>
                favorite.Provider != FavoriteProvider.Cp &&
                !string.IsNullOrWhiteSpace(favorite.LineNumber),
            FavoriteTransportMode.Train =>
                favorite.Provider == FavoriteProvider.Cp &&
                string.IsNullOrWhiteSpace(favorite.LineNumber),
            _ => false
        };
    }

    private static bool IsSameFavorite(TransitFavorite left, TransitFavorite right)
        => left.Mode == right.Mode &&
           left.Provider == right.Provider &&
           string.Equals(left.StopId, right.StopId, StringComparison.Ordinal) &&
           string.Equals(left.LineNumber, right.LineNumber, StringComparison.Ordinal);
}
