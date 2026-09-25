using Bridge.Models;
using Bridge.Services.Windows;

namespace Bridge.Services.Translation;

public static class TranslationTargetLanguagePreference
{
    public static LanguageDefinition Resolve(AppSettings settings, TranslationHotkey hotkey)
    {
        if (hotkey == TranslationHotkey.TranslateSelection)
        {
            return LanguageDefinition.FindByCode(settings.PrimaryLanguageCode) ?? LanguageDefinition.Spanish;
        }

        return LanguageDefinition.FindByCode(settings.LastTargetLanguageCode) ??
               LanguageDefinition.FindByCode(TranslationCoordinator.DefaultResponseLanguageCode)!;
    }
}
