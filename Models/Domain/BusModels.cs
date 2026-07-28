namespace MyBusApp.Models.Domain;

/// <summary>
/// Identifica qual a operadora de transportes.
/// </summary>
public enum BusProvider
{
    Metropolitana,
    CarrisLisboa,
    Transitland
}

/// <summary>
/// Representa uma linha/carreira (ex: 3710 ou 728).
/// </summary>
/// <param name="Id">ID interno da API</param>
/// <param name="ShortName">Número da linha visível ao utilizador</param>
/// <param name="LongName">Descrição do percurso</param>
/// <param name="Color">Cor da linha em HEX</param>
/// <param name="Provider">De qual API vieram os dados</param>
public record BusLine(
    string Id, 
    string ShortName, 
    string LongName, 
    string Color,
    BusProvider Provider);

/// <summary>
/// Representa um sentido ou variante (ex: "Lisboa (Areeiro)").
/// Mapeia o 'Pattern' da Metropolitana ou a 'Variant' da Carris.
/// </summary>
public record BusDirection(
    string Id, 
    string Name);

/// <summary>
/// Representa uma paragem física.
/// </summary>
public record BusStop(
    string Id, 
    string Name, 
    string Locality);

/// <summary>
/// Representa uma previsão de chegada unificada.
/// </summary>
public record BusArrival(
    string LineNumber, 
    string Destination, 
    string DisplayTime, 
    bool IsRealTime);