using MyBusApp.Models.Domain;

namespace MyBusApp.Services;

public interface ITrainService
{
    string OperatorName { get; }

    Task<List<TrainStation>> GetStationsAsync();

    Task<List<TrainDirection>> GetDestinationsAsync(string stationId);

    Task<List<TrainDeparture>> GetDeparturesAsync(string stationId, string directionId);
}