using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MyBusApp.Models.Domain;
using MyBusApp.Pages;
using MyBusApp.Services;
using MyBusApp.Utils;
using Xunit;

namespace MyBusApp.Tests;

public sealed class TrainsPageTests
{
    [Fact]
    public async Task FavoriteStar_TracksTheSelectedStationAndDestinationCombination()
    {
        var existingFavorite = new TransitFavorite(
            FavoriteTransportMode.Train,
            FavoriteProvider.Cp,
            "abrantes-id",
            "Abrantes",
            DestinationId: "route-entr-on");
        var jsRuntime = new MemoryJsRuntime(JsonSerializer.Serialize(
            new[] { existingFavorite },
            new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } }));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime>(jsRuntime);
        services.AddSingleton<FavoriteStore>();
        services.AddSingleton<AppLogger>();
        services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/trains"));
        services.AddSingleton<IEnumerable<ITrainService>>([new TestTrainService()]);
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        var componentId = renderer.AddRootComponent<Trains>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootAsync(componentId));
        Assert.False(renderer.HasAttributeInTree(componentId, "aria-pressed"));

        await renderer.Dispatcher.InvokeAsync(() => renderer.DispatchEventAsync(
            renderer.GetRootEventHandler(componentId, "onchange", 0),
            new ChangeEventArgs { Value = "CP" }));
        await renderer.Dispatcher.InvokeAsync(() => renderer.DispatchEventAsync(
            renderer.GetRootEventHandler(componentId, "onchange", 1),
            new ChangeEventArgs { Value = "abrantes-id" }));
        Assert.False(renderer.HasAttributeInTree(componentId, "aria-pressed"));

        await renderer.Dispatcher.InvokeAsync(() => renderer.DispatchEventAsync(
            renderer.GetRootEventHandler(componentId, "onchange", 2),
            new ChangeEventArgs { Value = "route-entr-on" }));
        Assert.True(renderer.HasAttributeValueInTree(componentId, "aria-pressed", "true"));
        Assert.True(renderer.HasAttributeContainingInTree(componentId, "aria-label", "Abrantes → Entroncamento"));

        await renderer.Dispatcher.InvokeAsync(() => renderer.DispatchEventAsync(
            renderer.GetRootEventHandler(componentId, "onchange", 2),
            new ChangeEventArgs { Value = "route-abr-lis" }));
        Assert.True(renderer.HasAttributeValueInTree(componentId, "aria-pressed", "false"));
        Assert.True(renderer.HasAttributeContainingInTree(componentId, "aria-label", "Abrantes → Lisboa Santa Apolonia"));

        await renderer.Dispatcher.InvokeAsync(() => renderer.DispatchEventAsync(
            renderer.GetEventHandlerInTree(componentId, "onclick"),
            new MouseEventArgs()));

        var savedFavorites = await provider.GetRequiredService<FavoriteStore>().LoadAsync();
        Assert.Equal(
            [existingFavorite, existingFavorite with { DestinationId = "route-abr-lis" }],
            savedFavorites);
        Assert.Contains("Próximas partidas para Lisboa Santa Apolonia", renderer.GetText(componentId));

        await renderer.Dispatcher.InvokeAsync(() =>
        {
            renderer.RemoveRoot(componentId);
            return Task.CompletedTask;
        });
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string uri)
            => Initialize("http://localhost/", uri);

        protected override void NavigateToCore(string uri, bool forceLoad)
            => Initialize("http://localhost/", uri);
    }

    [Fact]
    public async Task FavoriteRoute_PrefillsStationAndDestinationAndLoadsDepartures()
    {
        var favorite = new TransitFavorite(
            FavoriteTransportMode.Train,
            FavoriteProvider.Cp,
            "abrantes-id",
            "Abrantes",
            DestinationId: "route-entr-on");
        var jsRuntime = new MemoryJsRuntime(JsonSerializer.Serialize(
            new[] { favorite },
            new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } }));
        var trainService = new TestTrainService();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime>(jsRuntime);
        services.AddSingleton<FavoriteStore>();
        services.AddSingleton<AppLogger>();
        services.AddSingleton<NavigationManager>(
            new TestNavigationManager("http://localhost/trains?station=abrantes-id&destination=route-entr-on"));
        services.AddSingleton<IEnumerable<ITrainService>>([trainService]);
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        var componentId = renderer.AddRootComponent<Trains>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootAsync(componentId));

        var text = renderer.GetText(componentId);
        Assert.Contains("Abrantes", text);
        Assert.Contains("Próximas partidas para Entroncamento", text);
        Assert.Contains("route-entr-on", text);
        Assert.Equal(["route-entr-on"], trainService.RequestedDirectionIds);
        Assert.True(renderer.HasAttributeValueInTree(componentId, "value", "CP"));
        Assert.True(renderer.HasAttributeValueInTree(componentId, "value", "abrantes-id"));
        Assert.True(renderer.HasAttributeValueInTree(componentId, "value", "route-entr-on"));

        await renderer.Dispatcher.InvokeAsync(() =>
        {
            renderer.RemoveRoot(componentId);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task LegacyFavoriteRoute_PrefillsStationWithoutSelectingADestination()
    {
        var trainService = new TestTrainService();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime>(new MemoryJsRuntime("[]"));
        services.AddSingleton<FavoriteStore>();
        services.AddSingleton<AppLogger>();
        services.AddSingleton<NavigationManager>(
            new TestNavigationManager("http://localhost/trains?station=abrantes-id"));
        services.AddSingleton<IEnumerable<ITrainService>>([trainService]);
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        var componentId = renderer.AddRootComponent<Trains>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootAsync(componentId));

        var text = renderer.GetText(componentId);
        Assert.Contains("Abrantes", text);
        Assert.DoesNotContain("Próximas partidas para", text);
        Assert.Empty(trainService.RequestedDirectionIds);
        Assert.True(renderer.HasAttributeValueInTree(componentId, "value", "abrantes-id"));

        await renderer.Dispatcher.InvokeAsync(() =>
        {
            renderer.RemoveRoot(componentId);
            return Task.CompletedTask;
        });
    }

    private sealed class TestTrainService : ITrainService
    {
        public List<string> RequestedDirectionIds { get; } = [];
        public string OperatorName => "CP";

        public Task<List<TrainStation>> GetStationsAsync()
            => Task.FromResult(new List<TrainStation> { new("abrantes-id", "Abrantes") });

        public Task<List<TrainDirection>> GetDestinationsAsync(string stationId)
            => Task.FromResult(new List<TrainDirection>
            {
                new("route-entr-on", "Entroncamento"),
                new("route-abr-lis", "Guarda → Lisboa Santa Apolonia")
            });

        public Task<List<TrainDeparture>> GetDeparturesAsync(string stationId, string directionId)
        {
            RequestedDirectionIds.Add(directionId);
            return Task.FromResult(new List<TrainDeparture>
            {
                new("Regional", directionId, "12:00")
            });
        }
    }

    private sealed class MemoryJsRuntime(string? storedValue) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            if (identifier == "localStorage.getItem")
                return ValueTask.FromResult((TValue)(object?)storedValue!);
            if (identifier == "localStorage.setItem")
            {
                storedValue = (string)args![1]!;
                return ValueTask.FromResult(default(TValue)!);
            }

            throw new InvalidOperationException($"Unexpected JS interop call: {identifier}");
        }
    }

