namespace MyBusApp.Configuration;

public class ApiSettings
{
    public ApiEndpoint CarrisMetropolitana { get; set; } = new();
    public ApiEndpoint Carris { get; set; } = new();
    public ApiEndpoint Carris2 { get; set; } = new();
    public ApiEndpoint Transitland { get; set; } = new();
    public ApiEndpoint Cp { get; set; } = new();
}

public class ApiEndpoint
{
    public string BaseUrl { get; set; } = string.Empty;
    public string StaticDataBaseUrl { get; set; } = string.Empty;
    public string GtfsSourceUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Country { get; set; } = "PT";
}
