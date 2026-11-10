using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.JSInterop;
using MyBusApp.Models.Domain;

namespace MyBusApp.Services;

public sealed class FavoriteStore(IJSRuntime jsRuntime)
{
    private const string StorageKey = "mybusapp:favorites:v1";
    private readonly SemaphoreSlim operationGate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<List<TransitFavorite>> LoadAsync()
    {
        await operationGate.WaitAsync();
        try
        {
            return await LoadCoreAsync();
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async Task<List<TransitFavorite>> LoadCoreAsync()
    {
        var json = await jsRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);
        if (json is null)
            return [];

        var favorites = JsonSerializer.Deserialize<List<TransitFavorite>>(json, JsonOptions)
            ?? throw new InvalidDataException("The saved favorites document is empty.");
        if (favorites.Any(favorite => !IsValid(favorite)))
            throw new InvalidDataException("The saved favorites document contains an invalid favorite.");
        if (HasDuplicateFavorites(favorites))
            throw new InvalidDataException("The saved favorites document contains duplicate favorites.");

        return favorites;
    }

    public async Task SaveAsync(IReadOnlyCollection<TransitFavorite> favorites)
    {
        await operationGate.WaitAsync();
        try
        {
            await SaveCoreAsync(favorites);
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task<bool> AddAsync(TransitFavorite favorite)
    {
        ArgumentNullException.ThrowIfNull(favorite);
        if (!IsValid(favorite))
            throw new ArgumentException("The favorite is invalid.", nameof(favorite));

        await operationGate.WaitAsync();
        try
        {
            var favorites = await LoadCoreAsync();
            if (favorites.Any(existing => existing.HasSameIdentityAs(favorite)))
                return false;

            var legacyIndex = FindLegacyBusFavoriteIndex(favorites, favorite);
            if (legacyIndex >= 0)
                favorites[legacyIndex] = favorite;
            else
                favorites.Add(favorite);

            await SaveCoreAsync(favorites);
            return true;
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task<bool> IsFavoriteAsync(TransitFavorite favorite)
    {
        ArgumentNullException.ThrowIfNull(favorite);
        if (!IsValid(favorite))
            throw new ArgumentException("The favorite is invalid.", nameof(favorite));

        await operationGate.WaitAsync();
        try
        {
            var favorites = await LoadCoreAsync();
            return favorites.Any(existing => existing.HasSameIdentityAs(favorite));
        }
        finally
        {
            operationGate.Release();
        }
    }

    public async Task<bool> SetFavoriteAsync(TransitFavorite favorite, bool isFavorite)
    {
        ArgumentNullException.ThrowIfNull(favorite);
        if (!IsValid(favorite))
            throw new ArgumentException("The favorite is invalid.", nameof(favorite));

        await operationGate.WaitAsync();
        try
        {
            var favorites = await LoadCoreAsync();
            var index = favorites.FindIndex(existing => existing.HasSameIdentityAs(favorite));
            if (isFavorite)
            {
                if (index >= 0)
                    return true;

                var legacyIndex = FindLegacyBusFavoriteIndex(favorites, favorite);
                if (legacyIndex >= 0)
                    favorites[legacyIndex] = favorite;
                else
                    favorites.Add(favorite);
            }
            else
            {
                if (index < 0)
                    return false;

                favorites.RemoveAt(index);
            }

            await SaveCoreAsync(favorites);
            return isFavorite;
        }
        finally
        {
            operationGate.Release();
        }
    }

    private async Task SaveCoreAsync(IReadOnlyCollection<TransitFavorite> favorites)
    {
        ArgumentNullException.ThrowIfNull(favorites);
        if (favorites.Any(favorite => !IsValid(favorite)))
            throw new ArgumentException("The collection contains an invalid favorite.", nameof(favorites));
        if (HasDuplicateFavorites(favorites))
            throw new ArgumentException("The collection contains duplicate favorites.", nameof(favorites));

        var json = JsonSerializer.Serialize(favorites, JsonOptions);
        await jsRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, json);
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
                !string.IsNullOrWhiteSpace(favorite.LineNumber) &&
                favorite.DestinationId is null &&
                (favorite.DirectionId is null || !string.IsNullOrWhiteSpace(favorite.DirectionId)),
            FavoriteTransportMode.Train =>
                (favorite.Provider is FavoriteProvider.Cp or FavoriteProvider.Fertagus) &&
                string.IsNullOrWhiteSpace(favorite.LineNumber) &&
                favorite.DirectionId is null &&
                (favorite.DestinationId is null || !string.IsNullOrWhiteSpace(favorite.DestinationId)),
            _ => false
        };
    }

    private static int FindLegacyBusFavoriteIndex(
        IReadOnlyList<TransitFavorite> favorites,
        TransitFavorite favorite)
    {
        if (favorite.Mode != FavoriteTransportMode.Bus ||
            string.IsNullOrWhiteSpace(favorite.DirectionId))
            return -1;

        for (var index = 0; index < favorites.Count; index++)
        {
            var existing = favorites[index];
            if (existing.Mode == FavoriteTransportMode.Bus &&
                existing.Provider == favorite.Provider &&
                string.Equals(existing.StopId, favorite.StopId, StringComparison.Ordinal) &&
                string.Equals(existing.LineNumber, favorite.LineNumber, StringComparison.Ordinal) &&
                string.IsNullOrWhiteSpace(existing.DirectionId))
                return index;
        }

        return -1;
    }

    private static bool HasDuplicateFavorites(IReadOnlyCollection<TransitFavorite> favorites)
    {
        var items = favorites.ToArray();
        for (var index = 0; index < items.Length; index++)
        {
            for (var candidateIndex = index + 1; candidateIndex < items.Length; candidateIndex++)
            {
                if (items[index].HasSameIdentityAs(items[candidateIndex]))
                    return true;
            }
        }

        return false;
    }
}
