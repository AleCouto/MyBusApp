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
}
