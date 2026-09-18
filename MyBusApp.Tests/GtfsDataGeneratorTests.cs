using System.IO.Compression;
using System.Text.Json;
using MyBusApp.Tools.GtfsDataGenerator;
using Xunit;

namespace MyBusApp.Tests;

public sealed class GtfsDataGeneratorTests
{
    [Fact]
    public void Generate_writes_basic_gtfs_data_and_preserves_times_above_24_hours()
    {
        var root = Directory.CreateTempSubdirectory("mybusapp-generator-test-");
        try
        {
            var gtfsPath = Path.Combine(root.FullName, "feed.zip");
            var outputPath = Path.Combine(root.FullName, "output");
            CreateGtfsZip(gtfsPath);

            new GtfsStaticDataGenerator().Generate(gtfsPath, outputPath);

            var linePath = Assert.Single(Directory.GetFiles(Path.Combine(outputPath, "lines"), "*.json"));
            using var lineDocument = JsonDocument.Parse(File.ReadAllText(linePath));
            var stopTimes = lineDocument.RootElement.GetProperty("stopTimes").EnumerateArray().ToList();
            Assert.Equal(2, stopTimes.Count);
            var stopTime = stopTimes[0];

            Assert.Equal(88200, stopTime.GetProperty("arrivalTimeSeconds").GetInt32());
            Assert.Equal(90900, stopTime.GetProperty("departureTimeSeconds").GetInt32());
            Assert.DoesNotContain("timeSeconds", stopTime.GetRawText(), StringComparison.Ordinal);

            using var manifestDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputPath, "manifest.json")));
            Assert.Single(manifestDocument.RootElement.GetProperty("lines").EnumerateArray());
            Assert.Equal(2, manifestDocument.RootElement.GetProperty("version").GetInt32());
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Generate_groups_routes_with_the_same_short_name()
    {
        var root = Directory.CreateTempSubdirectory("gtfs-generator-grouping-");
        try
        {
            var gtfsPath = Path.Combine(root.FullName, "feed.zip");
            var outputPath = Path.Combine(root.FullName, "output");
            CreateZip(gtfsPath,
                ("routes.txt", "route_id,route_short_name,route_long_name\nroute-a,123,Route A\nroute-b,123,Route B\n"),
                ("trips.txt", "route_id,service_id,trip_id,direction_id,trip_headsign\nroute-a,service-1,trip-a,0,Destination A\nroute-b,service-1,trip-b,1,Destination B\n"),
                ("stops.txt", "stop_id,stop_name,stop_desc\nstop-a,Stop A,Lisboa\nstop-b,Stop B,Lisboa\n"),
                ("stop_times.txt", "trip_id,arrival_time,departure_time,stop_id,stop_sequence\ntrip-a,08:00:00,08:01:00,stop-a,1\ntrip-b,09:00:00,09:01:00,stop-b,1\n"),
                ("calendar.txt", "service_id,start_date,end_date,monday,tuesday,wednesday,thursday,friday,saturday,sunday\nservice-1,20260101,20261231,1,1,1,1,1,1,1\n"));

            new GtfsStaticDataGenerator().Generate(gtfsPath, outputPath);

            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(outputPath, "manifest.json")));
            var line = Assert.Single(manifest.RootElement.GetProperty("lines").EnumerateArray());
            Assert.Equal("123", line.GetProperty("number").GetString());
            var linePath = Path.Combine(outputPath, line.GetProperty("file").GetString()!);
            using var lineDocument = JsonDocument.Parse(File.ReadAllText(linePath));
            Assert.Equal(2, lineDocument.RootElement.GetProperty("trips").GetArrayLength());
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Generate_fails_when_route_short_name_is_empty()
    {
        var root = Directory.CreateTempSubdirectory("gtfs-generator-identity-");
        try
        {
            var gtfsPath = Path.Combine(root.FullName, "feed.zip");
            CreateZip(gtfsPath, ("routes.txt", "route_id,route_short_name,route_long_name\nroute-1,,Unnamed route\n"));

            var exception = Assert.Throws<InvalidDataException>(() => new GtfsStaticDataGenerator().Generate(gtfsPath, root.FullName));

            Assert.Contains("route_short_name", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            root.Delete(true);
        }
    }

    [Fact]
    public void Generate_fails_when_sanitized_filenames_collide()
    {
        var root = Directory.CreateTempSubdirectory("gtfs-generator-collision-");
        try
        {
            var gtfsPath = Path.Combine(root.FullName, "feed.zip");
            CreateZip(gtfsPath,
                ("routes.txt", "route_id,route_short_name,route_long_name\nroute-a,a/b,Route A\nroute-b,a_b,Route B\n"),
                ("trips.txt", "route_id,service_id,trip_id,direction_id,trip_headsign\nroute-a,service-1,trip-a,0,Destination A\nroute-b,service-1,trip-b,0,Destination B\n"),
                ("stops.txt", "stop_id,stop_name,stop_desc\nstop-a,Stop A,Lisboa\nstop-b,Stop B,Lisboa\n"),
                ("stop_times.txt", "trip_id,arrival_time,departure_time,stop_id,stop_sequence\ntrip-a,08:00:00,08:01:00,stop-a,1\ntrip-b,09:00:00,09:01:00,stop-b,1\n"));

            var exception = Assert.Throws<InvalidDataException>(() => new GtfsStaticDataGenerator().Generate(gtfsPath, root.FullName));

            Assert.Contains("collides", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            root.Delete(true);
        }
    }

    private static void CreateGtfsZip(string path)
    {
        CreateZip(path,
            ("routes.txt", "route_id,route_short_name,route_long_name\nroute-714,714,Test Route\n"),
            ("trips.txt", "route_id,service_id,trip_id,direction_id,trip_headsign\nroute-714,service-1,trip-1,0,Test Destination\n"),
            ("stops.txt", "stop_id,stop_name,stop_desc\nstop-1,Test Stop,Lisboa\nstop-2,Test Stop 2,Lisboa\n"),
            ("stop_times.txt", "trip_id,arrival_time,departure_time,stop_id,stop_sequence\ntrip-1,24:30:00,25:15:00,stop-1,1\ntrip-1,25:15:00,25:15:00,stop-2,2\n"),
            ("calendar.txt", "service_id,start_date,end_date,monday,tuesday,wednesday,thursday,friday,saturday,sunday\nservice-1,20260101,20261231,1,1,1,1,1,1,1\n"));
    }

    private static void CreateZip(string path, params (string Name, string Content)[] entries)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var entry in entries)
            AddEntry(archive, entry.Name, entry.Content);
    }

    private static void AddEntry(ZipArchive archive, string name, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(name).Open());
        writer.Write(content);
    }
}