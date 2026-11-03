namespace MyBusApp.Models.Domain;

public record TrainLine(
    string Id,
    string Name);

public record TrainDirection(
    string Id,
    string Name);

public record TrainStation(
    string Id,
    string Name);

public record TrainDeparture(
    string ServiceName,
    string Destination,
    string DisplayTime);