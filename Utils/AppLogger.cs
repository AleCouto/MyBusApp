using System.Net;

namespace MyBusApp.Utils;

public sealed class AppLogger
{
    public void Info(string source, string message) => Write("INFO", source, message);

    public void Warning(string source, string message) => Write("WARN", source, message);

    public void Error(string source, string message, Exception? exception = null)
    {
        var details = exception is null ? message : $"{message}. {exception.GetType().Name}: {exception.Message}";
        Write("ERROR", source, details);
    }

    public void ApiRequest(string source, Uri? baseAddress, string endpoint)
        => Info(source, $"API request: {RedactApiKey(new Uri(baseAddress ?? new Uri("http://localhost/"), endpoint))}");

    public void ApiResponse(string source, string endpoint, HttpStatusCode statusCode, int payloadLength)
        => Info(source, $"API response: {endpoint} -> {(int)statusCode} {statusCode}, payload={payloadLength} bytes");

    private static void Write(string level, string source, string message)
    {
        var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] [{source}] {message}{Environment.NewLine}";
        Console.Write(entry);
    }

    private static string RedactApiKey(Uri uri)
    {
        var query = string.Join('&', uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.StartsWith("apikey=", StringComparison.OrdinalIgnoreCase) ? "apikey=***" : part));
        return new UriBuilder(uri) { Query = query }.Uri.ToString();
    }
}