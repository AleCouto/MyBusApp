using MyBusApp.Models.Domain;
using MyBusApp.Models.DTOs.Transitland;
using MyBusApp.Services;
using Xunit;

namespace MyBusApp.Tests;

public class TransitlandServiceTests
{
    [Fact]
    public void MatchesLineNumber_ShouldRecognizeRouteShortNameAndRouteNumber()
    {
        var routeWithShortName = new TransitlandRoute(
            OnestopId: "r-foo-714",
            Id: "route-1",
            RouteNumber: null,
            RouteShortName: "714",
            Name: "Linha 714",
            LongName: "Linha 714"
        );

        var routeWithNumber = new TransitlandRoute(
            OnestopId: "r-foo-3710",
            Id: "route-2",
            RouteNumber: "3710",
            RouteShortName: null,
            Name: "Linha 3710",
            LongName: "Linha 3710"
        );

        Assert.True(TransitlandService.MatchesLineNumber(routeWithShortName, "714"));
        Assert.True(TransitlandService.MatchesLineNumber(routeWithNumber, "3710"));
        Assert.False(TransitlandService.MatchesLineNumber(routeWithShortName, "3710"));
    }

    [Fact]
    public async Task ResolveLineAsync_ShouldPreferTransitlandWhenAvailable()
    {
        var transitlandService = new FakeBusService(BusProvider.Transitland, new BusLine("tr-714", "714", "Linha 714", "#007bff", BusProvider.Transitland));
        var metropolitanaService = new FakeBusService(BusProvider.Metropolitana, new BusLine("met-714", "714", "Linha 714", "#6c757d", BusProvider.Metropolitana));
        var router = new BusServiceRouter(new[] { metropolitanaService, transitlandService });

        var result = await router.ResolveLineAsync("714");

        Assert.NotNull(result.Line);
        Assert.Same(transitlandService, result.Service);
        Assert.Equal("714", result.Line!.ShortName);
    }

    private sealed class FakeBusService : IBusService
    {
        private readonly BusLine? _line;

        public FakeBusService(BusProvider provider, BusLine? line)
        {
            Provider = provider;
            _line = line;
        }

        public BusProvider Provider { get; }

        public Task<BusLine?> GetLineAsync(string lineNumber) => Task.FromResult(_line);

        public Task<List<BusDirection>> GetDirectionsAsync(string lineId) => Task.FromResult(new List<BusDirection>());

        public Task<List<BusStop>> GetStopsAsync(string directionId, string lineId) => Task.FromResult(new List<BusStop>());

        public Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId) => Task.FromResult(new List<BusArrival>());
    }
}
