using System.Text.Json;
using Bridge.Models;
using Bridge.Services.Infrastructure;

namespace Bridge.Services.Settings;

public sealed class JsonSettingsService : ISettingsService
{
    private readonly IDiagnosticLogger _logger;
    private readonly string _settingsPath = Path.Combine(AppBranding.DataDirectory, "settings.json");
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public JsonSettingsService(IDiagnosticLogger logger)
    {
        _logger = logger;
    }

    public AppSettings Settings { get; private set; } = new();

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsPath))
        {
            return;
        }

        try
        {
            await using var stream = File.OpenRead(_settingsPath);
            Settings = await JsonSerializer.DeserializeAsync<AppSettings>(
                           stream,
                           _jsonOptions,
                           cancellationToken)
                       ?? new AppSettings();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            Settings = new AppSettings();
            _logger.Error("Loading application settings failed; defaults are in use.", exception);
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_settingsPath)!;
        Directory.CreateDirectory(directory);
        var temporaryPath = _settingsPath + ".tmp";

        await using (var stream = new FileStream(
                         temporaryPath,
                         FileMode.Create,
                         FileAccess.Write,
                         FileShare.None,
                         bufferSize: 4096,
                         useAsync: true))
        {
            await JsonSerializer.SerializeAsync(stream, Settings, _jsonOptions, cancellationToken);
        }

        File.Move(temporaryPath, _settingsPath, overwrite: true);
    }
}
