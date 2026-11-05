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

public sealed class BusesPageTests
{
    [Fact]
    public async Task FavoriteRoute_PrefillsProviderLineAndStopAndLoadsArrivals()
    {
        var favorite = new TransitFavorite(
            FavoriteTransportMode.Bus,
            FavoriteProvider.Transitland,
            "stop/id&1",
            "Paragem de teste",
            "4725");
        var json = JsonSerializer.Serialize(
            new[] { favorite },
            new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } });
        var busService = new TestBusService();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime>(new MemoryJsRuntime(json));
        services.AddSingleton<FavoriteStore>();
        services.AddSingleton<AppLogger>();
        services.AddSingleton<NavigationManager>(new TestNavigationManager(
            "http://localhost/buses?provider=Transitland&line=4725&stop=stop%2Fid%261"));
        services.AddSingleton(busService);
        services.AddSingleton<IBusService>(busService);
        services.AddSingleton<BusServiceRouter>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        var componentId = renderer.AddRootComponent<Buses>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootAsync(componentId));

        var text = renderer.GetText(componentId);
        Assert.Contains("Paragem de teste", text);
        Assert.Contains("Destino de teste", text);
        Assert.Contains("Próximas passagens", text);
        Assert.Equal("line-internal", busService.ArrivalsLineId);
        Assert.Equal("stop/id&1", busService.ArrivalsStopId);
        Assert.True(renderer.HasAttributeValueInTree(componentId, "value", "dir-one"));
        Assert.True(renderer.HasAttributeValueInTree(componentId, "value", "stop/id&1"));
        Assert.True(renderer.HasAttributeValueInTree(componentId, "aria-pressed", "true"));

        await renderer.Dispatcher.InvokeAsync(() =>
        {
            renderer.RemoveRoot(componentId);
            return Task.CompletedTask;
        });
    }

    private sealed class TestBusService : IBusService
    {
        public BusProvider Provider => BusProvider.Transitland;
        public string? ArrivalsLineId { get; private set; }
        public string? ArrivalsStopId { get; private set; }

        public Task<BusLine?> GetLineAsync(string lineNumber)
            => Task.FromResult<BusLine?>(new BusLine("line-internal", lineNumber, "Linha de teste", "#000000", Provider));

        public Task<List<BusDirection>> GetDirectionsAsync(string lineId)
            => Task.FromResult(new List<BusDirection> { new("dir-one", "Destino de teste") });

        public Task<List<BusStop>> GetStopsAsync(string directionId, string lineId)
            => Task.FromResult(new List<BusStop>
            {
                new("stop/id&1", "Paragem de teste", "Localidade")
            });

        public Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId)
        {
            ArrivalsLineId = lineId;
            ArrivalsStopId = stopId;
            return Task.FromResult(new List<BusArrival>
            {
                new("4725", "Destino de teste", "12:00", false)
            });
        }
    }

    private sealed class MemoryJsRuntime(string storedValue) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            if (identifier == "localStorage.getItem")
                return ValueTask.FromResult((TValue)(object)storedValue);
            if (identifier == "localStorage.setItem")
                return ValueTask.FromResult(default(TValue)!);

            throw new InvalidOperationException($"Unexpected JS interop call: {identifier}");
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string uri)
            => Initialize("http://localhost/", uri);

        protected override void NavigateToCore(string uri, bool forceLoad)
            => Initialize("http://localhost/", uri);
    }

#pragma warning disable BL0006
    private sealed class InteractiveTestRenderer(
        IServiceProvider services,
        ILoggerFactory loggerFactory) : Renderer(services, loggerFactory)
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public int AddRootComponent<TComponent>() where TComponent : IComponent
            => AssignRootComponentId(InstantiateComponent(typeof(TComponent)));

        public Task RenderRootAsync(int componentId)
            => RenderRootComponentAsync(componentId);

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

        public bool HasAttributeValueInTree(int componentId, string attributeName, string value)
        {
            var frames = GetCurrentRenderTreeFrames(componentId);
            for (var index = 0; index < frames.Count; index++)
            {
                var frame = frames.Array[index];
                if (frame.FrameType == RenderTreeFrameType.Attribute &&
                    frame.AttributeName == attributeName &&
                    Equals(frame.AttributeValue, value))
                    return true;
                if (frame.FrameType == RenderTreeFrameType.Component &&
                    HasAttributeValueInTree(frame.ComponentId, attributeName, value))
                    return true;
            }

            return false;
        }

        protected override void HandleException(Exception exception) => throw exception;

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }
#pragma warning restore BL0006
}
