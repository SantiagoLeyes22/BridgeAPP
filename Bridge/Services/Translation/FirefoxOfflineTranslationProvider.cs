using BergamotTranslatorSharp;
using Bridge.Models;

namespace Bridge.Services.Translation;

public sealed class FirefoxOfflineTranslationProvider : ITranslationProvider, IDisposable
{
    private readonly FirefoxModelManager _modelManager;
    private readonly OfflineLanguageDetector _languageDetector;
    private readonly Dictionary<string, BlockingService> _services = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _servicesGate = new();

    public FirefoxOfflineTranslationProvider(
        FirefoxModelManager modelManager,
        OfflineLanguageDetector languageDetector)
    {
        _modelManager = modelManager;
        _languageDetector = languageDetector;
    }

    public string ProviderName => "Offline standard";

    public bool IsAvailable => _modelManager.IsCorePackInstalled;

    public bool IsAuthenticated => true;

    public bool SupportsTranslationStyles => false;

    public Task<TranslationResult> TranslateToPrimaryLanguageAsync(
        string text,
        string primaryLanguage,
        TranslationStyleDefinition style,
        CancellationToken cancellationToken) =>
        TranslateCoreAsync(text, primaryLanguage, cancellationToken);

    public Task<TranslationResult> TranslateAsync(
        string text,
        string targetLanguage,
        TranslationStyleDefinition style,
        CancellationToken cancellationToken) =>
        TranslateCoreAsync(text, targetLanguage, cancellationToken);

    public void Dispose()
    {
        lock (_servicesGate)
        {
            foreach (var service in _services.Values)
            {
                service.Dispose();
            }

            _services.Clear();
        }
    }

    private async Task<TranslationResult> TranslateCoreAsync(
        string text,
        string targetLanguageName,
        CancellationToken cancellationToken)
    {
        if (!IsAvailable)
        {
            throw new TranslationEngineException(
                "Download the standard offline language pack from Settings before translating.");
        }

        var source = _languageDetector.Detect(text);
        var target = LanguageDefinition.Supported.FirstOrDefault(language =>
                         string.Equals(language.DisplayName, targetLanguageName, StringComparison.OrdinalIgnoreCase))
                     ?? LanguageDefinition.FindByCode(targetLanguageName)
                     ?? LanguageDefinition.Spanish;

        if (source.Code == target.Code)
        {
            return new TranslationResult(
                source.DisplayName,
                source.Code,
                target.DisplayName,
                target.Code,
                text);
        }

        try
        {
            var pairKey = $"{source.Code}-{target.Code}";
            var configPaths = GetTranslationPath(source.Code, target.Code);
            var service = GetOrCreateService(pairKey, configPaths);
            var translated = await Task.Run(
                () => service.Translate(text, html: false),
                cancellationToken);

            return new TranslationResult(
                source.DisplayName,
                source.Code,
                target.DisplayName,
                target.Code,
                translated.Trim());
        }
        catch (TranslationEngineException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new TranslationEngineException(
                "The offline translation engine could not process this text. Try the high-quality engine or restart the app.",
                exception);
        }
    }

    private string[] GetTranslationPath(string sourceCode, string targetCode)
    {
        if (sourceCode == "en" || targetCode == "en")
        {
            return [_modelManager.GetConfigurationPath($"{sourceCode}-{targetCode}")];
        }

        return
        [
            _modelManager.GetConfigurationPath($"{sourceCode}-en"),
            _modelManager.GetConfigurationPath($"en-{targetCode}")
        ];
    }

    private BlockingService GetOrCreateService(string key, string[] configurationPaths)
    {
        lock (_servicesGate)
        {
            if (_services.TryGetValue(key, out var existing))
            {
                return existing;
            }

            if (configurationPaths.Any(path => !File.Exists(path)))
            {
                throw new TranslationEngineException(
                    "One or more offline language files are missing. Download the standard pack again from Settings.");
            }

            var created = new BlockingService(configurationPaths);
            _services.Add(key, created);
            return created;
        }
    }
}
