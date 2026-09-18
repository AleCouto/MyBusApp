using System.Net.Http.Headers;

namespace MyBusApp.Tools.GtfsDataGenerator;

public static class GtfsDataGeneratorRunner
{
    private const string Usage = "Usage: dotnet run -- --output <directory> (--gtfs <gtfs.zip> | --gtfs-url <url>)";

    public static async Task<int> RunAsync(string[] args)
    {
        if (!TryParseArguments(args, out var outputDirectory, out var localGtfsPath, out var gtfsUrl))
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
                temporaryGtfsPath = Path.Combine(Path.GetTempPath(), $"gtfs-{Guid.NewGuid():N}.zip");
                Console.WriteLine($"A descarregar GTFS: {gtfsUrl}");
                await DownloadGtfsAsync(gtfsUrl!, temporaryGtfsPath);
                Console.WriteLine("Download do GTFS concluído.");
                gtfsPath = temporaryGtfsPath;
            }
            else
            {
                if (!File.Exists(gtfsPath))
                    throw new FileNotFoundException("O ficheiro GTFS local não existe.", gtfsPath);
                Console.WriteLine($"A usar GTFS local: {gtfsPath}");
            }

            Console.WriteLine("A gerar dados estáticos GTFS...");
            new GtfsStaticDataGenerator().Generate(gtfsPath, outputDirectory);
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

    private static bool TryParseArguments(
        string[] args,
        out string outputDirectory,
        out string? localGtfsPath,
        out string? gtfsUrl)
    {
        outputDirectory = string.Empty;
        localGtfsPath = null;
        gtfsUrl = null;
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
                case "--gtfs-url" when index + 1 < args.Length && gtfsUrl is null:
                    gtfsUrl = args[++index];
                    break;
                default:
                    return false;
            }
        }

        if (string.IsNullOrWhiteSpace(outputDirectory) || (localGtfsPath is null) == (gtfsUrl is null))
            return false;

        return gtfsUrl is null || Uri.TryCreate(gtfsUrl, UriKind.Absolute, out var uri) &&
            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
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

}