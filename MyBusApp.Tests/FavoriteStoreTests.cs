using Microsoft.JSInterop;
using MyBusApp.Models.Domain;
using MyBusApp.Services;
using Xunit;

namespace MyBusApp.Tests;

public sealed class FavoriteStoreTests
{
    [Fact]
    public async Task LoadAsync_ReturnsEmptyListWhenBrowserHasNoSavedFavorites()
    {
        var jsRuntime = new MemoryJsRuntime();
        var store = new FavoriteStore(jsRuntime);

        var favorites = await store.LoadAsync();

        Assert.Empty(favorites);
        Assert.Equal("localStorage.getItem", jsRuntime.LastIdentifier);
        Assert.Equal("mybusapp:favorites:v1", jsRuntime.LastArguments![0]);
    }

    [Fact]
    public async Task SaveAsyncAndLoadAsync_PreserveFavoriteContextAndOrder()
    {
        var jsRuntime = new MemoryJsRuntime();
        var store = new FavoriteStore(jsRuntime);
        TransitFavorite[] expected =
        [
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop-1", "Paragem A", "4725"),
            new(FavoriteTransportMode.Train, FavoriteProvider.Cp, "station-1", "Estação B")
        ];

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.Equal(expected, actual);
        Assert.Equal("localStorage.getItem", jsRuntime.LastIdentifier);
        Assert.Contains("\"Mode\":\"Bus\"", jsRuntime.StoredValue);
        Assert.Contains("\"Provider\":\"Cp\"", jsRuntime.StoredValue);
    }

