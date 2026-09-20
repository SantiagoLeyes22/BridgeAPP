using Bridge.Models;
using Bridge.Services.Settings;

namespace Bridge.Services.Translation;

public sealed class TranslationProviderRouter : ITranslationProvider
{
    private readonly FirefoxOfflineTranslationProvider _offline;
    private readonly OllamaTranslationProvider _ollama;
    private readonly Microsoft365CopilotTranslationProvider _copilot;
    private readonly ISettingsService _settings;

    public TranslationProviderRouter(
        FirefoxOfflineTranslationProvider offline,
        OllamaTranslationProvider ollama,
        Microsoft365CopilotTranslationProvider copilot,
        ISettingsService settings)
    {
        _offline = offline;
        _ollama = ollama;
        _copilot = copilot;
        _settings = settings;
    }

    private ITranslationProvider Current =>
        TranslationEngineDefinition.Find(_settings.Settings.TranslationEngineId) switch
        {
            { UsesGemma: true } => _ollama,
            { UsesCopilot: true } => _copilot,
            _ => _offline
        };

    public string ProviderName => Current.ProviderName;

    public bool IsAvailable => Current.IsAvailable;

    public bool IsAuthenticated => Current.IsAuthenticated;

    public bool SupportsTranslationStyles => Current.SupportsTranslationStyles;

    public Task<TranslationResult> TranslateToPrimaryLanguageAsync(
        string text,
        string primaryLanguage,
        TranslationStyleDefinition style,
        CancellationToken cancellationToken) =>
        Current.TranslateToPrimaryLanguageAsync(text, primaryLanguage, style, cancellationToken);

    public Task<TranslationResult> TranslateAsync(
        string text,
        string targetLanguage,
        TranslationStyleDefinition style,
        CancellationToken cancellationToken) =>
        Current.TranslateAsync(text, targetLanguage, style, cancellationToken);
}
