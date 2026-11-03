using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MyBusApp.Configuration;
using MyBusApp.Models.Domain;
using MyBusApp.Models.StaticData;
using MyBusApp.Services;
using Xunit;

namespace MyBusApp.Tests;

public sealed class CpServiceTests
{
    [Fact]
    public async Task GetStationsAsync_RequestsDefaultManifestRelativeToProvidedApplicationBaseUrl()
    {
        Uri? manifestRequestUri = null;
        var handler = new FakeHandler(request =>
        {
            manifestRequestUri = request.RequestUri;
            return JsonResponse(new GtfsStaticManifest(2, []));
        });
        var service = new CpService(new ApiSettings(), new HttpClient(handler)
        {
            BaseAddress = new Uri("https://app.test/")
        });

        await service.GetStationsAsync();

        Assert.Equal("https://app.test/data/cp/manifest.json", manifestRequestUri?.ToString());
    }

    [Fact]
    public async Task GetStationsAsync_ReturnsDistinctStationsFromCpLines()
    {
        var service = CreateService();

        var stations = await service.GetStationsAsync();

        Assert.Equal("CP", service.OperatorName);
        Assert.Equal(5, stations.Count);
        Assert.Contains(stations, station => station.Id == "porto");
        Assert.Contains(stations, station => station.Id == "lisboa");
        Assert.Contains(stations, station => station.Id == "rossio");
        Assert.Contains(stations, station => station.Id == "amadora");
        Assert.Contains(stations, station => station.Id == "sintra");
    }

    [Fact]
    public async Task GetDestinationsAsync_UsesRouteIdAndFriendlyEndpointsForStation()
    {
        var service = CreateService();

        var destinations = await service.GetDestinationsAsync("rossio");

        Assert.Equal(2, destinations.Count);
        Assert.Contains(destinations, direction => direction.Id == "Linha de Sintra\u001froute-rossio-sintra" && direction.Name == "Lisboa Rossio → Sintra");
        Assert.Contains(destinations, direction => direction.Id == "Linha de Sintra\u001froute-sintra-rossio" && direction.Name == "Sintra → Lisboa Rossio");
    }

    [Fact]
    public async Task GetStationsAsync_DeduplicatesStationIdsAcrossLines()
    {
        var service = CreateService();

        var stations = await service.GetStationsAsync();

        Assert.Equal(1, stations.Count(station => station.Id == "porto"));
        Assert.Equal(1, stations.Count(station => station.Id == "lisboa"));
    }

