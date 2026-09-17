using System.Net.Http.Headers;
using System.Text.Json;
using MyBusApp.Configuration;

namespace MyBusApp.Tools.CarrisLisboaDataGenerator;

public static class CarrisLisboaDataGeneratorRunner
{
    private const string Usage = "Usage: dotnet run -- --output <directory> [--gtfs <gtfs.zip>]";

    public static async Task<int> RunAsync(string[] args)
    {
        if (!TryParseArguments(args, out var outputDirectory, out var localGtfsPath))
        {
            Console.Error.WriteLine(Usage);
            return 2;
        }

        string? temporaryGtfsPath = null;
        try
        {
            var gtfsPath = localGtfsPath;
            if (gtfsPath is null)
            {
                var sourceUrl = await ReadGtfsSourceUrlAsync();
                temporaryGtfsPath = Path.Combine(Path.GetTempPath(), $"carris-lisboa-{Guid.NewGuid():N}.zip");
                Console.WriteLine($"A descarregar GTFS da Carris Lisboa: {sourceUrl}");
                await DownloadGtfsAsync(sourceUrl, temporaryGtfsPath);
                Console.WriteLine("Download do GTFS concluído.");
                gtfsPath = temporaryGtfsPath;
            }
            else
            {
                if (!File.Exists(gtfsPath))
                    throw new FileNotFoundException("O ficheiro GTFS local não existe.", gtfsPath);
                Console.WriteLine($"A usar GTFS local: {gtfsPath}");
            }

            Console.WriteLine("A gerar dados estáticos da Carris Lisboa...");
            new CarrisLisboaStaticDataGenerator().Generate(gtfsPath, outputDirectory);
            Console.WriteLine("Geração concluída.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Erro: {exception.Message}");
            return 1;
        }
        finally
        {
            if (temporaryGtfsPath is not null)
            {
                try
                {
                    File.Delete(temporaryGtfsPath);
                }
                catch (Exception exception)
                {
                    Console.Error.WriteLine($"Aviso: não foi possível apagar o GTFS temporário: {exception.Message}");
                }
            }
        }
    }

    private static bool TryParseArguments(string[] args, out string outputDirectory, out string? localGtfsPath)
    {
        outputDirectory = string.Empty;
        localGtfsPath = null;
        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--output" when index + 1 < args.Length && string.IsNullOrWhiteSpace(outputDirectory):
                    outputDirectory = args[++index];
                    break;
                case "--gtfs" when index + 1 < args.Length && localGtfsPath is null:
                    localGtfsPath = args[++index];
                    break;
                default:
                    return false;
            }
        }

        return !string.IsNullOrWhiteSpace(outputDirectory);
    }

    private static async Task<string> ReadGtfsSourceUrlAsync()
    {
        var settingsPath = FindAppSettingsPath();
        if (settingsPath is null)
            throw new InvalidOperationException("Não foi encontrado wwwroot/appsettings.json a partir da raiz do repositório.");

        await using var stream = File.OpenRead(settingsPath);
        var configuration = await JsonSerializer.DeserializeAsync<GeneratorConfiguration>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        var sourceUrl = configuration?.Apis?.Carris?.GtfsSourceUrl;
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException($"Apis:Carris:GtfsSourceUrl ausente ou inválido em '{settingsPath}'.");

        return uri.ToString();
    }

    private static async Task DownloadGtfsAsync(string sourceUrl, string destinationPath)
    {
        using var httpClient = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, sourceUrl);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/zip"));
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"O download do GTFS falhou com HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");

        await using var input = await response.Content.ReadAsStreamAsync();
        await using var output = File.Create(destinationPath);
        await input.CopyToAsync(output);
    }

    private static string? FindAppSettingsPath()
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var directory = new DirectoryInfo(startPath);
            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, "wwwroot", "appsettings.json");
                if (File.Exists(candidate)) return candidate;
                directory = directory.Parent;
            }
        }

        return null;
    }

    private sealed record GeneratorConfiguration(ApiSettings? Apis);
}