using System.Net;
using System.Text;
using System.Text.Json;
using MyBusApp.Configuration;
using MyBusApp.Models.StaticData;
using MyBusApp.Services;
using Xunit;

namespace MyBusApp.Tests;

public class CarrisLisboaStaticDataTests
{
    [Fact]
    public async Task GetLineAndDirections_LoadManifestAndLineOnlyOnce()
    {
        var line = CreateLine();
        var requestCount = 0;
        var handler = new FakeHandler(request =>
        {
            requestCount++;
            return request.RequestUri!.AbsolutePath.EndsWith("manifest.json", StringComparison.Ordinal)
                ? JsonResponse(new GtfsStaticManifest(2, [new("714", "lines/714.json", "Cais Sodré - Estr. Queluz")]))
                : JsonResponse(line);
        });
        var service = CreateService(handler);

        var resolvedLine = await service.GetLineAsync("714");
        var directions = await service.GetDirectionsAsync("714");

        Assert.Equal("714", resolvedLine?.ShortName);
        Assert.Single(directions);
        Assert.Equal("Cais Sodré - Estr. Queluz", directions[0].Name);
        Assert.Equal(2, requestCount);
    }

    [Fact]
    public void VersionedLineJson_UsesCurrentStopTimeSchema()
    {
        var manifestPath = FindRepositoryFile("wwwroot/data/carris-lisboa/manifest.json");
        var manifest = JsonSerializer.Deserialize<GtfsStaticManifest>(File.ReadAllText(manifestPath));

        Assert.NotNull(manifest);
        Assert.Equal(2, manifest.Version);
        Assert.NotEmpty(manifest.Lines);
        var manifestLine = manifest.Lines.First();
        var lineJson = File.ReadAllText(Path.Combine(Path.GetDirectoryName(manifestPath)!, manifestLine.File));
        var line = JsonSerializer.Deserialize<GtfsStaticLine>(lineJson);
        Assert.NotNull(line);
        var stopTime = line.StopTimes.First();

        Assert.True(stopTime.ArrivalTimeSeconds > 0);
        Assert.True(stopTime.DepartureTimeSeconds > 0);
        Assert.DoesNotContain("timeSeconds", lineJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetLineAsync_RejectsOldStopTimeSchema()
    {
        var oldLineJson = """
            {"version":2,"number":"714","name":"Old schema","directions":[],"stops":[],"trips":[],"stopTimes":[{"tripId":"trip-1","stopId":"stop-1","timeSeconds":3600,"sequence":1}],"calendar":[],"calendarDates":[]}
            """;
        var handler = new FakeHandler(request => request.RequestUri!.AbsolutePath.EndsWith("manifest.json", StringComparison.Ordinal)
            ? JsonResponse(new GtfsStaticManifest(2, [new("714", "lines/714.json", "Old schema")]))
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(oldLineJson, Encoding.UTF8, "application/json")
            });
        var service = CreateService(handler);

        Assert.Null(await service.GetLineAsync("714"));
    }

    [Fact]
    public void IsServiceActive_AppliesCalendarAndDateExceptions()
    {
        var date = new DateTime(2026, 9, 17);
        var line = CreateLine(
            calendar: [new("weekday", "20260101", "20261231", false, false, false, true, false, false, false)],
            calendarDates: [
                new("weekday", "20260917", 2),
                new("weekday", "20260918", 1)
            ]);

        Assert.False(CarrisLisboaService.IsServiceActive(line, "weekday", date));
        Assert.True(CarrisLisboaService.IsServiceActive(line, "weekday", date.AddDays(1)));
        Assert.False(CarrisLisboaService.IsServiceActive(line, "weekday", date.AddDays(2)));
    }

    [Fact]
    public async Task GetArrivals_FiltersPastAndInactiveServicesAndKeepsScheduledFlag()
    {
        var lisbonZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon");
        var today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, lisbonZone).Date;
        var date = today.ToString("yyyyMMdd");
        var currentSeconds = (int)TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, lisbonZone).TimeOfDay.TotalSeconds;
        var line = CreateLine(
            trips: [
                new("past", "102_4", "active", "0", "Past destination"),
                new("future", "102_4", "active", "0", "Future destination"),
                new("overnight", "102_4", "active", "0", "Overnight destination"),
                new("inactive", "102_4", "inactive", "0", "Inactive destination")
            ],
            stopTimes: [
                new("past", "stop-1", currentSeconds - 60, currentSeconds - 60, 1),
                new("future", "stop-1", currentSeconds + 60, currentSeconds + 60, 1),
                new("overnight", "stop-1", 26 * 60 * 60, 26 * 60 * 60, 1),
                new("inactive", "stop-1", currentSeconds + 120, currentSeconds + 120, 1)
            ],
            calendar: [
                new("active", date, date, true, true, true, true, true, true, true),
                new("inactive", "20000101", "20000102", true, true, true, true, true, true, true)
            ]);
        var handler = new FakeHandler(request => request.RequestUri!.AbsolutePath.EndsWith("manifest.json", StringComparison.Ordinal)
            ? JsonResponse(new GtfsStaticManifest(2, [new("714", "lines/714.json", line.Name)]))
            : JsonResponse(line));
        var service = CreateService(handler);

        var arrivals = await service.GetArrivalsAsync("stop-1", "714");