#pragma warning disable BL0006
    private sealed class InteractiveTestRenderer(
        IServiceProvider services,
        ILoggerFactory loggerFactory) : Renderer(services, loggerFactory)
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public int AddRootComponent<TComponent>() where TComponent : IComponent
            => AssignRootComponentId(InstantiateComponent(typeof(TComponent)));

        public Task RenderRootAsync(int componentId) => RenderRootComponentAsync(componentId);

        public Task DispatchEventAsync(ulong eventHandlerId, EventArgs eventArgs)
            => DispatchEventAsync(eventHandlerId, null, eventArgs, waitForQuiescence: true);

        public void RemoveRoot(int componentId) => RemoveRootComponent(componentId);

        public string GetText(int componentId)
        {
            var frames = GetCurrentRenderTreeFrames(componentId);
            var text = new StringBuilder();
            for (var index = 0; index < frames.Count; index++)
            {
                var frame = frames.Array[index];
                if (frame.FrameType == RenderTreeFrameType.Text)
                    text.Append(frame.TextContent);
                else if (frame.FrameType == RenderTreeFrameType.Markup)
                    text.Append(frame.MarkupContent);
            }

            return text.ToString();
        }

        public ulong GetRootEventHandler(int componentId, string attributeName, int index)
        {
            var frames = GetCurrentRenderTreeFrames(componentId);
            var handlers = Enumerable.Range(0, frames.Count)
                .Select(frameIndex => frames.Array[frameIndex])
                .Where(frame => frame.FrameType == RenderTreeFrameType.Attribute &&
                                frame.AttributeName == attributeName &&
                                frame.AttributeEventHandlerId != 0)
                .Select(frame => frame.AttributeEventHandlerId)
                .ToList();
            return handlers[index];
        }

        public ulong GetEventHandlerInTree(int componentId, string attributeName)
            => GetEventHandlerInTree(componentId, attributeName, []);

        public bool HasAttributeInTree(int componentId, string attributeName)
            => FindAttributeInTree(componentId, attributeName, null, []);

        public bool HasAttributeValueInTree(int componentId, string attributeName, string value)
            => FindAttributeInTree(componentId, attributeName, value, []);

        public bool HasAttributeContainingInTree(int componentId, string attributeName, string value)
            => FindAttributeContainingInTree(componentId, attributeName, value, []);

        protected override void HandleException(Exception exception) => throw exception;

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;

        private ulong GetEventHandlerInTree(int componentId, string attributeName, HashSet<int> visited)
        {
            if (!visited.Add(componentId))
                return 0;

            var frames = GetCurrentRenderTreeFrames(componentId);
            for (var index = 0; index < frames.Count; index++)
            {
                var frame = frames.Array[index];
                if (frame.FrameType == RenderTreeFrameType.Attribute &&
                    frame.AttributeName == attributeName &&
                    frame.AttributeEventHandlerId != 0)
                    return frame.AttributeEventHandlerId;
                if (frame.FrameType == RenderTreeFrameType.Component)
                {
                    var childHandler = GetEventHandlerInTree(frame.ComponentId, attributeName, visited);
                    if (childHandler != 0)
                        return childHandler;
                }
            }

            return 0;
        }

        private bool FindAttributeInTree(
            int componentId,
            string attributeName,
            string? expectedValue,
            HashSet<int> visited)
        {
            if (!visited.Add(componentId))
                return false;

            var frames = GetCurrentRenderTreeFrames(componentId);
            for (var index = 0; index < frames.Count; index++)
            {
                var frame = frames.Array[index];
                if (frame.FrameType == RenderTreeFrameType.Attribute &&
                    frame.AttributeName == attributeName &&
                    (expectedValue is null || Equals(frame.AttributeValue, expectedValue)))
                    return true;
                if (frame.FrameType == RenderTreeFrameType.Component &&
                    FindAttributeInTree(frame.ComponentId, attributeName, expectedValue, visited))
                    return true;
            }

            return false;
        }

        private bool FindAttributeContainingInTree(
            int componentId,
            string attributeName,
            string expectedValue,
            HashSet<int> visited)
        {
            if (!visited.Add(componentId))
                return false;

            var frames = GetCurrentRenderTreeFrames(componentId);
            for (var index = 0; index < frames.Count; index++)
            {
                var frame = frames.Array[index];
                if (frame.FrameType == RenderTreeFrameType.Attribute &&
                    frame.AttributeName == attributeName &&
                    frame.AttributeValue is string attributeValue &&
                    attributeValue.Contains(expectedValue, StringComparison.Ordinal))
                    return true;
                if (frame.FrameType == RenderTreeFrameType.Component &&
                    FindAttributeContainingInTree(frame.ComponentId, attributeName, expectedValue, visited))
                    return true;
            }

            return false;
        }
    }
#pragma warning restore BL0006
}
