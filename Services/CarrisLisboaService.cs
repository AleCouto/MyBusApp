using System.Net.Http.Json;
using System.Text.Json;
using MyBusApp.Configuration;
using MyBusApp.Models.Domain;
using MyBusApp.Models.DTOs.CarrisLisboa;

namespace MyBusApp.Services;
public class CarrisLiboaService : IBusService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    private List<CarrisRouteDto>? _cachedRoutes;

    public BusProvider Provider => BusProvider.CarrisLisboa;

    public CarrisLiboaService(ApiSettings settings)
    {
        _http = new HttpClient { BaseAddress = new Uri(settings.Carris.BaseUrl) };
    }

    public async Task<BusLine?> GetLineAsync(string lineNumber)
    {
        try 
        {
            _cachedRoutes ??= await _http.GetFromJsonAsync<List<CarrisRouteDto>>("Routes", _options) ?? [];

            var route = _cachedRoutes.FirstOrDefault(r => r.RouteNumber == lineNumber);
            if (route == null) return null;

            return new BusLine(
                Id: route.RouteNumber, 
                ShortName: route.RouteNumber, 
                LongName: route.Name, 
                Color: route.Color, 
                Provider: Provider);
        }
        catch (Exception ex)
        {
            // Se a API da Carris falhar, o log avisa, mas não quebra a aplicação
            Console.WriteLine($"Erro ao contactar Carris Lisboa: {ex.Message}");
            return null; 
        }
    }

    public async Task<List<BusDirection>> GetDirectionsAsync(string lineId)
    {
        try
        {
            var variants = await _http.GetFromJsonAsync<List<CarrisVariantDto>>($"Routes/{lineId}/Variants", _options) ?? [];

            // Filtramos apenas as variantes oficiais para não poluir a UI
            return variants
                .Where(v => v.IsOfficial)
                .Select(v => new BusDirection(v.Id, v.VariantName))
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao contactar Carris Lisboa: {ex.Message}");
            return [];
        }
    }

    public async Task<List<BusStop>> GetStopsAsync(string directionId, string lineId)
    {
        try
        {
            var stops = await _http.GetFromJsonAsync<List<CarrisStopDto>>($"Variants/{directionId}/Stops", _options) ?? [];

            return stops.Select(s => new BusStop(s.Id, s.Name, s.Locality)).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao contactar Carris Lisboa: {ex.Message}");
            return [];
        }
    }

    public async Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId)
    {
        try
        {
            var estimations = await _http.GetFromJsonAsync<List<CarrisEstimationDto>>($"Estimations/busStop/{stopId}", _options) ?? [];

            return estimations
                .Where(e => e.Line == lineId)
                .Select(e => new BusArrival(
                    LineNumber: e.Line,
                    Destination: e.Destination,
                    DisplayTime: e.Time,
                    IsRealTime: e.IsRealTime))
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao contactar Carris Lisboa: {ex.Message}");
            return [];
        }
    }


}