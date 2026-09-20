using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bridge.Models;
using Bridge.Services.Infrastructure;

namespace Bridge.Services.Translation;

public sealed class FirefoxModelManager
{
    private static readonly string[] CorePairs = ["en-es", "es-en", "en-pt", "pt-en"];
    private static readonly Uri RegistryUri = new(
        "https://storage.googleapis.com/moz-fx-translations-data--303e-prod-translations-data/db/models.json");

    private readonly HttpClient _httpClient;
    private readonly IDiagnosticLogger _logger;
    private readonly SemaphoreSlim _installGate = new(1, 1);
    private readonly string _modelsRoot = Path.Combine(
        AppBranding.DataDirectory,
        "Models",
        "bergamot");

    private string CorePackMarkerPath => Path.Combine(_modelsRoot, ".core-pack-v2.ready");

    public FirefoxModelManager(HttpClient httpClient, IDiagnosticLogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsCorePackInstalled =>
        File.Exists(CorePackMarkerPath) && CorePairs.All(IsPairInstalled);

    public string GetConfigurationPath(string pair) => Path.Combine(_modelsRoot, pair, "config.yml");

    public async Task InstallCorePackAsync(
        IProgress<ModelPreparationProgress>? progress,
        CancellationToken cancellationToken)
    {
        await _installGate.WaitAsync(cancellationToken);
        var stage = "initializing the offline model folder";
        try
        {
            if (IsCorePackInstalled)
            {
                progress?.Report(new ModelPreparationProgress("The offline language pack is already installed.", 100));
                return;
            }

            Directory.CreateDirectory(_modelsRoot);
            File.Delete(CorePackMarkerPath);

            stage = "reading Mozilla's model registry";
            progress?.Report(new ModelPreparationProgress("Reading Mozilla's HTTPS model registry…", 1));
            using var registryResponse = await GetWithRetryAsync(
                RegistryUri,
                HttpCompletionOption.ResponseContentRead,
                cancellationToken);
            await using var registryStream = await registryResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var registry = await JsonDocument.ParseAsync(registryStream, cancellationToken: cancellationToken);

            stage = "validating Mozilla's model registry";
            var baseUrl = registry.RootElement.GetProperty("baseUrl").GetString()
                          ?? throw new TranslationEngineException("The model registry did not provide a download location.");
            var models = registry.RootElement.GetProperty("models");
            var manifests = CorePairs.Select(pair => ReadManifest(models, pair)).ToArray();
            var totalFiles = manifests.Sum(manifest => manifest.Files.Count);
            var completedFiles = 0;

            foreach (var manifest in manifests)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var pairDirectory = Path.Combine(_modelsRoot, manifest.Pair);
                Directory.CreateDirectory(pairDirectory);

                foreach (var file in manifest.Files)
                {
                    stage = $"downloading {manifest.Pair} {file.Role}";
                    var fileIndex = completedFiles;
                    var fileProgress = new Progress<double>(fraction =>
                    {
                        var overall = Math.Clamp((fileIndex + fraction) / totalFiles * 100, 0, 100);
                        progress?.Report(new ModelPreparationProgress(
                            $"Downloading {manifest.Pair} · {file.Role}…",
                            overall));
                    });
                    await DownloadAndExpandAsync(baseUrl, pairDirectory, file, fileProgress, cancellationToken);
                    completedFiles++;
                }

                await WriteConfigurationAsync(pairDirectory, manifest, cancellationToken);
            }

            await File.WriteAllTextAsync(
                CorePackMarkerPath,
                "Firefox/Bergamot core pack v2",
                Encoding.UTF8,
                cancellationToken);

            progress?.Report(new ModelPreparationProgress("Offline language pack installed.", 100));
            _logger.Info("Firefox/Bergamot offline language pack installed.");
        }
        catch (TranslationEngineException)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException or UnauthorizedAccessException)
        {
            _logger.Error($"Offline language pack preparation failed while {stage}.", exception);
            throw CreatePreparationException(stage, exception);
        }
        finally
        {
            _installGate.Release();
        }
    }

    private async Task<HttpResponseMessage> GetWithRetryAsync(
        Uri uri,
        HttpCompletionOption completionOption,
        CancellationToken cancellationToken)
    {
        const int maximumAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                var response = await _httpClient.GetAsync(uri, completionOption, cancellationToken);
                if ((int)response.StatusCode is 408 or 429 || (int)response.StatusCode >= 500)
                {
                    var statusCode = response.StatusCode;
                    response.Dispose();
                    throw new HttpRequestException(
                        $"The model server returned HTTP {(int)statusCode}.",
                        null,
                        statusCode);
                }

                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (HttpRequestException exception) when (attempt < maximumAttempts)
            {
                _logger.Error($"Mozilla download attempt {attempt} failed; retrying.", exception);
                await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt), cancellationToken);
            }
        }
    }

    private static TranslationEngineException CreatePreparationException(string stage, Exception exception)
    {
        var message = exception switch
        {
            UnauthorizedAccessException =>
                $"The app does not have permission to store the offline models while {stage}. Check security software or folder permissions.",
            InvalidDataException =>
                $"A Mozilla model file was incomplete or failed integrity validation while {stage}. Try the download again.",
            JsonException =>
                "Mozilla's model registry returned an unreadable response. Try again; if it continues, a proxy or security filter may be changing the response.",
            HttpRequestException http when http.StatusCode is not null =>
                $"Mozilla's model server returned HTTP {(int)http.StatusCode.Value} while {stage}. Try again later.",
            HttpRequestException =>
                $"The connection to Mozilla's model server failed while {stage}. Check whether security software allows storage.googleapis.com.",
            IOException =>
                $"The app could not write or unpack an offline model while {stage}. Check free disk space and security software.",
            _ => "The offline language pack could not be prepared."
        };
        return new TranslationEngineException(message, exception);
    }

    private bool IsPairInstalled(string pair)
    {
        var configurationPath = GetConfigurationPath(pair);
        if (!File.Exists(configurationPath))
        {
            return false;
        }

        try
        {
            var directory = Path.GetDirectoryName(configurationPath)!;
            var assets = File.ReadLines(configurationPath)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("- ", StringComparison.Ordinal))
                .Select(line => line[2..].Trim())
                .Where(value => !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            return assets.Length >= 4 && assets.All(asset => File.Exists(Path.Combine(directory, asset)));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static ModelManifest ReadManifest(JsonElement models, string pair)
    {
        if (!models.TryGetProperty(pair, out var candidates))
        {
            throw new TranslationEngineException($"Mozilla does not currently publish the required {pair} model.");
        }

        var selected = candidates.EnumerateArray()
            .Where(candidate =>
                candidate.TryGetProperty("releaseStatus", out var releaseStatus) &&
                releaseStatus.ValueKind == JsonValueKind.String &&
                releaseStatus.GetString()!.StartsWith("Release", StringComparison.OrdinalIgnoreCase))
            .OrderBy(candidate => ArchitectureRank(candidate.GetProperty("architecture").GetString()))
            .FirstOrDefault();

        if (selected.ValueKind == JsonValueKind.Undefined)
        {
            throw new TranslationEngineException($"No released Mozilla model is available for {pair}.");
        }

        var filesElement = selected.GetProperty("files");
        var files = new List<ModelFile>();
        AddFile(files, filesElement, "model", "model");
        if (filesElement.TryGetProperty("vocab", out _))
        {
            AddFile(files, filesElement, "vocab", "vocabulary");
        }
        else
        {
            AddFile(files, filesElement, "srcVocab", "source vocabulary");
            AddFile(files, filesElement, "trgVocab", "target vocabulary");
        }

        AddFile(files, filesElement, "lexicalShortlist", "lexical shortlist");
        return new ModelManifest(pair, files);
    }

    private static int ArchitectureRank(string? architecture) => architecture switch
    {
        "base-memory" => 0,
        "base" => 1,
        "tiny" => 2,
        _ => 3
    };

    private static void AddFile(List<ModelFile> files, JsonElement container, string propertyName, string role)
    {
        var element = container.GetProperty(propertyName);
        var path = element.GetProperty("path").GetString()
                   ?? throw new TranslationEngineException($"The {role} file is missing from the model registry.");
        var hash = element.TryGetProperty("uncompressedHash", out var hashElement)
            ? hashElement.GetString()
            : null;
        files.Add(new ModelFile(propertyName, role, path, hash));
    }

    private async Task DownloadAndExpandAsync(
        string baseUrl,
        string destinationDirectory,
        ModelFile file,
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {
        var compressedName = Path.GetFileName(new Uri(file.Path, UriKind.Relative).OriginalString);
        var destinationName = compressedName.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            ? compressedName[..^3]
            : compressedName;
        var destinationPath = Path.Combine(destinationDirectory, destinationName);

        if (File.Exists(destinationPath) && await HasExpectedHashAsync(destinationPath, file.Hash, cancellationToken))
        {
            progress.Report(1);
            return;
        }

        var downloadPath = destinationPath + ".download";
        var expandedPath = destinationPath + ".partial";
        const int maximumAttempts = 3;
        for (var attempt = 1; ; attempt++)
        {
            File.Delete(downloadPath);
            File.Delete(expandedPath);
            try
            {
                var uri = new Uri(new Uri(baseUrl.TrimEnd('/') + "/"), file.Path);
                using var response = await GetWithRetryAsync(
                    uri,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken);
                var total = response.Content.Headers.ContentLength;
                await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
                await using (var destination = new FileStream(
                                 downloadPath,
                                 FileMode.CreateNew,
                                 FileAccess.Write,
                                 FileShare.None,
                                 81920,
                                 useAsync: true))
                {
                    var buffer = new byte[81920];
                    long written = 0;
                    int read;
                    while ((read = await source.ReadAsync(buffer, cancellationToken)) > 0)
                    {
                        await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                        written += read;
                        progress.Report(total is > 0 ? Math.Clamp((double)written / total.Value, 0, 1) : 0.5);
                    }
                }

                await using (var compressed = File.OpenRead(downloadPath))
                await using (var gzip = new GZipStream(compressed, CompressionMode.Decompress))
                await using (var expanded = new FileStream(
                                 expandedPath,
                                 FileMode.CreateNew,
                                 FileAccess.Write,
                                 FileShare.None,
                                 81920,
                                 useAsync: true))
                {
                    await gzip.CopyToAsync(expanded, cancellationToken);
                }

                if (!await HasExpectedHashAsync(expandedPath, file.Hash, cancellationToken))
                {
                    throw new InvalidDataException($"Hash verification failed for {file.Role}.");
                }

                File.Move(expandedPath, destinationPath, overwrite: true);
                progress.Report(1);
                return;
            }
            catch (Exception exception) when (
                attempt < maximumAttempts &&
                exception is HttpRequestException or IOException)
            {
                _logger.Error($"Model file attempt {attempt} failed for {file.Role}; retrying.", exception);
                await Task.Delay(TimeSpan.FromMilliseconds(750 * attempt), cancellationToken);
            }
            finally
            {
                File.Delete(downloadPath);
                File.Delete(expandedPath);
            }
        }
    }

    private static async Task<bool> HasExpectedHashAsync(
        string path,
        string? expectedHash,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(expectedHash))
        {
            return new FileInfo(path).Length > 0;
        }

        await using var stream = File.OpenRead(path);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return string.Equals(Convert.ToHexString(hash), expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteConfigurationAsync(
        string directory,
        ModelManifest manifest,
        CancellationToken cancellationToken)
    {
        var model = GetExpandedName(manifest.Files.Single(file => file.Key == "model").Path);
        var shortlist = GetExpandedName(manifest.Files.Single(file => file.Key == "lexicalShortlist").Path);
        var vocabularies = manifest.Files
            .Where(file => file.Key is "vocab" or "srcVocab" or "trgVocab")
            .Select(file => GetExpandedName(file.Path))
            .ToArray();

        var builder = new StringBuilder()
            .AppendLine("relative-paths: true")
            .AppendLine("models:")
            .AppendLine($"- {model}")
            .AppendLine("vocabs:");
        foreach (var vocabulary in vocabularies)
        {
            builder.AppendLine($"- {vocabulary}");
        }

        if (vocabularies.Length == 1)
        {
            // Bergamot requires one entry for each side even when both share the same vocabulary file.
            builder.AppendLine($"- {vocabularies[0]}");
        }

        builder
            .AppendLine("shortlist:")
            .AppendLine($"- {shortlist}")
            .AppendLine("- false")
            .AppendLine("beam-size: 1")
            .AppendLine("normalize: 1.0")
            .AppendLine("word-penalty: 0")
            .AppendLine("max-length-break: 128")
            .AppendLine("mini-batch-words: 1024")
            .AppendLine("workspace: 128")
            .AppendLine("max-length-factor: 2.0")
            .AppendLine("skip-cost: true")
            .AppendLine("cpu-threads: 0")
            .AppendLine("quiet: true")
            .AppendLine("quiet-translation: true")
            .AppendLine(model.Contains("alphas", StringComparison.OrdinalIgnoreCase)
                ? "gemm-precision: int8shiftAlphaAll"
                : "gemm-precision: int8shiftAll");

        await File.WriteAllTextAsync(
            Path.Combine(directory, "config.yml"),
            builder.ToString(),
            Encoding.UTF8,
            cancellationToken);
    }

    private static string GetExpandedName(string path)
    {
        var name = Path.GetFileName(path);
        return name.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) ? name[..^3] : name;
    }

    private sealed record ModelManifest(string Pair, IReadOnlyList<ModelFile> Files);

    private sealed record ModelFile(string Key, string Role, string Path, string? Hash);
}