    [Fact]
    public async Task GetDestinationsAsync_UsesOrdinalTieBreakForEquallyFrequentSequencesDeterministically()
    {
        const string routeId = "route-tie";
        var sequenceAlpha = new[] { "alpha-stop", "terminal" };
        var sequenceBeta = new[] { "beta-stop", "terminal" };
        var sequenceAlphaKey = string.Join("\u001f", sequenceAlpha);
        var sequenceBetaKey = string.Join("\u001f", sequenceBeta);
        var trips = new List<GtfsStaticTrip>
        {
            new("trip-beta-1", routeId, "service", "", ""),
            new("trip-alpha-1", routeId, "service", "", ""),
            new("trip-beta-2", routeId, "service", "", ""),
            new("trip-alpha-2", routeId, "service", "", "")
        };
        var stopTimes = new List<GtfsStaticStopTime>
        {
            new("trip-beta-1", "terminal", 120, 180, 2),
            new("trip-beta-1", "beta-stop", 60, 90, 1),
            new("trip-alpha-1", "terminal", 120, 180, 2),
            new("trip-alpha-1", "alpha-stop", 60, 90, 1),
            new("trip-beta-2", "terminal", 120, 180, 2),
            new("trip-beta-2", "beta-stop", 60, 90, 1),
            new("trip-alpha-2", "terminal", 120, 180, 2),
            new("trip-alpha-2", "alpha-stop", 60, 90, 1)
        };
        var tripSequenceKeys = trips
            .Select(trip => string.Join("\u001f", stopTimes
                .Where(stopTime => stopTime.TripId == trip.Id)
                .OrderBy(stopTime => stopTime.Sequence)
                .Select(stopTime => stopTime.StopId)))
            .ToArray();
        var sequenceFrequencies = tripSequenceKeys
            .GroupBy(sequence => sequence, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        Assert.NotEqual(sequenceAlphaKey, sequenceBetaKey);
        Assert.Equal(sequenceBetaKey, tripSequenceKeys[0]);
        Assert.Equal(2, sequenceFrequencies[sequenceAlphaKey]);
        Assert.Equal(2, sequenceFrequencies[sequenceBetaKey]);
        Assert.Equal(2, sequenceFrequencies.Count);
        Assert.True(StringComparer.Ordinal.Compare(sequenceAlphaKey, sequenceBetaKey) < 0);

        var line = new GtfsStaticLine(
            2,
            "Tie test",
            "Tie test",
            [new("route-tie|Terminal", routeId, "Terminal")],
            [
                new("alpha-stop", "Alpha Stop", ""),
                new("beta-stop", "Beta Stop", ""),
                new("terminal", "Terminal", "")
            ],
            trips,
            stopTimes,
            [new("service", "20260101", "20261231", true, true, true, true, true, true, true)],
            []);
        var manifest = new GtfsStaticManifest(2, [new("Tie test", "lines/tie.json", "Tie test")]);
        var handler = new FakeHandler(request => request.RequestUri!.AbsolutePath.EndsWith("manifest.json", StringComparison.Ordinal)
            ? JsonResponse(manifest)
            : JsonResponse(line));
        var settings = new ApiSettings();
        settings.Cp.StaticDataBaseUrl = "https://static.test/cp/";
        var service = new CpService(settings, new HttpClient(handler)
        {
            BaseAddress = new Uri("https://static.test/")
        });
        const string expectedDirectionName = "Alpha Stop → Terminal";
        var results = new List<string[]>();

        for (var repetition = 0; repetition < 10; repetition++)
        {
            var destinations = await service.GetDestinationsAsync("alpha-stop");
            results.Add(destinations.Select(destination => destination.Name).ToArray());
        }

        Assert.All(results, result => Assert.Equal([expectedDirectionName], result));
        Assert.All(results.Skip(1), result => Assert.Equal(results[0], result));
    }

    [Fact]
    public async Task GetDeparturesAsync_PrefersDepartureAndPreservesGtfsOver24Hours()
    {
        var localNow = TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon"));
        var line = CreateLine("AP", "route-ap", "Porto Campanha", "Lisboa Santa Apolonia", [
            new("trip-ap", "route-ap", "service", "", "Lisboa Santa Apolonia")
        ], [
            new("trip-ap", "porto", 24 * 3600 + 15 * 60, 25 * 3600 + 30 * 60, 1)
        ]) with
        {
            Calendar = [new(
                "service",
                localNow.Date.AddDays(-1).ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture),
                localNow.Date.AddDays(2).ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture),
                true, true, true, true, true, true, true)]
        };

        var service = CreateService(JsonSerializer.Serialize(line), onlyApLine: true);
        var direction = Assert.Single(await service.GetDestinationsAsync("porto"));
        var departures = await service.GetDeparturesAsync("porto", direction.Id);

