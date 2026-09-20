using Bridge.Models;
using Bridge.Services.Authentication;
using Bridge.Services.Infrastructure;
using Bridge.Services.Settings;

namespace Bridge.Services.Translation;

public sealed class TranslationEngineService : ITranslationEngineService
{
    private readonly FirefoxModelManager _firefoxModels;
    private readonly OllamaService _ollama;
    private readonly IAuthenticationService _authentication;
    private readonly AppConfiguration _configuration;
    private readonly ISettingsService _settings;

    public TranslationEngineService(
        FirefoxModelManager firefoxModels,
        OllamaService ollama,
        IAuthenticationService authentication,
        AppConfiguration configuration,
        ISettingsService settings,
        HardwareProfileService hardwareProfileService)
    {
        _firefoxModels = firefoxModels;
        _ollama = ollama;
        _authentication = authentication;
        _configuration = configuration;
        _settings = settings;
        Hardware = hardwareProfileService.GetCurrent();
    }

    public IReadOnlyList<TranslationEngineDefinition> Engines => TranslationEngineDefinition.Available;

    public HardwareProfile Hardware { get; }

    public TranslationEngineDefinition SelectedEngine =>
        TranslationEngineDefinition.Find(_settings.Settings.TranslationEngineId);

    public async Task<TranslationEngineStatus> GetStatusAsync(
        string engineId,
        CancellationToken cancellationToken = default)
    {
        var engine = TranslationEngineDefinition.Find(engineId);
        if (engine.Id == TranslationEngineDefinition.OfflineStandardId)
        {
            return _firefoxModels.IsCorePackInstalled
                ? new TranslationEngineStatus(true, "Offline language pack ready.")
                : new TranslationEngineStatus(false, "Download the offline language pack once before translating.");
        }

        if (engine.UsesGemma)
        {
            if (!_settings.Settings.AcceptedGemmaTerms)
            {
                return new TranslationEngineStatus(false, "Accept the Gemma terms before downloading this model.");
            }

            if (!await _ollama.IsRunningAsync(cancellationToken))
            {
                return new TranslationEngineStatus(false, "Ollama is not running. Install or open Ollama, then prepare this engine.");
            }

            return await _ollama.IsModelInstalledAsync(engine.OllamaModel!, cancellationToken)
                ? new TranslationEngineStatus(true, $"{engine.OllamaModel} is installed and ready.")
                : new TranslationEngineStatus(false, $"Download {engine.OllamaModel} once to use it offline.");
        }

        if (!_configuration.MicrosoftIdentity.IsConfigured)
        {
            return new TranslationEngineStatus(false, "Copilot has not been configured by the application publisher.");
        }

        return _authentication.IsAuthenticated
            ? new TranslationEngineStatus(true, "Microsoft 365 Copilot is connected.")
            : new TranslationEngineStatus(false, "Sign in with an approved and licensed work account.");
    }

    public Task PrepareAsync(
        string engineId,
        IProgress<ModelPreparationProgress>? progress,
        CancellationToken cancellationToken = default)
    {
        var engine = TranslationEngineDefinition.Find(engineId);
        if (engine.Id == TranslationEngineDefinition.OfflineStandardId)
        {
            return _firefoxModels.InstallCorePackAsync(progress, cancellationToken);
        }

        if (engine.UsesGemma)
        {
            if (!_settings.Settings.AcceptedGemmaTerms)
            {
                throw new TranslationEngineException("Accept the Gemma terms before downloading TranslateGemma.");
            }

            return _ollama.PullModelAsync(engine.OllamaModel!, progress, cancellationToken);
        }

        throw new TranslationEngineException("Use Sign in to prepare Microsoft 365 Copilot.");
    }
}
