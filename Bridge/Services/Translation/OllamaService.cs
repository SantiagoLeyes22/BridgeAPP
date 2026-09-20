using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bridge.Models;
using Bridge.Services.Infrastructure;

namespace Bridge.Services.Translation;

public sealed class OllamaService
{
    private readonly HttpClient _httpClient;
    private readonly IDiagnosticLogger _logger;

    public OllamaService(HttpClient httpClient, IDiagnosticLogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> IsRunningAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));
            using var response = await _httpClient.GetAsync("api/tags", timeout.Token);
            return response.IsSuccessStatusCode;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
        {
            return false;
        }
    }

    public async Task<bool> IsModelInstalledAsync(string model, CancellationToken cancellationToken)
    {
        if (!await IsRunningAsync(cancellationToken))
        {
            return false;
        }

        using var response = await _httpClient.GetAsync("api/tags", cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return document.RootElement.GetProperty("models").EnumerateArray().Any(entry =>
        {
            var name = entry.GetProperty("name").GetString();
            return string.Equals(name, model, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, model + ":latest", StringComparison.OrdinalIgnoreCase);
        });
    }

    public async Task EnsureRunningAsync(CancellationToken cancellationToken)
    {
        if (await IsRunningAsync(cancellationToken))
        {
            return;
        }

        var executable = FindExecutable();
        if (executable is null)
        {
            throw new TranslationEngineException(
                "Ollama is required for TranslateGemma. Install Ollama for Windows, open it, and try again.");
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = executable,
                Arguments = "serve",
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            throw new TranslationEngineException("Ollama could not be started. Open Ollama manually and try again.", exception);
        }

        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(500, cancellationToken);
            if (await IsRunningAsync(cancellationToken))
            {
                _logger.Info("Ollama local service started.");
                return;
            }
        }

        throw new TranslationEngineException("Ollama was found but its local service did not start.");
    }

    public async Task PullModelAsync(
        string model,
        IProgress<ModelPreparationProgress>? progress,
        CancellationToken cancellationToken)
    {
        await EnsureRunningAsync(cancellationToken);
        if (await IsModelInstalledAsync(model, cancellationToken))
        {
            progress?.Report(new ModelPreparationProgress($"{model} is already installed.", 100));
            return;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/pull")
        {
            Content = JsonContent.Create(new { name = model, stream = true })
        };
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            using var update = JsonDocument.Parse(line);
            var root = update.RootElement;
            if (root.TryGetProperty("error", out var error))
            {
                throw new TranslationEngineException(error.GetString() ?? "Ollama could not download the model.");
            }

            var status = root.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString() ?? "Downloading model…"
                : "Downloading model…";
            double? percent = null;
            if (root.TryGetProperty("completed", out var completed) &&
                root.TryGetProperty("total", out var total) &&
                total.GetInt64() > 0)
            {
                percent = Math.Clamp((double)completed.GetInt64() / total.GetInt64() * 100, 0, 100);
            }

            progress?.Report(new ModelPreparationProgress(status, percent));
        }

        if (!await IsModelInstalledAsync(model, cancellationToken))
        {
            throw new TranslationEngineException("Ollama finished without installing the selected TranslateGemma model.");
        }

        progress?.Report(new ModelPreparationProgress($"{model} installed.", 100));
        _logger.Info($"Ollama model installed: {model}.");
    }

    public async Task<string> TranslateAsync(
        string model,
        string prompt,
        CancellationToken cancellationToken)
    {
        await EnsureRunningAsync(cancellationToken);
        if (!await IsModelInstalledAsync(model, cancellationToken))
        {
            throw new TranslationEngineException(
                $"The {model} package is not installed. Prepare the selected engine from Settings.");
        }

        using var response = await _httpClient.PostAsJsonAsync(
            "api/chat",
            new
            {
                model,
                messages = new[] { new { role = "user", content = prompt } },
                stream = false,
                keep_alive = "5m",
                options = new { temperature = 0, num_predict = 4096 }
            },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var content = document.RootElement.GetProperty("message").GetProperty("content").GetString();
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new TranslationEngineException("TranslateGemma returned an empty translation.");
        }

        return content.Trim();
    }

    private static string? FindExecutable()
    {
        var candidates = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ollama", "ollama.exe")
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}
