using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MyBusApp.Components;
using MyBusApp.Models.Domain;
using MyBusApp.Services;
using MyBusApp.Utils;
using Xunit;

namespace MyBusApp.Tests;

public sealed class FavoritesDashboardTests
{
    [Fact]
    public async Task Dashboard_ShowsEachFavoriteStateWithoutBlockingSuccessfulEntries()
    {
        TransitFavorite[] favorites =
        [
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "unavailable", "Paragem indisponível", "4725"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "error", "Paragem com erro", "4725"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "empty", "Paragem sem partidas", "4725"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "working", "Paragem funcional", "4725")
        ];
        var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime>(new MemoryJsRuntime(json));
        services.AddSingleton<FavoriteStore>();
        services.AddSingleton<AppLogger>();
        services.AddSingleton<IBusService, FavoriteBusService>();
        services.AddSingleton<BusServiceRouter>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<FavoritesDashboard>();
            return component.ToHtmlString();
        });

        Assert.Contains("O serviço ou a fonte está indisponível.", html);
        Assert.Contains("Ocorreu um erro ao consultar este favorito.", html);
        Assert.Contains("Consulta concluída sem partidas disponíveis.", html);
        Assert.Contains("Destino funcional", html);
        Assert.Contains("Paragem funcional", html);
    }

    [Fact]
    public async Task Dashboard_PreservesPreviousDeparturesWhenRefreshFailsAndUpdatesOtherFavorites()
    {
        TransitFavorite[] favorites =
        [
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "retained", "Paragem preservada", "4725"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "working", "Outra paragem", "4725")
        ];
        var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IJSRuntime>(new MemoryJsRuntime(json));
        services.AddSingleton<FavoriteStore>();
        services.AddSingleton<AppLogger>();
        var busService = new RefreshSequenceBusService();
        services.AddSingleton<IBusService>(busService);
        services.AddSingleton<BusServiceRouter>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var componentId = renderer.AddRootComponent<FavoritesDashboard>();
            await renderer.RenderRootAsync(componentId);
            var initialHtml = renderer.GetText(componentId);
            Assert.Contains("Destino antigo mantido", initialHtml);
            Assert.Contains("Destino anterior funcional", initialHtml);

            var refreshHandler = renderer.GetFirstClickHandler(componentId);
            await renderer.DispatchEventAsync(refreshHandler, new MouseEventArgs());

            Assert.Equal(2, busService.GetCallCount("retained"));
            Assert.Equal(2, busService.GetCallCount("working"));
            return renderer.GetText(componentId);
        });

        Assert.Contains("O serviço ou a fonte está indisponível.", html);
        Assert.Contains("A apresentar as partidas da última consulta válida.", html);
        Assert.Contains("Destino antigo mantido", html);
        Assert.Contains("Destino atualizado", html);
        Assert.DoesNotContain("Consulta concluída sem partidas disponíveis.", html);
    }

    private sealed class FavoriteBusService : IBusService
    {
        public BusProvider Provider => BusProvider.Metropolitana;

        public Task<BusLine?> GetLineAsync(string lineNumber)
            => Task.FromResult<BusLine?>(new BusLine("line-4725", lineNumber, "Linha 4725", "#000000", Provider));

        public Task<List<BusDirection>> GetDirectionsAsync(string lineId) => Task.FromResult(new List<BusDirection>());

        public Task<List<BusStop>> GetStopsAsync(string directionId, string lineId) => Task.FromResult(new List<BusStop>());

        public Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId)
        {
            if (stopId == "unavailable")
                throw new TransitSourceUnavailableException("upstream unavailable");
            if (stopId == "error")
                throw new InvalidDataException("invalid upstream data");
            if (stopId == "empty")
                return Task.FromResult(new List<BusArrival>());

            return Task.FromResult(new List<BusArrival>
            {
                new("4725", "Destino funcional", "23:59", false)
            });
        }
    }

    private sealed class RefreshSequenceBusService : IBusService
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _calls = new();

        public BusProvider Provider => BusProvider.Metropolitana;

        public int GetCallCount(string stopId) => _calls.GetValueOrDefault(stopId);

        public Task<BusLine?> GetLineAsync(string lineNumber)
            => Task.FromResult<BusLine?>(new BusLine("line-4725", lineNumber, "Linha 4725", "#000000", Provider));

        public Task<List<BusDirection>> GetDirectionsAsync(string lineId) => Task.FromResult(new List<BusDirection>());

        public Task<List<BusStop>> GetStopsAsync(string directionId, string lineId) => Task.FromResult(new List<BusStop>());

        public Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId)
        {
            var call = _calls.AddOrUpdate(stopId, 1, (_, current) => current + 1);
            if (stopId == "retained" && call > 1)
                return Task.FromException<List<BusArrival>>(new TransitSourceUnavailableException("upstream unavailable"));

            var destination = stopId == "retained"
                ? "Destino antigo mantido"
                : call == 1 ? "Destino anterior funcional" : "Destino atualizado";
            return Task.FromResult(new List<BusArrival>
            {
                new("4725", destination, "23:59", false)
            });
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

        public ulong GetFirstClickHandler(int componentId)
        {
            var frames = GetCurrentRenderTreeFrames(componentId);
            for (var index = 0; index < frames.Count; index++)
            {
                var frame = frames.Array[index];
                if (frame.FrameType == RenderTreeFrameType.Attribute &&
                    frame.AttributeName == "onclick" &&
                    frame.AttributeEventHandlerId != 0)
                    return frame.AttributeEventHandlerId;
            }

            throw new InvalidOperationException("The dashboard did not render a click handler.");
        }

        protected override void HandleException(Exception exception) => throw exception;

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }
#pragma warning restore BL0006

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

            throw new InvalidOperationException($"Unexpected JS interop call: {identifier}");
        }
    }
}