    [Fact]
    public async Task AddAsync_AppendsFavoriteWithoutDuplicatingTheSameContext()
    {
        var jsRuntime = new MemoryJsRuntime();
        var store = new FavoriteStore(jsRuntime);
        var first = new TransitFavorite(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop-1", "Paragem A", "4725");
        var second = new TransitFavorite(FavoriteTransportMode.Train, FavoriteProvider.Cp, "station-1", "Estação B");
        await store.SaveAsync([first]);

        Assert.True(await store.AddAsync(second));
        Assert.False(await store.AddAsync(second));

        Assert.Equal([first, second], await store.LoadAsync());
    }

    [Fact]
    public async Task AddAsync_RejectsSameBusIdentityWhenNamesDiffer()
    {
        var jsRuntime = new MemoryJsRuntime();
        var store = new FavoriteStore(jsRuntime);
        var favorite = new TransitFavorite(
            FavoriteTransportMode.Bus,
            FavoriteProvider.Metropolitana,
            "stop-1",
            "Paragem A",
            "4725");

        Assert.True(await store.AddAsync(favorite));
        Assert.False(await store.AddAsync(favorite with { StopName = "Nome atualizado" }));

        Assert.Equal([favorite], await store.LoadAsync());
    }

    [Fact]
    public async Task AddAsync_AllowsSameBusStopOnDifferentLines()
    {
        var jsRuntime = new MemoryJsRuntime();
        var store = new FavoriteStore(jsRuntime);
        var favorite = new TransitFavorite(
            FavoriteTransportMode.Bus,
            FavoriteProvider.Metropolitana,
            "stop-1",
            "Paragem",
            "4725");
        var otherLine = favorite with { LineNumber = "3710" };

        Assert.True(await store.AddAsync(favorite));
        Assert.True(await store.AddAsync(otherLine));

        Assert.Equal([favorite, otherLine], await store.LoadAsync());
    }

    [Fact]
    public async Task AddAsync_AllowsSameBusIdentityFromDifferentProviders()
    {
        var jsRuntime = new MemoryJsRuntime();
        var store = new FavoriteStore(jsRuntime);
        var favorite = new TransitFavorite(
            FavoriteTransportMode.Bus,
            FavoriteProvider.Metropolitana,
            "stop-1",
            "Paragem",
            "4725");
        var otherProvider = favorite with { Provider = FavoriteProvider.Transitland };

        Assert.True(await store.AddAsync(favorite));
        Assert.True(await store.AddAsync(otherProvider));

        Assert.Equal([favorite, otherProvider], await store.LoadAsync());
    }

    [Fact]
    public async Task SetFavoriteAsync_TogglesTheMatchingFavoriteWithoutChangingOtherContexts()
    {
        var jsRuntime = new MemoryJsRuntime();
        var store = new FavoriteStore(jsRuntime);
        var busFavorite = new TransitFavorite(
            FavoriteTransportMode.Bus,
            FavoriteProvider.Metropolitana,
            "shared-stop",
            "Paragem",
            "4725");
        var otherBusFavorite = busFavorite with { LineNumber = "3710" };
        var trainFavorite = new TransitFavorite(
            FavoriteTransportMode.Train,
            FavoriteProvider.Cp,
            "station-1",
            "Estação");
        await store.SaveAsync([otherBusFavorite, trainFavorite]);

        Assert.False(await store.IsFavoriteAsync(busFavorite));
        Assert.True(await store.SetFavoriteAsync(busFavorite, true));
        Assert.True(await store.IsFavoriteAsync(busFavorite));
        Assert.True(await store.SetFavoriteAsync(busFavorite, true));
        Assert.Equal([otherBusFavorite, trainFavorite, busFavorite], await store.LoadAsync());

        Assert.False(await store.SetFavoriteAsync(busFavorite, false));
        Assert.False(await store.IsFavoriteAsync(busFavorite));
        Assert.Equal([otherBusFavorite, trainFavorite], await store.LoadAsync());
    }

    [Fact]
    public async Task TrainFavorites_StayDistinctAcrossOperatorsForTheSameStationAndDestination()
    {
        var store = new FavoriteStore(new MemoryJsRuntime());
        var cpFavorite = new TransitFavorite(
            FavoriteTransportMode.Train,
            FavoriteProvider.Cp,
            "roma-id",
            "Roma-Areeiro",
            DestinationId: "route-1");
        var fertagusFavorite = cpFavorite with { Provider = FavoriteProvider.Fertagus };

        Assert.True(await store.AddAsync(cpFavorite));
        Assert.True(await store.AddAsync(fertagusFavorite));
        Assert.False(await store.AddAsync(cpFavorite));

        Assert.Equal([cpFavorite, fertagusFavorite], await store.LoadAsync());
    }

    [Fact]
    public async Task TrainFavorites_AreUniqueByProviderStationAndDestination()
    {
        var jsRuntime = new MemoryJsRuntime();
        var store = new FavoriteStore(jsRuntime);
        var abrantesToEntroncamento = new TransitFavorite(
            FavoriteTransportMode.Train,
            FavoriteProvider.Cp,
            "abrantes-id",
            "Abrantes",
            DestinationId: "route-entr-on");
        var abrantesToLisbon = abrantesToEntroncamento with { DestinationId = "route-abr-lis" };

        Assert.True(await store.AddAsync(abrantesToEntroncamento));
        Assert.True(await store.AddAsync(abrantesToLisbon));
        Assert.False(await store.AddAsync(abrantesToEntroncamento));

        Assert.Equal([abrantesToEntroncamento, abrantesToLisbon], await store.LoadAsync());
    }

    [Fact]
    public async Task AddAsync_RejectsSameTrainIdentityWhenStationNameChanges()
    {
        var jsRuntime = new MemoryJsRuntime();
        var store = new FavoriteStore(jsRuntime);
        var favorite = new TransitFavorite(
            FavoriteTransportMode.Train,
            FavoriteProvider.Cp,
            "abrantes-id",
            "Abrantes",
            DestinationId: "route-entr-on");

        Assert.True(await store.AddAsync(favorite));
        Assert.False(await store.AddAsync(favorite with { StopName = "Nome atualizado" }));

        Assert.Equal([favorite], await store.LoadAsync());
    }

    [Fact]
    public async Task SaveAsync_RejectsDuplicateFavoritesWithoutChangingStoredData()
    {
        var jsRuntime = new MemoryJsRuntime();
        var store = new FavoriteStore(jsRuntime);
        var original = new TransitFavorite(
            FavoriteTransportMode.Bus,
            FavoriteProvider.Metropolitana,
            "stop-existing",
            "Paragem existente",
            "4725");
        await store.SaveAsync([original]);
        var storedValue = jsRuntime.StoredValue;
        var duplicate = original with { StopName = "Outro nome" };

        await Assert.ThrowsAsync<ArgumentException>(() => store.SaveAsync([original, duplicate]));

        Assert.Equal(storedValue, jsRuntime.StoredValue);
    }

    [Fact]
    public async Task LoadAsync_RejectsDuplicateSavedFavoritesWithoutChangingStoredData()
    {
        const string duplicatedJson = """[{"Mode":"Bus","Provider":"Metropolitana","StopId":"stop-1","StopName":"Paragem A","LineNumber":"4725"},{"Mode":"Bus","Provider":"Metropolitana","StopId":"stop-1","StopName":"Paragem B","LineNumber":"4725"}]""";
        var jsRuntime = new MemoryJsRuntime { StoredValue = duplicatedJson };
        var store = new FavoriteStore(jsRuntime);

        await Assert.ThrowsAsync<InvalidDataException>(store.LoadAsync);

        Assert.Equal(duplicatedJson, jsRuntime.StoredValue);
        Assert.Equal(0, jsRuntime.SetItemCallCount);
    }

    [Fact]
    public async Task AddAsync_ConcurrentAddsWithinSameStoreDoNotLoseUpdates()
    {
        var jsRuntime = new MemoryJsRuntime { DeferReads = true };
        var store = new FavoriteStore(jsRuntime);
        var first = new TransitFavorite(
            FavoriteTransportMode.Bus,
            FavoriteProvider.Metropolitana,
            "stop-1",
            "Paragem A",
            "4725");
        var second = new TransitFavorite(
            FavoriteTransportMode.Bus,
            FavoriteProvider.Transitland,
            "stop-2",
            "Paragem B",
            "3710");

        var results = await Task.WhenAll(store.AddAsync(first), store.AddAsync(second));

        Assert.All(results, Assert.True);
        Assert.Equal([first, second], await store.LoadAsync());
    }

    [Fact]
    public async Task RemovingTrainFavorite_KeepsOtherDestinationsAtSameStation()
    {
        var jsRuntime = new MemoryJsRuntime();
        var store = new FavoriteStore(jsRuntime);
        var abrantesToEntroncamento = new TransitFavorite(
            FavoriteTransportMode.Train,
            FavoriteProvider.Cp,
            "abrantes-id",
            "Abrantes",
            DestinationId: "route-entr-on");
        var abrantesToLisbon = abrantesToEntroncamento with { DestinationId = "route-abr-lis" };
        await store.SaveAsync([abrantesToEntroncamento, abrantesToLisbon]);

        Assert.False(await store.SetFavoriteAsync(abrantesToEntroncamento, false));

        Assert.Equal([abrantesToLisbon], await store.LoadAsync());
    }

    [Fact]
    public async Task LoadAsync_PreservesLegacyTrainFavoriteWithoutDestination()
    {
        var jsRuntime = new MemoryJsRuntime
        {
            StoredValue = """[{"Mode":"Train","Provider":"Cp","StopId":"abrantes-id","StopName":"Abrantes"}]"""
        };
        var store = new FavoriteStore(jsRuntime);

        var favorite = Assert.Single(await store.LoadAsync());

        Assert.Null(favorite.DestinationId);
        await store.SaveAsync([favorite]);
        Assert.Single(await store.LoadAsync());
        Assert.Contains("\"DestinationId\":null", jsRuntime.StoredValue);
    }

    [Fact]
    public async Task LoadAsync_PreservesLegacyTrainFavoriteAlongsideDestinationSpecificFavorite()
    {
        const string savedJson = """[{"Mode":"Train","Provider":"Cp","StopId":"abrantes-id","StopName":"Abrantes"},{"Mode":"Train","Provider":"Cp","StopId":"abrantes-id","StopName":"Abrantes","DestinationId":"route-entr-on"}]""";
        var jsRuntime = new MemoryJsRuntime { StoredValue = savedJson };
        var store = new FavoriteStore(jsRuntime);

        var favorites = await store.LoadAsync();

        Assert.Equal(2, favorites.Count);
        Assert.Null(favorites[0].DestinationId);
        Assert.Equal("route-entr-on", favorites[1].DestinationId);
        Assert.Equal(savedJson, jsRuntime.StoredValue);
        Assert.Equal(0, jsRuntime.SetItemCallCount);
    }

    [Fact]
    public async Task LoadAsync_RejectsMalformedSavedFavorites()
    {
        const string invalidDocument = "null";
        var jsRuntime = new MemoryJsRuntime { StoredValue = invalidDocument };
        var store = new FavoriteStore(jsRuntime);

        await Assert.ThrowsAsync<InvalidDataException>(store.LoadAsync);
        Assert.Equal(invalidDocument, jsRuntime.StoredValue);
        Assert.Equal(0, jsRuntime.SetItemCallCount);
    }

    [Fact]
    public async Task LoadAsync_RejectsInvalidFavoriteRecordWithoutChangingStoredData()
    {
        const string invalidRecord = """[{"Mode":"Bus","Provider":"Metropolitana","StopId":"","StopName":"Paragem","LineNumber":"4725"}]""";
        var jsRuntime = new MemoryJsRuntime { StoredValue = invalidRecord };
        var store = new FavoriteStore(jsRuntime);

        await Assert.ThrowsAsync<InvalidDataException>(store.LoadAsync);

        Assert.Equal(invalidRecord, jsRuntime.StoredValue);
        Assert.Equal(0, jsRuntime.SetItemCallCount);
    }

    [Fact]
    public async Task LoadAsync_RejectsUnknownEnumNameWithoutChangingStoredData()
    {
        const string unknownEnum = """[{"Mode":"Bus","Provider":"UnknownProvider","StopId":"stop-1","StopName":"Paragem","LineNumber":"4725"}]""";
        var jsRuntime = new MemoryJsRuntime { StoredValue = unknownEnum };
        var store = new FavoriteStore(jsRuntime);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(store.LoadAsync);

        Assert.Equal(unknownEnum, jsRuntime.StoredValue);
        Assert.Equal(0, jsRuntime.SetItemCallCount);
    }

    [Fact]
    public async Task LoadAsync_RejectsUndefinedNumericEnumValueWithoutChangingStoredData()
    {
        const string undefinedEnum = """[{"Mode":"Bus","Provider":99,"StopId":"stop-1","StopName":"Paragem","LineNumber":"4725"}]""";
        var jsRuntime = new MemoryJsRuntime { StoredValue = undefinedEnum };
        var store = new FavoriteStore(jsRuntime);

        await Assert.ThrowsAsync<InvalidDataException>(store.LoadAsync);

        Assert.Equal(undefinedEnum, jsRuntime.StoredValue);
        Assert.Equal(0, jsRuntime.SetItemCallCount);
    }

    [Fact]
    public async Task LoadAsync_PropagatesLocalStorageReadFailures()
    {
        var jsRuntime = new MemoryJsRuntime
        {
            GetItemException = new JSException("getItem failed")
        };
        var store = new FavoriteStore(jsRuntime);

        await Assert.ThrowsAsync<JSException>(store.LoadAsync);
        Assert.Equal(0, jsRuntime.SetItemCallCount);
    }

    [Fact]
    public async Task LoadAsync_PreservesInvalidJsonAndDoesNotWrite()
    {
        const string invalidJson = "{";
        var jsRuntime = new MemoryJsRuntime { StoredValue = invalidJson };
        var store = new FavoriteStore(jsRuntime);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(store.LoadAsync);

        Assert.Equal(invalidJson, jsRuntime.StoredValue);
        Assert.Equal(0, jsRuntime.SetItemCallCount);
    }

    [Fact]
    public async Task AddAsync_DoesNotWriteAfterAReadFailure()
    {
        var jsRuntime = new MemoryJsRuntime
        {
            StoredValue = "existing favorites",
            GetItemException = new JSException("getItem failed")
        };
        var store = new FavoriteStore(jsRuntime);
        var favorite = new TransitFavorite(
            FavoriteTransportMode.Bus,
            FavoriteProvider.Metropolitana,
            "stop-1",
            "Paragem",
            "4725");

        await Assert.ThrowsAsync<JSException>(() => store.AddAsync(favorite));

        Assert.Equal("existing favorites", jsRuntime.StoredValue);
        Assert.Equal(0, jsRuntime.SetItemCallCount);
    }

    [Fact]
    public async Task SaveAsync_PropagatesLocalStorageWriteFailuresWithoutChangingStoredData()
    {
        const string originalJson = "existing favorites";
        var jsRuntime = new MemoryJsRuntime
        {
            StoredValue = originalJson,
            SetItemException = new JSException("setItem failed")
        };
        var store = new FavoriteStore(jsRuntime);
        var favorite = new TransitFavorite(
            FavoriteTransportMode.Bus,
            FavoriteProvider.Metropolitana,
            "stop-1",
            "Paragem",
            "4725");

        await Assert.ThrowsAsync<JSException>(() => store.SaveAsync([favorite]));

        Assert.Equal(originalJson, jsRuntime.StoredValue);
        Assert.Equal(1, jsRuntime.SetItemCallCount);
    }

    [Fact]
    public async Task BusFavorites_AreUniqueByProviderLineStopAndDirection()
    {
        var store = new FavoriteStore(new MemoryJsRuntime());
        var toLisbon = new TransitFavorite(
            FavoriteTransportMode.Bus,
            FavoriteProvider.Metropolitana,
            "stop-1",
            "Paragem",
            "4725",
            DirectionId: "4725_0_2");
        var toSetubal = toLisbon with { DirectionId = "4725_0_1" };

        Assert.True(await store.AddAsync(toLisbon));
        Assert.True(await store.AddAsync(toSetubal));
        Assert.False(await store.AddAsync(toLisbon));

        Assert.Equal([toLisbon, toSetubal], await store.LoadAsync());
    }

    [Fact]
    public async Task AddAsync_UpgradesLegacyBusFavoriteWithTheSelectedDirection()
    {
        var store = new FavoriteStore(new MemoryJsRuntime());
        var legacy = new TransitFavorite(
            FavoriteTransportMode.Bus,
            FavoriteProvider.Metropolitana,
            "stop-1",
            "Paragem",
            "4725");
        var directed = legacy with { DirectionId = "4725_0_2" };
        await store.SaveAsync([legacy]);

        Assert.True(await store.AddAsync(directed));

        Assert.Equal([directed], await store.LoadAsync());
    }

    [Fact]
    public async Task SetFavoriteAsync_UpgradesLegacyBusFavoriteWithoutAddingADuplicate()
    {
        var store = new FavoriteStore(new MemoryJsRuntime());
        var legacy = new TransitFavorite(
            FavoriteTransportMode.Bus,
            FavoriteProvider.Metropolitana,
            "stop-1",
            "Paragem",
            "4725");
        var directed = legacy with { DirectionId = "4725_0_2" };
        await store.SaveAsync([legacy]);

        Assert.False(await store.IsFavoriteAsync(directed));
        Assert.True(await store.SetFavoriteAsync(directed, true));

        Assert.Equal([directed], await store.LoadAsync());
    }

    private sealed class MemoryJsRuntime : IJSRuntime
    {
        public string? StoredValue { get; set; }
        public bool DeferReads { get; init; }
        public string? LastIdentifier { get; private set; }
        public object?[]? LastArguments { get; private set; }
        public Exception? GetItemException { get; set; }
        public Exception? SetItemException { get; set; }
        public int SetItemCallCount { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            LastIdentifier = identifier;
            LastArguments = args;

            if (identifier == "localStorage.getItem")
            {
                if (GetItemException is not null)
                    throw GetItemException;

                var snapshot = StoredValue;
                return DeferReads
                    ? new ValueTask<TValue>(ReadSnapshotAsync<TValue>(snapshot))
                    : ValueTask.FromResult((TValue)(object?)snapshot!);
            }

            if (identifier == "localStorage.setItem")
            {
                SetItemCallCount++;
                if (SetItemException is not null)
                    throw SetItemException;

                StoredValue = (string)args![1]!;
                return ValueTask.FromResult(default(TValue)!);
            }

            throw new InvalidOperationException($"Unexpected JS interop call: {identifier}");
        }

        private static async Task<TValue> ReadSnapshotAsync<TValue>(string? snapshot)
        {
            await Task.Yield();
            return (TValue)(object?)snapshot!;
        }
    }
}
