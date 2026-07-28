using MyBusApp.Models.Domain;

namespace MyBusApp.Services;

public interface IBusService
{
    BusProvider Provider { get; }
    
    // Procura uma linha pelo número (ex: "3710")
    Task<BusLine?> GetLineAsync(string lineNumber);
    
    // Obtém os sentidos (Ida/Volta)
    Task<List<BusDirection>> GetDirectionsAsync(string lineId);
    
    // Obtém as paragens de um sentido específico
    Task<List<BusStop>> GetStopsAsync(string directionId, string lineId);
    
    // Obtém previsões em tempo real
    Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId);
}