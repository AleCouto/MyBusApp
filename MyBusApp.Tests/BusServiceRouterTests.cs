using MyBusApp.Models.Domain;
using MyBusApp.Services;
using Xunit;

namespace MyBusApp.Tests;

public sealed class BusServiceRouterTests
{
    [Fact]
    public async Task ResolveLineAsync_UsesTheConfiguredProviderPriority()
    {
        var metropolitana = new FakeBusService(BusProvider.Metropolitana, CreateLine(BusProvider.Metropolitana));
        var carris = new FakeBusService(BusProvider.CarrisLisboa, CreateLine(BusProvider.CarrisLisboa));
        var router = new BusServiceRouter([carris, metropolitana]);

        var result = await router.ResolveLineAsync("12");

        Assert.Same(metropolitana, result.Service);
        Assert.Equal(1, metropolitana.GetLineCallCount);
        Assert.Equal(0, carris.GetLineCallCount);
    }

    [Fact]
    public async Task ResolveLineAsync_WithProvider_UsesOnlyCarrisLisboa()
    {
        var metropolitana = new FakeBusService(BusProvider.Metropolitana, CreateLine(BusProvider.Metropolitana));
        var carris = new FakeBusService(BusProvider.CarrisLisboa, CreateLine(BusProvider.CarrisLisboa));
        var router = new BusServiceRouter([metropolitana, carris]);

        var result = await router.ResolveLineAsync(BusProvider.CarrisLisboa, "12");

        Assert.Same(carris, result.Service);
        Assert.Equal(0, metropolitana.GetLineCallCount);
        Assert.Equal(1, carris.GetLineCallCount);
    }

    [Fact]
    public async Task ResolveLineAsync_WithProvider_UsesOnlyMetropolitana()
    {
        var metropolitana = new FakeBusService(BusProvider.Metropolitana, CreateLine(BusProvider.Metropolitana));
        var carris = new FakeBusService(BusProvider.CarrisLisboa, CreateLine(BusProvider.CarrisLisboa));
        var router = new BusServiceRouter([carris, metropolitana]);

        var result = await router.ResolveLineAsync(BusProvider.Metropolitana, "12");

        Assert.Same(metropolitana, result.Service);
        Assert.Equal(1, metropolitana.GetLineCallCount);
        Assert.Equal(0, carris.GetLineCallCount);
    }

    [Fact]
    public async Task ResolveLineAsync_WithUnregisteredProvider_DoesNotUseFallback()
    {
        var carris = new FakeBusService(BusProvider.CarrisLisboa, CreateLine(BusProvider.CarrisLisboa));
        var router = new BusServiceRouter([carris]);

        var result = await router.ResolveLineAsync(BusProvider.Transitland, "12");

        Assert.Null(result.Line);
        Assert.Null(result.Service);
        Assert.Equal(0, carris.GetLineCallCount);
    }

    [Fact]
    public async Task ResolveLineAsync_ContinuesAfterProviderException()
    {
        var metropolitana = new FakeBusService(BusProvider.Metropolitana, CreateLine(BusProvider.Metropolitana))
        {
            Exception = new InvalidOperationException("provider failed")
        };
        var carris = new FakeBusService(BusProvider.CarrisLisboa, CreateLine(BusProvider.CarrisLisboa));
        var router = new BusServiceRouter([metropolitana, carris]);

        var result = await router.ResolveLineAsync("12");

        Assert.Same(carris, result.Service);
        Assert.Equal(1, metropolitana.GetLineCallCount);
        Assert.Equal(1, carris.GetLineCallCount);
    }

    [Fact]
    public async Task ResolveLineAsync_WithExplicitProviderException_DoesNotUseFallback()
    {
        var carris = new FakeBusService(BusProvider.CarrisLisboa, CreateLine(BusProvider.CarrisLisboa))
        {
            Exception = new InvalidOperationException("provider failed")
        };
        var metropolitana = new FakeBusService(BusProvider.Metropolitana, CreateLine(BusProvider.Metropolitana));
        var router = new BusServiceRouter([carris, metropolitana]);

        var result = await router.ResolveLineAsync(BusProvider.CarrisLisboa, "12");

        Assert.Null(result.Line);
        Assert.Null(result.Service);
        Assert.Equal(1, carris.GetLineCallCount);
        Assert.Equal(0, metropolitana.GetLineCallCount);
    }

    [Fact]
    public async Task ResolveLineAsync_ReturnsNoResultWhenNoProviderFindsLine()
    {
        var metropolitana = new FakeBusService(BusProvider.Metropolitana, null);
        var carris = new FakeBusService(BusProvider.CarrisLisboa, null);
        var router = new BusServiceRouter([metropolitana, carris]);

        var result = await router.ResolveLineAsync("999");

        Assert.Null(result.Line);
        Assert.Null(result.Service);
    }

    private static BusLine CreateLine(BusProvider provider)
        => new("line-12", "12", "Linha 12", "#000000", provider);

    private sealed class FakeBusService(BusProvider provider, BusLine? line) : IBusService
    {
        public BusProvider Provider { get; } = provider;
        public BusLine? Line { get; } = line;
        public Exception? Exception { get; init; }
        public int GetLineCallCount { get; private set; }

        public Task<BusLine?> GetLineAsync(string lineNumber)
        {
            GetLineCallCount++;
            if (Exception is not null)
                throw Exception;

            return Task.FromResult(Line);
        }

        public Task<List<BusDirection>> GetDirectionsAsync(string lineId) => Task.FromResult(new List<BusDirection>());
        public Task<List<BusStop>> GetStopsAsync(string directionId, string lineId) => Task.FromResult(new List<BusStop>());
        public Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId) => Task.FromResult(new List<BusArrival>());
    }
}