        Assert.Contains(departures, departure =>
            departure.ServiceName == "AP" &&
            departure.Destination == "Lisboa Santa Apolonia" &&
            departure.DisplayTime == "01:30");
    }

    [Fact]
    public async Task GetDeparturesAsync_AcceptsNumericStopTimeFieldsAndReturnsExpectedSchedule()
    {
        var localNow = TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon"));
        var scheduledSeconds = (int)localNow.TimeOfDay.TotalSeconds + 3600;
        var line = CreateLine("AP", "route-ap", "Porto Campanha", "Lisboa Santa Apolonia", [
            new("trip-ap", "route-ap", "service", "", "Lisboa Santa Apolonia")
        ], [new("trip-ap", "porto", scheduledSeconds - 60, scheduledSeconds, 1)]) with
        {
            Calendar = [new(
                "service",
                localNow.Date.AddDays(-1).ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture),
                localNow.Date.AddDays(1).ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture),
                true, true, true, true, true, true, true)]
        };
        var service = CreateService(JsonSerializer.Serialize(line), onlyApLine: true);

        var direction = Assert.Single(await service.GetDestinationsAsync("porto"));
        var departures = await service.GetDeparturesAsync("porto", direction.Id);

        Assert.NotEmpty(departures);
        Assert.Equal(
            TimeSpan.FromSeconds(scheduledSeconds % (24 * 60 * 60)).ToString(@"hh\:mm", System.Globalization.CultureInfo.InvariantCulture),
            departures[0].DisplayTime);
    }

    [Fact]
    public async Task TrainFlow_UsesITrainServiceForStationsDestinationsAndDepartures()
    {
        var localNow = TimeZoneInfo.ConvertTime(
            DateTimeOffset.UtcNow,
            TimeZoneInfo.FindSystemTimeZoneById("Europe/Lisbon"));
        var scheduledSeconds = (int)localNow.TimeOfDay.TotalSeconds + 3600;
        var line = CreateLine("AP", "route-ap", "Porto Campanha", "Lisboa Santa Apolonia", [
            new("trip-ap", "route-ap", "service", "", "Lisboa Santa Apolonia")
        ], [new("trip-ap", "porto", scheduledSeconds - 60, scheduledSeconds, 1)]) with
        {
            Calendar = [new(
                "service",
                localNow.Date.AddDays(-1).ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture),
                localNow.Date.AddDays(1).ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture),
                true, true, true, true, true, true, true)]
        };
        ITrainService service = CreateService(JsonSerializer.Serialize(line), onlyApLine: true);

        var station = Assert.Single(await service.GetStationsAsync(), item => item.Id == "porto");
        var direction = Assert.Single(await service.GetDestinationsAsync(station.Id));
        var departures = await service.GetDeparturesAsync(station.Id, direction.Id);

        Assert.Equal("CP", service.OperatorName);
        Assert.Contains(departures, departure =>
            departure.ServiceName == "AP" &&
            departure.Destination == "Lisboa Santa Apolonia");
    }

    [Theory]
    [InlineData("arrivalTimeSeconds", true)]
    [InlineData("arrivalTimeSeconds", false)]
    [InlineData("departureTimeSeconds", true)]
    [InlineData("departureTimeSeconds", false)]
    public async Task GetStationsAsync_ReportsMissingOrInvalidStopTimeFields(string fieldName, bool isMissing)
    {
        var service = CreateService(CreateApLineJsonWithInvalidStopTimeField(fieldName, isMissing), onlyApLine: true);

        await Assert.ThrowsAsync<InvalidDataException>(() => service.GetStationsAsync());
    }

    [Fact]
    public async Task GetStationsAsync_ThrowsUnavailableWhenManifestRequestFails()
    {
        var service = CreateService(new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        await Assert.ThrowsAsync<TransitSourceUnavailableException>(() => service.GetStationsAsync());
    }

    [Fact]
    public async Task GetStationsAsync_ThrowsUnavailableWhenManifestRequestRaisesNetworkException()
    {
        var service = CreateService(new FakeHandler(_ => throw new HttpRequestException("network failure")));

        await Assert.ThrowsAsync<TransitSourceUnavailableException>(() => service.GetStationsAsync());
    }

    [Fact]
    public async Task GetTrainDataAsync_LoadsRealSintraDataReferencedByManifest()
    {
        var cpDataDirectory = FindCpDataDirectory();
        var manifestPath = Path.Combine(cpDataDirectory, "manifest.json");
        Assert.True(File.Exists(manifestPath), $"CP manifest not found: {manifestPath}");

        var manifestJson = await File.ReadAllTextAsync(manifestPath);
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };
        var manifest = JsonSerializer.Deserialize<GtfsStaticManifest>(manifestJson, jsonOptions);
        Assert.NotNull(manifest);
        Assert.Equal(2, manifest.Version);

        var manifestLine = Assert.Single(manifest.Lines, entry => entry.Number == "Linha de Sintra");
        Assert.False(string.IsNullOrWhiteSpace(manifestLine.File));
        Assert.False(Path.IsPathRooted(manifestLine.File));

        var cpRoot = Path.TrimEndingDirectorySeparator(Path.GetFullPath(cpDataDirectory));
        var expectedPathPrefix = cpRoot + Path.DirectorySeparatorChar;
        var lineJsonByFile = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in manifest.Lines)
        {
            var linePath = Path.GetFullPath(Path.Combine(
                cpRoot,
                entry.File.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar)));
            Assert.StartsWith(expectedPathPrefix, linePath, StringComparison.Ordinal);
            Assert.True(File.Exists(linePath), $"Manifest-referenced CP line not found: {linePath}");
            lineJsonByFile.Add(entry.File, await File.ReadAllTextAsync(linePath));
        }

        var lineJson = lineJsonByFile[manifestLine.File];
        var line = JsonSerializer.Deserialize<GtfsStaticLine>(lineJson, jsonOptions);
        Assert.NotNull(line);
        Assert.Equal(2, line.Version);
        Assert.Equal("Linha de Sintra", line.Number);
        Assert.NotEmpty(line.Directions);
        Assert.NotEmpty(line.Stops);
        Assert.NotEmpty(line.Trips);
        Assert.NotEmpty(line.StopTimes);

        using (var lineDocument = JsonDocument.Parse(lineJson))
        {
            var rawStopTimes = lineDocument.RootElement.GetProperty("stopTimes");
            Assert.Equal(JsonValueKind.Array, rawStopTimes.ValueKind);
            Assert.Equal(line.StopTimes.Count, rawStopTimes.GetArrayLength());

            var stopTimeIndex = 0;
            foreach (var rawStopTime in rawStopTimes.EnumerateArray())
            {
                Assert.Equal(JsonValueKind.Object, rawStopTime.ValueKind);
                Assert.True(rawStopTime.TryGetProperty("arrivalTimeSeconds", out var arrivalTime));
                Assert.True(arrivalTime.TryGetInt32(out var arrivalSeconds));
                Assert.True(rawStopTime.TryGetProperty("departureTimeSeconds", out var departureTime));
                Assert.True(departureTime.TryGetInt32(out var departureSeconds));
                Assert.Equal(line.StopTimes[stopTimeIndex].ArrivalTimeSeconds, arrivalSeconds);
                Assert.Equal(line.StopTimes[stopTimeIndex].DepartureTimeSeconds, departureSeconds);
                stopTimeIndex++;
            }
        }

        Assert.Contains(line.StopTimes, stopTime =>
            stopTime.ArrivalTimeSeconds >= 24 * 60 * 60 ||
            stopTime.DepartureTimeSeconds >= 24 * 60 * 60);

        var handler = new FakeHandler(request =>
        {
            var requestPath = Uri.UnescapeDataString(request.RequestUri!.AbsolutePath);
            if (requestPath.EndsWith("/manifest.json", StringComparison.Ordinal))
                return RawJsonResponse(manifestJson);
            foreach (var entry in manifest.Lines)
            {
                if (requestPath.EndsWith("/" + entry.File, StringComparison.Ordinal))
                    return RawJsonResponse(lineJsonByFile[entry.File]);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var settings = new ApiSettings();
        settings.Cp.StaticDataBaseUrl = "https://static.test/cp/";
        var service = new CpService(settings, new HttpClient(handler)
        {
            BaseAddress = new Uri("https://static.test/")
        });

        Assert.Equal("CP", service.OperatorName);
        var stations = await service.GetStationsAsync();
        Assert.All(line.Stops, realStop => Assert.Contains(stations, station => station.Id == realStop.Id));

        var sampleStopTime = line.StopTimes.First(stopTime =>
            stopTime.ArrivalTimeSeconds >= 24 * 60 * 60 ||
            stopTime.DepartureTimeSeconds >= 24 * 60 * 60);
        var sampleTrip = line.Trips.Single(trip => trip.Id == sampleStopTime.TripId);
        var expectedDirectionId = $"{line.Number}\u001f{sampleTrip.RouteId}";
        var destinations = await service.GetDestinationsAsync(sampleStopTime.StopId);
        var destination = Assert.Single(destinations, item => item.Id == expectedDirectionId);
        Assert.False(string.IsNullOrWhiteSpace(destination.Name));
    }

    private static CpService CreateService(string? apLineJson = null, bool onlyApLine = false)
    {
        var lines = new Dictionary<string, GtfsStaticLine>(StringComparer.Ordinal)
        {
            ["ap.json"] = CreateLine("AP", "route-ap", "Porto Campanha", "Lisboa Santa Apolonia", [
                new("trip-ap", "route-ap", "service", "", "Lisboa Santa Apolonia")
            ], [new("trip-ap", "porto", 3600, 3660, 1)]),
            ["ic.json"] = CreateLine("IC", "route-ic", "Lisboa Santa Apolonia", "Guarda", [
                new("trip-ic", "route-ic", "service", "", "Guarda")
            ], [new("trip-ic", "lisboa", 7200, 7260, 1)]),
            ["sintra.json"] = CreateSintraLine()
        };
        var manifestLines = new List<GtfsStaticManifestLine>
        {
            new("AP", "lines/ap.json", "AP"),
            new("IC", "lines/ic.json", "IC"),
            new("Linha de Sintra", "lines/sintra.json", "Linha de Sintra")
        };
        if (onlyApLine)
            manifestLines = [manifestLines[0]];
        var manifest = new GtfsStaticManifest(2, manifestLines);
        var handler = new FakeHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("manifest.json", StringComparison.Ordinal))
                return JsonResponse(manifest);

            var fileName = Path.GetFileName(request.RequestUri.AbsolutePath);
            if (fileName == "ap.json" && apLineJson is not null)
                return RawJsonResponse(apLineJson);

            return lines.TryGetValue(fileName, out var line)
                ? JsonResponse(line)
                : new HttpResponseMessage(HttpStatusCode.NotFound);
        });
        var settings = new ApiSettings();
        settings.Cp.StaticDataBaseUrl = "https://static.test/cp/";
        return new CpService(settings, new HttpClient(handler)
        {
            BaseAddress = new Uri("https://static.test/")
        });
    }

    private static CpService CreateService(HttpMessageHandler handler)
    {
        var settings = new ApiSettings();
        settings.Cp.StaticDataBaseUrl = "https://static.test/cp/";
        return new CpService(settings, new HttpClient(handler)
        {
            BaseAddress = new Uri("https://static.test/")
        });
    }

    private static string CreateApLineJsonWithInvalidStopTimeField(string fieldName, bool isMissing)
    {
        var line = CreateLine("AP", "route-ap", "Porto Campanha", "Lisboa Santa Apolonia", [
            new("trip-ap", "route-ap", "service", "", "Lisboa Santa Apolonia")
        ], [new("trip-ap", "porto", 3600, 3660, 1)]);
        var json = JsonSerializer.SerializeToNode(line)!.AsObject();
        var stopTime = json["stopTimes"]![0]!.AsObject();
        if (isMissing)
            stopTime.Remove(fieldName);
        else
            stopTime[fieldName] = "invalid";

        return json.ToJsonString();
    }

    private static string FindCpDataDirectory()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var cpDataDirectory = Path.Combine(directory.FullName, "wwwroot", "data", "cp");
            if (File.Exists(Path.Combine(cpDataDirectory, "manifest.json")))
                return cpDataDirectory;
        }

        throw new DirectoryNotFoundException("Could not locate wwwroot/data/cp/ from the test assembly directory.");
    }

    private static GtfsStaticLine CreateSintraLine()
        => new(
            2,
            "Linha de Sintra",
            "Linha de Sintra",
            [
                new("route-rossio-sintra|Sintra", "route-rossio-sintra", "Sintra"),
                new("route-sintra-rossio|Lisboa Rossio", "route-sintra-rossio", "Lisboa Rossio")
            ],
            [
                new("rossio", "Lisboa Rossio", ""),
                new("amadora", "Amadora", ""),
                new("sintra", "Sintra", "")
            ],
            [
                new("sintra-1", "route-rossio-sintra", "service", "", "Sintra"),
                new("sintra-2", "route-rossio-sintra", "service", "", "Sintra"),
                new("sintra-3", "route-rossio-sintra", "service", "", "Sintra"),
                new("rossio-1", "route-sintra-rossio", "service", "", "Lisboa Rossio")
            ],
            [
                new("sintra-1", "rossio", 3600, 3660, 1),
                new("sintra-1", "amadora", 4200, 4260, 2),
                new("sintra-1", "sintra", 4800, 4860, 3),
                new("sintra-2", "rossio", 5400, 5460, 1),
                new("sintra-2", "amadora", 6000, 6060, 2),
                new("sintra-2", "sintra", 6600, 6660, 3),
                new("sintra-3", "rossio", 7200, 7260, 1),
                new("sintra-3", "sintra", 7800, 7860, 2),
                new("rossio-1", "sintra", 9000, 9060, 1),
                new("rossio-1", "amadora", 9600, 9660, 2),
                new("rossio-1", "rossio", 10200, 10260, 3)
            ],
            [new("service", "20260101", "20261231", true, true, true, true, true, true, true)],
            []);

    private static GtfsStaticLine CreateLine(
        string number,
        string routeId,
        string firstStopName,
        string lastStopName,
        List<GtfsStaticTrip> trips,
        List<GtfsStaticStopTime> stopTimes)
        => new(
            2,
            number,
            number,
            [new($"{routeId}|{lastStopName}", routeId, lastStopName)],
            [new("porto", firstStopName, ""), new("lisboa", lastStopName, "")],
            trips,
            stopTimes,
            [new("service", "20260101", "20261231", true, true, true, true, true, true, true)],
            []);

    private static HttpResponseMessage JsonResponse<T>(T value)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage RawJsonResponse(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }
}
