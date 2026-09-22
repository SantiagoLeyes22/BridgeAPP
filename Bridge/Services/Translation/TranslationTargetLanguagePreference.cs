using Bridge.Models;
using Bridge.Services.Windows;

namespace Bridge.Services.Translation;

public static class TranslationTargetLanguagePreference
{
    public static LanguageDefinition Resolve(AppSettings settings, TranslationHotkey hotkey) =>
        LanguageDefinition.FindByCode(settings.LastTargetLanguageCode) ??
        (hotkey == TranslationHotkey.TranslateSelection
            ? LanguageDefinition.FindByCode(settings.PrimaryLanguageCode) ?? LanguageDefinition.Spanish
            : LanguageDefinition.FindByCode(TranslationCoordinator.DefaultResponseLanguageCode)!);
}