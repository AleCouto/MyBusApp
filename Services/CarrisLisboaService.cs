using System.Text.Json;
using MyBusApp.Configuration;
using MyBusApp.Models.Domain;
using MyBusApp.Models.DTOs.CarrisLisboa;

namespace MyBusApp.Services;

/// <summary>
/// Serviço para a API da Carris Lisboa.
/// ATENÇÃO: A API pública em api.carris.pt está atualmente indisponível (DNS não resolve).
/// Este serviço retorna listas vazias e regista um aviso no console.
/// Quando a API voltar a ficar disponível, atualizar o BaseUrl no appsettings.json.
/// </summary>
public class CarrisLisboaService : IBusService
{
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };
    
    public BusProvider Provider => BusProvider.CarrisLisboa;

    public CarrisLisboaService(ApiSettings settings)
    {
        // A API pública da Carris Lisboa (api.carris.pt) está atualmente indisponível
        Console.WriteLine("⚠️ CarrisLisboaService: API em api.carris.pt está indisponível (DNS não resolve). Serviço temporariamente desativado.");
    }

    public async Task<BusLine?> GetLineAsync(string lineNumber)
    {
        if (string.IsNullOrWhiteSpace(lineNumber)) return await Task.FromResult<BusLine?>(null);

        Console.WriteLine($"⚠️ CarrisLisboaService: API indisponível — a linha {lineNumber} será tratada com fallback local.");
        return await Task.FromResult<BusLine?>(new BusLine(
            $"carris-{lineNumber}",
            lineNumber,
            $"Linha {lineNumber} (Carris Lisboa)",
            "#dc3545",
            Provider));
    }

    public async Task<List<BusDirection>> GetDirectionsAsync(string lineId)
    {
        return await Task.FromResult(new List<BusDirection>
        {
            new BusDirection("fallback-direction", "Sentido indisponível neste momento")
        });
    }

    public async Task<List<BusStop>> GetStopsAsync(string directionId, string lineId)
    {
        return await Task.FromResult(new List<BusStop>());
    }

    public async Task<List<BusArrival>> GetArrivalsAsync(string stopId, string lineId)
    {
        return await Task.FromResult(new List<BusArrival>());
    }
}