        Assert.Equal(2, arrivals.Count);
        Assert.DoesNotContain(arrivals, arrival => arrival.Destination == "Past destination");
        Assert.DoesNotContain(arrivals, arrival => arrival.Destination == "Inactive destination");
        Assert.Contains(arrivals, arrival => arrival.Destination == "Overnight destination" && arrival.DisplayTime == "02:00");
        Assert.All(arrivals, arrival => Assert.False(arrival.IsRealTime));
    }

    [Fact]
    public void GetUpcomingArrivals_PrefersDepartureTimeAndFallsBackToArrivalTime()
    {
        var date = new DateTime(2026, 9, 18);
        var line = CreateLine(
            trips: [
                new("departure", "102_4", "active", "0", "Departure destination"),
                new("arrival", "102_4", "active", "0", "Arrival destination")
            ],
            stopTimes: [
                new("departure", "stop-1", 3600, 3660, 1),
                new("arrival", "stop-1", 7200, 0, 1)
            ],
            calendar: [new("active", "20260918", "20260918", true, true, true, true, true, true, true)]);

        var arrivals = CarrisLisboaService.GetUpcomingArrivals(
            line,
            "stop-1",
            new DateTimeOffset(date, TimeSpan.FromHours(1)));

        Assert.Equal(["01:01", "02:00"], arrivals.Select(arrival => arrival.DisplayTime));
    }

    [Fact]
    public void GetUpcomingArrivals_UsesServiceDateForOvernightTripsAndExceptions()
    {
        var monday = new DateTime(2026, 9, 14);
        var now = new DateTimeOffset(new DateTime(2026, 9, 15, 0, 30, 0), TimeSpan.FromHours(1));
        var line = CreateLine(
            trips: [
                new("overnight", "102_4", "weekday", "0", "Overnight destination"),
                new("next-day", "102_4", "next-day", "0", "Next day destination")
            ],
            stopTimes: [
                new("overnight", "stop-1", 25 * 60 * 60, 25 * 60 * 60, 1),
                new("next-day", "stop-1", 2 * 60 * 60, 2 * 60 * 60, 1)
            ],
            calendar: [
                new("weekday", "20260101", "20261231", true, false, false, false, false, false, false),
                new("next-day", "20260101", "20261231", false, true, false, false, false, false, false)
            ]);

        var arrivals = CarrisLisboaService.GetUpcomingArrivals(line, "stop-1", now);

        Assert.Equal(["Overnight destination", "Next day destination"], arrivals.Select(arrival => arrival.Destination));
        Assert.Equal("01:00", arrivals[0].DisplayTime);
        Assert.All(arrivals, arrival => Assert.False(arrival.IsRealTime));

        var removedMonday = line with
        {
            CalendarDates = [new("weekday", monday.ToString("yyyyMMdd"), 2)]
        };
        var withoutRemovedService = CarrisLisboaService.GetUpcomingArrivals(removedMonday, "stop-1", now);

        Assert.DoesNotContain(withoutRemovedService, arrival => arrival.Destination == "Overnight destination");
        Assert.Contains(withoutRemovedService, arrival => arrival.Destination == "Next day destination");
    }

    [Fact]
    public async Task GetStops_FiltersByRouteAndDirection()
    {
        var line = CreateLine() with
        {
            Directions = [
                new("102_4|0", "102_4", "Rota 102_4"),
                new("102_5|0", "102_5", "Rota 102_5")
            ],
            Stops = [
                new("stop-4", "Paragem 102_4", "Lisboa"),
                new("stop-5", "Paragem 102_5", "Lisboa")
            ],
            Trips = [
                new("trip-4", "102_4", "active", "0", "Rota 102_4"),
                new("trip-5", "102_5", "active", "0", "Rota 102_5")
            ],
            StopTimes = [
                new("trip-4", "stop-4", 1, 1, 1),
                new("trip-5", "stop-5", 1, 1, 1)
            ]
        };
        var handler = new FakeHandler(request => request.RequestUri!.AbsolutePath.EndsWith("manifest.json", StringComparison.Ordinal)
            ? JsonResponse(new GtfsStaticManifest(2, [new("714", "lines/714.json", line.Name)]))
            : JsonResponse(line));
        var service = CreateService(handler);

        var stops = await service.GetStopsAsync("102_4|0", "714");

        Assert.Single(stops);
        Assert.Equal("stop-4", stops[0].Id);
    }

    private static CarrisLisboaService CreateService(HttpMessageHandler handler)
    {
        var settings = new ApiSettings();
        settings.Carris.StaticDataBaseUrl = "https://static.test/carris-lisboa/";
        return new CarrisLisboaService(settings, new HttpClient(handler)
        {
            BaseAddress = new Uri("https://static.test/")
        });
    }

    private static GtfsStaticLine CreateLine(
        List<GtfsStaticTrip>? trips = null,
        List<GtfsStaticStopTime>? stopTimes = null,
        List<GtfsStaticCalendar>? calendar = null,
        List<GtfsStaticCalendarDate>? calendarDates = null)
        => new(
            2,
            "714",
            "Cais Sodré - Estr. Queluz",
            [new("102_4|0", "102_4", "Cais Sodré - Estr. Queluz")],
            [new("stop-1", "Paragem 1", "Lisboa")],
            trips ?? [new("trip-1", "102_4", "active", "0", "Cais Sodré - Estr. Queluz")],
            stopTimes ?? [new("trip-1", "stop-1", 3600, 3600, 1)],
            calendar ?? [new("active", "20260101", "20261231", true, true, true, true, true, true, true)],
            calendarDates ?? []);

    private static string FindRepositoryFile(string relativePath)
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }
        }

        throw new FileNotFoundException($"Repository file not found: {relativePath}");
    }

    private static HttpResponseMessage JsonResponse<T>(T value)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
        };

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}