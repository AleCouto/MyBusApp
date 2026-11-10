using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Configuration;
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
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "unavailable", "Paragem indisponível", "4725", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "error", "Paragem com erro", "4725", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "empty", "Paragem sem partidas", "4725", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "working", "Paragem funcional", "4725", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "missing-destination", "Paragem sem destino", "4725", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop/id&1", "Paragem com ID reservado", "4725", DirectionId: "dir-test")
        ];
        var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FavoritesDashboard:RefreshIntervalSeconds"] = "300"
            })
            .Build());
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IJSRuntime>(new MemoryJsRuntime(json, new MemoryJsModule()));
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
        Assert.Contains("href=\"/buses?provider=Metropolitana&amp;line=4725&amp;stop=stop%2Fid%261&amp;direction=dir-test\"", html);
        Assert.Contains("Linha", html);
        Assert.Contains("Destino", html);
        Assert.Contains("Paragem", html);
        Assert.Contains("para cima", html);
        Assert.Contains("para baixo", html);
        Assert.Contains("aria-label=\"Remover", html);
        Assert.Contains("text-danger", html);
        Assert.Contains("Indisponível", System.Net.WebUtility.HtmlDecode(html));
    }

    [Fact]
    public async Task Dashboard_DoesNotShowEmptyStateAfterInitialStorageReadFailureAndCanRetry()
    {
        var jsModule = new MemoryJsModule();
        await using var provider = CreateProvider(
            [],
            new FavoriteBusService(),
            TimeProvider.System,
            jsModule,
            300);
        var jsRuntime = (MemoryJsRuntime)provider.GetRequiredService<IJSRuntime>();
        jsRuntime.GetItemException = new JSException("getItem failed");
        await using var renderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        var componentId = renderer.AddRootComponent<FavoritesDashboard>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootAsync(componentId));
        var failedLoadText = renderer.GetText(componentId);

        Assert.Contains("Não foi possível aceder aos favoritos guardados", failedLoadText);
        Assert.DoesNotContain("Ainda não guardou favoritos", failedLoadText);

        jsRuntime.GetItemException = null;
        var retryHandler = renderer.GetClickHandlerForButton(componentId, "Tentar novamente carregar favoritos");
        await renderer.Dispatcher.InvokeAsync(() =>
            renderer.DispatchEventAsync(retryHandler, new MouseEventArgs()));
        var recoveredText = renderer.GetText(componentId);

        Assert.Contains("Ainda não guardou favoritos", recoveredText);
        Assert.DoesNotContain("Não foi possível aceder aos favoritos guardados", recoveredText);
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            renderer.RemoveRoot(componentId);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Dashboard_KeepsFavoriteVisibleWhenRemovalWriteFails()
    {
        TransitFavorite[] favorites =
        [
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop-1", "Paragem A", "4725", DirectionId: "dir-test")
        ];
        var jsModule = new MemoryJsModule();
        await using var provider = CreateProvider(
            favorites,
            new FavoriteBusService(),
            TimeProvider.System,
            jsModule,
            300);
        var jsRuntime = (MemoryJsRuntime)provider.GetRequiredService<IJSRuntime>();
        var originalJson = jsRuntime.StoredValue;
        await using var renderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        var componentId = renderer.AddRootComponent<FavoritesDashboard>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootAsync(componentId));
        jsRuntime.SetItemException = new JSException("setItem failed");
        var removeHandler = renderer.GetClickHandlerForButton(componentId, "Remover 4725 · Paragem A");
        await renderer.Dispatcher.InvokeAsync(() =>
            renderer.DispatchEventAsync(removeHandler, new MouseEventArgs()));
        var text = renderer.GetText(componentId);

        Assert.Contains("Paragem A", text);
        Assert.Contains("Não foi possível guardar a alteração", text);
        Assert.Equal(originalJson, jsRuntime.StoredValue);
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            renderer.RemoveRoot(componentId);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Dashboard_KeepsFavoriteOrderWhenReorderWriteFails()
    {
        TransitFavorite[] favorites =
        [
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop-1", "Paragem A", "4725", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop-2", "Paragem B", "3710", DirectionId: "dir-test")
        ];
        var jsModule = new MemoryJsModule();
        await using var provider = CreateProvider(
            favorites,
            new FavoriteBusService(),
            TimeProvider.System,
            jsModule,
            300);
        var jsRuntime = (MemoryJsRuntime)provider.GetRequiredService<IJSRuntime>();
        var originalJson = jsRuntime.StoredValue;
        await using var renderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        var componentId = renderer.AddRootComponent<FavoritesDashboard>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootAsync(componentId));
        jsRuntime.SetItemException = new JSException("setItem failed");
        var moveHandler = renderer.GetClickHandlerForButton(componentId, "Mover 3710 · Paragem B para cima");
        await renderer.Dispatcher.InvokeAsync(() =>
            renderer.DispatchEventAsync(moveHandler, new MouseEventArgs()));
        var text = renderer.GetText(componentId);

        Assert.True(text.IndexOf("Paragem A", StringComparison.Ordinal) < text.IndexOf("Paragem B", StringComparison.Ordinal), text);
        Assert.Contains("Não foi possível guardar a alteração", text);
        Assert.Equal(originalJson, jsRuntime.StoredValue);
        await renderer.Dispatcher.InvokeAsync(() =>
        {
            renderer.RemoveRoot(componentId);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Dashboard_RemovesFavoriteAfterSuccessfulSaveAndReloadsRemainingFavorites()
    {
        TransitFavorite[] favorites =
        [
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop-1", "Paragem A", "4725", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop-2", "Paragem B", "3710", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Train, FavoriteProvider.Cp, "station-1", "Estação antiga")
        ];
        var jsModule = new MemoryJsModule();
        await using var provider = CreateProvider(
            favorites,
            new FavoriteBusService(),
            TimeProvider.System,
            jsModule,
            300);
        var jsRuntime = (MemoryJsRuntime)provider.GetRequiredService<IJSRuntime>();
        await using var renderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        var componentId = renderer.AddRootComponent<FavoritesDashboard>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootAsync(componentId));
        jsRuntime.BlockNextSetItem();
        var removeHandler = renderer.GetClickHandlerForButton(componentId, "Remover 4725 · Paragem A");
        var removalTask = renderer.Dispatcher.InvokeAsync(() =>
            renderer.DispatchEventAsync(removeHandler, new MouseEventArgs()));
        await jsRuntime.SetItemStarted;

        var whileSavingText = await renderer.Dispatcher.InvokeAsync(() => renderer.GetText(componentId));
        Assert.Contains("Paragem A", whileSavingText);
        Assert.Contains("Paragem B", whileSavingText);
        Assert.Equal(jsRuntime.StoredValue, JsonSerializer.Serialize(favorites, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        }));

        jsRuntime.CompletePendingSetItem();
        await removalTask;

        var afterRemovalText = await renderer.Dispatcher.InvokeAsync(() => renderer.GetText(componentId));
        Assert.DoesNotContain("Paragem A", afterRemovalText);
        Assert.Contains("Paragem B", afterRemovalText);
        Assert.Equal([favorites[1], favorites[2]], await provider.GetRequiredService<FavoriteStore>().LoadAsync());

        await renderer.Dispatcher.InvokeAsync(() =>
        {
            renderer.RemoveRoot(componentId);
            return Task.CompletedTask;
        });

        await using var reloadedRenderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());
        var reloadedComponentId = reloadedRenderer.AddRootComponent<FavoritesDashboard>();
        await reloadedRenderer.Dispatcher.InvokeAsync(() => reloadedRenderer.RenderRootAsync(reloadedComponentId));

        var reloadedText = await reloadedRenderer.Dispatcher.InvokeAsync(
            () => reloadedRenderer.GetText(reloadedComponentId));
        Assert.DoesNotContain("Paragem A", reloadedText);
        Assert.Contains("Paragem B", reloadedText);
        Assert.Contains("Estação antiga", reloadedText);
        Assert.Equal([favorites[1], favorites[2]], await provider.GetRequiredService<FavoriteStore>().LoadAsync());
        await reloadedRenderer.Dispatcher.InvokeAsync(() =>
        {
            reloadedRenderer.RemoveRoot(reloadedComponentId);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Dashboard_ReordersFavoritesAfterSuccessfulSaveAndReloadsPersistedOrder()
    {
        TransitFavorite[] favorites =
        [
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop-1", "Paragem A", "4725", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop-2", "Paragem B", "3710", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Train, FavoriteProvider.Cp, "station-1", "Estação antiga")
        ];
        var jsModule = new MemoryJsModule();
        await using var provider = CreateProvider(
            favorites,
            new FavoriteBusService(),
            TimeProvider.System,
            jsModule,
            300);
        await using var renderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        var componentId = renderer.AddRootComponent<FavoritesDashboard>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootAsync(componentId));
        var moveHandler = renderer.GetClickHandlerForButton(componentId, "Mover 3710 · Paragem B para cima");
        await renderer.Dispatcher.InvokeAsync(() =>
            renderer.DispatchEventAsync(moveHandler, new MouseEventArgs()));

        var expectedOrder = new[] { favorites[1], favorites[0], favorites[2] };
        var updatedText = await renderer.Dispatcher.InvokeAsync(() => renderer.GetText(componentId));
        Assert.True(
            updatedText.IndexOf("Paragem B", StringComparison.Ordinal) <
            updatedText.IndexOf("Paragem A", StringComparison.Ordinal),
            updatedText);
        Assert.Equal(expectedOrder, await provider.GetRequiredService<FavoriteStore>().LoadAsync());

        await renderer.Dispatcher.InvokeAsync(() =>
        {
            renderer.RemoveRoot(componentId);
            return Task.CompletedTask;
        });

        await using var reloadedRenderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());
        var reloadedComponentId = reloadedRenderer.AddRootComponent<FavoritesDashboard>();
        await reloadedRenderer.Dispatcher.InvokeAsync(() => reloadedRenderer.RenderRootAsync(reloadedComponentId));

        var reloadedText = await reloadedRenderer.Dispatcher.InvokeAsync(
            () => reloadedRenderer.GetText(reloadedComponentId));
        Assert.True(
            reloadedText.IndexOf("Paragem B", StringComparison.Ordinal) <
            reloadedText.IndexOf("Paragem A", StringComparison.Ordinal),
            reloadedText);
        Assert.Contains("Estação antiga", reloadedText);
        Assert.Equal(expectedOrder, await provider.GetRequiredService<FavoriteStore>().LoadAsync());
        await reloadedRenderer.Dispatcher.InvokeAsync(() =>
        {
            reloadedRenderer.RemoveRoot(reloadedComponentId);
            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Dashboard_PreservesPreviousDeparturesWhenRefreshFailsAndUpdatesOtherFavorites()
    {
        TransitFavorite[] favorites =
        [
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "retained", "Paragem preservada", "4725", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "working", "Outra paragem", "4725", DirectionId: "dir-test")
        ];
        var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FavoritesDashboard:RefreshIntervalSeconds"] = "300"
            })
            .Build());
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IJSRuntime>(new MemoryJsRuntime(json, new MemoryJsModule()));
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

    [Fact]
    public async Task Dashboard_LoadsOnlySavedTrainDestinationsAndIdentifiesLegacyFavorites()
    {
        TransitFavorite[] favorites =
        [
            new(FavoriteTransportMode.Train, FavoriteProvider.Cp, "abrantes-id", "Abrantes"),
            new(FavoriteTransportMode.Train, FavoriteProvider.Cp, "abrantes-id", "Abrantes", DestinationId: "route-entr-on"),
            new(FavoriteTransportMode.Train, FavoriteProvider.Cp, "abrantes-id", "Abrantes", DestinationId: "route-abr-lis")
        ];
        var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
        var trainService = new FavoriteTrainService();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FavoritesDashboard:RefreshIntervalSeconds"] = "300"
            })
            .Build());
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IJSRuntime>(new MemoryJsRuntime(json));
        services.AddSingleton<FavoriteStore>();
        services.AddSingleton<AppLogger>();
        services.AddSingleton<ITrainService>(trainService);
        services.AddSingleton<BusServiceRouter>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<FavoritesDashboard>();
            return component.ToHtmlString();
        });

        Assert.Equal(["route-abr-lis", "route-entr-on"], trainService.DepartureDirectionIds.Order(StringComparer.Ordinal));
        Assert.Equal(2, trainService.DestinationLookupCount);
        Assert.Contains("Destino por selecionar (favorito antigo)", html);
        Assert.Contains("Selecionar estação e destino", html);
        Assert.Contains("Destino: Entroncamento", html);
        Assert.Contains("Destino: Lisboa Santa Apolonia", html);
        Assert.Contains("href=\"/trains?provider=Cp&amp;station=abrantes-id&amp;destination=route-entr-on\"", html);
        Assert.Contains("href=\"/trains?provider=Cp&amp;station=abrantes-id&amp;destination=route-abr-lis\"", html);
        Assert.Contains("href=\"/trains?provider=Cp&amp;station=abrantes-id\"", html);
        Assert.DoesNotContain("Destino: Guarda", html);
        Assert.Contains("Ocorreu um erro ao consultar este favorito.", html);
        Assert.Contains("Partida para Entroncamento", html);
        Assert.Contains("Agendado", html);
        Assert.DoesNotContain("Partida para Lisboa", html);
    }

    [Fact]
    public async Task Dashboard_PreservesFavoritesWhenSavedLineStopOrDestinationIsUnavailable()
    {
        TransitFavorite[] favorites =
        [
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop-1", "Paragem sem linha", "missing-line", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "missing-stop", "Paragem indisponível", "4725", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Train, FavoriteProvider.Cp, "station-1", "Estação", DestinationId: "missing-destination")
        ];
        var savedJson = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FavoritesDashboard:RefreshIntervalSeconds"] = "300"
            })
            .Build());
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IJSRuntime>(new MemoryJsRuntime(savedJson));
        services.AddSingleton<FavoriteStore>();
        services.AddSingleton<AppLogger>();
        services.AddSingleton<IBusService, FavoriteBusService>();
        services.AddSingleton<BusServiceRouter>();
        services.AddSingleton<ITrainService, FavoriteTrainService>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<FavoritesDashboard>();
            return component.ToHtmlString();
        });

        var decodedHtml = System.Net.WebUtility.HtmlDecode(html);
        Assert.Contains("Paragem sem linha", decodedHtml);
        Assert.Contains("Paragem indisponível", decodedHtml);
        Assert.Contains("href=\"/buses?provider=Metropolitana&amp;line=missing-line&amp;stop=stop-1&amp;direction=dir-test\"", html);
        Assert.Contains("href=\"/buses?provider=Metropolitana&amp;line=4725&amp;stop=missing-stop&amp;direction=dir-test\"", html);
        Assert.Contains("O destino guardado já não está disponível na fonte.", decodedHtml);
        Assert.Equal(favorites, await provider.GetRequiredService<FavoriteStore>().LoadAsync());
        Assert.Equal(savedJson, ((MemoryJsRuntime)provider.GetRequiredService<IJSRuntime>()).StoredValue);
    }

    [Fact]
    public async Task Dashboard_RefreshesAutomaticallyAtConfiguredIntervalWithoutOverlappingManualRefresh()
    {
        var timeProvider = new ManualTimeProvider();
        var busService = new BlockingBusService();
        var jsModule = new MemoryJsModule();
        var favorites = new[]
        {
            new TransitFavorite(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop", "Paragem", "4725", DirectionId: "dir-test")
        };
        await using var provider = CreateProvider(favorites, busService, timeProvider, jsModule, 15);
        await using var renderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        var componentId = renderer.AddRootComponent<FavoritesDashboard>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootAsync(componentId));
        Assert.Equal(1, busService.CallCount);
        Assert.True(jsModule.IsObserving);

        timeProvider.AdvanceBy(TimeSpan.FromSeconds(14));
        Assert.Equal(1, busService.CallCount);

        timeProvider.AdvanceBy(TimeSpan.FromSeconds(1));
        await busService.RefreshStarted;
        Assert.Equal(2, busService.CallCount);
        await timeProvider.WaitForActiveTimerAsync();

        var manualRefreshHandler = renderer.GetFirstClickHandler(componentId);
        await renderer.Dispatcher.InvokeAsync(() =>
            renderer.DispatchEventAsync(manualRefreshHandler, new MouseEventArgs()));
        Assert.Equal(2, busService.CallCount);

        timeProvider.AdvanceBy(TimeSpan.FromSeconds(15));
        Assert.Equal(2, busService.CallCount);

        busService.ReleaseRefresh();
        await busService.RefreshCompleted;
        var refreshedHtml = await renderer.Dispatcher.InvokeAsync(() => renderer.GetText(componentId));
        Assert.Contains("Destino 2", refreshedHtml);
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            renderer.RemoveRoot(componentId);
            await Task.CompletedTask;
        });
        await jsModule.WaitUntilStoppedAsync();
        Assert.False(jsModule.IsObserving);
        Assert.True(jsModule.IsDisposed);
    }

    [Fact]
    public async Task Dashboard_SuspendsWhenDocumentIsInactiveResumesWhenVisibleAndStopsAfterDisposal()
    {
        var timeProvider = new ManualTimeProvider();
        var busService = new RefreshSequenceBusService();
        var jsModule = new MemoryJsModule();
        var favorites = new[]
        {
            new TransitFavorite(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop", "Paragem", "4725", DirectionId: "dir-test")
        };
        await using var provider = CreateProvider(favorites, busService, timeProvider, jsModule, 15);
        await using var renderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        var componentId = renderer.AddRootComponent<FavoritesDashboard>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootAsync(componentId));
        Assert.Equal(1, busService.GetCallCount("stop"));

        await jsModule.SetVisibilityAsync(false);
        timeProvider.AdvanceBy(TimeSpan.FromSeconds(30));
        Assert.Equal(1, busService.GetCallCount("stop"));

        await jsModule.SetVisibilityAsync(true);
        timeProvider.AdvanceBy(TimeSpan.FromSeconds(15));
        await busService.WaitForCallCountAsync("stop", 2);
        Assert.Equal(2, busService.GetCallCount("stop"));

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            renderer.RemoveRoot(componentId);
            await Task.CompletedTask;
        });
        await jsModule.WaitUntilStoppedAsync();
        Assert.False(jsModule.IsObserving);
        timeProvider.AdvanceBy(TimeSpan.FromSeconds(30));
        Assert.Equal(2, busService.GetCallCount("stop"));
    }

    [Fact]
    public async Task Dashboard_DoesNotScheduleAutomaticRefreshWhenThereAreNoFavorites()
    {
        var timeProvider = new ManualTimeProvider();
        var busService = new RefreshSequenceBusService();
        var jsModule = new MemoryJsModule();
        await using var provider = CreateProvider([], busService, timeProvider, jsModule, 15);
        await using var renderer = new InteractiveTestRenderer(
            provider,
            provider.GetRequiredService<ILoggerFactory>());

        var componentId = renderer.AddRootComponent<FavoritesDashboard>();
        await renderer.Dispatcher.InvokeAsync(() => renderer.RenderRootAsync(componentId));

        Assert.Equal(0, timeProvider.ActiveTimerCount);
        timeProvider.AdvanceBy(TimeSpan.FromMinutes(10));
        Assert.Equal(0, busService.TotalCallCount);
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            renderer.RemoveRoot(componentId);
            await Task.CompletedTask;
        });
    }

    [Fact]
    public async Task Dashboard_AsksForDirectionOnLegacyBusFavoritesWithoutQueryingAllDirections()
    {
        TransitFavorite[] favorites =
        [
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "legacy-stop", "Paragem antiga", "4725")
        ];
        var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FavoritesDashboard:RefreshIntervalSeconds"] = "300"
            })
            .Build());
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IJSRuntime>(new MemoryJsRuntime(json));
        services.AddSingleton<FavoriteStore>();
        services.AddSingleton<AppLogger>();
        var busService = new FavoriteBusService();
        services.AddSingleton<IBusService>(busService);
        services.AddSingleton<BusServiceRouter>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<FavoritesDashboard>();
            return component.ToHtmlString();
        });

        var decodedHtml = System.Net.WebUtility.HtmlDecode(html);
        Assert.Contains("Este favorito antigo não guarda o sentido", decodedHtml);
        Assert.Contains("Selecionar sentido e paragem", decodedHtml);
        Assert.Contains("href=\"/buses?provider=Metropolitana&amp;line=4725&amp;stop=legacy-stop\"", html);
        Assert.Null(busService.GetDirection("legacy-stop"));
    }

    [Fact]
    public async Task Dashboard_GroupsDeparturesByDestinationInOneRow()
    {
        TransitFavorite[] favorites =
        [
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "grouped", "Paragem agrupada", "4725", DirectionId: "dir-test")
        ];
        var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FavoritesDashboard:RefreshIntervalSeconds"] = "300"
            })
            .Build());
        services.AddSingleton<TimeProvider>(TimeProvider.System);
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

        var decodedHtml = System.Net.WebUtility.HtmlDecode(html);
        Assert.Single(Regex.Matches(decodedHtml, "Destino agrupado"));
        Assert.Contains("12:00", decodedHtml);
        Assert.Contains("12:10", decodedHtml);
        Assert.Equal(2, Regex.Matches(decodedHtml, "Agendado").Count);
    }

    [Fact]
    public async Task Dashboard_LoadsFertagusFavoritesWithTheFertagusService()
    {
        TransitFavorite[] favorites =
        [
            new(
                FavoriteTransportMode.Train,
                FavoriteProvider.Fertagus,
                "roma-id",
                "Roma-Areeiro",
                DestinationId: "Fertagus\u001fA_0|0")
        ];
        var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FavoritesDashboard:RefreshIntervalSeconds"] = "300"
            })
            .Build());
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IJSRuntime>(new MemoryJsRuntime(json));
        services.AddSingleton<FavoriteStore>();
        services.AddSingleton<AppLogger>();
        services.AddSingleton<ITrainService>(new FavoriteTrainService());
        var fertagusService = new FavoriteFertagusService();
        services.AddSingleton<ITrainService>(fertagusService);
        services.AddSingleton<BusServiceRouter>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<FavoritesDashboard>();
            return component.ToHtmlString();
        });

        Assert.Equal(["Fertagus\u001fA_0|0"], fertagusService.RequestedDirectionIds);
        var decodedHtml = System.Net.WebUtility.HtmlDecode(html);
        Assert.Contains("Destino: Setúbal", decodedHtml);
        Assert.Contains("Fertagus · Comboios", decodedHtml);
        Assert.Contains("Partida para Setúbal", decodedHtml);
    }

    [Fact]
    public async Task Dashboard_RequestsOnlyTheSavedBusDirectionAndKeepsTheSearchParameters()
    {
        TransitFavorite[] favorites =
        [
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop-1", "Paragem A", "4725", DirectionId: "4725_0_2"),
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop-2", "Paragem B", "4725")
        ];
        var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FavoritesDashboard:RefreshIntervalSeconds"] = "300"
            })
            .Build());
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IJSRuntime>(new MemoryJsRuntime(json));
        services.AddSingleton<FavoriteStore>();
        services.AddSingleton<AppLogger>();
        var busService = new FavoriteBusService();
        services.AddSingleton<IBusService>(busService);
        services.AddSingleton<BusServiceRouter>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<FavoritesDashboard>();
            return component.ToHtmlString();
        });

        Assert.Equal("4725_0_2", busService.GetDirection("stop-1"));
        Assert.Null(busService.GetDirection("stop-2"));
        Assert.Contains(
            "href=\"/buses?provider=Metropolitana&amp;line=4725&amp;stop=stop-1&amp;direction=4725_0_2\"",
            html);
        Assert.Contains(
            "href=\"/buses?provider=Metropolitana&amp;line=4725&amp;stop=stop-2\"",
            html);
        Assert.DoesNotContain("stop-2&amp;direction=", html);
    }

    [Fact]
    public async Task Dashboard_StacksEveryFavoriteHeaderOnSmallScreens()
    {
        TransitFavorite[] favorites =
        [
            new(FavoriteTransportMode.Bus, FavoriteProvider.Metropolitana, "stop-1", "Paragem A", "4725", DirectionId: "dir-test"),
            new(FavoriteTransportMode.Train, FavoriteProvider.Cp, "station-1", "Estação A", DestinationId: "route-entr-on")
        ];
        var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FavoritesDashboard:RefreshIntervalSeconds"] = "300"
            })
            .Build());
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IJSRuntime>(new MemoryJsRuntime(json));
        services.AddSingleton<FavoriteStore>();
        services.AddSingleton<AppLogger>();
        services.AddSingleton<IBusService, FavoriteBusService>();
        services.AddSingleton<BusServiceRouter>();
        services.AddSingleton<ITrainService, FavoriteTrainService>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = await renderer.RenderComponentAsync<FavoritesDashboard>();
            return component.ToHtmlString();
        });

        var headerClasses = Regex.Matches(html, "<div class=\"(card-header[^\"]*)\"")
            .Select(match => match.Groups[1].Value)
            .ToList();

        Assert.Equal(2, headerClasses.Count);
        Assert.All(headerClasses, classes =>
        {
            Assert.Contains("flex-column", classes);
            Assert.Contains("flex-lg-row", classes);
        });

        var actionClasses = Regex.Matches(html, "<div class=\"(d-flex flex-wrap gap-1 flex-shrink-0[^\"]*)\"")
            .Select(match => match.Groups[1].Value)
            .ToList();
        Assert.Equal(2, actionClasses.Count);
        Assert.All(actionClasses, classes =>
        {
            Assert.Contains("align-self-end", classes);
            Assert.Contains("align-self-lg-center", classes);
        });
    }

    private static ServiceProvider CreateProvider(
        TransitFavorite[] favorites,
        IBusService busService,
        TimeProvider timeProvider,
        MemoryJsModule jsModule,
        int refreshIntervalSeconds)
    {
        var json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() }
        });
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FavoritesDashboard:RefreshIntervalSeconds"] = refreshIntervalSeconds.ToString()
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddSingleton<IJSRuntime>(new MemoryJsRuntime(json, jsModule));
        services.AddSingleton<FavoriteStore>();
        services.AddSingleton<AppLogger>();
        services.AddSingleton(busService);
        services.AddSingleton<IBusService>(busService);
        services.AddSingleton<BusServiceRouter>();
        return services.BuildServiceProvider();
    }

    private sealed class FavoriteBusService : IBusService
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string?> _directions = new();

        public BusProvider Provider => BusProvider.Metropolitana;

        public string? GetDirection(string stopId) => _directions.GetValueOrDefault(stopId);

        public Task<BusLine?> GetLineAsync(string lineNumber)
            => Task.FromResult<BusLine?>(lineNumber == "missing-line"
                ? null
                : new BusLine("line-4725", lineNumber, "Linha 4725", "#000000", Provider));

        public Task<List<BusDirection>> GetDirectionsAsync(string lineId) => Task.FromResult(new List<BusDirection>());

        public Task<List<BusStop>> GetStopsAsync(string directionId, string lineId) => Task.FromResult(new List<BusStop>());

        public Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId, string? directionId = null)
        {
            _directions[stopId] = directionId;
            if (stopId == "missing-stop")
                throw new TransitSourceUnavailableException("stop unavailable");
            if (stopId == "unavailable")
                throw new TransitSourceUnavailableException("upstream unavailable");
            if (stopId == "error")
                throw new InvalidDataException("invalid upstream data");
            if (stopId == "empty")
                return Task.FromResult(new List<BusArrival>());
            if (stopId == "missing-destination")
                return Task.FromResult(new List<BusArrival>
                {
                    new("4725", "", "23:59", false)
                });
            if (stopId == "grouped")
                return Task.FromResult(new List<BusArrival>
                {
                    new("4725", "Destino agrupado", "12:00", false),
                    new("4725", "Destino agrupado", "12:10", false)
                });

            return Task.FromResult(new List<BusArrival>
            {
                new("4725", "Destino funcional", "23:59", false)
            });
        }
    }

    private sealed class RefreshSequenceBusService : IBusService
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> _calls = new();
        private readonly System.Collections.Concurrent.ConcurrentDictionary<(string StopId, int Count), TaskCompletionSource> _callWaiters = new();

        public BusProvider Provider => BusProvider.Metropolitana;

        public int TotalCallCount => _calls.Values.Sum();

        public int GetCallCount(string stopId) => _calls.GetValueOrDefault(stopId);

        public Task WaitForCallCountAsync(string stopId, int count)
        {
            if (GetCallCount(stopId) >= count)
                return Task.CompletedTask;

            var waiter = _callWaiters.GetOrAdd(
                (stopId, count),
                _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
            if (GetCallCount(stopId) >= count)
                waiter.TrySetResult();
            return waiter.Task;
        }

        public Task<BusLine?> GetLineAsync(string lineNumber)
            => Task.FromResult<BusLine?>(new BusLine("line-4725", lineNumber, "Linha 4725", "#000000", Provider));

        public Task<List<BusDirection>> GetDirectionsAsync(string lineId) => Task.FromResult(new List<BusDirection>());

        public Task<List<BusStop>> GetStopsAsync(string directionId, string lineId) => Task.FromResult(new List<BusStop>());

        public Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId, string? directionId = null)
        {
            var call = _calls.AddOrUpdate(stopId, 1, (_, current) => current + 1);
            if (_callWaiters.TryRemove((stopId, call), out var waiter))
                waiter.TrySetResult();
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

    private sealed class BlockingBusService : IBusService
    {
        private readonly TaskCompletionSource _refreshStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _releaseRefresh = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _refreshCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _callCount;

        public BusProvider Provider => BusProvider.Metropolitana;
        public int CallCount => Volatile.Read(ref _callCount);
        public Task RefreshStarted => _refreshStarted.Task;
        public Task RefreshCompleted => _refreshCompleted.Task;

        public Task<BusLine?> GetLineAsync(string lineNumber)
            => Task.FromResult<BusLine?>(new BusLine("line-4725", lineNumber, "Linha 4725", "#000000", Provider));

        public Task<List<BusDirection>> GetDirectionsAsync(string lineId) => Task.FromResult(new List<BusDirection>());

        public Task<List<BusStop>> GetStopsAsync(string directionId, string lineId) => Task.FromResult(new List<BusStop>());

        public async Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId, string? directionId = null)
        {
            var call = Interlocked.Increment(ref _callCount);
            if (call == 2)
            {
                _refreshStarted.TrySetResult();
                await _releaseRefresh.Task;
                _refreshCompleted.TrySetResult();
            }

            return [new BusArrival("4725", $"Destino {call}", "23:59", false)];
        }

        public void ReleaseRefresh() => _releaseRefresh.TrySetResult();
    }

    private sealed class FavoriteFertagusService : ITrainService
    {
        public string OperatorName => "Fertagus";
        public List<string> RequestedDirectionIds { get; } = [];

        public Task<List<TrainStation>> GetStationsAsync()
            => Task.FromResult(new List<TrainStation>());

        public Task<List<TrainDirection>> GetDestinationsAsync(string stationId)
            => Task.FromResult(new List<TrainDirection>
            {
                new("Fertagus\u001fA_0|0", "Roma-Areeiro → Setúbal")
            });

        public Task<List<TrainDeparture>> GetDeparturesAsync(string stationId, string directionId)
        {
            RequestedDirectionIds.Add(directionId);
            return Task.FromResult(new List<TrainDeparture>
            {
                new("Fertagus", "Partida para Setúbal", "12:00")
            });
        }
    }

    private sealed class FavoriteTrainService : ITrainService
    {
        private readonly System.Collections.Concurrent.ConcurrentBag<string> _departureDirectionIds = [];

        public string OperatorName => "CP";
        public int DestinationLookupCount { get; private set; }
        public IEnumerable<string> DepartureDirectionIds => _departureDirectionIds;

        public Task<List<TrainStation>> GetStationsAsync()
            => Task.FromResult(new List<TrainStation>());

        public Task<List<TrainDirection>> GetDestinationsAsync(string stationId)
        {
            DestinationLookupCount++;
            return Task.FromResult(new List<TrainDirection>
            {
                new("route-entr-on", "Entroncamento"),
                new("route-abr-lis", "Guarda → Lisboa Santa Apolonia")
            });
        }

        public Task<List<TrainDeparture>> GetDeparturesAsync(string stationId, string directionId)
        {
            _departureDirectionIds.Add(directionId);
            if (directionId == "route-abr-lis")
                return Task.FromException<List<TrainDeparture>>(new InvalidOperationException("destination lookup failed"));

            return Task.FromResult(new List<TrainDeparture>
            {
                new("Regional", "Partida para Entroncamento", "23:59")
            });
        }
    }

    private sealed class MemoryJsModule : IJSObjectReference
    {
        private DotNetObjectReference<FavoritesDashboard>? _componentReference;
        private readonly TaskCompletionSource _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsObserving { get; private set; }
        public bool IsDisposed { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            if (identifier == "observeVisibility")
            {
                _componentReference = (DotNetObjectReference<FavoritesDashboard>)args![0]!;
                IsObserving = true;
                return ValueTask.FromResult((TValue)(object)true);
            }

            if (identifier == "stopObservingVisibility")
            {
                IsObserving = false;
                _stopped.TrySetResult();
                return ValueTask.FromResult(default(TValue)!);
            }

            throw new InvalidOperationException($"Unexpected JS module call: {identifier}");
        }

        public Task SetVisibilityAsync(bool isVisible)
            => _componentReference!.Value.SetDocumentVisibilityAsync(isVisible);

        public Task WaitUntilStoppedAsync() => _stopped.Task;

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private readonly object _sync = new();
        private readonly List<ManualTimer> _timers = [];
        private DateTimeOffset _utcNow = DateTimeOffset.UnixEpoch;
        private TaskCompletionSource _timerActivated = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override DateTimeOffset GetUtcNow()
        {
            lock (_sync)
                return _utcNow;
        }

        public int ActiveTimerCount
        {
            get
            {
                lock (_sync)
                    return _timers.Count(timer => timer.IsActive);
            }
        }

        public Task WaitForActiveTimerAsync()
        {
            lock (_sync)
                return _timers.Any(timer => timer.IsActive)
                    ? Task.CompletedTask
                    : _timerActivated.Task;
        }

        public override ITimer CreateTimer(
            TimerCallback callback,
            object? state,
            TimeSpan dueTime,
            TimeSpan period)
        {
            var timer = new ManualTimer(this, callback, state);
            timer.Change(dueTime, period);
            lock (_sync)
            {
                _timers.Add(timer);
                if (timer.IsActive)
                    _timerActivated.TrySetResult();
            }
            return timer;
        }

        public void AdvanceBy(TimeSpan amount)
        {
            List<(TimerCallback Callback, object? State)> callbacks = [];
            lock (_sync)
            {
                _utcNow += amount;
                foreach (var timer in _timers)
                {
                    if (timer.TryFire(_utcNow, out var callback, out var state))
                        callbacks.Add((callback, state));
                }
                if (!_timers.Any(timer => timer.IsActive))
                    _timerActivated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }

            foreach (var (callback, state) in callbacks)
                callback(state);
        }

        private sealed class ManualTimer(
            ManualTimeProvider owner,
            TimerCallback callback,
            object? state) : ITimer
        {
            private DateTimeOffset? _dueAt;
            private TimeSpan _period;
            private bool _disposed;

            public bool IsActive => !_disposed && _dueAt.HasValue;

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                lock (owner._sync)
                {
                    if (_disposed)
                        return false;

                    _period = period;
                    _dueAt = dueTime == Timeout.InfiniteTimeSpan ? null : owner._utcNow + dueTime;
                    return true;
                }
            }

            public void Dispose()
            {
                lock (owner._sync)
                {
                    _disposed = true;
                    _dueAt = null;
                }
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }

            public bool TryFire(
                DateTimeOffset now,
                out TimerCallback timerCallback,
                out object? timerState)
            {
                lock (owner._sync)
                {
                    if (_disposed || !_dueAt.HasValue || _dueAt.Value > now)
                    {
                        timerCallback = null!;
                        timerState = null;
                        return false;
                    }

                    if (_period == Timeout.InfiniteTimeSpan || _period <= TimeSpan.Zero)
                        _dueAt = null;
                    else
                        _dueAt = now + _period;

                    timerCallback = callback;
                    timerState = state;
                    return true;
                }
            }
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

        public ulong GetClickHandlerForButton(int componentId, string label)
        {
            var frames = GetCurrentRenderTreeFrames(componentId);
            for (var index = 0; index < frames.Count; index++)
            {
                var button = frames.Array[index];
                if (button.FrameType != RenderTreeFrameType.Element || button.ElementName != "button")
                    continue;

                string? ariaLabel = null;
                var buttonText = new StringBuilder();
                ulong clickHandler = 0;
                var end = index + button.ElementSubtreeLength;
                for (var childIndex = index + 1; childIndex < end; childIndex++)
                {
                    var child = frames.Array[childIndex];
                    if (child.FrameType == RenderTreeFrameType.Attribute)
                    {
                        if (child.AttributeName == "aria-label")
                            ariaLabel = child.AttributeValue?.ToString();
                        else if (child.AttributeName == "onclick")
                            clickHandler = child.AttributeEventHandlerId;
                    }
                    else if (child.FrameType == RenderTreeFrameType.Text)
                    {
                        buttonText.Append(child.TextContent);
                    }
                }

                if (clickHandler != 0 &&
                    (string.Equals(ariaLabel, label, StringComparison.Ordinal) ||
                     buttonText.ToString().Contains(label, StringComparison.Ordinal)))
                    return clickHandler;
            }

            throw new InvalidOperationException($"The dashboard did not render a button labeled '{label}'.");
        }

        protected override void HandleException(Exception exception) => throw exception;

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }
#pragma warning restore BL0006

    private sealed class MemoryJsRuntime(string storedValue, MemoryJsModule? module = null) : IJSRuntime
    {
        private TaskCompletionSource? _setItemStarted;
        private TaskCompletionSource? _allowSetItem;

        public string StoredValue { get; private set; } = storedValue;
        public Exception? GetItemException { get; set; }
        public Exception? SetItemException { get; set; }
        public Task SetItemStarted
            => _setItemStarted?.Task ?? throw new InvalidOperationException("No localStorage write is pending.");

        public void BlockNextSetItem()
        {
            _setItemStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _allowSetItem = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public void CompletePendingSetItem()
            => (_allowSetItem ?? throw new InvalidOperationException("No localStorage write is pending."))
                .TrySetResult();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            if (identifier == "localStorage.getItem")
            {
                if (GetItemException is not null)
                    throw GetItemException;
                return ValueTask.FromResult((TValue)(object)StoredValue);
            }
            if (identifier == "localStorage.setItem")
            {
                if (SetItemException is not null)
                    throw SetItemException;
                var newValue = (string)args![1]!;
                if (_allowSetItem is not null)
                    return new ValueTask<TValue>(CompleteSetItemAsync<TValue>(newValue));
                StoredValue = newValue;
                return ValueTask.FromResult(default(TValue)!);
            }
            if (identifier == "import" && module is not null)
                return ValueTask.FromResult((TValue)(object)module);

            throw new InvalidOperationException($"Unexpected JS interop call: {identifier}");
        }

        private async Task<TValue> CompleteSetItemAsync<TValue>(string newValue)
        {
            _setItemStarted!.TrySetResult();
            await _allowSetItem!.Task;
            StoredValue = newValue;
            _allowSetItem = null;
            return default!;
        }
    }
}
