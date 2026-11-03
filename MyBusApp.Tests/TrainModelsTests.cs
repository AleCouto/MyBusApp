using MyBusApp.Models.Domain;
using Xunit;

namespace MyBusApp.Tests;

public sealed class TrainModelsTests
{
    [Fact]
    public void TrainLine_StoresServiceIdentityAndName()
    {
        var line = new TrainLine("Linha de Sintra", "Linha de Sintra");

        Assert.Equal("Linha de Sintra", line.Id);
        Assert.Equal("Linha de Sintra", line.Name);
    }

    [Fact]
    public void TrainDirection_StoresRouteIdentityAndName()
    {
        var direction = new TrainDirection("route-rossio-sintra", "Lisboa Rossio → Sintra");

        Assert.Equal("route-rossio-sintra", direction.Id);
        Assert.Equal("Lisboa Rossio → Sintra", direction.Name);
    }

    [Fact]
    public void TrainStation_StoresStationIdentityAndName()
    {
        var station = new TrainStation("94_59006", "Lisboa Rossio");

        Assert.Equal("94_59006", station.Id);
        Assert.Equal("Lisboa Rossio", station.Name);
    }

    [Fact]
    public void TrainDeparture_StoresServiceDestinationAndTime()
    {
        var departure = new TrainDeparture("Linha de Sintra", "Sintra", "15:47");

        Assert.Equal("Linha de Sintra", departure.ServiceName);
        Assert.Equal("Sintra", departure.Destination);
        Assert.Equal("15:47", departure.DisplayTime);
    }
}