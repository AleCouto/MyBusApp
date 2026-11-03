using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using MyBusApp.Configuration;
using MyBusApp.Models.DTOs.CarrisMetropolitana;
using MyBusApp.Services;
using Xunit;

namespace MyBusApp.Tests;

public sealed class CarrisMetropolitanaArrivalTests
{
    [Fact]
    public async Task GetArrivalsAsync_RecognizesPrefixedIdsAndPreservesExistingFilters()
    {
        var currentTime = DateTime.Now.TimeOfDay;
        var earliestFutureTime = FormatApiTime(currentTime.Add(TimeSpan.FromMinutes(10)));
        var nextFutureTime = FormatApiTime(currentTime.Add(TimeSpan.FromMinutes(15)));
        var middleFutureTime = FormatApiTime(currentTime.Add(TimeSpan.FromMinutes(18)));
        var laterFutureTime = FormatApiTime(currentTime.Add(TimeSpan.FromMinutes(20)));
        var latestFutureTime = FormatApiTime(currentTime.Add(TimeSpan.FromMinutes(25)));
        var pastTime = FormatApiTime(DateTime.Now.TimeOfDay.Subtract(TimeSpan.FromMinutes(30)));
        var arrivals = new[]
        {
            CreateArrival("[A2L1N]4725", "[A2L1N]4725_0", "[A2L1N]4725_0_2", latestFutureTime, "Prefixed line"),
            CreateArrival("[A2L1N]9999", "[A2L1N]9999_0", "[A2L1N]4725_0_2", earliestFutureTime, "Prefixed pattern"),
            CreateArrival("4725", "4725_0", "4725_0_1", laterFutureTime, "Unprefixed line"),
            CreateArrival("9999", "4725", "other-pattern", nextFutureTime, "Unprefixed route"),
            CreateArrival("[A2L1N]9999", "[A2L1N]4725", "other-pattern", middleFutureTime, "Prefixed route"),
            CreateArrival("[A2L1N]4726", "[A2L1N]4726_0", "[A2L1N]4726_0_2", earliestFutureTime, "Other line"),
            CreateArrival("14725", "14725_0", "14725_0_1", earliestFutureTime, "Partial line match"),
            CreateArrival("[A2L1N]4725", "[A2L1N]4725_0", "[A2L1N]4725_0_2", pastTime, "Past arrival")
        };

        using var listener = new HttpListener();
        var port = GetAvailablePort();
        var serverBaseAddress = new Uri($"http://127.0.0.1:{port}/");
        listener.Prefixes.Add(serverBaseAddress.AbsoluteUri);
        listener.Start();

        var responseTask = Task.Run(async () =>
        {
            var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(10));
            var payload = JsonSerializer.SerializeToUtf8Bytes(arrivals);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = payload.Length;
            await context.Response.OutputStream.WriteAsync(payload);
            context.Response.Close();
            return context.Request.Url?.AbsolutePath;
        });

        var settings = new ApiSettings
        {
            CarrisMetropolitana = new ApiEndpoint
            {
                BaseUrl = new Uri(serverBaseAddress, "v2/").AbsoluteUri
            }
        };
        var service = new CarrisMetropolitanaService(settings);

        var results = await service.GetArrivalsAsync("test-stop", "4725")
            .WaitAsync(TimeSpan.FromSeconds(10));
        var requestPath = await responseTask;

        Assert.Equal("/v2/arrivals/by_stop/test-stop", requestPath);
        Assert.Equal(
            ["Prefixed pattern", "Unprefixed route", "Prefixed route", "Unprefixed line", "Prefixed line"],
            results.Select(arrival => arrival.Destination));
        Assert.DoesNotContain(results, arrival => arrival.Destination is "Other line" or "Partial line match" or "Past arrival");
    }

    [Fact]
    public async Task GetArrivalsAsync_DisplaysPublicLineNumbersAndKeepsTechnicalIdsForMatching()
    {
        var currentTime = DateTime.Now.TimeOfDay;
        var arrivals = new[]
        {
            CreateArrival(
                "[A2L1N]4725",
                "[A2L1N]4725_0",
                "[A2L1N]4725_0_2",
                FormatApiTime(currentTime.Add(TimeSpan.FromMinutes(10))),
                "4725 destination"),
            CreateArrival(
                "[YA15B]3710",
                "[YA15B]3710_0",
                "[YA15B]3710_0_1",
                FormatApiTime(currentTime.Add(TimeSpan.FromMinutes(15))),
                "3710 destination")
        };
        var lines = new[]
        {
            new Line("4725", "4725", "Line 4725"),
            new Line("3710", "3710", "Line 3710")
        };

        using var listener = new HttpListener();
        var port = GetAvailablePort();
        var serverBaseAddress = new Uri($"http://127.0.0.1:{port}/");
        listener.Prefixes.Add(serverBaseAddress.AbsoluteUri);
        listener.Start();

        var responseTask = Task.Run(async () =>
        {
            var requestPaths = new List<string?>();
            for (var i = 0; i < 3; i++)
            {
                var context = await listener.GetContextAsync().WaitAsync(TimeSpan.FromSeconds(10));
                requestPaths.Add(context.Request.Url?.AbsolutePath);
                var payload = context.Request.Url?.AbsolutePath.EndsWith("/lines", StringComparison.Ordinal) == true
                    ? JsonSerializer.SerializeToUtf8Bytes(lines)
                    : JsonSerializer.SerializeToUtf8Bytes(arrivals);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "application/json";
                context.Response.ContentLength64 = payload.Length;
                await context.Response.OutputStream.WriteAsync(payload);
                context.Response.Close();
            }

            return requestPaths;
        });

        var settings = new ApiSettings
        {
            CarrisMetropolitana = new ApiEndpoint
            {
                BaseUrl = new Uri(serverBaseAddress, "v2/").AbsoluteUri
            }
        };
        var service = new CarrisMetropolitanaService(settings);

        var line4725 = await service.GetLineAsync("4725");
        var line3710 = await service.GetLineAsync("3710");
        var arrivals4725 = await service.GetArrivalsAsync("test-stop", line4725!.Id);
        var arrivals3710 = await service.GetArrivalsAsync("test-stop", line3710!.Id);
        var requestPaths = await responseTask;

        Assert.Equal("4725", line4725.ShortName);
        Assert.Equal("3710", line3710.ShortName);
        Assert.Single(arrivals4725);
        Assert.Equal("4725", arrivals4725[0].LineNumber);
        Assert.Equal("4725 destination", arrivals4725[0].Destination);
        Assert.Single(arrivals3710);
        Assert.Equal("3710", arrivals3710[0].LineNumber);
        Assert.Equal("3710 destination", arrivals3710[0].Destination);
        Assert.Equal(
            ["/v2/lines", "/v2/arrivals/by_stop/test-stop", "/v2/arrivals/by_stop/test-stop"],
            requestPaths);
        Assert.Equal("[A2L1N]4725", arrivals[0].LineId);
        Assert.Equal("[YA15B]3710", arrivals[1].LineId);
    }

    private static Arrival CreateArrival(
        string lineId,
        string routeId,
        string patternId,
        string scheduledArrival,
        string headsign)
        => new(lineId, "test-stop", routeId, scheduledArrival, string.Empty, headsign, patternId, 1);

    private static string FormatApiTime(TimeSpan time)
    {
        var sign = time < TimeSpan.Zero ? "-" : string.Empty;
        var absoluteTime = time.Duration();
        return $"{sign}{(int)absoluteTime.TotalHours:00}:{absoluteTime.Minutes:00}:{absoluteTime.Seconds:00}";
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
