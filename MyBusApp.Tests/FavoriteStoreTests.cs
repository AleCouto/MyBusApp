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
    public async Task LoadAsync_RejectsMalformedSavedFavorites()
    {
        var jsRuntime = new MemoryJsRuntime { StoredValue = "null" };
        var store = new FavoriteStore(jsRuntime);

        await Assert.ThrowsAsync<InvalidDataException>(store.LoadAsync);
    }

    private sealed class MemoryJsRuntime : IJSRuntime
    {
        public string? StoredValue { get; set; }
        public string? LastIdentifier { get; private set; }
        public object?[]? LastArguments { get; private set; }

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
                return ValueTask.FromResult((TValue)(object?)StoredValue!);

            if (identifier == "localStorage.setItem")
            {
                StoredValue = (string)args![1]!;
                return ValueTask.FromResult(default(TValue)!);
            }

            throw new InvalidOperationException($"Unexpected JS interop call: {identifier}");
        }
    }
}
