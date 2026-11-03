using Microsoft.Extensions.DependencyInjection;
using MyBusApp.Configuration;
using MyBusApp.Services;
using MyBusApp.Utils;
using Xunit;

namespace MyBusApp.Tests;

public sealed class TrainServiceCompositionTests
{
    [Fact]
    public void TrainServices_ComposesCpAsTheOnlyRegisteredTrainOperator()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new ApiSettings());
        services.AddSingleton<AppLogger>();
        services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("https://static.test/") });
        services.AddScoped<ITrainService, CpService>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var trainServices = scope.ServiceProvider.GetServices<ITrainService>().ToList();

        var trainService = Assert.Single(trainServices);
        Assert.IsType<CpService>(trainService);
        Assert.Equal("CP", trainService.OperatorName);
    }

    [Fact]
    public void ProvidersAndRouter_RemainSeparatedByTransportMode()
    {
        Assert.True(typeof(IBusService).IsAssignableFrom(typeof(CarrisMetropolitanaService)));
        Assert.True(typeof(IBusService).IsAssignableFrom(typeof(CarrisLisboaService)));
        Assert.True(typeof(IBusService).IsAssignableFrom(typeof(TransitlandService)));
        Assert.True(typeof(ITrainService).IsAssignableFrom(typeof(CpService)));
        Assert.False(typeof(IBusService).IsAssignableFrom(typeof(CpService)));
        Assert.False(typeof(ITrainService).IsAssignableFrom(typeof(TransitlandService)));

        var routerConstructor = typeof(BusServiceRouter).GetConstructors().Single();
        Assert.Equal(typeof(IEnumerable<IBusService>), routerConstructor.GetParameters()[0].ParameterType);
    }
}
