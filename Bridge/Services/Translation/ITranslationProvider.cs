using Bridge.Models;

namespace Bridge.Services.Translation;

public interface ITranslationProvider
{
    string ProviderName { get; }

    bool IsAvailable { get; }

    bool IsAuthenticated { get; }

    bool SupportsTranslationStyles { get; }

    Task<TranslationResult> TranslateToPrimaryLanguageAsync(
        string text,
        string primaryLanguage,
        TranslationStyleDefinition style,
        CancellationToken cancellationToken);

    Task<TranslationResult> TranslateAsync(
        string text,
        string targetLanguage,
        TranslationStyleDefinition style,
        CancellationToken cancellationToken);
}
