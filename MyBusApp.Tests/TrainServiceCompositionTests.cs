using Microsoft.Extensions.DependencyInjection;
using MyBusApp.Configuration;
using MyBusApp.Services;
using MyBusApp.Utils;
using Xunit;

namespace MyBusApp.Tests;

public sealed class TrainServiceCompositionTests
{
    [Fact]
    public void TrainServices_ComposesCpAndFertagusAsRegisteredTrainOperators()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new ApiSettings());
        services.AddSingleton<AppLogger>();
        services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("https://static.test/") });
        services.AddScoped<ITrainService>(sp => new CpService(
            sp.GetRequiredService<ApiSettings>(),
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<AppLogger>()));
        services.AddScoped<ITrainService>(sp => new FertagusService(
            sp.GetRequiredService<ApiSettings>(),
            sp.GetRequiredService<HttpClient>(),
            sp.GetRequiredService<AppLogger>()));

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var trainServices = scope.ServiceProvider.GetServices<ITrainService>().ToList();

        Assert.Equal(["CP", "Fertagus"], trainServices.Select(service => service.OperatorName));
        Assert.All(trainServices, service => Assert.IsNotType<IBusService>(service));
    }

    [Fact]
    public void ProvidersAndRouter_RemainSeparatedByTransportMode()
    {
        Assert.True(typeof(IBusService).IsAssignableFrom(typeof(CarrisMetropolitanaService)));
        Assert.True(typeof(IBusService).IsAssignableFrom(typeof(CarrisLisboaService)));
        Assert.True(typeof(IBusService).IsAssignableFrom(typeof(TransitlandService)));
        Assert.True(typeof(ITrainService).IsAssignableFrom(typeof(GtfsTrainService)));
        Assert.True(typeof(ITrainService).IsAssignableFrom(typeof(CpService)));
        Assert.True(typeof(ITrainService).IsAssignableFrom(typeof(FertagusService)));
        Assert.False(typeof(IBusService).IsAssignableFrom(typeof(GtfsTrainService)));
        Assert.False(typeof(IBusService).IsAssignableFrom(typeof(CpService)));
        Assert.False(typeof(IBusService).IsAssignableFrom(typeof(FertagusService)));
        Assert.False(typeof(ITrainService).IsAssignableFrom(typeof(TransitlandService)));

        var routerConstructor = typeof(BusServiceRouter).GetConstructors().Single();
        Assert.Equal(typeof(IEnumerable<IBusService>), routerConstructor.GetParameters()[0].ParameterType);
    }
}
